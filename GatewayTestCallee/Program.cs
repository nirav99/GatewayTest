using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CSTA;
using System.Threading;
using System.Windows.Forms;
using GatewayTestLibrary;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using EdbQa.Common.CDSHelper;

namespace GatewayTestCallee
{
    class Driver
    {
        private int iteration;              // Current iteration
        private Callee callee;              // Callee object
        private Logger resultLogger;        // Logger to log the results
        private static Driver drv = null;   // Its own instance

        private Driver(ConfigParameters cp)
        {
            resultLogger = Logger.getInstance(cp.logFileName);

            if (resultLogger == null)
            {
                throw new System.IO.IOException("Could not open the result file for writing");
            }

            callee = new Callee(cp);
            callee.OnIterationCompleted += new Callee.IterationCompletedEventHandler(callee_OnIterationCompleted);
    //        callee.OnStateChanged += new Callee.StateChangedEventHandler(callee_OnStateChanged);
            Console.WriteLine("Callee created");

            iteration = 0;
        }

        ///// <summary>
        ///// Event handler that observes changes in state of callee
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="state"></param>
        //void callee_OnStateChanged(object sender, CurrentState state)
        //{
        //    Console.WriteLine("State = " + state.ToString());

        //    if (state == CurrentState.EXECUTION_COMPLETED)
        //    {
        //        Console.WriteLine("Execution completed.. Exiting");
        //        Environment.Exit(0);
        //    }
        //}

        /// <summary>
        /// Event handler that logs the iteration result to the file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="iInfo"></param>
        void callee_OnIterationCompleted(object sender, IterationInfo iInfo)
        {
            Console.WriteLine("Logging result for iteration " + ++iteration);

            resultLogger.writeLog(iInfo.ToString());
        }

        /// <summary>
        /// Method to obtain an instance of driver. Driver and BargedCallee are only created if 
        /// result file can be created
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public static Driver getInstance(ConfigParameters cp, out int retCode)
        {
            retCode = 0;

            try
            {
                if (drv == null)
                    drv = new Driver(cp);
            }
            catch (COMException ce)
            {
                Console.WriteLine("COMException in Driver.getInstance : " + ce.Message);
                Trace.WriteLine("COMException in Creating Callee. Message " + ce.Message, "Error");
                retCode = ReturnCode.COM_EXCEPTION_CREATION;
                return null;
            }
            catch (IOException ie)
            {
                Console.WriteLine("IOException in Driver.getInstance : " + ie.Message);
                Trace.WriteLine("IOException in Creating Callee. Message " + ie.Message, "Error");
                retCode = ReturnCode.IO_EXCEPTION;
                return null;
            }

            catch (Exception e)
            {
                Console.WriteLine("Exception in Driver.getInstance : " + e.Message);
                Trace.WriteLine("Exception in creating Callee. Message " + e.Message, "Error");
                Trace.WriteLine("Stack Trace : " + e.StackTrace);
                retCode = ReturnCode.EXCEPTION_CREATION;
                return null;
            }
            return drv;
        }
    }

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Creating Callee...");

            try
            {
                // Create firewall exception
                Utilities.BypassFirewall("GatewayTestCallee");
            }
            catch (Exception e)
            {

            }

            ConfigParameters cp = null;
            Driver t = null;
            int retCode = 0;

            if (false == InputValidator.validate(args, out cp))
            {
                Trace.WriteLine("Bad Input Parameters. Press any key to exit");
              

                Console.WriteLine("You entered");

                foreach (string s in args)
                    Console.WriteLine(s);
                Console.ReadKey(true);
                Environment.Exit(ReturnCode.BAD_INPUT_PARAMETERS);
            }

            // Enable Tracing
            StreamWriter writer = new StreamWriter(args[5] + "\\GatewayTestCalleeLog.txt", false);
            Trace.Listeners.Add(new TextWriterTraceListener(writer));
            Trace.AutoFlush = true;

            t = Driver.getInstance(cp, out retCode);

            if (t == null)
            {
                Environment.Exit(retCode);
            }
            else
            {
                Application.Run();
            }
        }
    }
}
