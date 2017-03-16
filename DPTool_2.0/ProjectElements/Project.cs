using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DPTool_2
{
    public class Project
    {
        public string projectName;
        public List<Release> releaseSet;
        public List<Author> authorSet;
        public List<CommitChangeInfo> commitSet
        {
            get
            {
                var ret = new List<CommitChangeInfo>();
                foreach(var author in authorSet)
                {
                    foreach (var commit in author.commitChangeInfo) ret.Add(commit);
                }
                return ret;
            }
        }
        public List<string> buggyInterval;
            
        public Project(string name)
        {
            projectName = name;
            releaseSet = new List<Release>();
            authorSet = new List<Author>();

            var dir = string.Format(@"{0}\{1}_releases\CodeMetrics", Program.rootDir, projectName);
            if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);
            dir = string.Format(@"{0}\{1}_releases\ProcessMetrics", Program.rootDir, projectName);
            if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);
            dir = string.Format(@"{0}\{1}_releases\MixedMetrics", Program.rootDir, projectName);
            if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);
            //读入各种信息

            ReadAuthorInfo();
            GetReleases();
            GetBuggyInterval();
            
        }

        /// <summary>
        /// 返回指定版本号之间的版本
        /// </summary>
        /// <param name="startRel">起始版本</param>
        /// <param name="endRel">末尾版本</param>
        /// <returns>以版本时间排序的Release集合</returns>
        public SortedDictionary<DateTime,Release> GetReleaseBetween(string startRel, string endRel)
        {
            DateTime start = DateTime.Now;
            DateTime end = DateTime.Now;
            foreach (var rel in this.releaseSet)
            {
                if (rel.releaseNo == startRel) start = rel.releaseDate;
                if (rel.releaseNo == endRel) end = rel.releaseDate;
            }
            var pickedRel = new SortedDictionary<DateTime, Release>();
            foreach (var rel in this.releaseSet)
            {
                if (rel.releaseDate >= start && rel.releaseDate <= end) pickedRel.Add(rel.releaseDate, rel);
            }
            return pickedRel;
        }

        /// <summary>
        /// 读入作者信息
        /// </summary>
        public void ReadAuthorInfo()
        {
            var dir = string.Format(@"{0}\{1}_releases\authors\", Program.rootDir, projectName);
            foreach (var file in Directory.GetFiles(dir))
            {
                var author = new Author(Path.GetFileNameWithoutExtension(file));
                if (author.Import(File.ReadAllText(file)) == false)
                    Console.WriteLine("Author:[{0}] import error!", author.name);
                authorSet.Add(author);
            }
        }

        /// <summary>
        /// 读入版本信息
        /// </summary>
        public void GetReleases()
        {
            //读入release date
            var relDateFile = string.Format(@"{0}\{1}_releases\releaseDate.txt", Program.rootDir, projectName);
            StreamReader srDate = new StreamReader(relDateFile);
            var relDate = new Dictionary<string, DateTime>();
            foreach (var line in srDate.ReadToEnd().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                relDate.Add(line.Split(',')[0], Convert.ToDateTime(line.Split(',')[1]));
            }
            srDate.Close();
            //读入picked release
            DateTime lastRealease = Program.zeroDate;//记录上一个版本的发布时间,默认为2000-1-1
            var relPickedFile = string.Format(@"{0}\{1}_releases\releasePicked.txt", Program.rootDir, projectName);
            StreamReader srPicked = new StreamReader(relPickedFile);
            foreach (var line in srPicked.ReadToEnd().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (relDate.ContainsKey(line)==false)
                {
                    Program.Log(string.Format("Project.GetReleases: [{0} {1}] not exist in releaseDate.txt", this.projectName, line));
                    continue;
                }
                var r = new Release(line, relDate[line]);
                r.lastReleaseDate = lastRealease;
                releaseSet.Add(r);
                lastRealease = r.releaseDate;
            }
            srPicked.Close();
        }

        /// <summary>
        /// 读取bug文件的时间区间
        /// </summary>
        public void GetBuggyInterval()
        {
            StreamReader sr = new StreamReader(string.Format(@"{0}\{1}_releases\BuggyIntervals.csv", Program.rootDir, projectName));
            var file = sr.ReadToEnd().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Skip(1);
            sr.Close();
            buggyInterval = file.ToList();
        }

        /// <summary>
        /// 获取各版本code metrics
        /// </summary>
        public void GetCodeMetrics(bool existed = false)
        {
            StreamReader sr = new StreamReader(Program.rootDir + @"\codeMetricList.csv");
            var picked = sr.ReadLine().Split(',').ToList();
            foreach(var release in releaseSet)
            {
                release.GetCodeMetrics(projectName, buggyInterval,picked);
            }
        }

        /// <summary>
        /// 输出各版本code metrics
        /// </summary>
        public void ExportCodeMetrics()
        {
            var dir = string.Format(@"{0}\{1}_releases\CodeMetrics", Program.rootDir, projectName);
            if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);
            foreach (var release in releaseSet)
            {
                release.ExportCodeMetrics(projectName);
            }
        }

        /// <summary>
        /// 获取各版本process metrics
        /// </summary>
        public void GetProcessMetrics()
        {
            //生成作者的exp
            foreach(var a in authorSet)
            {
                a.GenerateExp(this.releaseSet.First().lastReleaseDate, this.releaseSet.Last().releaseDate);
            }
            //获取各版本process metrics
            foreach(var release in releaseSet)
            {
                release.GetProcessMetrics(projectName, authorSet, commitSet, buggyInterval);
            }
        }

        /// <summary>
        /// 输出各版本process metrics
        /// </summary>
        public void ExportProcessMetrics()
        {
            var dir = string.Format(@"{0}\{1}_releases\ProcessMetrics", Program.rootDir, projectName);
            if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);
            foreach (var release in releaseSet)
            {
                release.ExportProcessMetrics(projectName);
            }
        }

        /// <summary>
        /// 直接从文件中读取度量
        /// </summary>
        public void ImportMetrics()
        {
            foreach (var release in releaseSet)
            {
                if (release.ImportMetrics(projectName) == 1) release.ExportMixedMetrics(projectName);
            }
        }

        /// <summary>
        /// 输出混合度量
        /// </summary>
        public void ExportMixedMetrics()
        {
            foreach (var release in releaseSet)
            {
                release.ExportMixedMetrics(projectName);
            }
        }
    }
}
