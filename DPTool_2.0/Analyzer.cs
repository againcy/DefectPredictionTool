using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace DPTool_2
{
    public static class Analyzer
    {
        /// <summary>
        /// 统计各种类模块数量
        /// 结果：newborn,developing,dead,relLength,avgLength
        /// </summary>
        /// <param name="p">项目</param>
        /// <param name="startRel">起始版本</param>
        /// <param name="endRel">末尾版本</param>
        /// <returns>newborn,developing,dead,relLength,avgLength</returns>
        public static string ModuleType(Project p, string startRel, string endRel)
        {
            var pickedRel = p.GetReleaseBetween(startRel, endRel);
            var endRelease = pickedRel.Last().Value;
            //结果指标
            var newborn = 0;
            var developing = 0;
            var dead = 0;
            var avgLength = 0.0;

            var modules = new List<string>();
            foreach(DataRow row in endRelease.codeMetric.table.Rows)
            {
                var name = row["name"].ToString();
                modules.Add(name);
                //统计该模块出现在了几个版本中
                var relCnt = 0;
                foreach (var rel in pickedRel.Values)
                {
                    foreach (DataRow row2 in rel.codeMetric.table.Rows)
                    {
                        if (row2["name"].ToString() == name)
                        {
                            relCnt++;
                            break;
                        }
                    }
                }
                if (relCnt == 1) newborn++;
                else developing++;
                avgLength += relCnt;
            }
            avgLength /= endRelease.codeMetric.table.Rows.Count;
            //找到倒数第二个版本
            var endRel_2 = pickedRel.First().Value;
            foreach(var rel in pickedRel)
            {
                if (rel.Key < endRelease.releaseDate) endRel_2 = rel.Value;
            }
            //统计dead modules
            foreach(DataRow row in endRel_2.codeMetric.table.Rows)
            {
                if (modules.Contains(row["name"].ToString()) == false) dead++;
            }
            //返回结果
            return string.Format("{0},{1},{2},{3},{4}", newborn.ToString(), developing.ToString(), dead.ToString(), pickedRel.Count.ToString(), avgLength.ToString("0.00"));
                
        }

        /// <summary>
        /// 表头
        /// </summary>
        /// <returns></returns>
        public static string ModuleType_Header()
        {
            return "Newborn,Developing,Dead,RelLength,AvgHVSMLength";
        }

        /// <summary>
        /// 统计bug相关信息
        /// 结果：末版本中bug占比,末尾两个版本bug相同的模块数,bug相同的模块占比数
        /// </summary>
        /// <param name="p">项目</param>
        /// <param name="startRel">起始版本</param>
        /// <param name="endRel">末尾版本</param>
        /// <returns>末版本中bug占比,末尾两个版本bug相同的模块数</returns>
        public static string Bugs(Project p, string startRel, string endRel)
        {
            var pickedRel = p.GetReleaseBetween(startRel, endRel);
            var endRelease = pickedRel.Last().Value;
            //统计末版本中含bug的模块数占比
            var bugs = 0;
            foreach(DataRow row in endRelease.codeMetric.table.Rows)
            {
                if (row["bugs"].ToString() == "1") bugs++;
            }
            var bugRate = (double)bugs / (double)endRelease.codeMetric.table.Rows.Count;
            //统计在最后两个版本都出现的模块，bug情况相同的数量
            //找到倒数第二个版本
            var endRel_2 = pickedRel.First().Value;
            foreach (var rel in pickedRel)
            {
                if (rel.Key < endRelease.releaseDate) endRel_2 = rel.Value;
            }
            var samebug = 0;
            var developing = 0;
            foreach(DataRow row in endRelease.codeMetric.table.Rows)
            {
                var name = row["name"].ToString();
                foreach(DataRow row2 in endRel_2.codeMetric.table.Rows)
                {
                    if (row2["name"].ToString()==name)
                    {
                        developing++;
                        if (row2["bugs"].ToString() == row["bugs"].ToString()) samebug++;
                    }
                }
            }

            return string.Format("{0},{1},{2},{3}", bugs.ToString(), bugRate.ToString("0.000"), samebug.ToString(), ((double)samebug / (double)developing).ToString("0.000"));
        }

        /// <summary>
        /// 表头
        /// </summary>
        /// <returns></returns>
        public static string Bugs_Header()
        {
            return "BugModules,BugRate,SameBugModules,SameBugRate";
        }
    }
}
