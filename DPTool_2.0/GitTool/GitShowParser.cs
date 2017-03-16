using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPTool_2
{
    namespace AnalyzeGitLog
    {
        /*
         * info: (ignore) diffOfAFile*
         * head: text from start until meets a line start with ``diff''
         * diffOfAFile: diffLine (ignore) "---Line" "+++Line" change+
         * change: "@@ -" OldFileLineStart "," Lines " +"NewFileLineStart "," NewFileLineStart (newline) (lines|"+"lines|"-"lines)
         * */
        
        
            public class FileChange
            {
                public int[] OldVersionChangedLines, NewVersionChangedLines;
                public string Path;
            }

            public class GitShowParser
            {
                string content;
                string[] lines;
                int p = 0;
                public GitShowParser(string content)
                {
                    this.content = content;
                    this.lines = content.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Concat(new string[] { "EOF" }).ToArray();
                }

                public IEnumerable<FileChange> info()
                {
                    while (!lines[p].StartsWith("diff") && !lines[p].StartsWith("EOF"))
                        ++p;
                    while (p < lines.Length && lines[p] != "EOF")
                    {
                        yield return diffOfAFile();
                    }
                }

                public FileChange diffOfAFile()
                {
                    var ret = new FileChange();
                    while (!lines[p].StartsWith("---"))
                        ++p;
                    //p+=2;
                    //System.Diagnostics.Debug.Assert(lines[p].StartsWith("---"));
                    ret.Path = lines[p].Split(new string[] { "--- a" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    while (!lines[p].StartsWith("@@"))
                        ++p;
                    //++p;
                    //System.Diagnostics.Debug.Assert(lines[p].StartsWith("+++"));
                    //++p;
                    IEnumerable<int> oldlines = new List<int>();
                    IEnumerable<int> newlines = new List<int>();
                    while (lines[p].StartsWith("@@"))
                    {

                        var tmp = change();
                        oldlines = oldlines.Concat(tmp.Item1);
                        newlines = newlines.Concat(tmp.Item2);
                    }
                    ret.OldVersionChangedLines = oldlines.ToArray();
                    ret.NewVersionChangedLines = newlines.ToArray();
                    return ret;
                }

                public Tuple<List<int>, List<int>> change()
                {
                    System.Diagnostics.Debug.Assert(lines[p].StartsWith("@@"));
                    var numbers = lines[p].Split(new string[] { "@@" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var oldnumber = -int.Parse(numbers[0].Split(',')[0]);
                    var newnumber = int.Parse(numbers[1].Split(',')[0]);
                    var olddelta = 0;
                    var newdelta = 0;
                    ++p;
                    List<int> oldlist = new List<int>();
                    List<int> newlist = new List<int>();
                    while (lines[p][0] == ' ' || lines[p][0] == '-' || lines[p][0] == '+')
                    {

                        switch (lines[p][0])
                        {
                            case ' ':
                                olddelta++; newdelta++;
                                break;
                            case '-':
                                oldlist.Add(oldnumber + olddelta); olddelta++;
                                break;
                            case '+':
                                newlist.Add(newnumber + newdelta); newdelta++;
                                break;
                        }
                        ++p;
                    }
                    if (lines[p][0] == '\\')
                        ++p;
                    return new Tuple<List<int>, List<int>>(oldlist, newlist);
                }
            }
        }
    
}
