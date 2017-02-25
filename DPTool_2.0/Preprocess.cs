using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;

namespace DPTool_2
{
    public static class Preprocess
    {
        /// <summary>
        /// 获取HVSM
        /// </summary>
        /// <param name="metricType">度量类型</param>
        /// <param name="project">项目</param>
        /// <param name="startRel">起始版本</param>
        /// <param name="endRel">末尾版本</param>
        /// <param name="HVSM_metrics">HVSM的度量</param>
        /// <param name="HVSM_bugs">HVSM的bug</param>
        public static void GetHVSM(
            string metricType,
            Project project, 
            string startRel, 
            string endRel, 
            out List<string> HVSM_metrics, 
            out List<string> HVSM_bugs
            )
        {
            HVSM_metrics = new List<string>();
            HVSM_bugs = new List<string>();
            //选出startRel到endRel之间的版本
            var pickedRel = project.GetPickedRelease(startRel, endRel);
            //记录最后一个版本以及其度量（模块）
            var endRelease = pickedRel.Last().Value;
            Metrics endRelMetric;
            if (metricType == "process") endRelMetric = endRelease.processMetric;
            else if (metricType == "code") endRelMetric = endRelease.codeMetric;
            else endRelMetric = endRelease.mixedMetric;
            //记录每个版本的度量数，以及总的版本数
            var metricsEachRel = endRelMetric.table.Columns.Count-2;
            var totalRelCnt = pickedRel.Count;
            //遍历最终版本中的模块名，并生成HVSM
            foreach(DataRow module in endRelMetric.table.Rows)
            {
                var name = module["name"].ToString();
                //统计该模块出现在了几个版本中，并将度量串起来
                var relCnt = 0;
                var metrics = "";
                var bugs = "";
                foreach(var rel in pickedRel.Values)
                {
                    Metrics relMetric;
                    if (metricType == "process") relMetric = rel.processMetric;
                    else if (metricType == "code") relMetric = rel.codeMetric;
                    else relMetric = rel.mixedMetric;

                    foreach (DataRow row in relMetric.table.Rows)
                    { 
                        if (row["name"].ToString() == name)
                        {
                            relCnt++;
                            foreach(DataColumn col in relMetric.table.Columns)
                            {
                                if (col.ColumnName == "name") continue;
                                if (col.ColumnName == "bugs")
                                {
                                    var bugStr = "0";
                                    if (row[col].ToString() != "0") bugStr = "1";
                                    bugs += "," + bugStr;
                                }
                                else metrics += "," + row[col].ToString();
                            }
                            break;
                        }
                    }
                }
                //补0
                for (int i = 0; i < (totalRelCnt - relCnt) * metricsEachRel; i++) metrics += ",0";
                for (int i = 0; i < totalRelCnt - relCnt; i++) bugs += ",0";
                //连接
                //HVSM_metrics.Add(name + "," + relCnt.ToString() + metrics);
                //HVSM_bugs.Add(name + "," + relCnt.ToString() + bugs);
                HVSM_metrics.Add( relCnt.ToString() + metrics);
                HVSM_bugs.Add( relCnt.ToString() + bugs);
            }
        }

        /// <summary>
        /// 获取混合版本度量
        /// </summary>
        /// <param name="metricType">度量类型</param>
        /// <param name="project">项目</param>
        /// <param name="startRel">起始版本</param>
        /// <param name="endRel">末尾版本</param>
        /// <param name="header">表头</param>
        /// <param name="mixedRel">混合版本</param>
        public static void GetMixedRel(
            string metricType,
            Project project,
            string startRel,
            string endRel,
            out string header,
            out List<string> mixedRel
            )
        {
            mixedRel = new List<string>();
            header = "";
            //选出startRel到endRel之间的版本
            var pickedRel = project.GetPickedRelease(startRel, endRel);
            
            foreach (var rel in pickedRel.Values)
            {
                Metrics relMetric;
                if (metricType == "process") relMetric = rel.processMetric;
                else if (metricType == "code") relMetric = rel.codeMetric;
                else relMetric = rel.mixedMetric;
                if (header == "")
                {
                    //表头
                    foreach (DataColumn col in relMetric.table.Columns) header += "," + col.ColumnName;
                    header = header.Substring(header.IndexOf(',') + 1);
                }
                foreach (DataRow row in relMetric.table.Rows)
                {
                    var module = row["name"].ToString();
                    foreach (DataColumn col in relMetric.table.Columns)
                    {
                        if (col.ColumnName == "name" || col.ColumnName == "bugs") continue;
                        module += "," + row[col].ToString();
                    }
                    module += "," + (row["bugs"].ToString() == "0" ? "0" : "1");
                    mixedRel.Add(module);
                }
            }
        }
        
        public static void CreateCVList(
            string projectName,
            string release
            )
        {
            string path = string.Format(@"G:\PromissMetrics\{0}_releases\CodeMetrics\{1}.csv", projectName, release);
            StreamReader sr = new StreamReader(path);
            string line = sr.ReadLine();
            int n = 0;
            while ((line = sr.ReadLine()) != null) n++;
            sr.Close();
            var order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;
            Random rand = new Random();
            for (int i = 0; i < n * 2; i++)
            {
                int x = rand.Next(n);
                int y = rand.Next(n);
                var tmp = order[x];
                order[x] = order[y];
                order[y] = tmp;
            }
            StreamWriter sw = new StreamWriter(string.Format(@"G:\PromissMetrics\_cvList\{0}_{1}.csv", projectName, release));
            for (int i = 0; i < n; i++) sw.WriteLine(order[i].ToString());
            sw.Close();
        }

