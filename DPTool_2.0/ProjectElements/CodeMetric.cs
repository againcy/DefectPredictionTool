using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;

namespace DPTool_2
{
    public class CodeMetric:Metrics
    {
        public CodeMetric()
        {
            table = new DataTable();
        }

        /// <summary>
        /// 从指定路径读入code metrics，默认为Understand处理后的格式
        /// </summary>
        /// <param name="path">读入文件路径</param>
        /// <param name="formatArgs">对于Understand度量，文件名中需要删除的子串</param>
        public void Import(string path, string formatArgs = "")
        {
            StreamReader sr = new StreamReader(path);
            //header
            string line = sr.ReadLine();
            int haveBugs = 0;//记录是否有bug列
            foreach (var header in line.Split(','))
            {
                if (header == "name") this.AddNewColumn(header, "System.String");
                else this.AddNewColumn(header, "System.Double");
            }
            while ((line = sr.ReadLine()) != null)
            {
                var row = table.NewRow();
                var rowContent = line.Split(',');
                //name
                var name = rowContent[0];
                name = name.Replace(formatArgs, "").Replace(@"\","/");//将understand处理后的文件名的多余路径删除
                row[0] = name;
                //metrics
                for (int id = 1; id < rowContent.Length-haveBugs; id++)
                {
                    double tmp;
                    double.TryParse(rowContent[id], out tmp);
                    row[id] = tmp;
                }
                if (row["CountLineCode"].ToString() != "0") table.Rows.Add(row);
            }
            sr.Close();
        }
    }
}
