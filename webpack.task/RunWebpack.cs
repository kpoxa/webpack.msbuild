using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace webpack.task
{
    public class RunWebpack : Task
    {
        private string _webPackCommandLineParams = "--optimize-dedupe --optimize-occurence-order";

        public override bool Execute()
        {
            Log.LogMessage("Starting RunWebpack task...");
            var st = new Stopwatch();
            st.Start();
            var ret = executeWp(WebPackCommandLineParams);
            st.Stop();
            var elapsed = st.Elapsed;
            Log.LogMessage("RunWebpack : done, {0}", elapsed.ToString());
            return ret;
        }

        public string WebPackCommandLineParams
        {
            get { return _webPackCommandLineParams; }
            set { _webPackCommandLineParams = value; }
        }

        public string FindExePath(string exe)
        {
            exe = Environment.ExpandEnvironmentVariables(exe);
            if (!File.Exists(exe))
            {
                if (Path.GetDirectoryName(exe) == String.Empty)
                {
                    foreach (string test in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
                    {
                        string path = test.Trim('"', ' ');
                        Log.LogMessage(MessageImportance.Low, "Try {0} as webpack path", path);
                        if (!String.IsNullOrEmpty(path) && File.Exists(path = Path.Combine(path, exe)))
                            return Path.GetFullPath(path);
                    }
                }
                throw new FileNotFoundException(new FileNotFoundException().Message, exe);
            }
            return Path.GetFullPath(exe);
        }

        private bool executeWp(string Args)
        {
            var webpackPath = FindExePath("webpack.cmd");
            Log.LogMessage("WebPack path = {0}", webpackPath);
            var startInfo = CreateProcessStartInfo(Args, webpackPath);
            try
            {
                Log.LogCommandLine(MessageImportance.Normal, webpackPath + Args);
                ExecuteLogged(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }
        }

        private void ExecuteLogged(ProcessStartInfo startInfo)
        {
            using (Process p = new Process())
            {
                p.StartInfo = startInfo;
                p.OutputDataReceived += (sender, args) => LogInfo(args);
                p.ErrorDataReceived += (sender, args) => LogError(args);
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
            }
        }

        private static ProcessStartInfo CreateProcessStartInfo(string Args, string webpackPath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                FileName = webpackPath,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = Args
            };
            return startInfo;
        }

        private void LogInfo(DataReceivedEventArgs args)
        {
            if (!String.IsNullOrWhiteSpace(args.Data))
                Log.LogMessage(args.Data);
        }

        private void LogError(DataReceivedEventArgs args)
        {
            if (!String.IsNullOrWhiteSpace(args.Data))
                Log.LogError(args.Data);
        }
    }
}