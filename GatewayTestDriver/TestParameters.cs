using System;
using System.Collections.Generic;
using System.Text;
using GatewayTestLibrary;
using System.Diagnostics;
using ResultAnalyzer;
using System.IO;

namespace GatewayTestDriver 
{
    /// <summary>
    /// Class encapsulating the test parameters
    /// </summary>
    public class TestParameters : InputValidatorBase
    {
        public bool callerRecording = true;     // Allow caller side recording
        public bool calleeRecording = true;     // Allow callee side recording
        public int interSetInterval = 120;      // Inter-set interval
        public int interCallInterval = 30;      // Inter-call interval
        public int callDuration = 70;           // Duration of call
        public int iterationsPerSet = 100;      // Iterations per set
        public int numSets = 3;                 // Number of sets
        public int minSpeechBargeInterval = 0;  // Min speech barge interval
        public int maxSpeechBargeInterval = 5;  // Max speech barge interval
        public string serverIP = null;          // Edinburgh server IP
        public string numToDial = null;         // Num caller dials to reach callee
        public string callerExt = null;         // Caller Extension
        public string calleeExt = null;         // Callee Extension
        public string resultDir = null;         // Directory to write results to
        public string callerResultFile = null;  // File name to store caller results
        public string calleeResultFile = null;  // File name to store callee results
        public string[] callerWavFiles = null;  // Wav files selected for the caller
        public string[] calleeWavFiles = null;  // Wav files for callee to play
        public WavFileInfo wInfo = null;        // Class containing information about wav files

        private Dictionary<string, string> parameters;  // Parameters passed from command line
        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="args"></param>
        private TestParameters(string[] args)
        {
            if (args == null || args.Length == 0)
                printUsage();
            else
             parseArguments(args);
           
        }

        public static TestParameters getInstance(string[] args)
        {
            TestParameters tp = null;
            try
            {
                tp = new TestParameters(args);

                if (tp.validate() == false)
                    tp = null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(-2);
            }
            return tp;
        }

        private void parseArguments(string[] args)
        {
            if (args == null)
            {
                printUsage();
            }

            bool arg = true;
            string argument = "";
            parameters = new Dictionary<string, string>();
            foreach (string next in args)
            {
                if (next.Equals("/?") || next.Equals("-?") || next.Equals("/h") || next.Equals("-h"))
                {
                    printUsage();
                    break;
                }
                if (arg)
                {
                    argument = next;
                    arg = false;
                }
                else if (!arg)
                {
                    if (!parameters.ContainsKey(argument.ToLower()))
                        parameters.Add(argument.ToLower(), next);
                    else
                        parameters[argument.ToLower()] = next;
                    argument = "";
                    arg = true;
                }
            }
        }

        /// <summary>
        /// Find a unique directory name to store test results and recorded wav files, and create the directory. If such a name
        /// cannot be obtained, return null
        /// </summary>
        /// <returns></returns>
        private string getUniqueDirectoryName()
        {
            string dirName = null;
            dirName = "GatewayTestResult_" + DateTime.Now.Year + "_" + DateTime.Now.Month + "_" + DateTime.Now.Day + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second + "_" + Process.GetCurrentProcess().Id;

            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }
            else
                dirName = null;
            return dirName;
        }

