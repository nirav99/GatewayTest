using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using System.ComponentModel;
using ResultAnalyzer;
using GatewayTestLibrary;

namespace GatewayTestDriver
{
    /// <summary>
    /// Class that spawns caller and callee processes for echo testing of ATAs
    /// </summary>
    class GatewayTestDriver
    {
        private Process callerProcess;                          // Handle of caller process
        private Process calleeProcess;                          // Handle of callee process

        private string callerExtension;
        private string calleeExtension;

        private System.Timers.Timer calleeTerminationTimer;     // Timer to terminate callee sometime after caller terminates
        private System.Timers.Timer callerTerminationTimer;     // Timer to terminate caller sometime after callee terminates
        private Analyzer analyzer;                              // To analyze test results

        private string configFileName;                          // Configuration file to read for the test
        #region Members to create Analyzer
        private string callerResultFile;                        // Path of caller result file
        private string calleeResultFile;                        // path of callee result file
        private string calleeGrammarFile;                       // Path of callee's grammar file
        private string dirName;                                 // Name of directory where results, recorded files and trace logs are to be stored
        private WavFileInfo wInfo;                              // Instance containing information about files that callee can recognize
        #endregion

        /// <summary>
       /// Class constructor
       /// </summary>
        public GatewayTestDriver(string callerExeName, string calleeExeName, string sipServer, string callerExtension, string calleeExtension, string extnToDial, string callerGrammarFile, string calleeGrammarFile, string callerResultFile, string calleeResultFile, string[] callerWavFile, string calleeWavFile, WavFileInfo _wInfo, string _dirName, string _configFileName)
        {
            this.calleeExtension = calleeExtension;
            this.callerExtension = callerExtension;
            this.configFileName = _configFileName;

            this.dirName = _dirName;
            string calleeArguments;
            StringBuilder callerArguments = new StringBuilder();
            
            #region Code segment to initialize caller process
            callerProcess = new Process();
            callerProcess.StartInfo.FileName = callerExeName;
            callerProcess.StartInfo.UseShellExecute = true;
            callerProcess.StartInfo.RedirectStandardError = false;
            callerProcess.StartInfo.RedirectStandardOutput = false;

            callerArguments.Append(sipServer + " " + callerExtension + " " + extnToDial + " " + callerGrammarFile + " " + callerResultFile + " " + dirName + " " + configFileName);

      //      callerArguments.Append(numIterations);

            for (int i = 0; i < callerWavFile.Length; i++)
            {
                callerArguments.Append(" ");
                callerArguments.Append(callerWavFile[i]);
            }

            callerProcess.StartInfo.Arguments = callerArguments.ToString();

            callerProcess.Exited += new EventHandler(callerProcess_Exited);
            #endregion

            #region Code segment to initialize callee process
            calleeProcess = new Process();
            calleeProcess.StartInfo.FileName = calleeExeName;
            calleeProcess.StartInfo.UseShellExecute = true;
            calleeProcess.StartInfo.RedirectStandardError = false;
            calleeProcess.StartInfo.RedirectStandardOutput = false;

            calleeArguments = sipServer + " " + calleeExtension + " " + callerExtension + " " + calleeGrammarFile + " " + calleeResultFile + " " + dirName + " " + configFileName + " " + calleeWavFile;
            calleeProcess.StartInfo.Arguments = calleeArguments;
            calleeProcess.Exited += new EventHandler(calleeProcess_Exited);
            #endregion

            calleeTerminationTimer = new System.Timers.Timer();
            calleeTerminationTimer.Enabled = false;
            calleeTerminationTimer.Elapsed +=new ElapsedEventHandler(calleeTerminationTimer_Elapsed);
            calleeTerminationTimer.Interval = 30000;    // Set it to fire after 30 seconds once enabled

            callerTerminationTimer = new System.Timers.Timer();
            callerTerminationTimer.Enabled = false;
            callerTerminationTimer.Elapsed += new ElapsedEventHandler(callerTerminationTimer_Elapsed);
            callerTerminationTimer.Interval = 20000;    // Set it to fire after 20 seconds once enabled

            this.calleeResultFile = calleeResultFile;
            this.callerResultFile = callerResultFile;
            this.calleeGrammarFile = calleeGrammarFile;

            wInfo = _wInfo;
            analyzer = null;
        }

