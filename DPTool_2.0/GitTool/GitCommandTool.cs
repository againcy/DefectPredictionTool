using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DPTool_2
{
    namespace AnalyzeGitLog
    {
        public static class GitCommandTool
        {
            public static string gitCommand = "git.exe";
            public static string RunGitCommand(string workdir, string command)
            {
                Process process = new Process();
                StringBuilder outputStringBuilder = new StringBuilder();

                try
                {
                    process.StartInfo.FileName = gitCommand;
                    process.StartInfo.WorkingDirectory = workdir;
                    process.StartInfo.Arguments = command;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;
                    process.EnableRaisingEvents = false;
                    process.OutputDataReceived += (sender, eventArgs) => outputStringBuilder.AppendLine(eventArgs.Data);
                    process.ErrorDataReceived += (sender, eventArgs) => { };// outputStringBuilder.AppendLine(eventArgs.Data);
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    var output = outputStringBuilder.ToString();

                    if (process.ExitCode != 0)
                    {


                        throw new Exception("Process exited with non-zero exit code of: " + process.ExitCode + Environment.NewLine +
                        "Output from process: " + outputStringBuilder.ToString());
                    }
                    return output;
                }
                finally
                {
                    process.Close();
                }
            }

            /// <summary>
            /// Check if <c>length</c> lines starting at <c>linestart </c> in <c>file</c> are changed by <c>prevcommitno</c>.
            /// </summary>
            /// <param name="linestart"></param>
            /// <param name="length"></param>
            /// <param name="file"></param>
            /// <param name="workdir"></param>
            /// <param name="prevcommitno"></param>
            /// <returns></returns>
            public static bool CheckGitBlame(int linestart, int length, string file, string workdir, string commitno, DateTime date)
            {
                Regex r = new Regex(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}");
                var s = GitCommandTool.RunGitCommand(workdir, string.Format(@"blame -L {0},+{1} {2}^ .{3}", linestart, length, commitno, file))
                    .Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < s.Length; ++i)
                {
                    var date1 = DateTime.Parse(r.Match(s[i]).Value);
                    if (date1 > date)
                        return false;
                }
                return true;
            }
            private static Regex bugkeywordsmatcher = new Regex(@"(?i)[a-z]+");
            private static List<string> bugKeywords = new List<string> {
                "bug","bugs","bugzilla","fail","fails","failed","failing","fix","fixes","fixed","fixing","solve","solves","solved","solving","failure","failures" };
            public static bool ContainsBugKeywords(string info)
            {
                var s = info.ToLower();
                foreach (Match x in bugkeywordsmatcher.Matches(s))
                {
                    if (bugKeywords.Contains(x.Value))
                        //if (x.Groups[0].Value == "BUG" || x.Groups[0].Value == "FIX")
                        return true;
                }
                return false;
            }

            public static IEnumerable<Commit> getBugsNaive(Commit[] commits)
            {
                return commits.Where(x => ContainsBugKeywords(x.message));
            }

            public static IEnumerable<string> GetBugsLine(Commit[] commits, string workdir, DateTime date, string language)
            {
                var ret = new List<string>();
                var bugcommits = commits.Where(x => ContainsBugKeywords(x.message)).ToArray();
                var count = 0;
                foreach (var x in bugcommits)
                {
                    ++count;
                    System.Diagnostics.Debug.Write(count);
                    try
                    {
                        var gitshowcontent = GitCommandTool.RunGitCommand(workdir, string.Format("show {0}", x.commitno));
                        var gsp = new GitShow.GitShowParser(gitshowcontent);
                        GitShow.FileChange[] filechanges = null;
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

                        checkFileChanges2(workdir, date, ret, x, filechanges);
                        //System.Diagnostics.Debug.WriteLine("passed with flag = "+flag);
                    }
                    catch (Exception)
                    {
                        //flag = false;
                        System.Diagnostics.Debug.WriteLine("with exception in GitCommandTool.GetBugsLine");
                        //throw;
                    }
                }
                return ret;
            }

            /// <summary>
            /// 获取作者在每个commit中修改的文件和行数
            /// </summary>
            /// <param name="commits"></param>
            /// <param name="workdir">repository路径</param>
            /// <param name="language">程序文件后缀(.java)</param>
            /// <returns></returns>
            public static IEnumerable<Author> GetAuthorExp(Commit[] commits, string workdir,string language)
            {
                var ret = new Dictionary<string, Author>();
                //var bugcommits = commits.Where(x => ContainsBugKeywords(x.message)).ToArray();
                //var count = 0;
                foreach (var x in commits)
                {
                    try
                    {
                        var gitshowcontent = GitCommandTool.RunGitCommand(workdir, string.Format("show {0}", x.commitno));
                        var gsp = new GitShow.GitShowParser(gitshowcontent);
                        GitShow.FileChange[] filechanges = null;
                        if (language == "java") filechanges = gsp.info().Where(y => y.Path.EndsWith(".java")).ToArray();
                        else if (language == "c") filechanges = gsp.info().Where(y => (y.Path.EndsWith(".c") 
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
                        changeInfo.isBugCommit = ContainsBugKeywords(x.message);
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

            private static void checkFileChanges2(string workdir, DateTime date, List<string> ret, Commit x, GitShow.FileChange[] filechanges)
            {
                Regex r = new Regex(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}");
                var gitresult = "";
                foreach (var f in filechanges)
                    try
                    {
                        gitresult = GitCommandTool.RunGitCommand(workdir, string.Format(@"blame {0}^ .{1}", x.commitno, f.Path));
                        var s = gitresult.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                        var p = 0;
                        var flag = true;
                        foreach (var i in f.OldVersionChangedLines)
                            if (DateTime.Parse(r.Match(s[i - 1]).Value) > date)
                            {
                                flag = false;
                                break;
                            }
                        if (flag)
                            ret.Add(f.Path);
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
            }
            private static void checkFileChanges(string workdir, DateTime date, List<string> ret, Commit x, GitShow.FileChange[] filechanges)
            {
                foreach (var y in filechanges)
                {
                    var flag = true;
                    var l = 0;
                    var r = 0;
                    if (y.OldVersionChangedLines.Length == 0)
                    {

                    }
                    else if (y.OldVersionChangedLines.Length == 1)
                    {
                        var t = GitCommandTool.CheckGitBlame(y.OldVersionChangedLines[0],
                                1,
                                y.Path,
                                workdir,
                                x.commitno,
                                date);
                        flag = flag & t;
                    }
                    else
                        for (int i = 1; i < y.OldVersionChangedLines.Max(); ++i)
                        {
                            if (y.OldVersionChangedLines[i] - 1 != y.OldVersionChangedLines[i - 1])
                            {
                                r = i - 1;
                                var t = GitCommandTool.CheckGitBlame(y.OldVersionChangedLines[l],
                                    y.OldVersionChangedLines[i - 1] - y.OldVersionChangedLines[l] + 1,
                                    y.Path,
                                    workdir,
                                    x.commitno,
                                    date);
                                flag = flag & t;
                                if (!flag)
                                    break;
                                l = i;
                            }
                        }
                    if (flag)
                        ret.Add(y.Path);
                }
            }


        }
    }
}