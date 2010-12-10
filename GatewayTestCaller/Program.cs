using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CSTA;
using System.Threading;
//using SpeechLib;
using System.Windows.Forms;
using GatewayTestLibrary;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using EdbQa.Common.CDSHelper;

namespace GatewayTestCaller
{
    class Driver
    {
        private int currIter;               // Current iteration
        private Caller caller;              // Caller instance
        private Logger resultLogger;        // Instance of logger
        private static Driver drv = null;   // Its own instance
        
        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="cp"></param>
        private Driver(ConfigParameters cp)
        {
            resultLogger = Logger.getInstance(cp.logFileName);

            if (resultLogger == null)
            {
                throw new System.IO.IOException("Could not open the result file for writing");
            }

            caller = new Caller(cp);
      //      caller.OnStateChanged += new Caller.StateChangedEventHandler(caller_OnStateChanged);
            caller.OnIterationCompleted += new Caller.IterationCompletedEventHandler(caller_OnIterationCompleted);
            currIter = 0;
        }

        /// <summary>
        /// Event handler invoked when one call iteration is completed. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="iInfo"></param>
        void caller_OnIterationCompleted(object sender, IterationInfo iInfo)
        {
            currIter++;

            resultLogger.writeLog(iInfo.ToString());
            Console.WriteLine("Logging result for iteration " + currIter);
        }

        ///// <summary>
        ///// Event handler invoked when caller changes its state
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="state"></param>
        //void caller_OnStateChanged(object sender, CurrentState state)
        //{
        //    Console.WriteLine("State = " + state.ToString());

        //    if (state == CurrentState.EXECUTION_COMPLETED)
        //    {
        //        Console.WriteLine("Execution completed.. Exiting");
        //        Environment.Exit(ReturnCode.SUCCESS);
        //    }
        //}

        /// <summary>
        /// Method to obtain an instance of driver. Driver and BargingCaller are only created if 
        /// result file can be created
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public static Driver getInstance(ConfigParameters cp, out int retCode)
        {
            retCode = 0;

            try
            {
                if(drv == null)
                drv = new Driver(cp);
            }
            catch (COMException ce)
            {
                Console.WriteLine("COMException in Driver.getInstance : " + ce.Message);
                Trace.WriteLine("COMException in Creating Caller. Message " + ce.Message, "Error");
                retCode = ReturnCode.COM_EXCEPTION_CREATION;
                return null;
            }
            catch (IOException ie)
            {
                Console.WriteLine("IOException in Driver.getInstance : " + ie.Message);
                Trace.WriteLine("IOException in Creating Caller. Message " + ie.Message, "Error");
                retCode = ReturnCode.IO_EXCEPTION;
                return null;
            }

            catch (Exception e)
            {
                Console.WriteLine("Exception in Driver.getInstance : " + e.Message);
                Trace.WriteLine("Exception in creating Caller. Message " + e.Message, "Error");
                retCode = ReturnCode.EXCEPTION_CREATION;
                return null;
            }
            return drv;
        }

        /// <summary>
        /// Method to dial a call
        /// </summary>
        private void dial()
        {
            caller.placeCall();
        }

        /// <summary>
        /// Method to start the test case
        /// </summary>
        public void runTestCase()
        {
            CurrentState state;
         
            state = caller.getState();

            if (state == CurrentState.READY)
            {
                Console.WriteLine("Dialing now");
                dial();
            }
        }
    }

    /// <summary>
    /// The class that encapsulates "Main"
    /// </summary>
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Creating Caller");

            try
            {
                // Create firewall exception
                Utilities.BypassFirewall("GatewayTestCaller");
            }
            catch (Exception e)
            {

            }

            ConfigParameters cp = null;
            int retCode = 0;

            /**
             * If the input parameters could not be validated, exit with return code -2.
             */
            if (false == InputValidator.validate(args, out cp))
            {
                Console.WriteLine("Bad Input Parameters. Press any key to exit");
      //          Console.ReadKey(true);
                Environment.Exit(ReturnCode.BAD_INPUT_PARAMETERS);
            }
            // Enable Tracing
            StreamWriter writer = new StreamWriter(args[5] + "\\GatewayTestCallerLog.txt", false);
            Trace.Listeners.Add(new TextWriterTraceListener(writer));
            Trace.AutoFlush = true;

            Driver t = Driver.getInstance(cp, out retCode);

            if (t != null)
            {
                try
                {
                    t.runTestCase();

                    Application.Run();
                }
                catch (Exception e)
                {
                    Trace.TraceError("Exception thrown: " + e.Message + " " + e.StackTrace + " Time = " + DateTime.Now);
                }
            }
            else
            {
                Environment.Exit(retCode);
            }
         }
    }
}
