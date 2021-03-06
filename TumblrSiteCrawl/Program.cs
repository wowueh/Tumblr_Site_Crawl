﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Threading;

namespace TumblrSiteCrawl
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome! This is tool which auto download images from Tumblr.com site.");
            TumblrSite tumbSite = new TumblrSite();
            Console.WriteLine("Please choose Download Method: 1. Normal  2. Multithreading");
            string downloadMethod = Console.ReadLine();

            if (downloadMethod == "1")
            {
                ask:
                Console.WriteLine("Do you want to download all pages? (y/n)");
                string command = Console.ReadLine();
                if (command == "y")
                {
                    tumbSite.DownloadImagesFromAllPagesAsync();
                }
                else if (command == "n")
                {
                    tumbSite.DownloadImagesFromRangedPagesAsync();
                }
                else
                {
                    Console.WriteLine("Wrong answer!!! Please choose 'y' or 'n'");
                    goto ask;
                }
            }
            else if (downloadMethod == "2")
            {
                ask2:
                Console.WriteLine("Do you want to download all pages? (y/n)");
                string command = Console.ReadLine();
                if (command == "y")
                {
                    List<int[]> rangesList = tumbSite.SliceDownloadPages(1, tumbSite.PageNums);
                    int[] threadIdArray = new int[rangesList.Count];
                    for (int i = 0; i < rangesList.Count; i++)
                    {
                        threadIdArray[i] = i + 1;
                        int[] item = rangesList[i];
                        Task.Factory.StartNew(() => tumbSite.DownloadMultiThreadAsync(item[0], item[1], i+1));
                        Thread.Sleep(2000);
                    }

                }
                else if (command == "n")
                {
                    Console.Write("Start page's number: ");
                    int startPage = Convert.ToInt32(Console.ReadLine());
                    Console.Write("End page's number: ");
                    int endPage = Convert.ToInt32(Console.ReadLine());
                    List<int[]> rangesList = tumbSite.SliceDownloadPages(startPage, endPage);
                    int[] threadIdArray = new int[rangesList.Count];
                    for (int i = 0; i < rangesList.Count; i++)
                    {
                        threadIdArray[i] = i + 1;
                        int[] item = rangesList[i];
                        Task.Factory.StartNew(() => tumbSite.DownloadMultiThreadAsync(item[0], item[1], i + 1));
                        Thread.Sleep(2000);
                    }
                }
                else
                {
                    Console.WriteLine("Wrong answer!!! Please choose 'y' or 'n'");
                    goto ask2;
                }
            }
            Console.ReadLine();
        }
    }
    class TumblrSite
    {
        public string HomePage { get; set; }
        public int PageNums { get; set; }
        public int ImgNumsPerPage { get; set; }
        public int SizeLimit { get; set; }
        public int CompleteThreadNums { get; set; }
        public TumblrSite()
        {
            CompleteThreadNums = 0;
            SizeLimit = 0;
            checkTumbUrl:
            Console.WriteLine("Please paste valid Url of Tumblrsite:");
            string homePage = Console.ReadLine();
            string checkPattern = @"^http.+\.tumblr\.com/$";
            Regex checkRegex = new Regex(checkPattern);
            if (checkRegex.IsMatch(homePage))
            {

            }
            else
            {
                Console.WriteLine("It's not a Tumblr site!!! Try another one!");
                goto checkTumbUrl;
            }
            HomePage = homePage;
            PageNums = CountNumOfPages();
            Console.WriteLine("This site have {0} page(s)!", PageNums);
            ImgNumsPerPage = CountImgNumsPerPage();
            Console.WriteLine("Each page have {0} image(s)!", ImgNumsPerPage);
            Console.WriteLine("Please set limit of image size (KBs):");
            SizeLimit = Convert.ToInt32(Console.ReadLine()) * 1024;
        }
        public async void DownloadImagesFromAllPagesAsync()
        {
            int sequenceNo = 1;
            long totalSize = 0;
            //Create download folder:
            string siteNamePattern = @"[^\/]+\..+\..{3}";
            Regex siteNameRegex = new Regex(siteNamePattern);
            Match siteNameMatch = siteNameRegex.Match(HomePage);
            string siteName = siteNameMatch.Value;
            string downloadFolder = "d:/ImageCrawler/" + siteName + " (page All)";
            System.IO.Directory.CreateDirectory(downloadFolder);
            //Download each specific page of site:
            for (int pageNo = 1; pageNo <= PageNums; pageNo++)
            {
                Console.WriteLine("-------------- Starting download Page {0} --------------", pageNo);
                string childrenPageUrl = HomePage + "page/" + pageNo;
                Task<string> htmlTask = RequestHtmlAsync(childrenPageUrl);
                string html = htmlTask.Result;
                List<string> linksOfChildrenPage = GetLink(html);
                //Download each specific link of page:
                foreach (var item in linksOfChildrenPage)
                {
                    //Tim extension cua file anh
                    string fileTypePattern = "...$";
                    Regex fileTypeRegex = new Regex(fileTypePattern);
                    Match fileTypeMatch = fileTypeRegex.Match(item);
                    string fileType = fileTypeMatch.Value;
                    //Request image using it's URL
                    var client2 = new HttpClient();
                    var response = new HttpResponseMessage();
                    response = await client2.GetAsync(item);
                    var responseContent = await response.Content.ReadAsByteArrayAsync();
                    var fileSize = response.Content.Headers.ContentLength;
                    //Save to disk
                    if ((fileType == "jpg" || fileType == "png" || fileType == "gif") && fileSize >= SizeLimit)
                    {
                        var file = new FileStream(downloadFolder + "/" + sequenceNo + "." + fileType, FileMode.OpenOrCreate);
                        var bw = new BinaryWriter(file);
                        bw.Write(responseContent);
                        bw.Flush();
                        bw.Close();
                        Console.WriteLine("{0}.{1} Download completed...{2} KB(s)", sequenceNo, fileType, fileSize / 1024);
                        sequenceNo++;
                        totalSize += Convert.ToInt64(fileSize);
                    }
                }
            }
            Console.WriteLine("Download Complete. Total {0} image(s). Saved Location: {1}. Total size: {2} KB(s)", sequenceNo - 1, downloadFolder, totalSize / 1024);
        }
        public async void DownloadImagesFromRangedPagesAsync()
        {
            Console.Write("Start page's number: ");
            int startPage = Convert.ToInt32(Console.ReadLine());
            Console.Write("End page's number: ");
            int endPage = Convert.ToInt32(Console.ReadLine());
            int sequenceNo = 1;
            long totalSize = 0;
            //Create download folder:
            string siteNamePattern = @"[^\/]+\..+\..{3}";
            Regex siteNameRegex = new Regex(siteNamePattern);
            Match siteNameMatch = siteNameRegex.Match(HomePage);
            string siteName = siteNameMatch.Value;
            string downloadFolder = "d:/ImageCrawler/" + siteName + "/(page " + startPage + "_" + endPage + ")";
            System.IO.Directory.CreateDirectory(downloadFolder);
            //Download each specific page of site:
            for (int pageNo = startPage; pageNo <= endPage; pageNo++)
            {
                Console.WriteLine("-------------- Starting download Page {0} --------------", pageNo);
                string childrenPageUrl = HomePage + "page/" + pageNo;
                Task<string> htmlTask = RequestHtmlAsync(childrenPageUrl);
                string html = htmlTask.Result;
                List<string> linksOfChildrenPage = GetLink(html);
                //Download each specific link of page:
                foreach (var item in linksOfChildrenPage)
                {
                    //Tim extension cua file anh
                    string fileTypePattern = "...$";
                    Regex fileTypeRegex = new Regex(fileTypePattern);
                    Match fileTypeMatch = fileTypeRegex.Match(item);
                    string fileType = fileTypeMatch.Value;
                    //Request image using it's URL
                    var client2 = new HttpClient();
                    var response = new HttpResponseMessage();
                    response = await client2.GetAsync(item);
                    var responseContent = await response.Content.ReadAsByteArrayAsync();
                    var fileSize = response.Content.Headers.ContentLength;
                    //Save to disk
                    if ((fileType == "jpg" || fileType == "png" || fileType == "gif") && fileSize >= SizeLimit)
                    {
                        var file = new FileStream(downloadFolder + "/" + sequenceNo + "." + fileType, FileMode.OpenOrCreate);
                        var bw = new BinaryWriter(file);
                        bw.Write(responseContent);
                        bw.Flush();
                        bw.Close();
                        Console.WriteLine("{0}.{1} Download completed...{2} KB(s)", sequenceNo, fileType, fileSize / 1024);
                        sequenceNo++;
                        totalSize += Convert.ToInt64(fileSize);
                    }
                }
            }
            Console.WriteLine("Download Complete. Total {0} image(s). Saved Location: {1}. Total size: {2} KB(s)", sequenceNo - 1, downloadFolder, totalSize / 1024);
        }
        //This method use for Multithreading purpose:
        public async void DownloadMultiThreadAsync(int startPage, int endPage, int threadID)
        {
            Console.WriteLine("Thread {0} start ...", threadID);
            int sequenceNo = 1;
            long totalSize = 0;
            //Create download folder:
            string siteNamePattern = @"[^\/]+\..+\..{3}";
            Regex siteNameRegex = new Regex(siteNamePattern);
            Match siteNameMatch = siteNameRegex.Match(HomePage);
            string siteName = siteNameMatch.Value;
            string downloadFolder = "d:/ImageCrawler/" + siteName + "/(page " + startPage + "_" + endPage + ")";
            System.IO.Directory.CreateDirectory(downloadFolder);
            //Download each specific page of site:
            for (int pageNo = startPage; pageNo <= endPage; pageNo++)
            {
                Console.WriteLine("Thread--{0}-------------- Starting download Page {1} --------------{2} Thread(s) were completed", threadID, pageNo, CompleteThreadNums);
                string childrenPageUrl = HomePage + "page/" + pageNo;
                Task<string> htmlTask = RequestHtmlAsync(childrenPageUrl);
                string html = htmlTask.Result;
                List<string> linksOfChildrenPage = GetLink(html);
                //Download each specific link of page:
                foreach (var item in linksOfChildrenPage)
                {
                    //Tim extension cua file anh
                    string fileTypePattern = "...$";
                    Regex fileTypeRegex = new Regex(fileTypePattern);
                    Match fileTypeMatch = fileTypeRegex.Match(item);
                    string fileType = fileTypeMatch.Value;
                    //Request image using it's URL
                    var client2 = new HttpClient();
                    var response = new HttpResponseMessage();
                    response = await client2.GetAsync(item);
                    var responseContent = await response.Content.ReadAsByteArrayAsync();
                    var fileSize = response.Content.Headers.ContentLength;
                    //Save to disk
                    if ((fileType == "jpg" || fileType == "png" || fileType == "gif") && fileSize >= SizeLimit)
                    {
                        var file = new FileStream(downloadFolder + "/" + sequenceNo + "." + fileType, FileMode.OpenOrCreate);
                        var bw = new BinaryWriter(file);
                        bw.Write(responseContent);
                        bw.Flush();
                        bw.Close();
                        sequenceNo++;
                        totalSize += Convert.ToInt64(fileSize);
                    }
                }
            }
            CompleteThreadNums++;
            Console.WriteLine("(Thread {3})Download Complete. Total {0} image(s). Saved Location: {1}. Total size: {2} KB(s)", sequenceNo - 1, downloadFolder, totalSize / 1024, threadID);
        }
        private List<string> GetLink(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            List<HtmlNode> imageNodes = null;
            imageNodes = doc.DocumentNode.SelectNodes("//img[@src]").ToList();
            //int nodeNum = imageNodes.Count;
            //Console.WriteLine("Trang web co {0} hinh anh.", nodeNum);
            List<string> imageUrls = new List<string>();
            foreach (HtmlNode node in imageNodes)
            {
                string src = node.Attributes["src"].Value;
                string httppattern = "^http.+";
                Regex httpregex = new Regex(httppattern);
                if (httpregex.IsMatch(src))
                {
                    imageUrls.Add(src);
                    //Console.WriteLine(src);
                }
            }
            //Console.WriteLine("Co {0} link anh http.", imageUrls.Count());
            return imageUrls;
        }
        private async Task<string> RequestHtmlAsync(string pageAddress)
        {
            var client = new HttpClient();
            var response = new HttpResponseMessage();
            response = await client.GetAsync(pageAddress);
            //response = Task.Run(client.GetAsync(diaChiFile));
            var responseContent = await response.Content.ReadAsStringAsync();
            return responseContent;
        }
        private int CountNumOfPages()
        {
            Console.WriteLine("Counting number of pages ...");
            int startNum = 1;
            int endNum = 10000;
            int numOfPages = 1;
            int lastEndNum = endNum;
            while (endNum > startNum)
            {
                //Console.WriteLine("from {0} to {1}",startNum,endNum);
                string childrenPageUrl = HomePage + "page/" + endNum;
                Task<string> htmlTask = RequestHtmlAsync(childrenPageUrl);
                string html = htmlTask.Result;
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);
                if (doc.DocumentNode.SelectNodes("//@class[@class='post photo']") == null && doc.DocumentNode.SelectNodes("//@class[@class='photoset']") == null&& doc.DocumentNode.SelectNodes("//@class[@class='photo_wrap']") == null&& doc.DocumentNode.SelectNodes("//@id[@id='photo']") == null)
                {
                    if (endNum - startNum > 1)
                    {
                        int temp = (startNum + endNum) / 2;
                        lastEndNum = endNum;
                        endNum = temp;
                    }
                    else
                    {
                        endNum = startNum;
                    }
                }
                else
                {
                    if (endNum - startNum > 1)
                    {
                        startNum = endNum;
                        endNum = lastEndNum;
                    }
                    else
                    {
                        startNum = endNum;
                    }
                }
            }
            numOfPages = startNum;
            return numOfPages;
        }
        private int CountImgNumsPerPage()
        {
            Console.WriteLine("Counting Number of Images per Page ...");
            string firstPageUrl = HomePage + "page/1";
            Task<string> htmlTask = RequestHtmlAsync(firstPageUrl);
            string html = htmlTask.Result;
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            List<HtmlNode> imageNodes = null;
            imageNodes = doc.DocumentNode.SelectNodes("//img[@src]").ToList();
            int nodeNum = 0;
            foreach (var item in imageNodes)
            {
                string src = item.Attributes["src"].Value;
                string imgCheckPattern = @"(.+jpg|.+png|.+gif)";
                Regex imgCheckRegex = new Regex(imgCheckPattern);
                if (imgCheckRegex.IsMatch(src))
                {
                    nodeNum++;
                }
            }
            return nodeNum;
        }
        public List<int[]> SliceDownloadPages(int startPage, int endPage)
        {
            int threadNums = 10;
            List<int[]> rangesSet = new List<int[]>();
            int range = endPage - startPage + 1;
            if (range <= threadNums)
            {
                for (int i = 1; i <= range; i++)
                {
                    int[] j = new int[2] { i, i };
                    rangesSet.Add(j);
                }
            }
            else
            {
                int step = range /threadNums ;
                int check = range % threadNums;
                if (check == 0)
                {
                    int temp = startPage;
                    for (int i = 1; i <= threadNums; i++)
                    {
                        int[] j = new int[2] { temp, temp + step - 1};
                        rangesSet.Add(j);
                        temp += step;
                    }
                }
                else
                {
                    int temp = startPage;
                    for (int i = 1; i <= threadNums; i++)
                    {
                        if (temp + step - 1 <= endPage)
                        {
                            int[] j = new int[2] { temp, temp + step - 1};
                            rangesSet.Add(j);
                            temp += step;
                        }
                        else
                        {
                            int[] j = new int[2] { temp, temp + step - 2 };
                            rangesSet.Add(j);
                            temp += (step-1);
                        }
                    }
                }
            }
            return rangesSet;
        }
    }
}
