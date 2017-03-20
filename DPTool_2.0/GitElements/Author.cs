using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using DPTool_2.AnalyzeGitLog;

namespace DPTool_2
{
    
    public class Author
    {
        public string name;

        
        public List<CommitChangeInfo> commitChangeInfo;//<commit no, change info>

        public SortedDictionary<DateTime, int> experience;//<timepoint, exp>

        public Author(string name)
        {
            this.name = name;
            commitChangeInfo = new List<CommitChangeInfo>();
        }

        /// <summary>
        /// 将信息导出成字符串
        /// </summary>
        /// <returns></returns>
        public string ExportToString()
        {
            var str = "";
            foreach(var info in commitChangeInfo)
            {
                str += info.ExportToString() + Environment.NewLine + "===================" + Environment.NewLine;
            }
            return str;
        }

        /// <summary>
        /// 从字符串导入作者信息
        /// </summary>
        /// <param name="str"></param>
        /// <returns>指示导入过程中是否未出现问题</returns>
        public bool Import(string str)
        {
            bool check = true;

            var content = str.Split(new string[] { "===================" }, StringSplitOptions.RemoveEmptyEntries);
            foreach(var info in content)
            {
                var changeInfo = new CommitChangeInfo();
                check = check & changeInfo.Import(info);
                changeInfo.authorName = this.name;
                this.commitChangeInfo.Add(changeInfo);
            }
            return check;
        }

        /// <summary>
        /// 生成作者的experience
        /// </summary>
        /// <param name="startDate">起始时间</param>
        /// <param name="endDate">结束时间</param>
        public void GenerateExp(DateTime startDate, DateTime endDate)
        {
            experience = new SortedDictionary<DateTime, int>();
            var sorted = from c in commitChangeInfo
                         where startDate <= c.commitDate && c.commitDate <= endDate
                         orderby c.commitDate ascending
                         select c;
            var exp = 0;
            //使用作者修改过的文件数作为exp
            foreach(var c in sorted)
            {
                if (experience.ContainsKey(c.commitDate) == false) experience.Add(c.commitDate, 0);
                exp += Math.Max(c.oldLinesDelta.Count(), c.newLinesDelta.Count());
                experience[c.commitDate] = exp;
            }
        }

        /// <summary>
        /// 获得指定时间点作者的exp
        /// </summary>
        /// <param name="date">指定一个时间点</param>
        /// <returns>该时间点的exp</returns>
        public int GetExp(DateTime date)
        {
            if (date < experience.First().Key) return 0;
            //找到第一个大于date的时间点，将前一个时间点的exp作为返回值
            var former = experience.First();
            foreach(var exp in experience.Skip(1))
            {
                if (exp.Key > date) break;
                former = exp;
            }
            return former.Value;
        }

        /// <summary>
        /// 获取作者在每个commit中修改的文件和行数
        /// </summary>
        /// <param name="commits"></param>
        /// <param name="workdir">repository路径</param>
        /// <param name="language">程序语言(java|c)</param>
        /// <param name="bugChecker">bug commit 检测器</param>
        /// <param name="trackingSystem">bug id 所在的跟踪系统</param>
        /// <returns></returns>
        public static IEnumerable<Author> GetAuthorExp(
            Commit[] commits,
            string workdir, 
            string language,
            BugCommitChecker bugChecker,
            BugCommitChecker.CheckMode trackingSystem)
        {
            var ret = new Dictionary<string, Author>();
            foreach (var x in commits)
            {
                try
                {
                    var gitshowcontent = GitCommandTool.RunGitCommand(workdir, string.Format("show {0}", x.commitno));
                    var gsp = new GitShowParser(gitshowcontent);
                    FileChange[] filechanges = null;
                    if (language == "java") filechanges = gsp.info().Where(y => y.Path.EndsWith(".java")).ToArray();
                    else if (language == "c")
                        filechanges = gsp.info().Where(y => (y.Path.EndsWith(".c")
                                                          || y.Path.EndsWith(".cpp")
                                                          || y.Path.EndsWith(".h")
                                                          || y.Path.EndsWith(".hpp"))).ToArray();
                    if (filechanges.Count() == 0)
                    {
                        //System.Diagnostics.Debug.WriteLine("continued");
                        continue;
                    }
                    //将每个commit修改的文件名和每个文件修改的行数信息添加进作者的信息中
                    if (ret.ContainsKey(x.author) == false) ret.Add(x.author, new Author(x.author));
                    var changeInfo = new CommitChangeInfo(x.commitno);
                    changeInfo.isBugCommit = bugChecker.ContainsBug(x.message,trackingSystem,BugCommitChecker.CheckMode.NumberAndKeyword);
                    changeInfo.commitDate = x.commitdate;
                    changeInfo.authorName = x.author;
                    foreach (var fc in filechanges)
                    {
                        changeInfo.oldLinesDelta.Add(fc.Path, fc.OldVersionChangedLines.Count());
                        changeInfo.newLinesDelta.Add(fc.Path, fc.NewVersionChangedLines.Count());
                    }
                    ret[x.author].commitChangeInfo.Add(changeInfo);
                    //checkFileChanges2(workdir, date, ret, x, filechanges);
                    //System.Diagnostics.Debug.WriteLine("passed with flag = "+flag);
                }
                catch (Exception)
                {
                    //flag = false;
                    System.Diagnostics.Debug.WriteLine("with exception in GitCommandTool.GetAuthorExp");
                    //throw;
                }
            }
            return ret.Values.ToList();

        }
    }
}
