using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.ComponentModel;
using System.Data;

namespace DPTool_2
{
    /// <summary>
    /// 评估模式
    /// 包含auc, f1, ce
    /// </summary>
    public enum EvaluationMode { AUC, F1, CE, ACC };
    

    /// <summary>
    /// 根据分类器的结果，分析效果
    /// </summary>
    public class Evaluation
    {
        public struct Result
        {
            public string method;
            public string target;
            public EvaluationMode mode;
            public Dictionary<int, double> score;
            public Result(string _method,string _target,EvaluationMode _mode,Dictionary<int,double> _score)
            {
                method = _method;
                target = _target;
                mode = _mode;
                score = _score;
            }
        }
        private string Rdir;

        public Evaluation(string Rdir)
        {
            this.Rdir = Rdir;
        }

        /// <summary>
        /// 计算每个预测结果的performance
        /// </summary>
        /// <param name="methodList">方法列表</param>
        /// <param name="modeList">评估指标列表</param>
        public void DoWork(
            Dictionary<string, int> methodList,
            List<EvaluationMode> modeList
            )
        {
            var result = new List<Result>();

            //读入目标文件列表
            var targetList = new List<string>();
            StreamReader sr = new StreamReader(Rdir + @"\targetList.txt");
            string line;
            while ((line = sr.ReadLine())!=null)
            {
                targetList.Add(line);
            }
            sr.Close();
            //对每个方法，每个数据集，做所有需要做的度量评估
            foreach(var method in methodList.Keys)
            {
                //result.Add(method, new Dictionary<string, Dictionary<EvaluationMode, Dictionary<int, double>>>());
                foreach (var target in targetList)
                {
                    var arrTarget = target.Split(',');
                    
                    var targetName = target.Replace(',', '_');//_cm,ant,1.7.0,1.7.1
                    var targetLocPath = string.Format(@"{0}\_cm\{1}\{2}.csv", Rdir, arrTarget[1], arrTarget[3]);//G:\R\TraditionalML\code_and_process\_cm\ant
                    /*
                    var targetName = target.Replace(",", "_");//ant,1.6_1
                    var targetLocPath = string.Format(@"{0}\_code\{1}_test.csv", Rdir, targetName);
                    */
                    /*
                    var targetName = string.Format("{0}_{1}_{2}", arrTarget[0], arrTarget[1], arrTarget[2]);//camel_2.0.0_2.4.0,camel,2.6.0,2.0.0
                    var targetLocPath = string.Format(@"{0}\_test\{1}\{2}.csv", Rdir, arrTarget[1], arrTarget[2]);//G:\R\TraditionalML\mixed_mixed\_test\ant
                    */
                    var metricValue = new Dictionary<EvaluationMode, Dictionary<int, double>>();
                    ProcessMethod(method, targetName, targetLocPath, methodList[method], modeList, 5, out metricValue,0.2);
                    foreach (var mv in metricValue.Keys)
                    {
                        var newitem = new Result(method,targetName,mv,metricValue[mv]);
                        result.Add(newitem);
                    }
                }
            }
            Console.WriteLine("Now showing results...");
            ShowResult(result, Rdir);
            ShowResult_NoAvg(result, Rdir);
        }

        /// <summary>
        /// 处理一个方法的数据
        /// </summary>
        /// <param name="methodName">方法名称</param>
        /// <param name="targetName">要计算的目标文件名</param>
        /// <param name="targetLocPath">目标的包含loc的文件名</param>
        /// <param name="methodType">1:随机方法 0:非随机方法</param>
        /// <param name="modeList">评估方法</param>
        /// <param name="repeatTime">随机次数</param>
        /// <param name="metricValue">评估度量的值</param>
        /// <param name="ceP">ce的百分比</param>
        public void ProcessMethod(
            string methodName,
            string targetName,
            string targetLocPath,
            int methodType,
            IEnumerable<EvaluationMode> modeList,
            int repeatTime,
            out Dictionary<EvaluationMode, Dictionary<int, double>> metricValue,//metricValue[mode][repeatID]=value
            double ceP = 0.2
            )
        {
            //初始化
            var conditionLabel = new Dictionary<int, int>();
            var testLabel = new Dictionary<int, double>();
            var loc = new Dictionary<int, int>();
            metricValue = new Dictionary<EvaluationMode, Dictionary<int, double>>();
            foreach (var mode in modeList) metricValue.Add(mode, new Dictionary<int,double>());

            //对于每个数据集的每一次随机结果进行处理（无随机的方法只处理第一次）
            for (int repeatID = 1; repeatID <= repeatTime; repeatID++)
            {
                string fileName;
                if (methodType == 1) fileName = string.Format(@"{0}\{1}\{2}_{3}.csv", Rdir, methodName, targetName, repeatID.ToString());
                else fileName = string.Format(@"{0}\{1}\{2}.csv", Rdir, methodName, targetName);
                //读取conditionlabel 和 testlabel
                EvaluationTool.ReadBoth(fileName, out conditionLabel, out testLabel);
                
                //计算评估结果
                foreach (var mode in modeList)
                {
                    switch (mode)
                    {
                        case EvaluationMode.AUC:
                            metricValue[mode].Add(repeatID, EvaluationTool.AUC(conditionLabel, testLabel));
                            break;
                        case EvaluationMode.F1:
                            metricValue[mode].Add(repeatID, EvaluationTool.F1measure(conditionLabel, testLabel));
                            break;
                        case EvaluationMode.CE:
                            var locFile = string.Format(@"{0}\{1}\{2}.csv", Rdir, methodName, targetName);
                            EvaluationTool.ReadLoc(targetLocPath, "loc", out loc);
                            double acc;
                            metricValue[mode].Add(repeatID, EvaluationTool.CE(conditionLabel, testLabel, loc, out acc, ceP));
                            break;
                            /*
                        case EvaluationMode.ACC:
                            CE(conditionLabel, testLabel, loc, out acc); 
                            metricValue[mode].Add(repeatID, acc);
                            break;
                            */
                    };
                }
                if (methodType != 1) break;
            }
        }

