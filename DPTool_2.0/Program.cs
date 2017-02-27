using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Configuration;

namespace DPTool_2
{
    class Program
    {
        public static string rootDir;
        public static DateTime zeroDate;

        static void Initialize(out List<string>projectList, out Dictionary<string, int> methodList)
        {
            
            projectList = new List<string>();
            /*
            projectList.Add("ant,2005-6-3,2016-4-11,java");
            projectList.Add("camel,2009-5-14,2014-9-14,java");
            projectList.Add("derby,2007-12-11,2016-10-26,java");
            projectList.Add("hive,2010-2-23,2016-6-21,java");
            projectList.Add("jmeter,2011-11-2,2017-1-1,java");
            projectList.Add("log4j,2000-1-1,2014-7-13,java");
            projectList.Add("poi,2007-1-1,2017-1-1,java");
            projectList.Add("xerces-c,1999-11-9,2017-1-1,c");
            //projectList.Add("qpid,2009-1-28,2012-8-31,java");
            */
            projectList.Add("ant");
            projectList.Add("camel");
            //projectList.Add("ivy");
            projectList.Add("jedit");
            projectList.Add("log4j");
            projectList.Add("lucene");
            projectList.Add("poi");
            projectList.Add("velocity");
            projectList.Add("xalan");
            projectList.Add("xerces");
            

            methodList = new Dictionary<string, int>();
            methodList.Add("nb", 0);
            methodList.Add("regression", 0);
            methodList.Add("rf", 1);
            methodList.Add("J48", 0);
            //methodList.Add("rnn1", 1);
            //methodList.Add("rnn2", 1);
            //methodList.Add("rnn3", 1);
            methodList.Add("svm", 0);
            methodList.Add("knn", 0);
            methodList.Add("nnet", 1);
            methodList.Add("C5.0", 0);
        }

        /// <summary>
        /// 发布一个询问
        /// </summary>
        /// <param name="retNo">true: 返回选项编号; false: 返回选项字符串</param>
        /// <param name="msg">询问主题</param>
        /// <param name="options">选项</param>
        /// <returns></returns>
        static string Request(bool retNo,string msg,params string[] options)
        {
            //发布
            Console.WriteLine(msg);
            var no = 0;
            foreach(var op in options)
            {
                no++;
                Console.WriteLine("{0}: {1}", no.ToString(), op);
            }
            Console.WriteLine("0: 结束");
            //接收 直到回答合法
            string ret="";
            int ans = -1;
            while (ans == -1)
            {
                var ansStr = Console.ReadLine();
                if (int.TryParse(ansStr, out ans) == true)
                {
                    if (ans <= no)
                    {
                        //成功
                        if (retNo == true) ret = ans.ToString();
                        else if (ans != 0) ret = options[ans-1];
                        else ret = "0";
                        break;
                    }
                }
                Console.WriteLine("指令无效...");
                ans = -1;
            }
            return ret;
        }

        static void Main(string[] args)
        {
            zeroDate = Convert.ToDateTime("1999-1-1");
            rootDir = @"G:\PromissMetrics";
            //读入项目列表
            var projectList = new List<string>();
            var methodList = new Dictionary<string, int>();
            Initialize(out projectList,out methodList);

            string ans = "";
            while (ans != "0")
            {
                ans = Request(true,"输入需要执行的操作",
                    "执行GitLogAnalyzer",
                    "执行ProjectAnalyzer",
                    "分析各项目",
                    "获取HVSM",
                    "获取混合版本",
                    "评估分类方法的结果",
                    "处理RNN数据",
                    "执行CodeDiff",
                    "生成cv");
                Answer(projectList, methodList, ans);
                Console.WriteLine("完成...");
            }
            Console.WriteLine("End...");
        }

