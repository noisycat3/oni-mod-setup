using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMS
{
    enum ELogLevel
    {
        Debug,
        Verbose,
        Info,
        Warning,
        Error,
        Fatal,
        None,
    }

    interface ILogger
    {
        void WriteLine(ELogLevel level, string cat, string str = "");
        ILogger Intend(int spaces = 4);
    }

    static class Log
    {
        public static ELogLevel logLevel = ELogLevel.Debug;

        public static void WriteLine(ELogLevel level, string cat, string str)
        {
            if ((int)level < Math.Min((int)logLevel, (int)ELogLevel.Fatal))
                return;

            if (level >= ELogLevel.Fatal)
                throw new OMSException(EErrorCode.LogFatal, '[' + cat + ']' + str);

            Console.WriteLine(('[' + level.ToString() + "][" + cat + ']').PadRight(20) + "> " + str);
        }

        class LoggerImpl : ILogger
        {
            public LoggerImpl(int spaces = 0)
            {
                this.spaces = spaces;
                spaceString = new string(' ', spaces);
            }

            int spaces;
            string spaceString;

            public ILogger Intend(int spaces = 4)
            {
                return new LoggerImpl(this.spaces + spaces);
            }

            public void WriteLine(ELogLevel level, string cat, string str)
            {
                Log.WriteLine(level, cat, spaceString + str);
            }
        }

        private static LoggerImpl rootLogger = new LoggerImpl();
        public static ILogger GetLog() { return rootLogger; }
    }
}
