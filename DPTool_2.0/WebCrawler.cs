using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using HtmlAgilityPack;

namespace DPTool_2
{
    class WebCrawler
    {
        public enum TrackingSystem { JIRA, Bugzilla };
        public struct ReleaseBugInfo
        {
            public string releaseNo;
            public string releaseDate;
            public List<string> bugIDs;
        }

        private string urlHeadJIRA = @"https://issues.apache.org";
        private string project;

        public void DoWork(string _project,string rootDir,TrackingSystem system)
        {
            project = _project;
            Console.WriteLine("Processing [{0}]", project);

            string url;
            switch (system)
            {
                case TrackingSystem.JIRA:
                    url = string.Format(@"{0}/jira/browse/{1}?selectedTab=com.atlassian.jira.jira-projects-plugin:changelog-panel&allVersions=true", urlHeadJIRA, project);
                    Output(rootDir, Analyse_JIRA(GetDocument(url)), TrackingSystem.JIRA);
                    break;
                case TrackingSystem.Bugzilla:
                    url = string.Format(@"https://bz.apache.org/bugzilla/buglist.cgi?bug_status=RESOLVED&limit=0&product={0}&query_format=advanced&resolution=FIXED", "Log4j%20-%20Now%20in%20Jira");
                    Output(rootDir, Analyse_Bugzilla(GetDocument(url)), TrackingSystem.Bugzilla);
                    break;
            };
        }

        /// <summary>
        /// 输出
        /// </summary>
        /// <param name="rootDir"></param>
        /// <param name="result"></param>
        private void Output(string rootDir, IEnumerable<ReleaseBugInfo> result, TrackingSystem system)
        {
            //bug id
            StreamWriter sw = new StreamWriter(string.Format(@"{0}\{1}_releases\bugID.txt", rootDir, project));
            foreach(var rel in result)
            {
                foreach(var bugId in rel.bugIDs)
                {
                    sw.WriteLine(bugId);
                }
            }
            sw.Close();

            //release date
            if (system != TrackingSystem.JIRA) return;
            sw = new StreamWriter(string.Format(@"{0}\{1}_releases\releaseDate.txt", rootDir, project));
            var relList = from rel in result
                       orderby rel.releaseDate ascending
                       select new { no = rel.releaseNo, date = rel.releaseDate };
            foreach(var rel in relList)
            {
                sw.WriteLine(rel.no + "," + rel.date);
            }                        
            sw.Close();
        }

        /// <summary>
        /// 载入指定的url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="printLoadingInfo">是否打印加载过程</param>
        /// <returns></returns>
        private HtmlDocument GetDocument(string url, bool printLoadingInfo=false)
        {
            if (printLoadingInfo == true) Console.WriteLine("Loading [{0}]", url);
            try
            {
                WebRequest webRequest = WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader sr = new StreamReader(dataStream);
                var content = sr.ReadToEnd();
                var doc = new HtmlDocument();
                doc.LoadHtml(content);

                sr.Close();
                dataStream.Close();
                response.Close();

                if (printLoadingInfo == true) Console.WriteLine("Complete loading [{0}]", url);
                return doc;
            }
            catch
            {
                Console.WriteLine("Loading page error...");
                Program.Log(string.Format("WebCrawler.GetDocument: Loading page error [{0}]", url));
                return null;
            }
        }

        /// <summary>
        /// 分析JIRA页面
        /// </summary>
        /// <param name="doc">项目主页面</param>
        /// <param name="path">输出路径</param>
        private IEnumerable<ReleaseBugInfo> Analyse_JIRA(HtmlDocument doc)
        {
            var result = new List<ReleaseBugInfo>();
            if (doc == null)
            {
                Program.Log(string.Format("WebCrawler.Analyse_JIRA: [{0}], project page error", project));
                return null;
            }
            var relPageNodes = doc.DocumentNode.SelectNodes(@"//li[@class='version-block-container']");
            foreach (var releaseNode in relPageNodes)
            {
                var newRelease = new ReleaseBugInfo();
                //release information
                var node_relNo = releaseNode.SelectSingleNode(@"descendant::h3[@class='version-title']");
                var node_relDate = releaseNode.SelectSingleNode(@"descendant::span[@title='Release date']");
                var relNo = (node_relNo != null)? node_relNo.InnerText : "null";
                var relDate = (node_relDate != null) ? node_relDate.InnerText : "1/Jan/70";
                newRelease.releaseNo = relNo.Replace(" ", "");
                var date = relDate.Split('/');
                newRelease.releaseDate = string.Format("20{0}/{1}/{2}", date[2], date[1], date[0]);
                newRelease.bugIDs = new List<string>();
                Console.WriteLine("Now Release [{0}]", relNo);

                //release notes(bug report)
                var newUrl = releaseNode.SelectSingleNode(@"descendant::a[@title='View release notes']").Attributes["href"].Value;
                var newPage = GetDocument(urlHeadJIRA + newUrl);
                if (newPage == null)
                {
                    Program.Log(string.Format("WebCrawler.Analyse_JIRA: [{0} {1}], page error", project, relNo));
                    continue;
                }
                foreach (var node in newPage.DocumentNode.SelectNodes(@"//h2"))
                {
                    if (node.InnerText == "Bug")
                    {
                        var bugNodes = node.SelectNodes(@"following-sibling::ul[1]/li");
                        foreach (var bugNode in bugNodes)
                        {
                            var bugID = bugNode.SelectSingleNode(@"descendant::a").InnerText;
                            newRelease.bugIDs.Add(bugID);
                        }
                        break;
                    }
                }
                result.Add(newRelease);
            }
            return result;
        }

        /// <summary>
        /// 分析bugzilla页面
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        private IEnumerable<ReleaseBugInfo> Analyse_Bugzilla(HtmlDocument doc)
        {
            var result = new List<ReleaseBugInfo>();
            var allRel = new ReleaseBugInfo();
            allRel.bugIDs = new List<string>();
            
            if (doc == null)
            {
                Program.Log(string.Format("WebCrawler.Analyse_Bugzilla: [{0}], project page error", project));
                return null;
            }
            var bugReportNodes = doc.DocumentNode.SelectNodes(@"//td[@class='first-child bz_id_column']");
            foreach(var node in bugReportNodes)
            {
                var idNode = node.SelectSingleNode(@"descendant::a");
                if (idNode != null) allRel.bugIDs.Add(idNode.InnerText);
            }
            result.Add(allRel);
            return result;
        }
    }
}