        private bool validate()
        {
            bool errorFound = false;

            if (parameters.ContainsKey("/serverip"))
            {
                serverIP = parameters["/serverip"];
            }

            if (parameters.ContainsKey("/callerext"))
                callerExt = parameters["/callerext"];

            if (parameters.ContainsKey("/calleeext"))
                calleeExt = parameters["/calleeext"];

            if (parameters.ContainsKey("/numtodial"))
                numToDial = parameters["/numtodial"];

            try
            {
                if (parameters.ContainsKey("/allowcallerrecording"))
                    callerRecording = Convert.ToBoolean(parameters["/allowcallerrecording"]);

                if (parameters.ContainsKey("/allowcalleerecording"))
                    calleeRecording = Convert.ToBoolean(parameters["/allowcalleerecording"]);
            }
            catch(InvalidCastException ice)
            {
                Console.WriteLine("Values for parameters \"CallerRecording\" and \"CalleeRecording\" should be True or False");
                errorFound = true;
            }

            try
            {
                if (parameters.ContainsKey("/intersetinterval"))
                    interSetInterval = Convert.ToInt32(parameters["/intersetinterval"]);

                if (parameters.ContainsKey("/intercallinterval"))
                    interCallInterval = Convert.ToInt32(parameters["/intercallinterval"]);

                if (parameters.ContainsKey("/callduration"))
                    callDuration = Convert.ToInt32(parameters["/callduration"]);

                if (parameters.ContainsKey("/iterationsperset"))
                    iterationsPerSet = Convert.ToInt32(parameters["/iterationsperset"]);

                if (parameters.ContainsKey("/numsets"))
                    numSets = Convert.ToInt32(parameters["/numsets"]);

                if (parameters.ContainsKey("/minspeechbargeinterval"))
                    minSpeechBargeInterval = Convert.ToInt32(parameters["/minspeechbargeinterval"]);

                if (parameters.ContainsKey("/maxspeechbargeinterval"))
                    maxSpeechBargeInterval = Convert.ToInt32(parameters["/maxspeechbargeinterval"]);
            }
            catch(Exception e)
            {
                errorFound = true;
                Console.WriteLine("Some or all of the following parameters are not a valid number.");
                Console.WriteLine("\tParameters /intersetinterval", "/intercallinterval", "/callduration", "/iterationsperset", "/numsets", "/minspeechbargeinterval", "/maxspeechbargeinterval should be non-negative numbers");
            }

            if (serverIP == null || validateIP(serverIP) == false)
            {
                Console.WriteLine("Invalid server IP address specified");
                errorFound = true;
            }
            else
            if (callerExt != null && calleeExt != null && callerExt.Equals(calleeExt, StringComparison.CurrentCultureIgnoreCase))
            {
                Console.WriteLine("Caller and callee should bind to different extensions");
                errorFound = true;
            }
            else
                if ((callerExt != null && calleeExt == null) || (callerExt == null && calleeExt != null))
                {
                    Console.WriteLine("If caller extension is specified, callee extension must be specified and vice versa");
                    errorFound = true;
                }
                else
                    if (numToDial == null)
                    {
                        Console.WriteLine("Number caller dials to reach callee not specified");
                        errorFound = true;
                    }
                    else
                        if (numToDial != null && callerExt != null && callerExt.Equals(numToDial, StringComparison.CurrentCultureIgnoreCase))
                        {
                            Console.WriteLine("Caller's extension should be different from the number caller uses to dial callee");
                            errorFound = true;
                        }

            if (!errorFound)
            {
                resultDir = getUniqueDirectoryName();

                if (resultDir == null)
                {
                    Console.WriteLine("Error in creating a unique directory to store results");
                    errorFound = true;
                }
            }

            if (!errorFound)
            {
                callerResultFile = resultDir + "\\" + "CallerResult.txt";
                calleeResultFile = resultDir + "\\" + "CalleeResult.txt";

                try
                {
                    StreamWriter w = new StreamWriter(callerResultFile, false);
                    w.Close();
                    StreamWriter w2 = new StreamWriter(calleeResultFile, false);
                    w2.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Caller or callee's result file cannot be opened for writing.");
                    errorFound = true;
                }
            }

            if (!errorFound)
            {
                if (selectWavFiles() == false)
                    errorFound = true;
            }
            return !errorFound;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            result.Append("COUNT_ITERATIONS=" + iterationsPerSet + "\r\n");
            result.Append("COUNT_SETS=" + numSets + "\r\n");
            result.Append("INTER_SET_INTERVAL=" + interSetInterval + "\r\n");
            result.Append("INTER_CALL_INTERVAL=" + interCallInterval + "\r\n");

            if (callerRecording == true)
                result.Append("CALLER_RECORD=YES\r\n");
            else
                result.Append("CALLER_RECORD=NO\r\n");

            if (calleeRecording == true)
                result.Append("CALLEE_RECORD=YES\r\n");
            else
                result.Append("CALLEE_RECORD=NO\r\n");

            result.Append("CALL_DURATION=" + callDuration + "\r\n");
            result.Append("MAX_SPEECH_BARGE_INTERVAL=" + maxSpeechBargeInterval + "\r\n");
            result.Append("MIN_SPEECH_BARGE_INTERVAL=" + minSpeechBargeInterval + "\r\n");
            result.Append("MAX_CALLEE_PROMPT_WAIT_INTERVAL=3");

            return result.ToString();
        }

