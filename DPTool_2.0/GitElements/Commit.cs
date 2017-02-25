using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPTool_2
{
    public class Commit
    {
        public string commitno, author, message;
        public string[] changedfiles;
        public DateTime commitdate, authordate;
    }

    public class CommitChangeInfo
    {
        public string commitNo;
        public DateTime commitDate;
        public bool isBugCommit;
        public string authorName;

        //<filepath, #changed lines>
        public Dictionary<string, int> oldLinesDelta;
        public Dictionary<string, int> newLinesDelta;

        public CommitChangeInfo(string commitNo)
        {
            this.commitNo = commitNo;
            oldLinesDelta = new Dictionary<string, int>();
            newLinesDelta = new Dictionary<string, int>();
        }

        public CommitChangeInfo()
        {
            oldLinesDelta = new Dictionary<string, int>();
            newLinesDelta = new Dictionary<string, int>();
        }

        /// <summary>
        /// 将信息导出成字符串
        /// </summary>
        /// <param name="author">是否需要导入作者名</param>
        /// <returns></returns>
        public string ExportToString(bool author = false)
        {
            var str = "" + commitNo+Environment.NewLine;
            str += this.isBugCommit.ToString() + Environment.NewLine;
            str += this.commitDate.ToShortDateString() + Environment.NewLine;
            if (author == true) str += this.authorName + Environment.NewLine;
            foreach(var file in oldLinesDelta.Keys)
            {
                try
                {
                    str += string.Format("{0},{1},{2}", file, oldLinesDelta[file], newLinesDelta[file]) +Environment.NewLine;
                }
                catch(Exception)
                {
                    System.Diagnostics.Debug.WriteLine("Delta file name not exist");
                }
            }
            return str;
        }

        /// <summary>
        /// 将字符串导入，格式：
        /// line 0: commit no
        /// line 1: 是否为bug commit
        /// line 2: commit date
        /// [line 3: author Name]
        /// line i: 文件名,旧文件删除行数,新文件增加行数
        /// </summary>
        /// <param name="str"></param>
        /// <param name="author">是否包含作者名</param>
        /// <returns>指示导入是否成功</returns>
        public bool Import(string str,bool author = false)
        {
            bool check = true;
            var content = str.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (content.Count() == 0) return true;
            this.commitNo = content[0];//line 0
            this.isBugCommit = (content[1] == "True") ? true : false;//line 1
            this.commitDate = Convert.ToDateTime(content[2]);//line 2
            var skipLines = 3;
            if (author == true)
            {
                skipLines = 4;
                authorName = content[3];//line 3
            }
            foreach (var line in content.Skip(skipLines))
            {
                try
                {
                    var tmp = line.Split(',');
                    var file = tmp[0];
                    var oldDelta = Convert.ToInt32(tmp[1]);
                    var newDelta = Convert.ToInt32(tmp[2]);
                    oldLinesDelta.Add(file, oldDelta);
                    newLinesDelta.Add(file, newDelta);
                }
                catch
                {
                    check = false;
                }
            }
            return check;
        }
    }
}
