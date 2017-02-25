using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPTool_2
{
    namespace AnalyzeGitLog
    {

        public class GitLogParser
        {
            private string content, seperator;
            private string[] lines;
            public GitLogParser(string content, string seperator)
            {
                this.seperator = seperator;
                this.content = content;
                lines = content.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            }

            public IEnumerable<Commit> Commits()
            {
                for (int i = 0; i < lines.Length; ++i)
                {
                    if (lines[i].Contains(seperator))
                    {
                        Commit c = new Commit();
                        var items = lines[i].Split(new string[] { seperator }, StringSplitOptions.None);
                        c.commitno = items[0]; c.author = items[1];
                        c.authordate = DateTime.Parse(items[2]); c.commitdate = DateTime.Parse(items[3]);
                        c.message = items[4];
                        var p = i;
                        while (p + 1 < lines.Length && !lines[p + 1].Contains(seperator) && lines[p + 1] != "")
                            ++p;
                        if (p != i)
                        {
                            c.changedfiles = lines.Skip(i + 1).Take(p - i).ToArray();
                            i = p + 1;
                        }
                        yield return c;
                    }
                }

            }
        }
    }
}
