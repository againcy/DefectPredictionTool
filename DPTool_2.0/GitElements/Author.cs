using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

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
    }
}