        /// <summary>
        /// 执行操作
        /// </summary>
        /// <param name="projectList"></param>
        /// <param name="methodList"></param>
        static void Answer(List<string> projectList, Dictionary<string, int> methodList,string answer)
        {
            switch (answer)
            {
                case "1":
                    foreach (var project in projectList)
                    {
                        //对各项目从git中获取信息
                        var projectName = project.Split(',')[0];
                        var startDate = project.Split(',')[1];
                        var endDate = project.Split(',')[2];
                        var language = project.Split(',')[3];
                        Run_GitLogAnalyzer(projectName, Convert.ToDateTime(startDate), Convert.ToDateTime(endDate), language);
                    }
                    break;
                case "2":
                    foreach (var project in projectList)
                    {
                        //对各项目进行度量抽取
                        var projectName = project.Split(',')[0];
                        var startDate = project.Split(',')[1];
                        var endDate = project.Split(',')[2];
                        var language = project.Split(',')[3];
                        Run_ProjectAnalyzer(projectName);
                    }
                    break;
                case "3":
                    Run_Analysis();
                    break;
                case "4":
                    GetHVSMs();
                    break;
                case "5":
                    GetMixedRel();
                    break;
                case "6":
                    //评估结果 
                    var modeList = new List<EvaluationMode>() { EvaluationMode.AUC, EvaluationMode.CE };
                    Run_Evaluation(methodList, modeList);
                    break;
                case "7":
                    Run_RNNHandler();
                    break;
                case "8":
                    Run_CodeDiff(projectList);
                    break;
                case "9":
                    Run_CreateCV();
                    break;
            };
        }

        /// <summary>
        /// 获取git可得的相关信息
        /// </summary>
        /// <param name="projectName"></param>
        /// <param name="startDate"></param>
        static void Run_GitLogAnalyzer(string projectName, DateTime startDate, DateTime endDate, string language)
        {
            StreamWriter sw;
            //buggy interval
            Console.WriteLine("Processing buggy intervals of [{0}]...", projectName);
            var logPath = string.Format(@"{0}\{1}_releases\git_log.csv", rootDir, projectName); //@"G:\GitRepos\ant_releases\git_log.csv";
            var repoPath = string.Format(@"{0}\{1}\", rootDir, projectName);//@"G:\GitRepos\ant\

            var intervals = GitLogAnalyzer.GetBuggyIntervals(projectName, logPath, repoPath, "#SEP#", endDate, language);
            var outPath = string.Format(@"{0}\{1}_releases\BuggyIntervals.csv", rootDir, projectName); // @"G:\GitRepos\ant_releases\BuggyIntervals.csv"
            sw = new StreamWriter(outPath);
            foreach (var line in intervals) sw.WriteLine(line);
            sw.Close();

            //author
            Console.WriteLine("Processing author infos of [{0}]...", projectName);
            var authors = GitLogAnalyzer.GetAuthorExp(logPath, "#SEP#", startDate, endDate, repoPath, language);
            var authorDir = string.Format(@"{0}\{1}_releases\authors", rootDir, projectName);//@"G:\GitRepos\ant_releases\authors\
            if (Directory.Exists(authorDir) == false) Directory.CreateDirectory(authorDir);
            foreach (var author in authors)
            {
                sw = new StreamWriter(string.Format(@"{0}\{1}.txt", authorDir, author.name));//@"G:\GitRepos\ant_releases\authors\xxx.txt
                sw.Write(author.ExportToString());
                sw.Close();
            }
        }

        /// <summary>
        /// 获取项目度量
        /// </summary>
        /// <param name="projectName"></param>
        static void Run_ProjectAnalyzer(string projectName)
        {
            Project p = new Project(projectName);
            //p.ImportMetrics();

            p.GetCodeMetrics();
            p.ExportCodeMetrics();
            p.GetProcessMetrics();
            p.ExportProcessMetrics();
            p.ExportMixedMetrics();

        }

        /// <summary>
        /// 分析数据集
        /// </summary>
        static void Run_Analysis()
        {
            StreamReader sr = new StreamReader(string.Format(@"{0}\HVSM.txt", rootDir));
            StreamWriter sw = new StreamWriter(string.Format(@"{0}\Analysis.csv", rootDir));

            sw.WriteLine("Project,Start,End,{0},{1}", Analysis.ModuleType_Header(), Analysis.Bugs_Header());
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                Console.WriteLine("Now analyzing [{0}]...", line);
                var tmp = line.Split(',');
                var projectName = tmp[0];
                Project p = new Project(projectName);
                p.ImportMetrics();
                var startRel = tmp[1];
                var endRel = tmp[2];
                var moduleType = Analysis.ModuleType(p, startRel, endRel);
                var bugs = Analysis.Bugs(p, startRel, endRel);
                sw.WriteLine("{0},{1},{2},{3},{4}", p.projectName, startRel, endRel, moduleType, bugs);
            }
            sr.Close();
            sw.Close();
        }

