using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DPTool_2
{
    static class CodeDiff
    {
        private static IEnumerable<string> ReadFirstCol(string path, bool header = true, char seperator = ',')
        {
            var content = new List<string>();
            StreamReader sr = new StreamReader(path);
            string line;
            if (header) line = sr.ReadLine();
            while ((line = sr.ReadLine()) != null)
            {
                content.Add(line.Split(seperator)[0]);
            }
            sr.Close();
            return content;
        }

        /// <summary>
        /// 寻找相同模块
        /// </summary>
        /// <param name="project"></param>
        public static void FindSameFiles(string project)
        {
            Console.WriteLine("Processing <{0}>...", project);
            //读入release
            var release = new SortedDictionary<int,string>();
            StreamReader sr = new StreamReader(string.Format(@"G:\OpenSourceCode\{0}\release.txt",project));
            string line;
            int relNo = 1;
            while ((line=sr.ReadLine())!=null)
            {
                release.Add(relNo, line);
                relNo++;
            }
            sr.Close();
            //找到相邻两个release相同的模块
            for (int rel = 2; rel < relNo; rel++)
            {
                string path;
                path = string.Format(@"G:\PromissMetrics\{0}_releases\CodeMetrics\{1}.csv", project, release[rel - 1]);
                var promisePrevRel = ReadFirstCol(path);
                path = string.Format(@"G:\PromissMetrics\{0}_releases\CodeMetrics\{1}.csv", project, release[rel]);
                var promiseRel = ReadFirstCol(path);
                
                path = string.Format(@"G:\OpenSourceCode\{0}\{1}.csv", project, release[rel - 1]);
                var prevRel = ReadFirstCol(path);
                path = string.Format(@"G:\OpenSourceCode\{0}\{1}.csv", project, release[rel]);
                var newRel = ReadFirstCol(path);

                var developing = new List<Tuple<string, string, string>>();
                foreach(var promiseM in promiseRel)
                {
                    if (promisePrevRel.Contains(promiseM) == false) continue;
                    foreach(var newM in newRel)
                    {
                        if (newM.Replace(@"\",".").Contains(promiseM))
                        {
                            foreach(var prevM in prevRel)
                            {
                                if (prevM.Replace(@"\", ".").Contains(promiseM))
                                {
                                    developing.Add(new Tuple<string, string, string>(promiseM, prevM, newM));
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }

                StreamWriter sw = new StreamWriter(string.Format(@"G:\OpenSourceCode\{0}\{1}_modules.txt", project, release[rel]));
                foreach (var dev in developing)
                {
                    sw.WriteLine("{0},{1},{2}", dev.Item1, dev.Item2, dev.Item3);
                }
                sw.Close();
            }
        }

        /// <summary>
        /// 生成code churn
        /// </summary>
        /// <param name="project"></param>
        public static void GenerateCodeChurn(string project)
        {
            //读入release
            var release = new SortedDictionary<int, string>();
            StreamReader sr = new StreamReader(string.Format(@"G:\OpenSourceCode\{0}\release.txt", project));
            string line;
            int relNo = 1;
            while ((line = sr.ReadLine()) != null)
            {
                release.Add(relNo, line);
                relNo++;
            }
            sr.Close();

            //
            var lastRel = new Dictionary<string, Tuple<int, int>>();
            for (int rel = 1; rel < relNo; rel++)
            {
                //读入diff
                //G:\OpenSourceCode\ant\1.4_diff.txt
                var diff = new Dictionary<string, Tuple<int, int>>();
                var diffFile = string.Format(@"G:\OpenSourceCode\{0}\{1}_diff.txt", project, release[rel]);
                if (File.Exists(diffFile) == true)
                { 
                    sr = new StreamReader(diffFile);
                    while ((line = sr.ReadLine()) != null)
                    {
                        var arr = line.Split(',');
                        diff.Add(arr[0], new Tuple<int, int>(Convert.ToInt32(arr[1]), Convert.ToInt32(arr[2])));
                    }
                    sr.Close();
                }
                //读入loc和bugs
                var loc = new Dictionary<string, int>();
                var bugs = new Dictionary<string, int>();
                sr = new StreamReader(string.Format(@"G:\PromissMetrics\{0}_releases\CodeMetrics\{1}.csv", project, release[rel]));
                var header = sr.ReadLine().Split(',');
                var colLoc = 0;
                var colBug = 0;
                for (int i = 0; i < header.Count(); i++)
                {
                    if (header[i]=="loc") colLoc = i;
                    if (header[i] == "bugs") colBug = i;
                }
                while ((line=sr.ReadLine())!=null)
                {
                    var name = line.Split(',')[0];
                    var nloc = Convert.ToInt32(line.Split(',')[colLoc]);
                    var nbug = Convert.ToInt32(line.Split(',')[colBug]);
                    nbug = (nbug > 0) ? 1 : 0;
                    loc.Add(name, nloc);
                    bugs.Add(name, nbug);
                }
                sr.Close();

                //生成code churn
                var curRel = new Dictionary<string, Tuple<int, int>>();
                foreach (var module in loc)
                {
                    var name = module.Key;
                    if (diff.ContainsKey(name))
                    {
                        curRel.Add(name, new Tuple<int, int>(diff[name].Item1, diff[name].Item2));
                    }
                    else
                    {
                        curRel.Add(name, new Tuple<int, int>(module.Value, 0));
                    }
                }
                //输出
                StreamWriter sw = new StreamWriter(string.Format(@"G:\PromissMetrics\{0}_releases\ProcessMetrics\{1}.csv", project, release[rel]));
                sw.WriteLine("name,ADD,DEL,CADD,CDEL,bugs");
                var curC = new Dictionary<string, Tuple<int, int>>();
                foreach (var module in curRel)
                {
                    var name = module.Key;
                    var ADD = module.Value.Item1;
                    var DEL = module.Value.Item2;
                    var CADD = ADD;
                    var CDEL = DEL;
                    if (lastRel.ContainsKey(name))
                    {
                        CADD += lastRel[name].Item1;
                        CDEL += lastRel[name].Item2;
                    }
                    curC.Add(name, new Tuple<int, int>(CADD, CDEL));

                    sw.WriteLine("{0},{1},{2},{3},{4},{5}", name, ADD.ToString(), DEL.ToString(), CADD.ToString(), CDEL.ToString(), bugs[name].ToString());
                }
                sw.Close();

                lastRel = curC;
            }
        }
    }
}
