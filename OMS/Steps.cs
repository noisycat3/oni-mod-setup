using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OMS
{
    struct StepResult
    {
        public StepResult(int inCode, string inMessage = null, object inPayload = null)
        {
            code = inCode;
            message = inMessage;
            payload = inPayload;
        }

        public int code;
        public string message;
        public object payload;
    }

    //Marks step constructor as command entry point
    class OMSCommand : Attribute
    {
        public OMSCommand(string inName)
        {
            name = inName;
        }

        public string name;
    }

    abstract class Step
    {
        public abstract StepResult Execute(SystemProxy system, ILogger log);

        public static Step FindCommand(SystemProxy system, string commandName)
        {
            Type stepType = null;
            ConstructorInfo stepConstructor = null;

            foreach (Type currentStepType in Assembly.GetAssembly(typeof(Step)).GetTypes())
            {
                stepType = currentStepType;
                stepConstructor = currentStepType.GetConstructors().FirstOrDefault((ConstructorInfo c) =>
                {
                    OMSCommand cmd = c.GetCustomAttribute<OMSCommand>();
                    return cmd != null && cmd.name == commandName;
                });

                if (stepConstructor != null)
                    break;
            }

            if (stepType == null)
                throw new OMSException(EErrorCode.CommandNotFound, "Command not found `" + commandName + "`");
            if (stepConstructor == null)
                throw new OMSException(EErrorCode.CommandNoEntry, "No entry point for command `" + commandName + "`");

            List<object> taskArgs = new List<object>();
            foreach (ParameterInfo paramInfo in stepConstructor.GetParameters())
            {
                if (paramInfo.ParameterType != typeof(string))
                    throw new OMSException(EErrorCode.CommandBadParam, "Command can only support string params `" + commandName + "`");

                string paramValue = system.GetArgument(paramInfo.Name, paramInfo.DefaultValue as string);
                if (paramValue == null)
                    throw new OMSException(EErrorCode.CommandMissingParam, "Command `" + commandName + "` requires `" + paramInfo.Name + "` parameter");

                taskArgs.Add(paramValue);
            }

            Step step = stepConstructor.Invoke(taskArgs.ToArray()) as Step;
            if (step == null)
                throw new OMSException(EErrorCode.CommandConstructFailed, "Failed to prepare command `" + commandName + '`');

            return step;
        }
    }

    /*********************************************************************************/

    class StepGitClone : Step
    {
        public StepGitClone(string inSource, string inTarget)
        {
            sourcePath = inSource;
            targetPath = inTarget;
        }

        private string sourcePath;
        private string targetPath;

        public override StepResult Execute(SystemProxy system, ILogger log)
        {
            if (!Tool.Get<ToolGit>().Clone(sourcePath, targetPath))
                return new StepResult(1, "git clone failed");

            return new StepResult();
        }
    }

    /*********************************************************************************/

    class StepInstall : Step
    {
        [OMSCommand("install")]
        public StepInstall() 
        {
            
        }

        public override StepResult Execute(SystemProxy system, ILogger log)
        {
            log.WriteLine(ELogLevel.Info, "install", "Install complete");
            return new StepResult();
        }
    }
}
