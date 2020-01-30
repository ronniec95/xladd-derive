using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace AARC.Utilities
{
    public class PythonHelper
    {
        // Also see http://stackoverflow.com/questions/14070051/checking-if-process-returned-an-error-c-sharp

        private static readonly string PythonLocation = @"E:\Apps\Anaconda3\python.exe";

        public static void RunPythonScript(string scriptlocation, string[] arguments, string workingDirectory = null)
        {
            Process myProcess = null;
            StringBuilder sb = new StringBuilder();
            bool success = false;
            try
            {
                // Create new process start info 
                ProcessStartInfo myProcessStartInfo = new ProcessStartInfo(PythonLocation);

                // make sure we can read the output from stdout 
                myProcessStartInfo.UseShellExecute = false;
                myProcessStartInfo.RedirectStandardOutput = true;

                if (workingDirectory != null)
                    myProcessStartInfo.WorkingDirectory = workingDirectory;

                // start python app with 3 arguments  
                // 1st arguments is pointer to itself,  
                // 2nd and 3rd are actual arguments we want to send 
                myProcessStartInfo.Arguments = scriptlocation + " " + arguments.Aggregate((a, b) => a + " " + b);

                Console.WriteLine("Calling Python script {0} with arguments {1}", scriptlocation, arguments);

                myProcess = Process.Start(myProcessStartInfo);

                //myProcess = new Process();
                // assign start information to the process 
                //myProcess.StartInfo = myProcessStartInfo;

                // start the process 
                //myProcess.Start();

                // Read the standard output of the app we called.  
                // in order to avoid deadlock we will read output first 
                // and then wait for process terminate: 

                if (myProcess != null)
                {
                    using (StreamReader reader = myProcess.StandardOutput)
                    {
                        sb.AppendFormat(reader.ReadToEnd());
                    }
                    success = true;

                    // wait exit signal from the app we called and then close it. 
                    myProcess.WaitForExit();
                    Console.WriteLine("Process exit code: {0}", myProcess.ExitCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred: {0}", ex.Message);
            }
            finally
            {
                myProcess?.Close();
            }

            // write the output we got from python app 
            if (success)
                Console.WriteLine("Value received from script: " + sb);
        }

        public static void RunPythonScript(string cmd, string args)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "my/full/path/to/python.exe";
            start.Arguments = string.Format("{0} {1}", cmd, args);
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            using (Process process = Process.Start(start))
            {
                if (process != null)
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = reader.ReadToEnd();
                        Console.Write(result);
                    }
                }
            }
        }
    }
}
