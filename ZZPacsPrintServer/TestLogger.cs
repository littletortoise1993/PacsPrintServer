using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace ZZPacsPrintServer
{
    public enum LogType
    {
        All,
        Information,
        Debug,
        Success,
        Failure,
        Warning,
        Error
    }
    public class TestLogger
    {
        #region Instance
        private static object logLock;

        private static TestLogger _instance;

        private static string logFileName;
        private TestLogger() { }

        /// <summary>
        /// Logger instance
        /// </summary>
        public static TestLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TestLogger();
                    logLock = new object();                    
                }
                logFileName = DateTime.Now.ToString("yyyyMMdd") + ".log";
                return _instance;
            }
        }
        #endregion

        /// <summary>
        /// Write log to log file
        /// </summary>
        /// <param name="logContent">Log content</param>
        /// <param name="logType">Log type</param>
        public void WriteLog(string logContent, LogType logType = LogType.Information, string fileName = null)
        {
            try
            {
                string basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                basePath = basePath + @"\Log";
                if (!Directory.Exists(basePath))
                {
                    Directory.CreateDirectory(basePath);
                }

                //string dataString = DateTime.Now.ToString("yyyy-MM-dd");
                //if (!Directory.Exists(basePath + "\\" + dataString))
                //{
                //    Directory.CreateDirectory(basePath + "\\" + dataString);
                //}

                string[] logText = new string[] { DateTime.Now.ToString("hh:mm:ss:fff") + ": " + logType.ToString() + ": " + logContent };
                if (!string.IsNullOrEmpty(fileName))
                {
                    fileName = fileName + "_" + logFileName;
                }
                else
                {
                    fileName = logFileName;
                }
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
                lock (logLock)
                {
                    File.AppendAllLines(basePath + "\\"  + fileName, logText);
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Write exception to log file
        /// </summary>
        /// <param name="exception">Exception</param>
        public void WriteException(Exception exception, string specialText = null)
        {
            if (exception != null)
            {
                Type exceptionType = exception.GetType();
                string text = string.Empty;
                if (!string.IsNullOrEmpty(specialText))
                {
                    text = text + specialText + Environment.NewLine;
                }
                text = "Exception: " + exceptionType.Name + Environment.NewLine;
                text += "               " + "Message: " + exception.Message + Environment.NewLine;
                text += "               " + "Source: " + exception.Source + Environment.NewLine;
                text += "               " + "StackTrace: " + exception.StackTrace + Environment.NewLine;
                WriteLog(text, LogType.Error);
            }
        }
    }
}