        /// <summary>
        /// Method to start caller and callee
        /// </summary>
        public void startTest()
        {
            IntPtr calleeRegistrationHandler;
            IntPtr callerRegistrationHandler;       
            
            string calleeEventName = calleeExtension + " REGISTERED";   // Name of event for callee's registration
            string callerEventNamne = callerExtension + " REGISTERED";  // Name of event for caller's registration

            int registrationTimeout = 5 * 60 * 1000;    // Set registration timeout to 5 minutes
            int actualRegistrationTime = 0;             // Actual registration timeout

            // Create custom events to receive registration events
            calleeRegistrationHandler = Win32CustomEvent.createCustomEvent(calleeEventName);
            callerRegistrationHandler = Win32CustomEvent.createCustomEvent(callerEventNamne);

            Console.WriteLine("Starting Callee process");
            #region Code segment to start Callee process
            try
            {
                calleeProcess.Start();
            }
            catch(InvalidOperationException ie)
            {
                Console.WriteLine("GatewayTestDriver.startTest : Error in starting Callee Process\n\tMessage : " + ie.Message);
                Trace.TraceError("GatewayTestDriver.startTest : Error in starting Callee Process\n\tMessage : " + ie.Message);
                return;
            }catch(Win32Exception we)
            {
                Console.WriteLine("GatewayTestDriver.startTest : Win32 exception in starting Callee Process\n\tMessage : " + we.Message);
                Trace.TraceError("GatewayTestDriver.startTest : Win32 exception in starting Callee Process\n\tMessage : " + we.Message);
                return;
            }
            #endregion

            /**
             * Wait to receive registration event from the callee process for the specified timeout.
             * If the registration interval is not received within the specified timeout, kill the callee process
             * and exit
             */
            actualRegistrationTime = Win32CustomEvent.waitForCustomEvent(calleeRegistrationHandler, registrationTimeout);

            if (actualRegistrationTime != 0)
            {
                Console.WriteLine("Callee could not register within the specified timeout.");
                Trace.WriteLine("Callee could not register within the specified timeout.", "Registration_Timeout");
                if (calleeProcess.HasExited == false)
                {
                    calleeProcess.Kill();
                }
                return;
            }

            Console.WriteLine("Starting Caller process");
            #region Code segment to start Caller process
            try
            {
               callerProcess.Start();
            }
            catch (InvalidOperationException ie2)
            {
                Console.WriteLine("GatewayTestDriver.startTest : Error in starting Caller Process\n\tMessage : " + ie2.Message);
                Trace.TraceError("GatewayTestDriver.startTest : Error in starting Caller Process\n\tMessage : " + ie2.Message);
                calleeProcess.Kill();
                return;
            }
            catch (Win32Exception we2)
            {
                Console.WriteLine("GatewayTestDriver.startTest : Win32 exception in starting Caller Process\n\tMessage : " + we2.Message);
                Trace.TraceError("GatewayTestDriver.startTest : Win32 exception in starting Caller Process\n\tMessage : " + we2.Message);
                calleeProcess.Kill();
                return;
            }
            #endregion

            /**
             * Wait to receive registration event from the caller process for the specified timeout.
             * If the registration interval is not received within the specified timeout, kill the caller process, the callee process
             * and exit
             */
            actualRegistrationTime = Win32CustomEvent.waitForCustomEvent(callerRegistrationHandler, registrationTimeout);

            if (actualRegistrationTime != 0)
            {
                Console.WriteLine("Caller could not register within the specified timeout");
                Trace.WriteLine("Caller could not register within the specified timeout", "Registration_Timeout");

                if (callerProcess.HasExited == false)
                {
                    callerProcess.Kill();
                }
                calleeProcess.Kill();
                return;
            }

            Console.WriteLine("Waiting for Caller and Callee processes to complete execution...");
            /**
             * This means that both processes have successfully started. 
             * Wait till both end.
             */
            while(callerProcess.HasExited == false && calleeProcess.HasExited == false)
            {
                Thread.Sleep(10000);
            }

            terminateCaller();
            terminateCallee();
            
            while (callerProcess.HasExited == false)
            {
                Thread.Sleep(5000);
            }
            
            while (calleeProcess.HasExited == false)
            {
                Thread.Sleep(5000);
            }

            /**
             * Now analyze the results from the caller and the callee
             */
            if (callerProcess.ExitCode == ReturnCode.BAD_INPUT_PARAMETERS || calleeProcess.ExitCode == ReturnCode.BAD_INPUT_PARAMETERS)
            {
                Console.WriteLine("Incorrect parameters supplied to Caller or Callee. Exiting...");
                return;
            }

            if (callerProcess.ExitCode == ReturnCode.IO_EXCEPTION || calleeProcess.ExitCode == ReturnCode.IO_EXCEPTION)
            {
                Console.WriteLine("IOException in either Caller or Callee. Exiting...");
                return;
            }

            if (callerProcess.ExitCode == ReturnCode.ERROR_OPENING_RESULT_FILE || calleeProcess.ExitCode == ReturnCode.ERROR_OPENING_RESULT_FILE)
            {
                Console.WriteLine("Caller or Callee could not open the specified result file for writing. Exiting...");
                return;
            }

            if (callerProcess.ExitCode == ReturnCode.NOT_REGISTERED_WITHIN_TIMEOUT || calleeProcess.ExitCode == ReturnCode.NOT_REGISTERED_WITHIN_TIMEOUT)
            {
                Console.WriteLine("Caller or Callee could not get registered with the server within specified interval. Exiting...");
                return;
            }

            if (callerProcess.ExitCode == ReturnCode.FAILED_REGISTRATION || calleeProcess.ExitCode == ReturnCode.FAILED_REGISTRATION)
            {
                Console.WriteLine("Caller or Callee's Registration failed. Exiting...");
                return;
            }

            if (callerProcess.ExitCode == ReturnCode.COM_EXCEPTION_CREATION || calleeProcess.ExitCode == ReturnCode.COM_EXCEPTION_CREATION)
            {
                Console.WriteLine("COMException in creating Caller or Callee process. Exiting...");
                return;
            }

            if (callerProcess.ExitCode == ReturnCode.EXCEPTION_CREATION || calleeProcess.ExitCode == ReturnCode.EXCEPTION_CREATION)
            {
                Console.WriteLine("Exception in creating Caller or Callee process. Exiting...");
                return;
            }

            Console.WriteLine("Generating report...");
            analyzeResult();
        }

