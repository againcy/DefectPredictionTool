using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DPTool_2
{
    /// <summary>
    /// ROC曲线上每个点的阈值和confusion matrix
    /// </summary>
    public class ROCPoint
    {
        public double threshold;
        //confusion matrix
        public int tp;
        public int fp;
        public int tn;
        public int fn;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="thres">阈值</param>
        public ROCPoint(double thres, int tp, int fp, int tn, int fn)
        {
            threshold = thres;
            this.tp = tp;
            this.fp = fp;
            this.tn = tn;
            this.fn = fn;
        }
    }

    /// <summary>
    /// alberg图的点
    /// </summary>
    public class AlbergPoint
    {
        public double x;
        public double y;
        public AlbergPoint(double x_value, double y_value)
        {
            x = x_value;
            y = y_value;
        }
    }

    public static class EvaluationTool
    {
        /// <summary>
        /// 读入包含测试score和原始label的数据
        /// </summary>
        /// <param name="file">文件名</param>
        /// <param name="conditionLabel">返回值 真实类标</param>
        /// <param name="testLabel">返回值 测试类标</param>
        public static void ReadBoth(
            string file,
            out Dictionary<int, int> conditionLabel,
            out Dictionary<int, double> testLabel)
        {
            /*
             * 文件格式
             * 表头
             * testlabel,conditionlabel
             */
            conditionLabel = new Dictionary<int, int>();
            testLabel = new Dictionary<int, double>();
            try
            {
                StreamReader sr = new StreamReader(file);
                string line = sr.ReadLine();//表头
                int cnt = 1;
                while ((line = sr.ReadLine()) != null)
                {
                    try
                    {
                        var arr = line.Split(',');
                        testLabel.Add(cnt, Convert.ToDouble(arr[0]));
                        string label = arr[arr.Length - 1];
                        var label_ = 0.0;
                        double.TryParse(label, out label_); 
                        if (label == "P" || label_ == 1) conditionLabel.Add(cnt, 1);
                        else conditionLabel.Add(cnt, 0);
                        cnt++;
                    }
                    catch { }
                }
                sr.Close();
            }
            catch
            {
                //添加一组随机数据
                conditionLabel.Add(1, 0);
                conditionLabel.Add(2, 1);
                testLabel.Add(1, 0.5);
                testLabel.Add(2, 0.5);
            }
        }

        /// <summary>
        /// 读取loc
        /// </summary>
        /// <param name="file">文件路径</param>
        /// <param name="locColName">loc所在列的列名</param>
        /// <param name="loc">返回值 包含模块序号和loc</param>
        public static void ReadLoc(
            string file,
            string locColName,
            out Dictionary<int, int> loc)
        {
            loc = new Dictionary<int, int>();
            StreamReader sr = new StreamReader(file);
            string line = sr.ReadLine();//表头
            //找到loc对应的列号
            var header = line.Split(',');
            int colLoc = 0;
            while (colLoc < header.Length && header[colLoc] != locColName) colLoc++;
            if (colLoc >= header.Length)
            {
                loc = null;
                return;
            }
            //读取
            int cnt = 1;
            while ((line = sr.ReadLine()) != null)
            {
                var tmp = line.Split(',');
                string vLoc = tmp[colLoc];
                loc.Add(cnt, Convert.ToInt32(vLoc));
                cnt++;
            }
            sr.Close();
        }

        /// <summary>
        /// 绘制Alberg图（bug%-module%）
        /// </summary>
        /// <param name="conditionLabel">真实类标</param>
        /// <param name="testLabel">测试类标</param>
        /// <param name="loc">各模块loc</param>
        /// <param name="albergOptimal">理想模型的alberg图</param>
        /// <param name="albergModel">当前模型的alberg图</param>
        private static void DrawAlberg(
            Dictionary<int, int> conditionLabel,
            Dictionary<int, double> testLabel,
            Dictionary<int, int> loc,
            out SortedDictionary<int, AlbergPoint> albergOptimal,
            out SortedDictionary<int, AlbergPoint> albergModel)
        {
            albergModel = new SortedDictionary<int, AlbergPoint>();
            albergOptimal = new SortedDictionary<int, AlbergPoint>();
            //optimal
            //计算bug密度
            Dictionary<int, double> bugDensity = new Dictionary<int, double>();
            int totalBugs = 0;
            int totalLoc = 0;
            foreach (var cnt in testLabel.Keys)
            {
                bugDensity.Add(cnt, (double)conditionLabel[cnt] / (double)loc[cnt]);//bug密度
                totalBugs += conditionLabel[cnt];
                totalLoc += loc[cnt];
            }
            //按bug密度从大到小排序
            var list = bugDensity.ToList();
            list.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));
            int graphID = 1;
            double x = 0;
            double y = 0;
            albergOptimal.Add(0, new AlbergPoint(0, 0));
            foreach (var pair in list)
            {
                int id = pair.Key;
                x += (double)loc[id] / (double)totalLoc;//模块（loc）百分比
                y += (double)conditionLabel[id] / (double)totalBugs;//bug百分比
                albergOptimal.Add(graphID, new AlbergPoint(x, y));
                graphID++;
            }

            //model
            //按testlabel从大到小排序（默认label越大越可能有bug）
            var modelScore = new Dictionary<int, double>();
            foreach (var cnt in testLabel.Keys)
            {
                modelScore.Add(cnt, testLabel[cnt] / loc[cnt]);
            }
            var list_model = modelScore.ToList();
            list_model.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));
            graphID = 1;
            x = 0;
            y = 0;
            albergModel.Add(0, new AlbergPoint(0, 0));
            foreach (var pair in list_model)
            {
                int id = pair.Key;
                x += (double)loc[id] / (double)totalLoc;//模块（loc）百分比
                y += (double)conditionLabel[id] / (double)totalBugs;//bug百分比
                albergModel.Add(graphID, new AlbergPoint(x, y));
                graphID++;
            }
        }

        /// <summary>
        /// 输出alberg图
        /// </summary>
        /// <param name="albergOptimal">理想模型的alberg图</param>
        /// <param name="albergModel">当前模型的alberg图</param>
        private static void OutputAlberg(
            SortedDictionary<int, AlbergPoint> albergOptimal,
            SortedDictionary<int, AlbergPoint> albergModel)
        {
            StreamWriter sw = new StreamWriter("Alberg_model.csv");
            for (int i = 0; i < albergModel.Count; i++)
            {
                sw.WriteLine(albergModel[i].x.ToString() + "," + albergModel[i].y.ToString());
            }
            sw.Close();
            sw = new StreamWriter("Alberg_optimal.csv");
            for (int i = 0; i < albergOptimal.Count; i++)
            {
                sw.WriteLine(albergOptimal[i].x.ToString() + "," + albergOptimal[i].y.ToString());
            }
            sw.Close();
        }

        /// <summary>
        /// 求得通过p1和p2两点的直线上横坐标为x3的点的纵坐标Y
        /// </summary>
        /// <param name="p1">p1点坐标</param>
        /// <param name="p2">p2点坐标</param>
        /// <param name="x3">p3点横坐标</param>
        /// <returns>p3点纵坐标</returns>
        private static double GetYonLine(AlbergPoint p1, AlbergPoint p2, double x3)
        {
            //构建alberg图的过程中保证了p1.x!=p2.x
            //if (p1.x==p2.x) return ;
            double k = (p2.y - p1.y) / (p2.x - p1.x);
            double b = p2.y - k * p2.x;
            return k * x3 + b;
        }

        /// <summary>
        /// 计算CE
        /// </summary>
        /// <param name="conditionLabel">真实类标</param>
        /// <param name="testLabel">测试类标</param>
        /// <param name="loc">各模块loc</param>
        /// <param name="ACC">检查20%的代码可以查出的bug百分比</param>
        /// <param name="percentage">选取前百分之多少的模块</param>
        /// <returns>cost-effective值</returns>
        public static double CE(
            Dictionary<int, int> conditionLabel,
            Dictionary<int, double> testLabel,
            Dictionary<int, int> loc,
            out double ACC,
            double percentage = 1.0)
        {
            SortedDictionary<int, AlbergPoint> albergOptimal;
            SortedDictionary<int, AlbergPoint> albergModel;
            DrawAlberg(conditionLabel, testLabel, loc, out albergOptimal, out albergModel);
            //OutputAlberg();
            //面积采用梯形计算
            double areaModel = 0;
            ACC = 0;
            for (int i = 1; i < albergModel.Count; i++)
            {
                var point1 = albergModel[i - 1];
                var point2 = albergModel[i];
                if (point1.x < 0.2 && point2.x > 0.2)
                {
                    ACC = GetYonLine(point1, point2, 0.2);
                }
                if (point2.x > percentage)
                {
                    AlbergPoint pointEnd = new AlbergPoint(percentage, GetYonLine(point1, point2, percentage));
                    areaModel += (point1.y + pointEnd.y) * (pointEnd.x - point1.x) / 2;
                    break;
                }
                areaModel += (point1.y + point2.y) * (point2.x - point1.x) / 2;
            }
            double areaOptimal = 0;
            for (int i = 1; i < albergOptimal.Count; i++)
            {
                var point1 = albergOptimal[i - 1];
                var point2 = albergOptimal[i];
                if (point2.x > percentage)
                {
                    AlbergPoint pointEnd = new AlbergPoint(percentage, GetYonLine(point1, point2, percentage));
                    areaOptimal += (point1.y + pointEnd.y) * (pointEnd.x - point1.x) / 2;
                    break;
                }
                areaOptimal += (point1.y + point2.y) * (point2.x - point1.x) / 2;
            }
            double areaRandom = percentage * percentage / 2;
            return ((areaModel - areaRandom) / (areaOptimal - areaRandom));
        }

        /// <summary>
        /// 计算auc
        /// </summary>
        /// <param name="conditionLabel">真实类标</param>
        /// <param name="testLabel">测试类标</param>
        /// <returns>auc值</returns>
        public static double AUC(
            Dictionary<int, int> conditionLabel,
            Dictionary<int, double> testLabel)
        {
            //统计一下所有的 M×N(M为正类样本的数目，N为负类样本的数目)个正负样本对中，有多少个组中的正样本的score大于负样本的score
            //http://taoo.iteye.com/blog/760589
            //假设score越大越可能为正样本
            Dictionary<int, double> PositiveSample = new Dictionary<int, double>();
            Dictionary<int, double> NegativeSample = new Dictionary<int, double>();
            foreach (var sample in testLabel.Keys)
            {
                if (conditionLabel[sample] == 1) PositiveSample.Add(sample, testLabel[sample]);
                else NegativeSample.Add(sample, testLabel[sample]);
            }
            double pln = 0;//positive larger than negative
            foreach (var p in PositiveSample)
            {
                foreach (var n in NegativeSample)
                {
                    if (p.Value > n.Value) pln += 1;
                    else if (p.Value == n.Value) pln += 0.5;
                }
            }
            return pln / (PositiveSample.Count * NegativeSample.Count);
        }

        /// <summary>
        /// 生成ROC曲线上所有的点
        /// </summary>
        /// <param name="conditionLabel">真实类标</param>
        /// <param name="testLabel">测试类标</param>
        /// <param name="roc">包含roc图上所有点的信息的列表</param>
        private static void GenerateROC(
            Dictionary<int, int> conditionLabel,
            Dictionary<int, double> testLabel,
            out List<ROCPoint> roc)
        {
            //枚举所有的阈值
            List<double> candidate = new List<double>();
            foreach (var score in testLabel.Values)
            {
                if (candidate.Contains(score) == false) candidate.Add(score);
            }
            roc = new List<ROCPoint>();
            //对每个阈值求对应的tpr和fpr
            /*
                       test p   test n
           condition p   tp       fn
           condition n   fp       tn
            */
            foreach (var threshold in candidate)
            {
                //计算confusion matrix
                int tp = 0;//true positive
                int fp = 0;//false positive
                int tn = 0;//true negative
                int fn = 0;//false negative
                foreach (var id in testLabel.Keys)
                {
                    if (conditionLabel[id] == 1 && testLabel[id] >= threshold) tp++;
                    else if (conditionLabel[id] == 1 && testLabel[id] < threshold) fn++;
                    else if (conditionLabel[id] == 0 && testLabel[id] >= threshold) fp++;
                    else if (conditionLabel[id] == 0 && testLabel[id] < threshold) tn++;
                }
                roc.Add(new ROCPoint(threshold, tp, fp, tn, fn));
            }
        }

        /// <summary>
        /// 求在ROC曲线上离(0,1)点最近的点所用的阈值
        /// 作为f-measure的阈值
        /// </summary>
        /// <param name="roc">roc曲线</param>
        /// <param name="threshold_fmeasure">阈值</param>
        private static void ChooseThresholdForF1(List<ROCPoint> roc, out double threshold_fmeasure)
        {
            //找到ROC曲线上离(0,1)点最近的点所用的阈值
            double min = 2;
            threshold_fmeasure = 0;
            foreach (var point in roc)
            {
                //roc曲线上每个点坐标为(fpr,tpr)
                //求距离(0,1)点最近的点的位置
                double tpr = (double)point.tp / (double)(point.tp + point.fn);
                double fpr = (double)point.fp / (double)(point.fp + point.tn);
                var dist = Math.Sqrt((Math.Pow(0 - fpr, 2) + Math.Pow(1 - tpr, 2)) / 2);
                if (dist < min)
                {
                    min = dist;
                    threshold_fmeasure = point.threshold;
                }
            }
        }

        /// <summary>
        /// 采用bpp算法计算F1，使用roc曲线上距离(0,1)点最近的一个点的阈值，并计算f1
        /// </summary>
        /// <param name="conditionLabel">真实类标</param>
        /// <param name="testLabel">测试类标</param>
        /// <param name="bpp">是否采用bpp算法</param>
        /// <returns></returns>
        public static double F1measure(
            Dictionary<int, int> conditionLabel,
            Dictionary<int, double> testLabel,
            bool bpp = true)
        {
            if (testLabel.Count == 2) return 0;
            if (bpp == true)
            {
                List<ROCPoint> roc;
                GenerateROC(conditionLabel, testLabel, out roc);
                double threshold;
                ChooseThresholdForF1(roc, out threshold);
                foreach (var point in roc)
                {
                    if (point.threshold == threshold)
                    {
                        double recall = (double)point.tp / (double)(point.tp + point.fn);
                        double precision = (double)point.tp / (double)(point.tp + point.fp);
                        return 2 * recall * precision / (recall + precision);
                    }
                }
                return 0;
            }
            else
            {
                double threshold = 0.5;
                //计算confusion matrix
                int tp = 0;//true positive
                int fp = 0;//false positive
                int tn = 0;//true negative
                int fn = 0;//false negative
                foreach (var id in testLabel.Keys)
                {
                    if (conditionLabel[id] == 1 && testLabel[id] >= threshold) tp++;
                    else if (conditionLabel[id] == 1 && testLabel[id] < threshold) fn++;
                    else if (conditionLabel[id] == 0 && testLabel[id] >= threshold) fp++;
                    else if (conditionLabel[id] == 0 && testLabel[id] < threshold) tn++;
                }
                if (tp == 0) return 0;
                if (tp + fp == 0)
                {
                    double max = 0;
                    double min = 100000;
                    foreach (var id in testLabel.Keys)
                    {
                        if (testLabel[id] > max) max = testLabel[id];
                        if (testLabel[id] < min) min = testLabel[id];
                    }
                    threshold = (max + min) / 2;
                    tp = 0;//true positive
                    fp = 0;//false positive
                    tn = 0;//true negative
                    fn = 0;//false negative
                    foreach (var id in testLabel.Keys)
                    {
                        if (conditionLabel[id] == 1 && testLabel[id] >= threshold) tp++;
                        else if (conditionLabel[id] == 1 && testLabel[id] < threshold) fn++;
                        else if (conditionLabel[id] == 0 && testLabel[id] >= threshold) fp++;
                        else if (conditionLabel[id] == 0 && testLabel[id] < threshold) tn++;
                    }
                }
                double recall = (double)tp / (double)(tp + fn);
                double precision = (double)tp / (double)(tp + fp);
                return 2 * recall * precision / (recall + precision);
            }
        }

    }
}
