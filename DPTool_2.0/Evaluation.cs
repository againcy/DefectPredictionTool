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
        private string Rdir;
        
        //private Dictionary<string, int> moduleCnt;//存储每个数据集应有的模块数

        public Evaluation(string Rdir)
        {
            this.Rdir = Rdir;
        }

        public void DoWork(
            Dictionary<string, int> methodList,
            List<EvaluationMode> modeList
            )
        {
            //result[method][data][evaluation mode] = <repeatID,value>
            var result = new Dictionary<string, Dictionary<string, Dictionary<EvaluationMode, Dictionary<int, double>>>>();

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
                result.Add(method, new Dictionary<string, Dictionary<EvaluationMode, Dictionary<int, double>>>());
                foreach (var target in targetList)
                {
                    var arrTarget = target.Split(',');
                    
                    var targetName = target.Replace(',', '_');//_cm,ant,1.7.0,1.7.1
                    var targetLocPath = string.Format(@"{0}\_cm\{1}\{2}.csv", Rdir, arrTarget[1], arrTarget[3]);//G:\R\TraditionalML\code_and_process\_cm\ant
                    /*
                    var targetName = string.Format("{0}_{1}_{2}", arrTarget[0], arrTarget[1], arrTarget[2]);//camel_2.0.0_2.4.0,camel,2.6.0,2.0.0
                    var targetLocPath = string.Format(@"{0}\_test\{1}\{2}.csv", Rdir, arrTarget[1], arrTarget[2]);//G:\R\TraditionalML\mixed_mixed\_test\ant
                    */
                    result[method].Add(targetName, new Dictionary<EvaluationMode, Dictionary<int, double>>());
                    var metricValue = new Dictionary<EvaluationMode, Dictionary<int, double>>();
                    ProcessMethod(method, targetName, targetLocPath, methodList[method], modeList, 5, out metricValue,0.2);
                    foreach (var mv in metricValue.Keys) result[method][targetName].Add(mv, metricValue[mv]);
                }
            }
            Console.WriteLine("Now showing results...");
            ShowResult(result, methodList,Rdir);
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

            //随机值重复不足10次的补足10次
            foreach (var mode in modeList)
            {
                if (metricValue[mode].Count < 10)
                {
                    int nrepeat = metricValue[mode].Count;
                    int curPos = metricValue[mode].Count+1;
                    for (int i = 0; i < 10 / nrepeat - 1; i++)
                    {
                        for (int j = 1; j <= nrepeat; j++)
                        {
                            metricValue[mode].Add(curPos, metricValue[mode][j]);
                            curPos++;
                        }
                    }

                }
            }
        }

        private void ShowResult(
            Dictionary<string, Dictionary<string, Dictionary<EvaluationMode, Dictionary<int, double>>>> result,
            Dictionary<string, int> methodList,
            string path
            )
        {
            //result[method][data][evaluation mode] = <repeatID,value>
            var output = new Dictionary<EvaluationMode, Dictionary<string, Dictionary<string, double>>>();//result[mode][target][method,value]
            foreach(var method in methodList.Keys)
            {
                foreach(var target in result[method].Keys)
                {
                    foreach (var mode in result[method][target].Keys)
                    {
                        var value = result[method][target][mode].Values.Average();
                        if (output.ContainsKey(mode) == false) output.Add(mode, new Dictionary<string, Dictionary<string, double>>());
                        if (output[mode].ContainsKey(target) == false) output[mode].Add(target, new Dictionary<string, double>());
                        output[mode][target].Add(method, value);
                    }
                }
            }
            foreach(var mode in output.Keys)
            {
                StreamWriter sw = null;
                switch (mode)
                {
                    case EvaluationMode.AUC:sw = new StreamWriter(path + @"\AUC.csv");break;
                    case EvaluationMode.F1: sw = new StreamWriter(path + @"\F1.csv"); break;
                    case EvaluationMode.CE: sw = new StreamWriter(path + @"\CE.csv"); break;
                };
                //表头
                sw.Write("Target");
                foreach(var method in methodList.Keys)
                {
                    sw.Write("," + method);
                }
                sw.WriteLine();
                //主体
                foreach (var target in output[mode].Keys)
                {
                    sw.Write(target);
                    foreach (var method in methodList.Keys)
                    {
                        sw.Write("," + output[mode][target][method].ToString("0.000"));
                    }
                    sw.WriteLine();
                }                
                sw.Close();
            }
        }

        private void ShowResult_NoAvg(
            Dictionary<string, Dictionary<string, Dictionary<EvaluationMode, Dictionary<int, double>>>> result,
            Dictionary<string, int> methodList,
            string path
            )
        {
            //result[method][data][evaluation mode] = <repeatID,value>
            var output = new Dictionary<EvaluationMode, Dictionary<string, Dictionary<string, List<double>>>>();//output[mode][target][method,value]
            foreach (var method in methodList.Keys)
            {
                foreach (var target in result[method].Keys)
                {
                    foreach (var mode in result[method][target].Keys)
                    {
                        var value = result[method][target][mode].Values.Average();
                        if (output.ContainsKey(mode) == false) output.Add(mode, new Dictionary<string, Dictionary<string, List<double>>>());
                        if (output[mode].ContainsKey(target) == false) output[mode].Add(target, new Dictionary<string, List<double>>());
                        output[mode][target].Add(method, new List<double>());
                        foreach (var v in result[method][target][mode].Values) output[mode][target][method].Add(v);
                    }
                }
            }
            foreach (var mode in output.Keys)
            {
                
                var dir = "";
                switch (mode)
                {
                    case EvaluationMode.AUC: dir = path + @"\_forTest\AUC"; break;
                    case EvaluationMode.F1: dir = path + @"\_forTest\F1"; break;
                    case EvaluationMode.CE: dir = path + @"\_forTest\CE"; break;
                };
                if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);
                foreach(var target in output[mode].Keys)
                {
                    var sw = new StreamWriter(string.Format(@"{0}\{1}.csv", dir, target));
                    var outputList = new Dictionary<int, string>();
                    for (int i = 0; i <= 10; i++) outputList.Add(i, "");
                    foreach (var method in methodList.Keys)
                    {
                        //output[mode][target][method, value]
                        outputList[0] += "," + method;
                        int cntLine = 1;
                        foreach (var v in output[mode][target][method])
                        {
                            outputList[cntLine] += "," + v.ToString();
                            cntLine++;
                        }
                    }
                    foreach(var line in outputList.Values)
                    {
                        sw.WriteLine(line.Substring(line.IndexOf(',') + 1));
                    }
                    sw.Close();
                }
               
            }
        }
    }
}
