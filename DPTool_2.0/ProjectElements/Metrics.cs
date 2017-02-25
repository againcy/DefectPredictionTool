using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;

namespace DPTool_2
{
    public class Metrics
    {
        public DataTable table;

        public Metrics()
        {
            table = new DataTable();
        }
        /// <summary>
        /// 添加新列
        /// </summary>
        /// <param name="name">列名</param>
        /// <param name="type">数据类型</param>
        public void AddNewColumn(string name, string type)
        {
            DataColumn column = new DataColumn();
            column.ColumnName = name;
            column.DataType = System.Type.GetType(type);
            table.Columns.Add(column);
        }

        /// <summary>
        /// 直接从文件中读取度量（无论是否包含bug）
        /// true:成功导入; false:文件不存在
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>true:成功导入; false:文件不存在</returns>
        public bool ImportFromFile(string path)
        {
            if (File.Exists(path) == false) return false;
            StreamReader sr = new StreamReader(path);
            //header
            string line = sr.ReadLine();
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
                row[0] = rowContent[0];
                //metrics
                for (int id = 1; id < rowContent.Length; id++)
                {
                    double tmp;
                    double.TryParse(rowContent[id], out tmp);
                    row[id] = tmp;
                }
                table.Rows.Add(row);
            }
            sr.Close();
            return true;
        }

        /// <summary>
        /// 将code metrics输出为到文件
        /// </summary>
        /// <param name="path">文件路径</param>
        public void ExportToFile(string path)
        {
            StreamWriter sw = new StreamWriter(path);
            //header
            sw.Write("name,");
            foreach (DataColumn col in table.Columns)
            {
                if (col.ColumnName != "bugs" && col.ColumnName != "name") sw.Write(col.ColumnName + ",");
            }
            sw.WriteLine("bugs");

            //body
            foreach (DataRow row in table.Rows)
            {
                sw.Write(row["name"].ToString() + ",");
                foreach (DataColumn col in table.Columns)
                {
                    if (col.ColumnName != "bugs" && col.ColumnName != "name") sw.Write(row[col] + ",");
                }
                sw.WriteLine(row["bugs"].ToString() == "0" ? "0" : "1");
            }
            sw.Close();
        }

        /// <summary>
        /// 加入bug信息
        /// </summary>
        /// <param name="buggyInterval">文件存在bug的时间区间</param>
        /// <param name="releaseDate">当前版本时间点</param>
        public void MapBugs(IEnumerable<string> buggyInterval, DateTime releaseDate)
        {
            //找到所有在当前版本有bug的文件
            var buggyFiles = new List<string>();
            foreach (var line in buggyInterval)
            {
                var tmp = line.Split(',');
                var file = tmp[0];
                var startDate = Convert.ToDateTime(tmp[1]);
                var endDate = Convert.ToDateTime(tmp[2]);
                if (startDate <= releaseDate && releaseDate < endDate && buggyFiles.Contains(file) == false) buggyFiles.Add(file);
            }
            //在表中进行标识
            DataColumn newCol = new DataColumn();
            newCol.ColumnName = "bugs";
            newCol.DataType = System.Type.GetType("System.Int32");
            table.Columns.Add(newCol);
            foreach (DataRow row in table.Rows)
            {
                if (buggyFiles.Contains(row["name"]) == true) row["bugs"] = 1;
                else row["bugs"] = 0;
            }
        }

        /// <summary>
        /// 根据度量列表挑选需要的度量，并删除其余度量
        /// </summary>
        /// <param name="metricList">需要的度量列表</param>
        public void PickMetrics(IEnumerable<string> metricList)
        {
            var colName = new List<string>();
            foreach (DataColumn col in table.Columns) colName.Add(col.ColumnName);
            foreach (var col in colName)
            {
                if (col == "name") continue;
                if (metricList.Contains(col) == false) table.Columns.Remove(col);
            }
        }

        /// <summary>
        /// 将两套度量合并
        /// </summary>
        /// <param name="m1">度量m1的table</param>
        /// <param name="m2">度量m2的table</param>
        public void MixMetrics(DataTable m1, DataTable m2)
        {
            table = new DataTable();
            var metric1 = new List<string>();
            var metric2 = new List<string>();
            this.AddNewColumn("name", "System.String");
            foreach (DataColumn col in m1.Columns)
            {
                if (col.ColumnName == "name" || col.ColumnName == "bugs") continue;
                this.AddNewColumn(col.ColumnName, col.DataType.ToString());
                metric1.Add(col.ColumnName);
            }
            foreach (DataColumn col in m2.Columns)
            {
                if (col.ColumnName == "name" || col.ColumnName == "bugs") continue;
                this.AddNewColumn(col.ColumnName, col.DataType.ToString());
                metric2.Add(col.ColumnName);
            }
            this.AddNewColumn("bugs", "System.Double");

            foreach(DataRow row1 in m1.Rows)
            {
                var newRow = table.NewRow();
                var name = row1["name"].ToString();
                newRow["name"] = name;
                //metrics
                foreach (var metric in metric1) newRow[metric] = row1[metric];
                foreach(DataRow row2 in m2.Rows)
                {
                    if (row2["name"].ToString() == name)
                    {
                        foreach (var metric in metric2) newRow[metric] = row2[metric];
                        break;
                    }
                }
                newRow["bugs"] = row1["bugs"];
                table.Rows.Add(newRow);
            }
        }
    }
}
