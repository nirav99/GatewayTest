using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace GatewayTestLibrary
{
    /// <summary>
    /// This class provides the basic methods necessary to validate common input parameters. Derived
    /// classes in Caller, Callee and test driver can extend this class for more advanced parameter
    /// validation.
    /// </summary>
    public class InputValidatorBase
    {
        /// <summary>
        /// Method to validate IP address
        /// </summary>
        /// <param name="ipAddr"></param>
        /// <returns></returns>
        protected static bool validateIP(string ipAddr)
        {
            char[] sept = { '.' };
            string[] tokens = ipAddr.Split(sept);
            int temp;

            if (tokens.Length != 4)
            {
                return false;
            }

            for (int i = 0; i < tokens.Length; i++)
            {
                try
                {
                    temp = Convert.ToInt32(tokens[i]);

                    if (temp < 0)
                        return false;
                }
                catch (Exception e)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Method to check existence of grammar file
        /// </summary>
        /// <param name="gmFile"></param>
        /// <returns></returns>
        protected static bool checkGrammarFile(string gmFile)
        {
            return File.Exists(gmFile);
        }

        /// <summary>
        /// Method to check existence of wav file
        /// </summary>
        /// <param name="wavFile"></param>
        /// <returns></returns>
        protected static bool checkWavFile(string wavFile)
        {
            return File.Exists(wavFile);
        }

        /// <summary>
        /// Method to check if the numIterations is a valid positive integer greater than zero.
        /// The method returns true if the supplied string can be converted an integer greater
        /// than zero, false otherwise.
        /// </summary>
        /// <param name="strIter"></param>
        /// <param name="numIter"></param>
        /// <returns></returns>
        protected static bool validateNumIterations(string strIter, out int numIter)
        {
            bool result = true;

            try
            {
                numIter = Convert.ToInt32(strIter);

                if(numIter <= 0)
                {
                    numIter = -1;
                    result = false;
                }
            }
            catch (Exception e)
            {
                result = false;
                numIter = -1;
            }
            return result;
        }
    }
}
