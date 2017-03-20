using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace DPTool_2
{
    public class BugCommitChecker
    {
        private static Regex regexBugNumber = new Regex(@"bug[# \t]*[0-9]+|pr[# \t]*[0-9]+|show_bug\.cgi\?id=[0-9]+|\[[0-9]+\]");
        private static Regex number = new Regex(@"[0-9]+");
        private static Regex regexKeyword = new Regex(@"fix(e[ds])?|defects?|patch");

        public enum CheckMode { JIRA, BugNumber, Keyword, NumberAndKeyword, Bugzilla };
        private List<string> bugID;
        private string projectName;

        public BugCommitChecker(string _projectName)
        {
            projectName = _projectName;
        }
        
        /// <summary>
        /// 读入bug ID
        /// </summary>
        /// <param name="path"></param>
        public void ReadBugID(string path)
        {
            bugID = new List<string>();
            StreamReader sr = new StreamReader(path);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (bugID.Contains(line) == false) bugID.Add(line.ToLower());
            }
            sr.Close();
        }

        /// <summary>
        /// 检测一条commit message是否为bugcommit
        /// </summary>
        /// <param name="commit"></param>
        /// <param name="checkMode"></param>
        /// <returns></returns>
        public bool ContainsBug(string commit, params CheckMode[] checkMode)
        {
            commit = commit.ToLower();
            bool flag = false;
            foreach (var mode in checkMode)
            {
                Match match;
                switch (mode)
                {
                    case CheckMode.JIRA:
                        var JIRA = new Regex(projectName.ToLower() + @"\-[0-9]+");
                        match = JIRA.Match(commit);
                        flag = match.Success && bugID.Contains(match.ToString());
                        break;
                    case CheckMode.Keyword:
                        flag = regexKeyword.Match(commit).Success;
                        break;
                    case CheckMode.BugNumber:
                        flag = regexBugNumber.Match(commit).Success;
                        break;
                    case CheckMode.NumberAndKeyword:
                        flag = regexBugNumber.Match(commit).Success && regexKeyword.Match(commit).Success;
                        break;
                    case CheckMode.Bugzilla:
                        var bugzilla = new Regex(@"bug[# \t]*[0-9]+|show_bug\.cgi\?id=[0-9]+");
                        match = bugzilla.Match(commit);
                        if (match.Success == true)
                        {
                            var bugnum = number.Match(match.ToString()).ToString();
                            flag = bugID.Contains(bugnum);
                        }
                        break;
                    default:
                        break;
                };
                if (flag == true) break;
            }
            return flag;
        }
    }
}
