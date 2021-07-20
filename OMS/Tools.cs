using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMS
{
    class ToolException : Exception
    {
        public ToolException(string inMessage, Exception innerException = null) 
            : base(inMessage, innerException) { }
    }

    abstract class Tool
    {
        public Tool(string inName)
        {
            name = inName;
            path = null;
        }

        static Dictionary<Type, Tool> toolbox = new Dictionary<Type, Tool>();

        string name;
        SystemProxy system;
        ILogger log;
        string path;

        public static void LoadTools(SystemProxy inSystem, ILogger log, Tool[] toolList)
        {
            foreach (Tool tool in toolList)
            {
                tool.LoadToolSingle(inSystem, log);
                toolbox.Add(tool.GetType(), tool);
            }
        }

        public static T Get<T>() where T : Tool
        {
            Tool outTool;
            if (toolbox.TryGetValue(typeof(T), out outTool))
                return outTool as T;
            return null;
        }

        void LoadToolSingle(SystemProxy inSystem, ILogger inLog)
        {
            system = inSystem;
            inLog.WriteLine(ELogLevel.Verbose, "tools", "Locating tool: " + name);

            log = inLog.Intend();
            try
            {
                path = Locate();
            } 
            catch (ToolException ex)
            {
                throw new OMSException(EErrorCode.ToolMissing, "Failed to locate `" + name + "` caused by ", ex);
            }

            if (path == null)
                throw new OMSException(EErrorCode.ToolMissing, "Failed to locate `" + name + "`");

            inLog.WriteLine(ELogLevel.Verbose, "tools", "Locating tool: " + name + " success!");
        }

        //Use to perform system-specific operations
        protected SystemProxy GetSystem() { return system; }
        protected ILogger GetLog() { return log; }

        //Use to actually invoke the tool, implement helper methods in implementation classes
        protected SystemProxy.ConsoleOutput InvokeTool(string arguments = "", string cwd = null)
        {
            return system.ExecuteCommand(path, arguments, cwd);
        }

        //Find the tool path, allowed to block for input
        //If null is returned, then the command fails
        protected abstract string Locate();
    }

    /*********************************************************************************/

    internal class ToolGit : Tool
    {
        public ToolGit() : base("git") { }

        protected override string Locate()
        {
            SystemProxy.ConsoleOutput result = GetSystem().ExecuteCommand("git", "--version");

            if (result.exitCode != 0)
                throw new ToolException("git must be in system path");

            const string gitVersionTemplate = "git version ";
            int gitVersionLength = result.output.IndexOf('\n') - gitVersionTemplate.Length;
            string gitVersion = (gitVersionLength > 0)
                ? result.output.Substring(gitVersionTemplate.Length, gitVersionLength) : "unknown";

            GetLog().WriteLine(ELogLevel.Info, "tool-git", "Found git, version: `" + gitVersion + "`");
            return "git";
        }

        public bool Clone(string from, string to)
        {
            GetLog().WriteLine(ELogLevel.Info, "tool-git", "Cloning... `" + from + "` -> `" + to + "`");
            SystemProxy.ConsoleOutput result = InvokeTool("clone \"" + from + "\" \"" + to + '"');

            if (result.exitCode == 0)
                return true;

            GetLog().WriteLine(ELogLevel.Error, "tool-git", "FAILED!");
            GetLog().WriteLine(ELogLevel.Error, "tool-git", result.output);
            if (result.errorOutput != null && result.errorOutput.Length > 0)
            {
                GetLog().WriteLine(ELogLevel.Error, "tool-git", "STDERR");
                GetLog().WriteLine(ELogLevel.Error, "tool-git", result.errorOutput);
            }

            return false;
        }
    }

    internal class ToolMSBuild : Tool
    {
        public ToolMSBuild() : base("MSBuild") { }

        protected override string Locate()
        {
            const string pathRegLocation = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0";
            const string pathRegKey = @"MSBuildToolsPath";

            SystemProxy.ConsoleOutput result = GetSystem().ExecuteCommand("reg", "query \"" + pathRegLocation + "\" /v " + pathRegKey);
            if (result.exitCode != 0)
                throw new ToolException("failed to query registry at `" + pathRegLocation + "`, key `" + pathRegKey + "`");

            /* (EXAMPLE OUTPUT)
             * 
             * HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0
             *     MSBuildToolsPath REG_SZ    C:\Windows\Microsoft.NET\Framework64\v4.0.30319\
             */

            string[] regQueryLines = result.output.Split('\n');
            for (int i = 1; i < regQueryLines.Length; i++)
            {
                if (!regQueryLines[i - 1].StartsWith(pathRegLocation))
                    continue;

                const string regValueHeader = @"    MSBuildToolsPath    REG_SZ    ";
                if (!regQueryLines[i].StartsWith(regValueHeader))
                    throw new ToolException("invalid registry key: `" + regQueryLines[i] + "`");

                string regValue = regQueryLines[i].Substring(regValueHeader.Length).TrimEnd();

                string msbuildPath = regValue + "msbuild";
                GetLog().WriteLine(ELogLevel.Info, "tool-msbuild", "Found msbuild, path: `" + msbuildPath + "`");
                return msbuildPath;
            }

            throw new ToolException("failed to locate reg value");
        }
    }
}
