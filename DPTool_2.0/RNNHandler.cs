using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DPTool_2
{
    static class RNNHandler
    {
        public static void ReadTarget(string RDir,out List<string>targetList)
        {
            StreamReader sr = new StreamReader(string.Format(@"{0}\targetList.txt",RDir));
            targetList = new List<string>();
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                targetList.Add(line);
            }
            sr.Close();
        }
        public static void DoWork(string RDir)
        {
            List<string> targetList;
            ReadTarget(RDir, out targetList);
            foreach (var target in targetList)
            {
                //camel_2.0.0_2.4.0,camel,2.6.0,2.0.0
                var arr = target.Split(',');
                var targetName = string.Format("{0}-{1}_{2}_{3}", arr[0], arr[1], arr[3], arr[2]);
                var outputName = string.Format("{0}_{1}_{2}", arr[0], arr[1], arr[2]);

                if (File.Exists(string.Format(@"{0}\rnn\checkpoints-lr-4-0\{1}\record_sorted.txt", RDir, targetName)) == false) continue;
                StreamReader sr = new StreamReader(string.Format(@"{0}\rnn\checkpoints-lr-4-0\{1}\record_sorted.txt", RDir, targetName));
                var noList = new List<string>();
                string line = sr.ReadLine();
                while ((line = sr.ReadLine()) != null)
                {
                    //Model 43, 1, 13, False, 1.76570e-01, 1.000000; 0.78977, 0.80354
                    var str = line.Split(',')[0];
                    var no = str.Substring(str.IndexOf(' ') + 1);
                    noList.Add(no);
                    if (noList.Count >= 3) break;
                }
                sr.Close();

                var RNNcnt = 1;
                foreach (var no in noList)
                {
                    if (Directory.Exists(string.Format(@"{0}\rnn{1}", RDir, RNNcnt.ToString())) == false)
                        Directory.CreateDirectory(string.Format(@"{0}\rnn{1}", RDir, RNNcnt.ToString()));
                    //model-0-0.csv
                    for (int i = 0; i < 5; i++)
                    {
                        sr = new StreamReader(string.Format(@"{0}\rnn\checkpoints-lr-4-0\{1}\model-{2}-{3}.csv", RDir, targetName, no, i.ToString()));
                        var test = new SortedDictionary<int, string>();
                        var condition = new SortedDictionary<int, string>();
                        //转置
                        var x = 0;
                        foreach (var str in sr.ReadLine().Split(','))
                        {
                            test.Add(x, str);
                            x++;
                        }
                        x = 0;
                        foreach (var str in sr.ReadLine().Split(','))
                        {
                            condition.Add(x, str);
                            x++;
                        }
                        sr.Close();

                        StreamWriter sw = new StreamWriter(string.Format(@"{0}\rnn{1}\{2}_{3}.csv", RDir, RNNcnt.ToString(), outputName, (i+1).ToString()));
                        sw.WriteLine("test,conditions");
                        foreach (var num in test.Keys)
                        {
                            sw.WriteLine(test[num] + "," + condition[num]);
                        }
                        sw.Close();
                    }
                    RNNcnt++;
                }
            }
        }

        public static void DoWork_cv(string RDir)
        {
            var targetList = new List<string>();
            ReadTarget(RDir, out targetList);
            var rnnDir = @"G:\R\TraditionalML\promise_cv\rnn\CV\CV";
            //ant,1.6_0
            foreach (var target in targetList)
            {
                var project = target.Split(',')[0];
                var endRel = target.Split(',')[1].Split('_')[0];
                var cvNo = target.Split(',')[1].Split('_')[1];
                foreach(var subDir in Directory.GetDirectories(rnnDir))
                {
                    var tmp = subDir.Split('\\');
                    var dirName = tmp[tmp.Count() - 1];
                    if (dirName.Split('_')[0]==project && dirName.Split('_')[2]==endRel)
                    {
                        int cnt = 1;
                        foreach(var file in Directory.GetFiles(subDir+@"\"+cvNo.ToString()))
                        {
                            //转置
                            var sr = new StreamReader(file);
                            var test = new SortedDictionary<int, string>();
                            var condition = new SortedDictionary<int, string>();
                            var x = 0;
                            foreach (var str in sr.ReadLine().Split(','))
                            {
                                test.Add(x, str);
                                x++;
                            }
                            x = 0;
                            foreach (var str in sr.ReadLine().Split(','))
                            {
                                condition.Add(x, str);
                                x++;
                            }
                            sr.Close();
                            //ant_1.6_0_10.csv
                            StreamWriter sw = new StreamWriter(string.Format(@"{0}\rnn\{1}_{2}_{3}_{4}.csv", RDir, project, endRel, cvNo, cnt.ToString()));
                            sw.WriteLine("test,conditions");
                            foreach (var num in test.Keys)
                            {
                                sw.WriteLine(test[num] + "," + condition[num]);
                            }
                            sw.Close();
                            cnt++;
                        }
                    }
                }
            }

        }
    }
}
