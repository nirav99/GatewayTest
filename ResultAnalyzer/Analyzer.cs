using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.IO;
using System.Xml;
using GatewayTestLibrary;
using System.Diagnostics;

namespace ResultAnalyzer
{
    /// <summary>
    /// This class acts as a facade for the subsystem that reads the caller and callee result files
    /// and computes the results
    /// </summary>
    public class Analyzer
    {
       // private WavFileValidator wavValidator;    // To validate grammar/text equivalent of wav files
        private WavFileInfo wInfo;                  // To find out which files can be recognized by callee
        private ResultInterpreter ri = null;       // Instance of result interpreter class
        private AggregateResult aggResult = null;   // To compute and store overall test results       
        private StreamReader callerFileReader;      // To read caller file
        private StreamReader calleeFileReader;      // To read callee file
        public string resultDir;                   // Location of result directory
        private int iterationNum;                   // Iteration number

        private int currentCallerLineNum = 0;       // Current record processed from caller file
        private int currentCalleeLineNum = 0;       // Current record processed from callee file

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="_callerLog"></param>
        /// <param name="_calleeLog"></param>
        /// <param name="wavValidator"></param>
        private Analyzer(string _callerLog, string _calleeLog, WavFileInfo _wavInfo, string _resultDir)
        {
            try
            {
                callerFileReader = new StreamReader(_callerLog);
                calleeFileReader = new StreamReader(_calleeLog);
                this.resultDir = _resultDir;
            }
            catch (Exception e)
            {
                if (callerFileReader != null)
                    callerFileReader.Close();
                if (calleeFileReader != null)
                    calleeFileReader.Close();

                throw new Exception(e.Message);
            }
            this.wInfo = _wavInfo;
            ri = new ResultInterpreter(wInfo);
            aggResult = new AggregateResult(this);
            iterationNum = 0;
        }

        /// <summary>
        /// Method to return an instance of Analyzer
        /// </summary>
        /// <param name="_callerLog"></param>
        /// <param name="_calleeLog"></param>
        /// <param name="_wavValidator"></param>
        /// <returns></returns>
        public static Analyzer getInstance(string _callerLog, string _calleeLog, WavFileInfo _wavInfo, string _resultDir)
        {
            Analyzer a = null;
            try
            {
                a = new Analyzer(_callerLog, _calleeLog, _wavInfo, _resultDir);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in creating analyzer instance. Message:\n" + e.Message);
                a = null;
            }
            return a;
        }

        /// <summary>
        /// Method that reads the corresponding callee and caller log files and finds out which lines of caller
        /// and callee belong to same call and processes them
        /// </summary>
        public void generateResults()
        {
            string callerLine;
            string calleeLine;
            CallerIterationInfo callerInfo = null;      // Caller's iteration info
            CalleeIterationInfo calleeInfo = null;      // Callee's iteration info
            int returnCode;
            bool calleeFileEmpty = false;

            while (true)
            {
                // If callerInfo is null, read the next line and create a callerInfo from it.
                // If callerFile is empty or exception in creating callerInfo, break.
                if (callerInfo == null)
                {
                    callerLine = callerFileReader.ReadLine();
                    currentCallerLineNum++;

                    if (callerLine == null)
                    {
                        break;
                    }
                    else
                    {
                        try
                        {
                            callerInfo = new CallerIterationInfo(callerLine);
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine("Error in Analyer.generateResults. Could not create CallerIterationInfo instance. Terminating reading of input logs");
                            Trace.TraceError("Exception while creating callerInfo: " + e.Message + "\r\nStack Trace : " + e.StackTrace);
                            break;
                        }
                    }
                }

                // If calleeInfo is null, read the next line and create a calleeInfo from it.
                // If calleeFile is empty then create a dummy callee object. Exception in creating calleeInfo, break.
                if (calleeInfo == null)
                {
                    calleeLine = calleeFileReader.ReadLine();
                    currentCalleeLineNum++;

                    if (calleeLine == null)
                    {
                        calleeFileEmpty = true;
                        calleeInfo = new CalleeIterationInfo();
                    }
                    else
                    {
                        try
                        {
                            calleeInfo = new CalleeIterationInfo(calleeLine);
                        }
                        catch(Exception e2)
                        {
                            Console.WriteLine("Error in Analyer.generateResults. Could not create CalleeIterationInfo instance. Terminating reading of input logs");
                            Trace.TraceError("Exception while creating calleeInfo: " + e2.Message + "\r\nStack Trace : " + e2.StackTrace);
                            break;
                        }
                    }
                }

                if (!calleeFileEmpty)
                {
                    returnCode = causalOrderBetweenCallerAndCallee(callerInfo, calleeInfo);

                    switch (returnCode)
                    {
                        case 0: // both belong to same call
                        case -3: // both have uninitialized connect timestamps
                            processTokens(callerInfo, calleeInfo, true);
                            callerInfo = null;
                            calleeInfo = null;
                            break;

                        case 1: // caller's current call after callee's call
                        case -2: // callee's current call had uninitialized connection timestamp
                            // discard calleeInfo as it could not be matched with caller
                            // continue to store callerInfo
                            calleeInfo = null;
                            break;

                        case 2: // callee's current call after caller's call
                        case -1: // caller's current call had uninitialized conneciton timestamp
                            // processTokens with callerInfo and dummy calleeInfo
                            // and discard callerInfo
                            processTokens(callerInfo, new CalleeIterationInfo(), false);
                            callerInfo = null;
                            break;
                    }
                }
                else
                {
                    processTokens(callerInfo, new CalleeIterationInfo(), false);
                    callerInfo = null;
                }
            }
            aggResult.displayResult(resultDir + "\\GatewayTestResults.txt");
        }

