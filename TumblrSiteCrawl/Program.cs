using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace TumblrSiteCrawl
{
    class Program
    {
        static void Main(string[] args)
        {
            TumblrSite tumbSite = new TumblrSite();
            Console.WriteLine("Welcome! This is tool which auto download images from Tumblr.com site.");
            Console.WriteLine("Please paste Url of Tumblrsite:");
            tumbSite.HomePage = Console.ReadLine();
            Console.WriteLine("How many pages?");
            string PageNums = Console.ReadLine();
            tumbSite.PageNums = Convert.ToInt32(PageNums);
            tumbSite.DownloadImagesAsync();
            Console.ReadLine();
        }
    }
    class TumblrSite
    {
        int sequenceNo = 1;
        long totalSize = 0;
        public string HomePage { get; set; }
        public int PageNums { get; set; }
        public async void DownloadImagesAsync()
        {
            //Create download folder:
            string siteNamePattern = @"[^\/]+\..+\..{3}";
            Regex siteNameRegex = new Regex(siteNamePattern);
            Match siteNameMatch = siteNameRegex.Match(HomePage);
            string siteName = siteNameMatch.Value;
            string downloadFolder = "d:/ImageCrawler/" + siteName;
            System.IO.Directory.CreateDirectory(downloadFolder);
            //Download each specific page of site:
            for (int pageNo = 1; pageNo <= PageNums; pageNo++)
            {
                Console.WriteLine("-------------- Starting download Page {0} --------------",pageNo);
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
                    if (fileType == "jpg"||fileType=="png"||fileType=="gif")
                    {
                        var file = new FileStream(downloadFolder + "/" + sequenceNo + "." + fileType, FileMode.OpenOrCreate);
                        var bw = new BinaryWriter(file);
                        bw.Write(responseContent);
                        bw.Flush();
                        bw.Close();
                        Console.WriteLine("{0}.{1} Download completed...{2} KB(s)", sequenceNo, fileType, fileSize/1024);
                        sequenceNo++;
                        totalSize += Convert.ToInt64(fileSize);
                    }
                }
            }
            Console.WriteLine("Download Complete. Total {0} image(s). Saved Location: {1}. Total size: {2} KB(s)",sequenceNo-1,downloadFolder,totalSize/1024);
        }
        private List<string> GetLink(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            List<HtmlNode> imageNodes = null;
            imageNodes = doc.DocumentNode.SelectNodes("//img").ToList();
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

    }
}