        /// <summary>
        /// 输出结果
        /// </summary>
        /// <param name="result">结果</param>
        /// <param name="dir">输出目录</param>
        private void ShowResult(
            IEnumerable<Result> result,
            string dir
            )
        {
            /*
            AUC.csv   (mode.csv)
            seperator:  ,
            line 1: Target      method_1    method_2    ...
            line 2: target_1    0.800       0.900       ...
            line 3: target_2    0.850       0.950       ...
            */
            var methodList = (from item in result
                              select item.method).Distinct();
            var modeList = (from item in result
                            select item.mode).Distinct();
            var targetList = (from item in result
                              select item.target).Distinct();
            foreach (var mode in modeList)
            {
                StreamWriter sw = null;
                switch (mode)
                {
                    case EvaluationMode.AUC: sw = new StreamWriter(dir + @"\AUC.csv"); break;
                    case EvaluationMode.F1: sw = new StreamWriter(dir + @"\F1.csv"); break;
                    case EvaluationMode.CE: sw = new StreamWriter(dir + @"\CE.csv"); break;
                };
                //表头
                sw.Write("Target");
                foreach (var method in methodList) sw.Write("," + method);
                sw.WriteLine();
                //主体
                foreach(var target in targetList)
                {
                    sw.Write(target);
                    foreach(var method in methodList)
                    {
                        var v = from item in result
                                where (item.mode == mode && item.target == target && item.method == method)
                                select item.score.Values.Average();
                        if (v.Count() > 1) sw.Write(",error");
                        else sw.Write("," + v.First().ToString("0.000"));
                    }
                    sw.WriteLine();
                }                    
                sw.Close();
            }
        }

        /// <summary>
        /// 输出结果（对每个方法的结果不取平均值而是全部输出）
        /// </summary>
        /// <param name="result">结果</param>
        /// <param name="dir">输出路径</param>
        /// <param name="repeatTime">每个方法随机重复的次数上限</param>
        private void ShowResult_NoAvg(
            IEnumerable<Result> result,
            string dir,
            int repeatTime = 10
            )
        {
            /*
            AUC\target_1.csv
            seperator:  ,
            line 1: method_1    method_2    method_3    ...
            line 2: 0.900       0.800       0.800       ...
            line 3: 0.890       0.810       0.820       ...
            */
            var methodList = (from item in result
                              select item.method).Distinct();
            var modeList = (from item in result
                            select item.mode).Distinct();
            var targetList = (from item in result
                              select item.target).Distinct();
            foreach (var mode in modeList)
            {
                var subDir = "";
                switch (mode)
                {
                    case EvaluationMode.AUC: subDir = dir + @"\_forTest\AUC"; break;
                    case EvaluationMode.F1: subDir = dir + @"\_forTest\F1"; break;
                    case EvaluationMode.CE: subDir = dir + @"\_forTest\CE"; break;
                };
                if (Directory.Exists(subDir) == false) Directory.CreateDirectory(subDir);
                foreach (var target in targetList)
                {
                    var sw = new StreamWriter(string.Format(@"{0}\{1}.csv", subDir, target));
                    var outputLines = new Dictionary<int, string>();
                    for (int i = 0; i <= repeatTime; i++) outputLines.Add(i, "");
                    foreach (var method in methodList)
                    {
                        outputLines[0] += "," + method;
                        var v = from item in result
                                where (item.mode == mode && item.target == target && item.method == method)
                                select item.score.Values;
                        var arr = v.First().ToArray();
                        //不足repeatTime次数的补足repeatTime
                        for (int i = 1; i <= repeatTime; i++) outputLines[i] += "," + arr[(i - 1) % arr.Length].ToString("0.000");
                    }
                    foreach (var line in outputLines.Values)
                    {
                        sw.WriteLine(line.Substring(line.IndexOf(',') + 1));
                    }
                    sw.Close();
                }

            }
        }
    }
}
