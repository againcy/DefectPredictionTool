using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DPTool_2.AnalyzeGitLog;

namespace DPTool_2
{
    public static class GitLogAnalyzer
    { 
        /// <summary>
        /// 通过git log文件获取所有commit中每个作者修改的文件路径和行数
        /// </summary>
        /// <param name="logPath">log文件路径</param>
        /// <param name="seperator">log文件分隔符</param>
        /// <param name="startDate">查询开始时间</param>
        /// <param name="endDate">查询结束时间</param>
        /// <param name="repoPath">git repository路径</param>
        /// <param name="language">项目语言</param>
        public static IEnumerable<Author> GetAuthorExp(
            string logPath, 
            string seperator, 
            DateTime startDate, 
            DateTime endDate, 
            string repoPath,
            string language
            )
        { 
            var s = new StreamReader(logPath).ReadToEnd();
            var p = new GitLogParser(s, seperator);//"#SEP#");
            var results = p.Commits().Where(x => x.commitdate >= startDate
                && x.commitdate <= endDate).ToArray();
            return GitCommandTool.GetAuthorExp(results, repoPath,language);
        }

        /// <summary>
        /// 获取每个文件存在bug的时间区间
        /// </summary>
        /// <param name="projectName">项目名</param>
        /// <param name="logPath">log文件路径</param>
        /// <param name="repoPath">git repository路径</param>
        /// <param name="seperator">分隔符</param>
        /// <param name="endDate">查找的终止时间</param>
        /// <param name="language">项目语言</param>
        public static IEnumerable<string> GetBuggyIntervals(
            string projectName,
            string logPath,
            string repoPath,
            string seperator,
            DateTime endDate,
            string language
            )
        {
            var intervals = BuggyIntervalFinder.GetBuggyIntervals(logPath, repoPath, seperator, endDate, language);
            var ret = new List<string>();
            foreach (var interval in intervals)
            {
                if (interval == null) continue;
                ret.Add(string.Format("{0},{1},{2}", interval.file, interval.startdate.ToShortDateString(), interval.enddate.ToShortDateString()));
            }
            return ret;
        }
    }
}
