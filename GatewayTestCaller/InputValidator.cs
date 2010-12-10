using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using GatewayTestLibrary;

namespace GatewayTestCaller
{
    /// <summary>
    /// Class to retrieve the command line parameters and validate them
    /// </summary>
    class InputValidator : InputValidatorBase
    {
        /// <summary>
        /// Class constructor 
        /// </summary>
        /// <param name="args"></param>
        public InputValidator() : base()
        {
           
        }

        /// <summary>
        /// Member method to validate input
        /// </summary>
        /// <param name="_inp"></param>
        /// <returns></returns>
        public static bool validate(string[] args, out ConfigParameters _inp)
        {
            bool error = false;
            int numIter = 0;
            string[] wavFileList = null;

            if (args.Length < 8)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("GatewayTestCaller.exe SipServerIP CallerExtension GrammarFileName OutputFileName ResultDirectory ConfigurationFileName WavFileList");
                Console.WriteLine("\tSipServerIP - IP address of the SIP server");
                Console.WriteLine("\tCallerExtension - Extension number for the caller");
                Console.WriteLine("\tDialedExtension - Extension to dial to reach callee");
                Console.WriteLine("\tGrammarFileName - Name for the grammar file for caller");
                Console.WriteLine("\tOutputFileName - Name of the output file");
                Console.WriteLine("\tResultDirectory - Directory name to store result, trace and recorded files");
                Console.WriteLine("\tConfigurationFileName - Name of file to read configuration parameters from");

          //      Console.WriteLine("\tNumIterations - Number of calls to place");
                Console.WriteLine("\tWavFileList - List of wave file to play to barge");
                error = true;
            }
            else
            {
                if (validateIP(args[0]) == false)
                {
                    Console.WriteLine(args[0] + " is not a valid IP address");
                    error = true;
                }
                if (!error && checkGrammarFile(args[3]) == false)
                {
                    Console.WriteLine("Specified Grammar file " + args[3] + " does not exist");
                    error = true;
                }
                if (Directory.Exists(args[5]) == false)
                {
                    error = true;
                    Console.WriteLine("Specified directory to store results \"{0}\" does not exist", args[5]);
                }
                if (File.Exists(args[6]) == false)
                {
                    error = true;
                    Console.WriteLine("Specified configuration file \"{0}\" does not exist", args[6]);
                }

                //if (false == validateNumIterations(args[6], out numIter))
                //{
                //    error = true;
                //    Console.WriteLine("Number of iterations = " + args[6] + " is invalid.");
                //}
                
                /**
                 * Build a wav file list
                 */
                wavFileList = new string[args.Length - 7];

                for (int i = 7; i < args.Length && !error; i++)
                {
                    if (checkWavFile(args[i]) == false)
                    {
                        error = true;
                        Console.WriteLine("Specified Wav file " + args[i] + " does not exist");
                    }
                    else
                    {
                        wavFileList[i - 7] = args[i];
                    }
                }
            }
            if (error == true)
            {
                _inp = null;
                wavFileList = null;
            }
            else
            {
                _inp = new ConfigParameters(args[0], args[1], args[2], args[3], args[4], args[5], args[6], wavFileList);
            }
            return !error;
            }        
    }
}
