using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMS
{
    //OS-specific stuff
    class SystemProxy
    {
        public struct ConsoleOutput
        {
            public int exitCode;
            public string output;
            public string errorOutput;
        }

        public ConsoleOutput ExecuteCommand(string command, string arguments = "", string cwd = null)
        {
            ConsoleOutput result = new ConsoleOutput();

            Process p = new Process();
            p.StartInfo.FileName = command;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.WorkingDirectory = cwd;

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;

            try
            {
                p.Start();

                result.output = p.StandardOutput.ReadToEnd();
                result.errorOutput = p.StandardError.ReadToEnd();

                p.WaitForExit();
                result.exitCode = p.ExitCode;
            }
            catch (Exception)
            {
                result.exitCode = 9009; //Emulate windows errorlevel
            }

            return result;
        }

        Dictionary<string, string> arguments = new Dictionary<string, string>();
        public void ParseArguments(string[] inArgs)
        {
            foreach (string arg in inArgs)
            {
                string key = arg;
                string value = null;

                int separatorIndex = arg.IndexOf('=');
                if (separatorIndex > 0)
                {
                    key = arg.Substring(0, separatorIndex);
                    value = arg.Substring(separatorIndex + 1);
                }

                arguments.Add(key, value);
            }
        }

        public string GetArgument(string key, string fallback = null)
        {
            string value;
            if (arguments.TryGetValue(key, out value))
                return value;

            return fallback;
        }

        public bool HasArgument(string key)
        {
            return arguments.ContainsKey(key);
        }
    }
}
