using System;
using System.Collections.Generic;
using System.Text;

namespace GatewayTestLibrary
{
    /// <summary>
    /// Class that encapsulates possible return code from caller and the callee
    /// </summary>
    public class ReturnCode
    {
        public static readonly int SUCCESS = 0;
        public static readonly int BAD_INPUT_PARAMETERS = -2;
        public static readonly int ERROR_OPENING_RESULT_FILE = -3;
        public static readonly int NOT_REGISTERED_WITHIN_TIMEOUT = -4;
        public static readonly int IO_EXCEPTION = -5;
        public static readonly int FAILED_REGISTRATION = -6;
        public static readonly int COM_EXCEPTION_CREATION = -10;
        public static readonly int EXCEPTION_CREATION = -11;
    }
}

  