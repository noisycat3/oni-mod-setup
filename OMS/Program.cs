using System;
using System.Linq;

namespace OMS
{
    enum EErrorCode
    {
        Ok = 0,

        ToolGeneric = 100,
        ToolMissing,

        CommandGeneric = 200,
        CommandNotFound,
        CommandNoEntry,
        CommandBadParam,
        CommandMissingParam,
        CommandConstructFailed,

        LogGeneric = 300,
        LogFatal,
    }

    class OMSException : Exception
    {
        public OMSException(EErrorCode inCode, string inMessage = null, Exception inner = null)
            : base("OMS Error code " + (int)inCode + ": " + inMessage, inner) { }
    }

    class Program
    {
        static void Main(string[] args)
        {
            ILogger log = Log.GetLog();

            string mainCommand = args.Length > 0 ? args[0] : "install";
            args = args.Skip(1).ToArray();

            log.WriteLine(ELogLevel.Info, "system", "Loading system...");
            SystemProxy system = new SystemProxy();
            system.ParseArguments(args);
            log.WriteLine(ELogLevel.Info, "system", "System ready!");

            log.WriteLine(ELogLevel.Info, "system", "Loading tools...");
            Tool.LoadTools(system, log.Intend(), new Tool[] {
                new ToolGit(),
                new ToolMSBuild()
            });
            log.WriteLine(ELogLevel.Info, "system", "Tools ready!");

            StepResult result = new StepResult();
            try
            {
                Step targetStep = Step.FindCommand(system, mainCommand);
                if (targetStep != null)
                    result = targetStep.Execute(system, log);
            }
            catch (OMSException ex)
            {
                log.WriteLine(ELogLevel.Error, "system", "Failed to execute command: ");
                log.WriteLine(ELogLevel.Error, "system", ex.ToString());
                log.WriteLine(ELogLevel.Error, "system");

                result.code = -1;
            }

            log.WriteLine(ELogLevel.Info, "system", "Code: " + result.code);
            if (!string.IsNullOrEmpty(result.message))
                log.WriteLine(ELogLevel.Info, "system", "Message: " + result.message);

            //new StepGitClone(@"https://github.com/romen-h/kanim-explorer.git", @"E:\ONIMods\oni-mod-setup\test").Execute(system);
        }
    }
}
