using System;
using System.Collections.Generic;
using System.Text;

namespace GatewayTestLibrary
{
    /// <summary>
    /// Contains useful helper functions for use by caller and callee
    /// </summary>
    public class Helper
    {
        /// <summary>
        /// Helper method to parse the configuration file
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static int parseValue(string line)
        {
            char[] sept = { '=' };
            string[] tokens;
            int retVal;

            tokens = line.Split(sept);

            if (tokens == null || tokens.Length != 2)
                retVal = -1;
            else
            {
                try
                {
                    retVal = Convert.ToInt32(tokens[1]);
                }
                catch (Exception e)
                {
                    retVal = -1;
                }
            }
            return retVal;
        }
    }
}