        public static void printUsage()
        {
            Console.WriteLine("\r\nRecommended Usage Scenario: \r\n(when the test creates the required extensions and configures the server ");
            Console.WriteLine("to forward incoming calls to the callee extension)");
            Console.WriteLine();
            Console.WriteLine("GatewayTestDriver /ServerIP value /NumToDial value");
            Console.WriteLine("\t/ServerIP               - Server IP address");
            Console.WriteLine("\t/NumToDial              - Number for caller to dial to reach callee");
            Console.WriteLine("\r\nAlternate Usage Scenario: \r\n(When ATA is pre-configured to forward incoming calls to the callee extension)");
            Console.WriteLine();
            Console.WriteLine("GatewayTestDriver /ServerIP value /NumToDial value /CallerExt value /CalleeExt value");
            Console.WriteLine("\t/ServerIP               - Server IP address");
            Console.WriteLine("\t/NumToDial              - Number for caller to dial to reach callee");
            Console.WriteLine("\t/CallerExt              - Caller Extension");
            Console.WriteLine("\t/CalleeExt              - Callee Extension");
            Console.WriteLine("\r\nNote about /NumToDial: This should be the line number the caller uses to reach \r\nthe callee. For direct calls between the caller and the callee, set it to the\r\ncallee's extension.");
            Console.WriteLine("When /NumToDial represents a PSTN line through an ATA, add a prefix 9 to the number.\r\n");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Optional Parameters  (can be used with either scenario)");
            Console.WriteLine("\t/AllowCallerRecording   - Enable/disable Caller's recording\r\n\t                          Allowed values {true/false}");
            Console.WriteLine("\t/AllowCalleeRecording   - Enable/disable Callee's recording\r\n\t                          Allowed values {true/false}");
            Console.WriteLine("\t/InterSetInterval       - Time interval (in sec) to wait between\r\n\t                          consecutive calls of different sets");
            Console.WriteLine("\t/InterCallInterval      - Time interval (in sec) to wait between\r\n\t                          consecutive calls of same set");
            Console.WriteLine("\t/CallDuration           - Duration of a call (in sec)");
            Console.WriteLine("\t                          Recommended value 60");
            Console.WriteLine("\t/IterationsPerSet       - Number of call iterations per set");
            Console.WriteLine("\t/NumSets                - Number of sets of calls");
            Console.WriteLine("\t/MinSpeechbargeInterval - Minimum Speech barge interval");

            Console.WriteLine("\t                          Min value 0 Max value <= (1/4 * CallDuration)");
            Console.WriteLine("\t/MaxSpeechbargeInterval - Maximum Speech barge interval");
            Console.WriteLine("\t                          Min value >= MinSpeechBargeInterval");
            Console.WriteLine("\t                          Max value <= (1/4 * CallDuration)");
            Console.WriteLine();
            Environment.Exit(-2);
        }

        private bool selectWavFiles()
        {
            wInfo = WavFileInfo.getInstance("GatewayTestCallee.xml", "MapFile.txt");

            if (wInfo == null)
            {
                Console.WriteLine("Main : Error in creating wav File validator. Exiting.");
                Environment.Exit(-1);
            }
            /**
           * Retrieve list of valid wav files for caller and callee
           */
            callerWavFiles = selectWavFiles(wInfo, "CallerWav*.wav");
            calleeWavFiles = selectWavFiles(wInfo, "CalleeWav*.wav");

            Console.WriteLine("Wav files selected for caller");

            for (int i = 0; i < callerWavFiles.Length; i++)
                Console.WriteLine(callerWavFiles[i]);

            Console.WriteLine("Wav files selected for callee");

            for (int i = 0; i < calleeWavFiles.Length; i++)
                Console.WriteLine(calleeWavFiles[i]);

            if (callerWavFiles == null || callerWavFiles.Length == 0 || calleeWavFiles == null || calleeWavFiles.Length == 0)
            {
                Console.WriteLine("No valid wav files for caller or callee to play in the execution directory {0}.", Environment.CurrentDirectory);
                return false;
            }
            else
                return true;
        }

        /// <summary>
        /// Helper method to select wav files for caller and callee to play. These files should be recognized by the callee.
        /// </summary>
        /// <param name="wInfo"></param>
        /// <returns></returns>
        private string[] selectWavFiles(WavFileInfo wInfo, string searchPattern)
        {
            string[] wavFileList = null;
            string[] result = null;
            List<string> validatedWavFiles = new List<string>();

            wavFileList = Directory.GetFiles(Environment.CurrentDirectory, searchPattern);

            for (int i = 0; i < wavFileList.Length; i++)
            {
                if (wInfo.wavFileRecognizedByGrammar(wavFileList[i]) == true)
                    validatedWavFiles.Add(wavFileList[i]);
            }

            if (validatedWavFiles.Count > 0)
            {
                result = new string[validatedWavFiles.Count];

                for (int j = 0; j < validatedWavFiles.Count; j++)
                {
                    result[j] = validatedWavFiles[j];
                }
            }
            return result;
        }
    }
}
