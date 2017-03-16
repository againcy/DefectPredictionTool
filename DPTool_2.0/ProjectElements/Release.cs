using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPTool_2
{
    public class Release
    {
        public DateTime lastReleaseDate;//上一个版本发布时间
        public DateTime releaseDate;//当前版本发布时间
        public string releaseNo;
        public CodeMetric codeMetric;
        public ProcessMetric processMetric;
        public Metrics mixedMetric;

        /// <summary>
        /// 由包含版本时间和版本号的指定格式字符串创建版本
        /// </summary>
        /// <param name="releaseInfo"></param>
        public Release(string releaseInfo)
        {
            var tmp = releaseInfo.Split(',');
            releaseNo = tmp[0];
            releaseDate = Convert.ToDateTime(tmp[1]);

            codeMetric = new CodeMetric();
            processMetric = new ProcessMetric();
            mixedMetric = new Metrics();
        }

        public Release(string relNo,DateTime relDate)
        {
            releaseNo = relNo;
            releaseDate = relDate;

            codeMetric = new CodeMetric();
            processMetric = new ProcessMetric();
            mixedMetric = new Metrics();
        }

        /// <summary>
        /// 直接从文件中读取度量
        /// 0:成功; 1:缺少mixedMetric; 2:缺少code或process
        /// </summary>
        /// <param name="projectName"></param>
        /// <returns>0:成功; 1:缺少mixedMetric; 2:缺少code或process</returns>
        public int ImportMetrics(string projectName)
        {
            var codePath = string.Format(@"{0}\{1}_releases\CodeMetrics\{2}.csv", Program.rootDir, projectName, releaseNo);
            var processPath = string.Format(@"{0}\{1}_releases\ProcessMetrics\{2}.csv", Program.rootDir, projectName, releaseNo);
            var mixedPath = string.Format(@"{0}\{1}_releases\MixedMetrics\{2}.csv", Program.rootDir, projectName, releaseNo);

            if (codeMetric.ImportFromFile(codePath) == false || processMetric.ImportFromFile(processPath) == false) return 2;
            if (mixedMetric.ImportFromFile(mixedPath) == false) return 1;
            return 0;
        }

        /// <summary>
        /// 混合code和process度量
        /// </summary>
        /// <param name="projectName"></param>
        public void ExportMixedMetrics(string projectName)
        {
            mixedMetric.MixMetrics(codeMetric.table, processMetric.table);
            var path = string.Format(@"{0}\{1}_releases\MixedMetrics\{2}.csv", Program.rootDir, projectName, releaseNo);
            mixedMetric.ExportToFile(path);
        }

        /// <summary>
        /// 获得code metrics
        /// </summary>
        /// <param name="projectName"></param>
        /// <param name="buggyInterval">bug文件时间区间</param>
        public void GetCodeMetrics(
            string projectName, 
            IEnumerable<string> buggyInterval,
            IEnumerable<string> pickedMetrics
            )
        {
            codeMetric.Import(
                string.Format(@"{0}\{1}_releases\UnderstandMetrics\{2}.csv", Program.rootDir, projectName, releaseNo),
                string.Format(@"{0}\{1}_releases\{2}", Program.rootDir, projectName, releaseNo));
            codeMetric.PickMetrics(pickedMetrics);
            codeMetric.MapBugs(buggyInterval, releaseDate);
        }

        public void ExportCodeMetrics(string projectName)
        {
            var path = string.Format(@"{0}\{1}_releases\CodeMetrics\{2}.csv", Program.rootDir, projectName, releaseNo);
            codeMetric.ExportToFile(path);
        }

        /// <summary>
        /// 获得process metrics
        /// </summary>
        /// <param name="projectName"></param>
        /// <param name="authorSet">作者信息列表</param>
        /// <param name="commitSet">commit信息列表</param>
        /// <param name="buggyInterval">bug文件时间区间</param>
        public void GetProcessMetrics(
            string projectName, 
            IEnumerable<Author>authorSet, 
            IEnumerable<CommitChangeInfo>commitSet,
            IEnumerable<string> buggyInterval
            )
        { 
            processMetric.ReadModuleName(string.Format(@"{0}\{1}_releases\CodeMetrics\{2}.csv", Program.rootDir, projectName, releaseNo), "Standard");

            Console.WriteLine("Processing process metrics of {0} v{1} ...", projectName, this.releaseNo);
            processMetric.Add_COMM(commitSet, lastReleaseDate, releaseDate);//how and why
            processMetric.Add_ADEV(authorSet, lastReleaseDate, releaseDate);//how and why
            processMetric.Add_DDEV(authorSet, releaseDate);//how and why
            processMetric.Add_ADD_DEL(commitSet, lastReleaseDate, releaseDate);//how and why
            processMetric.Add_OWN(commitSet, lastReleaseDate, releaseDate, codeMetric);//how and why
            processMetric.Add_MINOR_MAJOR(commitSet, lastReleaseDate, releaseDate);//dont touch my code
            processMetric.Add_EXP(commitSet, authorSet, lastReleaseDate, releaseDate);//how and why
            processMetric.Add_OEXP(commitSet, authorSet, lastReleaseDate, releaseDate);//how and why
            processMetric.Add_NeighbourMetrics(commitSet, lastReleaseDate, releaseDate, "COMM");
            processMetric.Add_NeighbourMetrics(commitSet, lastReleaseDate, releaseDate, "ADEV");
            processMetric.Add_NeighbourMetrics(commitSet, lastReleaseDate, releaseDate, "DDEV");

            processMetric.MapBugs(buggyInterval, releaseDate);
        }

        public void ExportProcessMetrics(string projectName)
        {
            var path = string.Format(@"{0}\{1}_releases\ProcessMetrics\{2}.csv", Program.rootDir, projectName, releaseNo);
            processMetric.ExportToFile(path);
        }


    }
}