        /// <summary>
        /// 制作HVSM
        /// </summary>
        static void GetHVSMs()
        {
            //询问
            string mode = Request(false, "输入需要采用的度量类型", "mixed", "code", "process");
            StreamReader sr = new StreamReader(string.Format(@"{0}\HVSM.txt", rootDir));
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                var tmp = line.Split(',');
                var projectName = tmp[0];
                Project p = new Project(projectName);
                p.ImportMetrics();
                var startRel = tmp[1];
                var endRel = tmp[2];
                var HVSM_metrics = new List<string>();
                var HVSM_bugs = new List<string>();
                Preprocess.GetHVSM(mode, p, startRel, endRel, out HVSM_metrics, out HVSM_bugs);

                var dirPath = string.Format(@"{0}\_HVSM_{1}", rootDir, mode);
                if (Directory.Exists(dirPath) == false) Directory.CreateDirectory(dirPath);
                StreamWriter sw;
                sw = new StreamWriter(string.Format(@"{0}\{1}_{2}_{3}_metrics.csv", dirPath, projectName, startRel, endRel));
                foreach (var x in HVSM_metrics) sw.WriteLine(x);
                sw.Close();
                sw = new StreamWriter(string.Format(@"{0}\{1}_{2}_{3}_bugs.csv", dirPath, projectName, startRel, endRel));
                foreach (var x in HVSM_bugs) sw.WriteLine(x);
                sw.Close();
            }
            sr.Close();
        }

        /// <summary>
        /// 获取混合版本
        /// </summary>
        static void GetMixedRel()
        {
            string mode = Request(false, "输入需要采用的度量类型", "mixed", "code", "process");
            StreamReader sr = new StreamReader(string.Format(@"{0}\HVSM.txt", rootDir));
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                var tmp = line.Split(',');
                var projectName = tmp[0];
                Project p = new Project(projectName);
                p.ImportMetrics();
                var startRel = tmp[1];
                var endRel = tmp[2];
                var mixedModules = new List<string>();
                var header = "";
                Preprocess.GetMixedRel(mode, p, startRel, endRel,out header, out mixedModules);

                StreamWriter sw;
                var dirPath = string.Format(@"{0}\_mixedRel", rootDir);
                if (Directory.Exists(dirPath) == false) Directory.CreateDirectory(dirPath);
                sw = new StreamWriter(string.Format(@"{0}\{1}_{2}_{3}.csv", dirPath, projectName, startRel, endRel));
                sw.WriteLine(header);
                foreach (var x in mixedModules) sw.WriteLine(x);
                sw.Close();
                
            }
            sr.Close();
        }

        /// <summary>
        /// 评估结果
        /// </summary>
        /// <param name="methodList"></param>
        /// <param name="modeList"></param>
        static void Run_Evaluation(Dictionary<string, int> methodList, IEnumerable<EvaluationMode> modeList)
        {
            Console.WriteLine(System.DateTime.Now.ToLongTimeString());
            Evaluation ev = new Evaluation(@"G:\R\TraditionalML\promise_codechurn");
            ev.DoWork(methodList, modeList.ToList());
            Console.WriteLine(System.DateTime.Now.ToLongTimeString());
        }

        /// <summary>
        /// 处理RNN数据
        /// </summary>
        static void Run_RNNHandler()
        {
            RNNHandler.DoWork(@"G:\R\TraditionalML\mixed_mixed");
        }

        /// <summary>
        /// 寻找相同模块
        /// </summary>
        static void Run_CodeDiff(List<string> projectList)
        {
            
            /*
            var projectList = new List<string>();
            StreamReader sr = new StreamReader(@"G:\OpenSourceCode\projectList.txt");
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                projectList.Add(line);
                
            }
            sr.Close();
            */

            foreach(var p in projectList)
            {
                //var p = "ivy";
                //CodeDiff.FindSameFiles(line);
                CodeDiff.GenerateCodeChurn(p);
                Project project = new Project(p);
                project.ImportMetrics();
                project.ExportMixedMetrics();
                //project.ExportCodeMetrics();
            }
            
        }

        static void Run_CreateCV()
        {
            
            StreamReader sr = new StreamReader(@"G:\PromissMetrics\cvRelList.txt");
            StreamWriter sw = new StreamWriter(@"G:\PromissMetrics\Rlist.txt");

            string line;
            while ((line = sr.ReadLine()) != null)
            {
                var tmp = line.Split(',');
                //for (int i = 0; i < 5; i++) sw.WriteLine("{0},{1}_{2}", tmp[0], tmp[1], i.ToString());
                //Preprocess.CreateCVList(tmp[0], tmp[1]);  //cv list
                //Preprocess.CreateCV(tmp[0], tmp[1], "CodeMetrics", 5);
                Preprocess.CreateCVHVSM(tmp[0], tmp[1], "code", 5);
            }
            sw.Close();
            sr.Close();
            
        }
    }
}
