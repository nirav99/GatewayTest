using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using ResultAnalyzer;
using GatewayTestLibrary;
using System.Diagnostics;


namespace GatewayTestDriver
{
    class MainProgram : InputValidatorBase
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                TestParameters.printUsage();
            }
            else
            {
                TestParameters testParams = null;

                CDSWrapper cdsWrapper = null;

                string actualCallerExtension = null; // Actual caller extension
                string actualCalleeExtension = null; // Actual callee extension

                bool changeCallDistroPlan = false;   // Whether to change call distribution plan
                StreamWriter configFileWriter = null;// To write config file
                string configFileName = null;

                testParams = TestParameters.getInstance(args);

                if (testParams == null)
                {
                    //            TestParameters.printUsage();
                    Environment.Exit(-2);
                }

                cdsWrapper = new CDSWrapper(testParams.serverIP);

                /**
                 * If the user specified Caller and Callee extensions, then attempt to create those extensions. However,
                 * if these extensions are already created, fail and stop. If these extensions are not created, create them.
                 * When user specifies these extensions, they have to configure ATA to forward the calls to the callee extension. Thus, don't change
                 * the call distribution plan in this case.
                 * 
                 * If the user does not specify the caller and callee extensions, then find two extensions that are not already created and use them.
                 * In this case, change the call distribution plan to forward all the incoming calls to the callee extension.
                 */

                if (testParams.calleeExt != null && testParams.callerExt != null)
                {
                    changeCallDistroPlan = false;
                }
                else
                {
                    changeCallDistroPlan = true;
                }

                if (false == cdsWrapper.createExtensionsAndPhone(testParams.callerExt, testParams.calleeExt, out actualCallerExtension, out actualCalleeExtension))
                {
                    Console.WriteLine("Could not create a phone with caller and callee extensions. Exiting...");
                    Environment.Exit(-1);
                }

                testParams.calleeExt = actualCalleeExtension;
                testParams.callerExt = actualCallerExtension;

                if (changeCallDistroPlan == true)
                {
                    if (false == cdsWrapper.changeCallDistributionPlan(actualCalleeExtension))
                    {
                        Console.WriteLine("Could not change server's call distribution plan. Exiting...");
                        cdsWrapper.cleanupTest();
                        Environment.Exit(-1);
                    }
                }

                #region Write up all the test configuration parameters to a file and pass that file name as a parameter to GatewayTestDriver

                try
                {
                    // ChadO: 09-02-08
                    // Changing the naming process to now match the directory name where the results are stored.
                    // Otherwise, the params file is almost - but not quite - the same name as the results directory
                    // that contains results based on the information from the params file.  It makes much more sense
                    // for it to be a package deal and the params filename to match the results directory name.
                    string sTemp = testParams.resultDir.Substring(testParams.resultDir.IndexOf('_'));
                    configFileName = "GatewayTestConfigParams" + sTemp + ".txt";
                    configFileWriter = new StreamWriter(configFileName, false);
                    configFileWriter.Write(testParams.ToString());
                    configFileWriter.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error in creating or writing to config file. Exception message = " + e.Message);
                }
                #endregion

                try
                {
                    //Create a trace instance here.
                    StreamWriter writer = new StreamWriter(testParams.resultDir + "\\GatewayTestDriverLog.txt", false);
                    Trace.Listeners.Add(new TextWriterTraceListener(writer));
                    Trace.AutoFlush = true;

                    //TODO: Right now, callee only plays the first wav file from its set. 
                    // If needed modify GatewayTestCallee.cs and GatewayTestDriver.cs to handle list of filenames that callee can play
                    GatewayTestDriver driver = new GatewayTestDriver("GatewayTestCaller.exe",   // Caller's EXE
                                                                "GatewayTestCallee.exe",        // Callee's EXE
                                                                testParams.serverIP,            // SIP Server
                                                                actualCallerExtension,          // Caller Extension
                                                                actualCalleeExtension,          // Callee Extension
                                                                testParams.numToDial,           // Number for caller to call to reach callee
                                                                "GatewayTestCaller.xml",        // Caller Grammar
                                                                "GatewayTestCallee.xml",        // Callee Grammar
                                                                testParams.callerResultFile,    // Caller Result
                                                                testParams.calleeResultFile,    // Callee Result
                                                                testParams.callerWavFiles,      // List of wav files for caller
                                                                testParams.calleeWavFiles[0],   // Wav file for callee - selected as first wav file
                                                                testParams.wInfo,               // For use with RuleValidator and Analyzer
                                                                testParams.resultDir,           // Test result directory name
                                                                configFileName                  // Configuration file for the test
                                                               );

                    Console.WriteLine("Creating Caller at extension : " + testParams.callerExt);
                    Console.WriteLine("Creating Callee at extension : " + testParams.calleeExt);
                    Console.WriteLine("Extension to dial to reach Callee : " + testParams.numToDial);

                    Console.WriteLine();
                    Console.WriteLine();

                    driver.startTest();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception encountered : " + e.Message);
                    Trace.TraceError("Exception : " + e.Message + "\r\nStack Trace : " + e.StackTrace);
                }
                finally
                {
                    // Delete the extensions created in the test
                    cdsWrapper.cleanupTest();

                }

                // ChadO: 09-02-08
                // We actually want this file around!  Let's not delete it!
                /*
                try
                {
                    File.Delete(configFileName);
                }
                catch (Exception e)
                {

                }
                */
            }
        }
    }
}
