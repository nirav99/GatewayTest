using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using GatewayTestLibrary;

namespace GatewayTestCallee
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
       
            if (args == null || args.Length != 8)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("GatewayTestCallee.exe EDServerIP CalleeExtension CallerExtension GrammarFileName OutputFileName ResultDirectory ConfigFileName WavFileName");
                Console.WriteLine("\tEDServerIP - IP address of the Edinburgh server");
                Console.WriteLine("\tCalleeExtension - Extension number for the callee");
                Console.WriteLine("\tCallerExtension - Extension number for the caller");
                Console.WriteLine("\tGrammarFileName - Name for the grammar file for callee");
                Console.WriteLine("\tOutputFileName - Name of the output file");
                Console.WriteLine("\tResultDirectory - Name of directory to store results");
                Console.WriteLine("\tConfigFileName - Name of file to read config parameters from");
                Console.WriteLine("\tWavFileName - Name of wave file to play");
          //      Console.WriteLine("\tNumIterations - Number of calls to receive");
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
                if (!error && !Directory.Exists(args[5]))
                {
                    Console.WriteLine("Specified Result Directory " + args[5] + " does not exist");
                    error = true;
                }
                if (!error && File.Exists(args[6]) == false)
                {
                    Console.WriteLine("Specified configuration file \"{0}\" does not exist", args[6]);
                    error = true;
                }
                if (!error && checkWavFile(args[7]) == false)
                {
                    Console.WriteLine("Specified Wav file " + args[7] + " does not exist");
                    error = true;
                }
                
            }

            //if (error == false)
            //{
            //    if (false == validateNumIterations(args[7], out numIter))
            //    {
            //        error = true;
            //        Console.WriteLine("Number of iterations = " + args[7] + " is invalid.");
            //    }
            //}
            if (error == true)
            {
                _inp = null;
            }
            else
            {
               _inp = new ConfigParameters(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7]);
            }
            return !error;
            }        
    }
}