        public static void CreateCV(
            string projectName,
            string release,
            string metricType,
            int fold
            )
        {
            //读入cv list
            var order = new List<int>();
            StreamReader sr = new StreamReader(string.Format(@"G:\PromissMetrics\_cvList\{0}_{1}.csv", projectName, release));
            string line;
            while ((line = sr.ReadLine()) != null) order.Add(Convert.ToInt32(line));
            sr.Close();
            var arrOrder = order.ToArray();
            int n = order.Count();
            //读入度量
            sr = new StreamReader(string.Format(@"G:\PromissMetrics\{0}_releases\{1}\{2}.csv", projectName, metricType, release));
            string header = sr.ReadLine();
            var metrics = new Dictionary<int,string>();
            var cnt = 0;
            while ((line = sr.ReadLine())!=null)
            {
                metrics.Add(cnt, line);
                cnt++;
            }
            sr.Close();
            //输出cv
            var ntest = n / fold;
            for(int f = 0;f<fold;f++)
            {
                //挑出test集的序号
                var test = new List<int>();
                var start = f * ntest;
                var end = (f == fold - 1) ? n : (f + 1) * ntest;
                for (int i = start; i < end; i++)
                {
                    test.Add(arrOrder[i]);
                }
                //分离test和train
                var swTrain = new StreamWriter(string.Format(@"G:\PromissMetrics\_cv\{0}\{1}_{2}_{3}_train.csv", metricType, projectName, release, f.ToString()));//ant_1.3_1_train
                var swTest = new StreamWriter(string.Format(@"G:\PromissMetrics\_cv\{0}\{1}_{2}_{3}_test.csv", metricType, projectName, release, f.ToString()));
                swTrain.WriteLine(header);
                swTest.WriteLine(header);
                foreach (var m in metrics)
                {
                    if (test.Contains(m.Key)) swTest.WriteLine(m.Value);
                    else swTrain.WriteLine(m.Value);
                }
                swTrain.Close();
                swTest.Close();
            }
        }

        public static void CreateCVHVSM(
            string projectName,
            string release,
            string metricType,
            int fold
            )
        {
            //读入cv list
            var order = new List<int>();
            StreamReader sr = new StreamReader(string.Format(@"G:\PromissMetrics\_cvList\{0}_{1}.csv", projectName, release));
            string line;
            while ((line = sr.ReadLine()) != null) order.Add(Convert.ToInt32(line));
            sr.Close();
            var arrOrder = order.ToArray();
            int n = order.Count();
            //读入度量
            var metricPath = "";
            var bugPath = "";
            foreach (var file in Directory.GetFiles(@"G:\PromissMetrics\_HVSM_"+metricType))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.StartsWith(projectName))
                {
                    if (fileName.EndsWith(release + "_metrics")) metricPath = file;
                    if (fileName.EndsWith(release + "_bugs")) bugPath = file;
                }
                if (metricPath != "" && bugPath != "") break;
            }
            var metricFileName = Path.GetFileNameWithoutExtension(metricPath);
            var bugFileName = Path.GetFileNameWithoutExtension(bugPath);
            sr = new StreamReader(metricPath);
            var metrics = new Dictionary<int, string>();
            var cnt = 0;
            while ((line = sr.ReadLine()) != null)
            {
                metrics.Add(cnt, line);
                cnt++;
            }
            sr.Close();
            sr = new StreamReader(bugPath);
            var bugs = new Dictionary<int, string>();
            cnt = 0;
            while ((line = sr.ReadLine()) != null)
            {
                bugs.Add(cnt, line);
                cnt++;
            }
            sr.Close();

            //输出cv
            var ntest = n / fold;
            for (int f = 0; f < fold; f++)
            {
                //挑出test集的序号
                var test = new List<int>();
                var start = f * ntest;
                var end = (f == fold - 1) ? n : (f + 1) * ntest;
                for (int i = start; i < end; i++)
                {
                    test.Add(arrOrder[i]);
                }
                //分离test和train
                var swTrain = new StreamWriter(string.Format(@"G:\PromissMetrics\_cvHVSM\{0}\{1}_{2}_train.csv",metricType, metricFileName, f.ToString()));
                var swTest = new StreamWriter(string.Format(@"G:\PromissMetrics\_cvHVSM\{0}\{1}_{2}_test.csv", metricType, metricFileName, f.ToString()));
                foreach (var m in metrics)
                {
                    if (test.Contains(m.Key)) swTest.WriteLine(m.Value);
                    else swTrain.WriteLine(m.Value);
                }
                swTrain.Close();
                swTest.Close();

                swTrain = new StreamWriter(string.Format(@"G:\PromissMetrics\_cvHVSM\{0}\{1}_{2}_train.csv", metricType, bugFileName, f.ToString()));
                swTest = new StreamWriter(string.Format(@"G:\PromissMetrics\_cvHVSM\{0}\{1}_{2}_test.csv", metricType, bugFileName, f.ToString()));
                foreach (var m in bugs)
                {
                    if (test.Contains(m.Key)) swTest.WriteLine(m.Value);
                    else swTrain.WriteLine(m.Value);
                }
                swTrain.Close();
                swTest.Close();
            }
        }
    }
}
