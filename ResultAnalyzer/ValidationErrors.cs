using System;
using System.Collections.Generic;
using System.Text;

namespace ResultAnalyzer
{
    /// <summary>
    /// Class that represents all the validation errors
    /// </summary>
    public class ValidationErrors
    {
        public static readonly int NO_ERROR = 0;                        // No error while applying this rule
        public static readonly int FAILED_CALL = 1;                     // Call not completed
        public static readonly int MISSING_HANGUP = 2;                  // If callee does not detect the hangup from the caller
        public static readonly int CALLEE_PROMPT_NOT_PLAYED = 4;        // If callee's prompt could not be played
        public static readonly int CALLER_NOISE_DETECTED = 8;           // Noise detected on caller
        public static readonly int CALLEE_NOISE_DETECTED = 16;          // Noise detected on callee
        public static readonly int ECHO_DETECTED = 32;                  // Echo detected
        public static readonly int CALLER_NOT_HEARD = 64;               // Caller spoke but callee did not hear - barge-in failed
        public static readonly int CALLEE_NOT_HEARD = 128;              // Callee spoke but caler did not hear
        public static readonly int BAD_SCENARIO_EXECUTION = 256;        // Generic scenario execution errors
        public static readonly int TEXT_MAPPING_ABSENT_IN_MAPFILE = 512;// Text mapping for wav file absent in map file
        public static readonly int SPOKEN_TEXT_ABSENT_CALLEE_GRAMMAR = 1024; // Caller's speech is not in callee's grammar
        public static readonly int PROMPT_OR_LISTENER_NOT_STARTED = 2048;   // Prompt or listener not started
    }
}
