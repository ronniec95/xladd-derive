using System;
using System.Runtime.CompilerServices;
using System.Threading;
using log4net;
using log4net.Appender;

namespace AARC.Utilities
{
    public class DiagnosticLogger : IAarcLogger
    {
        public DiagnosticLogger()
        {
            log4net.Config.XmlConfigurator.Configure();

            _log = LogManager.GetLogger(typeof(DiagnosticLogger));
        }

        public DiagnosticLogger(string path)
        {
            log4net.Config.XmlConfigurator.Configure();

            RollingFileAppender appender = LogManager.GetRepository().GetAppenders()[1] as RollingFileAppender;
            if (appender != null)
            {
                appender.File = path;
                appender.ActivateOptions();
            }

            _log = LogManager.GetLogger(typeof(DiagnosticLogger));
        }

        //public LogType LogType { get; set; }
        private readonly ILog _log;// = LogManager.GetLogger(typeof(DiagnosticLogger));

        private void TimeStamp()
        {
            System.Diagnostics.Debug.Write(DateTime.Now.ToString(@"yyyyMMdd HH:mm:ss.fff "));
        }

        public void Write(string message)
        {
            System.Diagnostics.Debug.Write(message);
        }

        public void WriteLine(string message, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            string shortFilePath = file;
            if (file != null)
            {
                int index = file.LastIndexOf("\\", StringComparison.CurrentCulture);
                int index2 = file.LastIndexOf(".cs", StringComparison.CurrentCulture);
                shortFilePath = file.Substring(index + 1, index2 - index);
            }

            TimeStamp();
            string msg = $"[{Thread.CurrentThread.ManagedThreadId}] {shortFilePath}{caller}({lineNumber}): {message}";
            System.Diagnostics.Debug.WriteLine(msg);
            /*
            System.Diagnostics.Debug.WriteLine(msg);
            _log.Debug(msg);
            */

            //ILog l4Log = LogManager.GetLogger("Bob");
            //_log.Debug(message);
        }

        public void WriteLine(string format, params object[] p)
        {
            //TimeStamp();
            //System.Diagnostics.Debug.WriteLine(format, p);
            _log.DebugFormat(format, p);
        }
        public void WriteLine(Exception e)
        {
            //TimeStamp();
            //System.Diagnostics.Debug.WriteLine(e);
            _log.Error(e);
        }


        public void Debug(object message)
        {
            _log.Debug(message);
        }

        public void Debug(object message, Exception exception)
        {
            _log.Debug(message, exception);
        }

        public void DebugFormat(string format, params object[] args)
        {
            _log.DebugFormat(format, args);
        }

        public void DebugFormat(string format, object arg0)
        {
            _log.DebugFormat(format, arg0);
        }

        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            _log.DebugFormat(provider, format, args);
        }

        public void DebugFormat(string format, object arg0, object arg1)
        {
            _log.DebugFormat(format, arg0, arg1);
        }

        public void DebugFormat(string format, object arg0, object arg1, object arg2)
        {
            _log.DebugFormat(format, arg0, arg1, arg2);
        }
        public void Info(object message)
        {
            _log.Info(message);
        }

        public void Info(object message, Exception exception)
        {
            _log.Info(message, exception);
        }

        public void InfoFormat(string format, params object[] args)
        {
            _log.InfoFormat(format, args);
        }

        public void InfoFormat(string format, object arg0)
        {
            _log.InfoFormat(format, arg0);
        }

        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            _log.InfoFormat(provider, format, args);
        }

        public void InfoFormat(string format, object arg0, object arg1)
        {
            _log.InfoFormat(format, arg0, arg1);
        }

        public void InfoFormat(string format, object arg0, object arg1, object arg2)
        {
            _log.InfoFormat(format, arg0, arg1, arg2);
        }

        public void Warn(object message)
        {
            _log.Warn(message);
        }

        public void Warn(object message, Exception exception)
        {
            _log.Warn(message, exception);
        }

        public void WarnFormat(string format, params object[] args)
        {
            _log.WarnFormat(format, args);
        }

        public void WarnFormat(string format, object arg0)
        {
            _log.WarnFormat(format, arg0);
        }

        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            _log.WarnFormat(provider, format, args);
        }

        public void WarnFormat(string format, object arg0, object arg1)
        {
            _log.WarnFormat(format, arg0, arg1);
        }

        public void WarnFormat(string format, object arg0, object arg1, object arg2)
        {
            _log.WarnFormat(format, arg0, arg1, arg2);
        }

        public void Error(object message)
        {
            _log.Error(message);
        }

        public void Error(object message, Exception exception)
        {
            _log.Error(message, exception);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            _log.ErrorFormat(format, args);
        }

        public void ErrorFormat(string format, object arg0)
        {
            _log.ErrorFormat(format, arg0);
        }

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            _log.ErrorFormat(provider, format, args);
        }

        public void ErrorFormat(string format, object arg0, object arg1)
        {
            _log.ErrorFormat(format, arg0, arg1);
        }

        public void ErrorFormat(string format, object arg0, object arg1, object arg2)
        {
            _log.ErrorFormat(format, arg0, arg1, arg2);
        }
    }
}
