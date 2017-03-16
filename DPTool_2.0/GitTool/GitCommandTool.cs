using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DPTool_2
{
    namespace AnalyzeGitLog
    {
        public static class GitCommandTool
        {
            public static string gitCommand = "git.exe";
            public static string RunGitCommand(string workdir, string command)
            {
                Process process = new Process();
                StringBuilder outputStringBuilder = new StringBuilder();

                try
                {
                    process.StartInfo.FileName = gitCommand;
                    process.StartInfo.WorkingDirectory = workdir;
                    process.StartInfo.Arguments = command;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;
                    process.EnableRaisingEvents = false;
                    process.OutputDataReceived += (sender, eventArgs) => outputStringBuilder.AppendLine(eventArgs.Data);
                    process.ErrorDataReceived += (sender, eventArgs) => { };// outputStringBuilder.AppendLine(eventArgs.Data);
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    var output = outputStringBuilder.ToString();

                    if (process.ExitCode != 0)
                    {


                        throw new Exception("Process exited with non-zero exit code of: " + process.ExitCode + Environment.NewLine +
                        "Output from process: " + outputStringBuilder.ToString());
                    }
                    return output;
                }
                finally
                {
                    process.Close();
                }
            }      

            

        }
    }
}