        /// <summary>
        /// Event handler called when caller process terminates
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void calleeProcess_Exited(object sender, EventArgs e)
        {
            Console.WriteLine("Callee exited");
        }

        /// <summary>
        /// Event handler called when callee process terminates
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void callerProcess_Exited(object sender, EventArgs e)
        {
            Console.WriteLine("Caller exited");
        }

        /// <summary>
        /// Timer event handler fired after specified time interval after caller ends. It is used to kill
        /// callee if it has not already ended.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void  calleeTerminationTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (calleeProcess.HasExited == false)
                    calleeProcess.Kill();
                calleeTerminationTimer.Enabled = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Method calleeTerminationTimer_Elapsed Exception  : " + ex.Message);
                Trace.TraceError("Method calleeTerminationTimer_Elapsed Exception  : " + ex.Message);
            }
        }

        /// <summary>
        /// Timer event handler fired after specified time interval after caller ends. It is used to kill
        /// caller if it has not already ended.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void callerTerminationTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (callerProcess.HasExited == false)
                    callerProcess.Kill();
                callerTerminationTimer.Enabled = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Method callerTerminationTimer_Elapsed Exception  : " + ex.Message);
                Trace.TraceError("Method callerTerminationTimer_Elapsed Exception  : " + ex.Message);
            }
        }

        /// <summary>
        /// Method to kill callee if it has not already ended. This method enables callee termination timer.
        /// The timer event fires after the specified duration and kills the process if it did not end.
        /// </summary>
        private void terminateCallee()
        {
            calleeTerminationTimer.Enabled = true;
        }

        /// <summary>
        /// Method to kill caller if it has not already ended. This method enables caller termination timer.
        /// The timer event fires after the specified duration and kills the process if it did not end.
        /// </summary>
        private void terminateCaller()
        {
            callerTerminationTimer.Enabled = true;
        }

        /// <summary>
        /// Method that instantiates the analyzer.
        /// </summary>
        private void analyzeResult()
        {
            analyzer = null;
            int numTries = 10;

            /**
             * In order to account for the delay in closing the result files, this method attempts to instantiate the object
             * 10 times, waiting 10 seconds between each iteration. If the object gets instantiated, the results are generated,
             * else a suitable error message is shown.
             */
            
            for (int i = 0; i < numTries && analyzer == null; i++)
            {
                Console.WriteLine("Instantiating Analyzer...");
                Thread.Sleep(10000);
                analyzer = Analyzer.getInstance(callerResultFile, calleeResultFile, wInfo, dirName);
            }
               
            if (analyzer != null)
                analyzer.generateResults();
            else
            {
                Console.WriteLine("Error in creating analyzer. Test results cannot be analyzed");
                Trace.TraceError("Error in creating analyzer. Test results cannot be analyzed");
            }
        }
    }
}
