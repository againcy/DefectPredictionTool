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
        private static Regex regexKeyword = new Regex(@"fix(e[ds])?|defects?|patch");

        public enum CheckMode { JIRA, BugNumber, Keyword, NumberAndKeyword };
        private List<string> bugID_JIRA;
        private string projectName;

        public BugCommitChecker(string _projectName)
        {
            projectName = _projectName;
        }
        
        /// <summary>
        /// 读入bug ID
        /// </summary>
        /// <param name="path"></param>
        public void ReadBugID_JIRA(string path)
        {
            bugID_JIRA = new List<string>();
            StreamReader sr = new StreamReader(path);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (bugID_JIRA.Contains(line) == false) bugID_JIRA.Add(line.ToLower());
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
                switch (mode)
                {
                    default:
                    case CheckMode.JIRA:
                        var JIRA = new Regex(projectName.ToLower() + @"\-[0-9]+");
                        var match = JIRA.Match(commit);
                        flag = match.Success && bugID_JIRA.Contains(match.ToString());
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
                };
                if (flag == true) break;
            }
            return flag;
        }
    }
}
