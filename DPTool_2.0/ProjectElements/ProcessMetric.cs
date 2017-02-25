using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;

namespace DPTool_2
{
    public class ProcessMetric:Metrics
    {
        public ProcessMetric()
        {
            table = new DataTable();
        }
        /// <summary>
        /// 从文件中读入模块名
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="format">Understand:需要修改模块名; Standard:不需要修改</param>
        /// <param name="formatArgs">对于Understand度量，文件名中需要删除的子串</param>
        public void ReadModuleName(string path,string format, string formatArgs = "")
        {
            this.AddNewColumn("name", "System.String");
            //读入
            StreamReader sr = new StreamReader(path);
            string line = sr.ReadLine();//跳过表头
            while ((line = sr.ReadLine()) != null)
            {
                var row = table.NewRow();
                var name = line.Split(',')[0];
                if (format == "Understand") name = name.Replace(formatArgs, "").Replace(@"\","/");
                row["name"] = name;
                table.Rows.Add(row);
            }
            sr.Close();
        }

        /// <summary>
        /// 根据条件获取相关的commit
        /// </summary>
        /// <param name="commits">commit全集</param>
        /// <param name="file">文件名（忽略文件名请输入"NULL"）</param>
        /// <param name="startDate">开始时间</param>
        /// <param name="endDate">结束时间</param>
        /// <returns></returns>
        private IEnumerable<CommitChangeInfo> RelatedCommits(
            IEnumerable<CommitChangeInfo> commits,
            string file,
            DateTime startDate,
            DateTime endDate)
        {
            var relatedCommits = from x in commits
                                 where (x.oldLinesDelta.ContainsKey(file) || x.newLinesDelta.ContainsKey(file) || file=="NULL") 
                                    && (startDate <= x.commitDate && x.commitDate < endDate)
                                 select x;
            return relatedCommits;
        }

        /// <summary>
        /// COMM 每个文件相关的commit数
        /// </summary>
        public void Add_COMM(IEnumerable<CommitChangeInfo> commits,DateTime startDate, DateTime endDate)
        {
            Console.WriteLine("Processing COMM...");
            this.AddNewColumn("COMM", "System.Int32");

            foreach (DataRow row in table.Rows)
            {
                var file = row["name"].ToString();
                row["COMM"] = RelatedCommits(commits, file, startDate, endDate).Count();
            }
        }

        /// <summary>
        /// ADEV 当前版本对该文件做出修改的作者人数
        /// </summary>
        public void Add_ADEV(IEnumerable<Author> authors, DateTime startDate, DateTime endDate)
        {
            Console.WriteLine("Processing ADEV...");
            this.AddNewColumn("ADEV", "System.Int32");
            foreach (DataRow row in table.Rows)
            {
                var cnt = 0;//计数
                var file = row["name"].ToString();
                foreach (var a in authors)
                {
                    var relatedCommit = RelatedCommits(a.commitChangeInfo, file, startDate, endDate);
                    if (relatedCommit.Count() > 0) cnt++;
                }
                row["ADEV"] = cnt;
            }
        }

        /// <summary>
        /// DDEV 截止当前版本，对该文件做出修改的作者总数
        /// </summary>
        public void Add_DDEV(IEnumerable<Author> authors, DateTime endDate)
        {
            Console.WriteLine("Processing DDEV...");
            this.AddNewColumn("DDEV", "System.Int32");
            foreach (DataRow row in table.Rows)
            {
                var cnt = 0;//计数
                var file = row["name"].ToString();
                foreach (var a in authors)
                {
                    var relatedCommit = RelatedCommits(a.commitChangeInfo, file, Program.zeroDate, endDate);
                    if (relatedCommit.Count() > 0) cnt++;
                }
                row["DDEV"] = cnt;
            }
        }

        /// <summary>
        /// ADD 该文件新增的行数
        /// DEL 该文件被删除的行数
        /// 用总新增/删除行数归一化
        /// </summary>
        public void Add_ADD_DEL(IEnumerable<CommitChangeInfo> commits, DateTime startDate, DateTime endDate)
        {
            Console.WriteLine("Processing ADD & DEL...");
            this.AddNewColumn("ADD", "System.Double");
            this.AddNewColumn("DEL", "System.Double");
            //获取总新增/删除行数
            var totalAdd = 0;
            var totalDel = 0;
            foreach(var c in RelatedCommits(commits, "NULL", startDate, endDate))
            {
                totalAdd += c.newLinesDelta.Values.Sum();
                totalDel += c.oldLinesDelta.Values.Sum();
            }
            if (totalAdd == 0) totalAdd = 1;//防止NaN
            if (totalDel == 0) totalDel = 1;
            //获取每个文件的新增/删除行数
            foreach(DataRow row in table.Rows)
            {
                var add = 0;
                var del = 0;
                var file = row["name"].ToString();
                foreach (var c in RelatedCommits(commits, file, startDate, endDate))
                {
                    try
                    {
                        add += c.newLinesDelta[file];
                        del += c.oldLinesDelta[file];
                    }
                    catch { }
                }
                row["ADD"] = (double)add / (double)totalAdd;
                row["DEL"] = (double)del / (double)totalDel;
            }
        }

        /// <summary>
        /// OWN 对一个文件贡献的行数修改最多的作者所修改的行数百分比(修改行数占文件行数)
        /// </summary>
        public void Add_OWN(IEnumerable<CommitChangeInfo> commits, DateTime startDate, DateTime endDate, CodeMetric code)
        {
            Console.WriteLine("Processing OWN...");
            this.AddNewColumn("OWN", "System.Double");
            foreach (DataRow row in table.Rows)
            {
                var file = row["name"].ToString();
                //记录每个作者对该文件修改的行数
                var commitLines = new Dictionary<string, int>();//<author,lines>
                foreach (var c in RelatedCommits(commits, file, startDate, endDate))
                {
                    if (commitLines.ContainsKey(c.authorName) == false) commitLines.Add(c.authorName, 0);
                    commitLines[c.authorName] += c.newLinesDelta[file] + c.oldLinesDelta[file];
                }
                if (commitLines.Count == 0) row["OWN"] = 0;
                else
                {
                    foreach (DataRow codeRow in code.table.Rows)
                    {
                        if (codeRow["name"].ToString() == file)
                            row["OWN"] = (double)commitLines.Values.Max() / (double)codeRow["CountLineCode"];
                    }
                    //row["OWN"] = (double)commitLines.Values.Max() 
                }
            }
        }

        /// <summary>
        /// MINOR 对一个文件的commit数低于该文件总commit数5%的作者数
        /// MAJOR 大于等于5%
        /// </summary>
        public void Add_MINOR_MAJOR(IEnumerable<CommitChangeInfo> commits, DateTime startDate, DateTime endDate)
        {
            Console.WriteLine("Processing MINOR & MAJOR...");
            this.AddNewColumn("MINOR", "System.Int32");
            this.AddNewColumn("MAJOR", "System.Int32");
            foreach (DataRow row in table.Rows)
            {
                var file = row["name"].ToString();
                //记录每个作者相关该文件的commit数
                var authorCnt = new Dictionary<string, int>();
                foreach (var c in RelatedCommits(commits, file, startDate, endDate))
                {
                    if (authorCnt.ContainsKey(c.authorName) == false) authorCnt.Add(c.authorName, 0);
                    authorCnt[c.authorName]++;
                }
                var sum = authorCnt.Values.Sum();
                var threshold = 0.05;//5%作为阈值
                var minor = from a in authorCnt.Values
                            where a < sum * threshold
                            select a;
                row["MINOR"] = minor.Count();
                row["MAJOR"] = authorCnt.Keys.Count() - minor.Count();
            }
        }

        /// <summary>
        /// EXP 对一个文件做出修改的作者的加权平均exp
        /// </summary>
        public void Add_EXP(IEnumerable<CommitChangeInfo> commits,IEnumerable<Author> authors, DateTime startDate, DateTime endDate)
        {
            Console.WriteLine("Processing EXP...");
            this.AddNewColumn("EXP", "System.Double");
            foreach (DataRow row in table.Rows)
            {
                var file = row["name"].ToString();
                int totalDelta = 0;
                double exp = 0;
                foreach (var c in RelatedCommits(commits, file, startDate, endDate))
                {
                    //找到该文件相关的所有commit，每个作者的权值为他在这个commit中对该文件做出的修改行数
                    var e = authors.Single(x => x.name == c.authorName).GetExp(c.commitDate);
                    try
                    {
                        var delta = c.newLinesDelta[file] + c.oldLinesDelta[file];
                        exp += e * delta;
                        totalDelta += delta;
                    }
                    catch { }
                }
                if (totalDelta == 0) totalDelta = 1;
                exp /= (double)totalDelta;
                row["EXP"] = exp;
            }
        }

        /// <summary>
        /// OEXP 对一个文件修改量最多的作者的exp
        /// </summary>
        public void Add_OEXP(IEnumerable<CommitChangeInfo> commits, IEnumerable<Author> authors, DateTime startDate, DateTime endDate)
        {
            Console.WriteLine("Processing OEXP...");
            this.AddNewColumn("OEXP", "System.Double");
            foreach (DataRow row in table.Rows)
            {
                var file = row["name"].ToString();
                var rc = RelatedCommits(commits, file, startDate, endDate);
                //找到贡献最多的作者owner
                var commitLines = new Dictionary<string, int>();//<author,lines>
                foreach (var c in rc)
                {
                    if (commitLines.ContainsKey(c.authorName) == false) commitLines.Add(c.authorName, 0);
                    commitLines[c.authorName] += c.newLinesDelta[file] + c.oldLinesDelta[file];
                }
                if (commitLines.Count() == 0)
                {
                    row["OEXP"] = 0;
                    continue;
                }
                var downOrder = from x in commitLines
                                orderby x.Value descending
                                select x;
                
                var owner = downOrder.First().Key;
                //获取owner的exp
                var exp = authors.Single(x => x.name == owner).GetExp(endDate);
                row["OEXP"] = exp;
            }
        }

        public void Add_SCTR(IEnumerable<CommitChangeInfo> commits, DateTime startDate, DateTime endDate)
        {

        }

        /// <summary>
        /// 计算某个度量metric的Neighbour度量N-metric
        /// 对一个文件F，与它一起出现在同一个commit中的其它文件的集合为{co-file}，F的N-metric为这些co-file的metric的加权和，权为每个co-file与F一同出现在一个commit中的频率
        /// </summary>
        public void Add_NeighbourMetrics(IEnumerable<CommitChangeInfo> commits, DateTime startDate, DateTime endDate, string metric)
        {
            var metricName = "N" + metric;
            Console.WriteLine("Processing {0}...",metricName);
            this.AddNewColumn(metricName, "System.Double");

            foreach (DataRow row in this.table.Rows)
            {
                var file = row["name"].ToString();
                var cofile = new Dictionary<string, int>();//记录该文件的每个co-file（即和file出现在同一个commit中的其它file）与file一同出现在几个commit中
                var commitCnt = 0;
                foreach (var c in RelatedCommits(commits,file,startDate,endDate))
                {
                    commitCnt++;
                    //找到该commit中除file以外的其它修改的文件
                    var cf = new List<string>();
                    foreach (var f in c.oldLinesDelta.Keys) if (cf.Contains(f) == false) cf.Add(f);
                    foreach (var f in c.newLinesDelta.Keys) if (cf.Contains(f) == false) cf.Add(f);
                    cf.Remove(file);
                    //记录每个cofile出现的commit数
                    foreach(var f in cf)
                    {
                        if (cofile.ContainsKey(f) == false) cofile.Add(f, 0);
                        cofile[f]++;
                    }
                }
                //计算neighbour metric
                var value = 0.0;
                foreach(DataRow r in this.table.Rows)
                {
                    var f = r["name"].ToString();
                    if (cofile.ContainsKey(f) == false) continue;
                    value += Convert.ToDouble(r[metric].ToString()) * (double)cofile[f] / (double)commitCnt;
                }
                row[metricName] = value.ToString();
            }
        }

    }
}
