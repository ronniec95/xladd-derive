using System;
using System.Runtime.CompilerServices;

namespace AARC.Utilities
{
    //[Flags]
    //public enum LogType
    //{
    //    Debug = 1,
    //    Info = 2,
    //    Warning = 4,
    //    Error = 8
    //};

    public interface IAarcLogger
    {
        //LogType LogType { get; set; }
        void Write(string message);

        //void WriteLine(string message);
        void WriteLine(string message, [CallerFilePath] string name = null, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void WriteLine(Exception e);
        void WriteLine(string format, params object[] p);

        // Log4Net Wrapper
        void Debug(object message);

        void Debug(object message, Exception exception);

        void DebugFormat(string format, params object[] args);

        void DebugFormat(string format, object arg0);

        void DebugFormat(IFormatProvider provider, string format, params object[] args);

        void DebugFormat(string format, object arg0, object arg1);

        void DebugFormat(string format, object arg0, object arg1, object arg2);

        void Info(object message);

        void Info(object message, Exception exception);

        void InfoFormat(string format, params object[] args);

        void InfoFormat(string format, object arg0);

        void InfoFormat(IFormatProvider provider, string format, params object[] args);

        void InfoFormat(string format, object arg0, object arg1);

        void InfoFormat(string format, object arg0, object arg1, object arg2);

        void Warn(object message);

        void Warn(object message, Exception exception);

        void WarnFormat(string format, params object[] args);

        void WarnFormat(string format, object arg0);

        void WarnFormat(IFormatProvider provider, string format, params object[] args);

        void WarnFormat(string format, object arg0, object arg1);

        void WarnFormat(string format, object arg0, object arg1, object arg2);

        void Error(object message);

        void Error(object message, Exception exception);

        void ErrorFormat(string format, params object[] args);

        void ErrorFormat(string format, object arg0);

        void ErrorFormat(IFormatProvider provider, string format, params object[] args);

        void ErrorFormat(string format, object arg0, object arg1);

        void ErrorFormat(string format, object arg0, object arg1, object arg2);
    }
}