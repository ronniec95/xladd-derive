using System;
using System.Collections.Generic;

namespace AARC.Utilities
{
    public class ErrorLogger
    {
        private static ErrorLogger _instance;
        private static List<string> _messages;

        private ErrorLogger()
        {

        }

        public static void LogFormat(string message, params object[] args)
        {
            string m = string.Format(message, args);
            Console.WriteLine("{0:HH:mm:ss} : {1}", DateTime.Now, m);
        }

        public static void Log(string message)
        {
            Console.WriteLine("{0:HH:mm:ss} : {1}", DateTime.Now, message);
        }

        public static List<string> Messages
        {
            get
            {
                if (_instance == null)
                    _instance = new ErrorLogger();

                if (_messages == null)
                    _messages = new List<string>();

                return _messages;
            }
        }

    }
}