        /// <summary>
        /// Method to determine causal ordering between caller and callee's current call
        /// </summary>
        /// <param name="callerInfo"></param>
        /// <param name="calleeInfo"></param>
        /// <returns></returns>
        private int causalOrderBetweenCallerAndCallee(CallerIterationInfo callerInfo, CalleeIterationInfo calleeInfo)
        {
            DateTime uninitDate = new DateTime(); // Uninitialized date
            int result = -1;
            /**
             * If either caller or callee did not have valid connection time, return -1 to indicate that no matching is possible
             * with this caller callee pair
             */
            if (callerInfo.callConnectTime == uninitDate && calleeInfo.callConnectTime == uninitDate)
            {
                return -3;
            }
            else
            if(callerInfo.callConnectTime == uninitDate) 
            {
                result = -1;
            }
            else
            if (calleeInfo.callConnectTime == uninitDate)
            {
                result = -2;
            }
            else
                if ((callerInfo.callConnectTime <= calleeInfo.callConnectTime && calleeInfo.callConnectTime < callerInfo.callReleaseTime) ||
                    (calleeInfo.callConnectTime <= callerInfo.callConnectTime && callerInfo.callConnectTime < calleeInfo.callReleaseTime))
                {
                    /**
                     * If caller and callee belong to same call return 0
                     */
                    result = 0;
                }
                else
                    if (calleeInfo.callConnectTime >= callerInfo.callReleaseTime)
                    {
                        /**
                         * If callee's current call is causally after caller's current call, return 2.
                         */
                        result = 2;
                    }
                    else
                        if (calleeInfo.callReleaseTime <= callerInfo.callConnectTime)
                        {
                            /**
                             * If caller's current call is causally before caller's current call, return 2
                             */
                            result = 1;
                        }
            return result;
        }
        
        /// <summary>
        /// Private method that applies the token validation rules and generates iteration result instances
        /// </summary>
        /// <param name="callerInfo"></param>
        /// <param name="calleeInfo"></param>
        private void processTokens(CallerIterationInfo callerInfo, CalleeIterationInfo calleeInfo, bool matchRecordedWav)
        {
            
            IterationResult result = null;

            /**
             * If the current objects indeed belong to a same call, we can locate corresponding wav files and rename them
             * to ensure that the file from caller and the callee can be correlated.
             */
            if (matchRecordedWav == true)
            {
                string callerFile = resultDir + "\\Caller_RecordedWav_" + currentCallerLineNum + ".wav";
                string calleeFile = resultDir + "\\Callee_RecordedWav_" + currentCalleeLineNum + ".wav";
                string newCallerFile = resultDir + "\\Caller_RecordedWav_" + currentCallerLineNum + "_matched.wav";
                string newCalleeFile = resultDir + "\\Callee_RecordedWav_" + currentCallerLineNum + "_matched.wav";

                try
                {
                    if (File.Exists(callerFile) && File.Exists(calleeFile))
                    {
                        File.Move(callerFile, newCallerFile);
                        File.Move(calleeFile, newCalleeFile);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception encountered in matching " + callerFile + " with " + calleeFile + ". Message:\n" + e.Message);
                    Trace.TraceError("Exception occurred. Message : " + e.Message + "\r\nStack Trace : " + e.StackTrace);
                }
            }

            result = ri.applyValidationRules(callerInfo, calleeInfo);
                      
            Console.WriteLine("\nIteration = " + ++iterationNum + "\n" + result.ToString());
            aggResult.addIterationResult(result);
        }
     }
}
