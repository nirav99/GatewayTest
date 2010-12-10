using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace GatewayTestLibrary
{
    /// <summary>
    /// The class used to log the test result to specified log file
    /// </summary>
    public class Logger
    {
        private StreamWriter writer = null;        // Handler for log file
        private string logFileName = null;         // Log file path
        private static Logger l = null;            // Static reference of logger to create singleton instance
       
        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="_logFileName">Path of output file</param>
        private Logger(string _logFileName)
        {
            logFileName = _logFileName;
            writer = new StreamWriter(logFileName, false);
        }

        /// <summary>
        /// Return an instance of logger class. 
        /// </summary>
        /// <param name="_logFileName"></param>
        /// <returns>Instance of logger class</returns>
        public static Logger getInstance(string _logFileName)
        {
            if (l == null)
            {
                try
                {
                    l = new Logger(_logFileName);
                }
                catch (Exception e)
                {
                    return null;
                }
            }
            return l;
        }

        /// <summary>
        /// Method that appends the specified line to the log file
        /// </summary>
        /// <param name="line">line to be logged</param>
        /// <returns>One if line was written, zero if file was not opened or line was null, -1 if some error encountered in writing </returns>
        public int writeLog(string line)
        {
            lock (this)
            {
                if (writer != null && line != null)
                {
                    try
                    {
                        writer.WriteLine(line);
                        writer.Flush();
                        return 1;
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError("Exception in writing text = " + line + "\nMessage = " + e.Message + "\nStack Trace = " + e.StackTrace);
                        return -1;
                    }
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Method to close the file handle. Once closeFile is invoked, a new instance must be obtained in order to write to file.
        /// </summary>
        public void closeFile()
        {
            if (writer != null)
            {
                writer.Close();
                l = null;
                writer = null;
            }
        }
    }
}
