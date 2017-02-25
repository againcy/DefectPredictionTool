using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using DPTool_2.AnalyzeGitLog;
using System.IO;

namespace DPTool_2
{
    static class BuggyIntervalFinder
    {
        static void ForEach<T>(IEnumerable<T> item, Action<T> action)
        {
            foreach (var x in item)
            {
                action.Invoke(x);
            }
        }

        public static IEnumerable<Tuple<string, DateTime>> GitBlame(int[] lines, string commitno, string workdir, string file)
        {
            var ret = new List<Tuple<string, DateTime>>();
            Regex r = new Regex(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}");
            var gitresult = "";
            try
            {
                gitresult = GitCommandTool.RunGitCommand(workdir, string.Format(@"blame {0}^ .{1}", commitno, file));
                var s = gitresult.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                var p = 0;
                for (int i = 0; i < s.Length; ++i)
                {
                    if (lines[p] == i + 1)
                    {
                        var date = DateTime.Parse(r.Match(s[i]).Value);
                        ret.Add(new Tuple<string, DateTime>(s[i].Split(' ')[0], date));
                        ++p;
                    }
                }
            }
            catch (Exception e)
            {

            }
            return ret.Distinct();

        }

        /// <summary>
        /// 获得每个文件存在bug的时间区间
        /// </summary>
        /// <param name="logpath">git log文件路径</param>
        /// <param name="workdir">git repository路径</param>
        /// <param name="sep">分隔符</param>
        /// <param name="endDate">查找的截止时间</param>
        /// <param name="language">程序文件后缀(.java)</param>
        /// <returns></returns>
        public static IEnumerable<BuggyInterval> GetBuggyIntervals(string logpath, string workdir, string sep,DateTime endDate,string language)
        {
            var glp = new AnalyzeGitLog.GitLogParser(new StreamReader(logpath).ReadToEnd(), sep);
            /*var commitdates = new Dictionary<string, DateTime>();
            foreach(var commit in glp.Commits())
            {
                commitdates[commit.commitno] = commit.commitdate;
                
            }*/
            var intervals = new List<BuggyInterval>();
            var counter = 0;
            var glpcommits = glp.Commits().ToArray();
            var total = glpcommits.Count();
            Parallel.
            ForEach(glpcommits, c =>
            {
                //Console.Write(++counter + "/" + total);
                if (c.commitdate <= endDate && GitCommandTool.ContainsBugKeywords(c.message))
                {
                    try
                    {
                        var cntnt = GitCommandTool.RunGitCommand(workdir, string.Format("show {0}", c.commitno));
                        var gsp = new AnalyzeGitLog.GitShow.GitShowParser(cntnt);
                        //var filechanges = gsp.info().Where(y => y.Path.EndsWith(language)).ToArray();
                        AnalyzeGitLog.GitShow.FileChange[] filechanges = null;
                        if (language == "java") filechanges = gsp.info().Where(y => y.Path.EndsWith(".java")).ToArray();
                        else if (language == "c") filechanges = gsp.info().Where(y => (y.Path.EndsWith(".c")
                                                                              || y.Path.EndsWith(".cpp")
                                                                              || y.Path.EndsWith(".h")
                                                                              || y.Path.EndsWith(".hpp"))).ToArray();
                        foreach (var fc in filechanges)
                        {
                            var commits = GitBlame(fc.OldVersionChangedLines, c.commitno, workdir, fc.Path);
                            foreach (var c2 in commits)
                            {
                                var interval = new BuggyInterval();
                                interval.startcommit = c2.Item1;
                                interval.startdate = c2.Item2;
                                interval.endcommit = c.commitno;
                                interval.enddate = c.commitdate;
                                interval.file = fc.Path;
                                intervals.Add(interval);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        //      Console.Write("X");
                    }
                }
                //Console.WriteLine();
            });
            return intervals;
        }
    }
}
