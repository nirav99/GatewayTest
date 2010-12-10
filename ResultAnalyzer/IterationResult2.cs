using System;
using System.Collections.Generic;
using System.Text;
using GatewayTestLibrary;

namespace ResultAnalyzer
{
    /// <summary>
    /// Enumeration listing possible outcome of each iteration
    /// </summary>
    public enum TestIterationCategory
    {
        FAILED,     // If test iteration failed
        PASSED,     // If test iteration passed
        INVALID     // If test iteration did not execute successfully   
    }

    /// <summary>
    /// Enumeration listing possible outcome of speech recognizer
    /// </summary>
    public enum SpeechRecognizerQuality
    {
        RECOGNIZED,      // Valid speech correctly recognized
        UNRECOGNIZED,    // Valid speech not recognized
        MISRECOGNIZED,   // Valid speech in grammar recognized as something else
        CALLER_ECHO,     // Recognizer detected echo on caller side
        CALLEE_ECHO,     // Recognizer detected echo on callee side
    }

    /// <summary>
    /// Enumeration listing possible outcomes of audio power analyzer
    /// </summary>
    public enum AudioVolume
    {
        // The average power of received audio was reduced by the threshold as compared to the power of the played audio 
        // Threshold is defined in AggregateResult.cs
        ATTENUATED,    
        // The average power of received audio was not sufficiently reduced (i.e. the difference between the avg. power of
        // the received audio to the played audio was greater than the threshold
        NOT_ATTENUATED
    }

    // Enumeration that represents the accuracy (confidence) of the audio power analysis algorithm
    public enum AudioVolumeConfidence
    {
        GOOD_CONFIDENCE,    // The algorithm has good confidence in its analysis
        LOW_CONFIDENCE      // The algorithm does not have good confidence in its analysis
    }

    /// <summary>
    /// Class to encapsulate the information related to speech recognizer
    /// </summary>
    public class SpeechRecognizerResult
    {
        public SpeechRecognizerQuality speechOutcome;   // Result of Speech recognizer
        public string property;                         // Represents grammar property name set only for recognized
        public string msg;                              // Custom message
        public double confidence = 0;                   // Represents confidence when speechOutcome is recognized
    }

    /// <summary>
    /// Class that encapsulates result of an iteration
    /// </summary>
    public class IterationResult
    {
        private int iterationResult;                        // Stores result of iteration
        private TestIterationCategory category;             // Category of test result      
        private CallerIterationInfo callerData;            // Data from the caller
        private CalleeIterationInfo calleeData;            // Data from the callee
        private SpeechRecognizerResult callerSpeechResult;  // Result from speech recognizer of caller
        private SpeechRecognizerResult calleeSpeechResult;  // Result from speech recognizer of callee
        internal AudioVolume audioPowerResult;               // Whether audio attenuation was detected
        internal AudioVolumeConfidence audioVolConf;         // Confidence of algorithm when it computed audio attenuation

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="_iterationResult"></param>
        /// <param name="_category"></param>
        /// <param name="_callerData"></param>
        /// <param name="_calleeData"></param>
        /// <param name="_spResult"></param>
        private IterationResult(int _iterationResult, CallerIterationInfo _callerData, CalleeIterationInfo _calleeData, SpeechRecognizerResult _callerSpResult, SpeechRecognizerResult _calleeSpResult)
        {
            iterationResult = _iterationResult;
            callerData = _callerData;
            calleeData = _calleeData;
            callerSpeechResult = _callerSpResult;
            calleeSpeechResult = _calleeSpResult;

            audioPowerResult = AudioVolume.NOT_ATTENUATED;
            audioVolConf = AudioVolumeConfidence.GOOD_CONFIDENCE;

            // determine the classification (category) for IterationResult
            if (iterationResult == ValidationErrors.NO_ERROR)
                category = TestIterationCategory.PASSED;
            else
                if (conditionExists(iterationResult, ValidationErrors.FAILED_CALL) || conditionExists(iterationResult, ValidationErrors.MISSING_HANGUP) ||
                    conditionExists(iterationResult, ValidationErrors.ECHO_DETECTED) || conditionExists(iterationResult, ValidationErrors.CALLER_NOISE_DETECTED) ||
                    conditionExists(iterationResult, ValidationErrors.CALLEE_NOISE_DETECTED) || conditionExists(iterationResult, ValidationErrors.CALLEE_NOT_HEARD) ||
                    conditionExists(iterationResult, ValidationErrors.CALLER_NOT_HEARD))
                    category = TestIterationCategory.FAILED;

            //if(iterationResult == ValidationErrors.FAILED_CALL || iterationResult == ValidationErrors.NOISE_DETECTED || iterationResult == ValidationErrors.ECHO_DETECTED)
                //    category = TestIterationCategory.FAILED;
                else
                {
                    // Use invalid for all others errors such as callee could not play prompt, execution error etc
                    category = TestIterationCategory.INVALID;
                }
        }

        /// <summary>
        /// Helper method to check if a specific error flag is set
        /// </summary>
        /// <param name="result"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool conditionExists(int result, int error)
        {
            if ((result & error) != 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Method to create an instance of IterationResult.
        /// </summary>
        /// <param name="_iterationResult"></param>
        /// <param name="_category"></param>
        /// <param name="_callerData"></param>
        /// <param name="_calleeData"></param>
        /// <param name="_spResult"></param>
        /// <returns></returns>
        public static IterationResult getInstance(int _iterationResult, CallerIterationInfo _callerData, CalleeIterationInfo _calleeData, SpeechRecognizerResult _callerSpResult, SpeechRecognizerResult _calleeSpResult)
        {
            try
            {
                return new IterationResult(_iterationResult, _callerData, _calleeData, _callerSpResult, _calleeSpResult);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Method to generate a string representation of object's state for logging and displaying purposes
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            double latency; // Latency in call establishment

            StringBuilder result = new StringBuilder("Result = ");

            if (category == TestIterationCategory.PASSED)
            {
                result.Append("Passed.\r\n");
            }
            else
                if (category == TestIterationCategory.FAILED)
                {
                    result.Append("Failed. Failure cause(s):\r\n");
                    if (conditionExists(iterationResult, ValidationErrors.FAILED_CALL))
                    {
                        result.Append("\tCall could not be established\r\n");
                    }
                    if (conditionExists(iterationResult, ValidationErrors.MISSING_HANGUP))
                    {
                        result.Append("\tCallee did not detect caller's hangup\r\n");
                    }
                    if (conditionExists(iterationResult, ValidationErrors.ECHO_DETECTED))
                    {
                        result.Append("\tEcho detected\r\n");
                    }
                    if (conditionExists(iterationResult, ValidationErrors.CALLEE_NOISE_DETECTED))
                    {
                        result.Append("\tPhantom noise detected by callee\r\n");
                    }
                    if (conditionExists(iterationResult, ValidationErrors.CALLER_NOISE_DETECTED))
                    {
                        result.Append("\tPhantom noise detected by caller\r\n");
                    }
                    if (conditionExists(iterationResult, ValidationErrors.CALLEE_NOT_HEARD))
                    {
                        result.Append("\tCaller could not detect audio from callee\r\n");
                    }
                    if (conditionExists(iterationResult, ValidationErrors.CALLER_NOT_HEARD))
                    {
                        result.Append("\tCallee could not detect audio from caller\r\n");
                    }
                }
                else // bad test execution - ignored iteration
                {
                    result.Append("Ignored. Cause(s):\r\n");

                    if (conditionExists(iterationResult, ValidationErrors.BAD_SCENARIO_EXECUTION))
                    {
                        result.Append("\tBad scenario execution.\r\n");
                    }
                    if (conditionExists(iterationResult, ValidationErrors.CALLEE_PROMPT_NOT_PLAYED))
                    {
                        result.Append("\tCallee did not speak over the audio channel.\r\n");
                    }
                }
            if ((true == getCallConnectionLatency(out latency)))
            {
                result.Append("Call Connection Latency = " + latency + " sec\r\n");
            }

            if ((true == getCallHangupLatency(out latency)))
            {
                result.Append("Call Hangup Latency = " + latency + " sec\r\n");
            }

            result.Append("Caller ID = " + calleeIterationData.remotePartyExtension + "\r\n");

            if (callerSpeechResult != null)
            {
                result.Append("Caller's speech Reocgnizer's result : " + callerSpeechResult.msg);
                if (callerSpeechResult.speechOutcome == SpeechRecognizerQuality.RECOGNIZED)
                {
                    result.Append("\r\nConfidence of recognized speech = " + callerSpeechResult.confidence);
                }
            }

            if (calleeSpeechResult != null)
            {
                result.Append("\r\n");
                result.Append("Callee's speech Reocgnizer's result : " + calleeSpeechResult.msg);
                if (calleeSpeechResult.speechOutcome == SpeechRecognizerQuality.RECOGNIZED)
                {
                    result.Append("\r\nConfidence of recognized speech = " + calleeSpeechResult.confidence);
                }
            }

            if (audioPowerResult == AudioVolume.ATTENUATED)
            {
                result.Append("\r\nAudio Volume Attenuation experienced by either caller or callee.");

                if (audioVolConf == AudioVolumeConfidence.GOOD_CONFIDENCE)
                {
                    result.Append(" Confidence Level of Analyzer : Good.");
                }
                else
                {
                    result.Append(" Confidence Level of Analyzer : Low.");
                }
            }
            //else
            //{
            //    result.Append("\r\nNo Attenuation in Audio Signal Strength.");
            //}
            return result.ToString();
        }

        #region Method to retrieve properties
        public int ResultCode
        {
            get
            {
                return iterationResult;
            }
        }

        public TestIterationCategory ResultCategory
        {
            get
            {
                return category;
            }
        }

        public SpeechRecognizerResult callerSpeechRecognizerResult
        {
            get
            {
                return callerSpeechResult;
            }
        }

        public SpeechRecognizerResult calleeSpeechRecognizerResult
        {
            get
            {
                return calleeSpeechResult;
            }
        }

        public CallerIterationInfo callerIterationData
        {
            get
            {
                return callerData;
            }
        }

        public CalleeIterationInfo calleeIterationData
        {
            get
            {
                return calleeData;
            }
        }

        ///// <summary>
        ///// Method to compute and return the call establishment latency for the iteration
        ///// </summary>
        ///// <returns></returns>
        //public bool getCallConnectionLatency(out double latency)
        //{
        //    DateTime uninitDate = new DateTime();

        //    latency = -1;
        //    // Compute latency only if the objects are valid and call connect timestamps are valid
        //    // else return false to indicate that latency is not valid
        //    if (callerData != null && calleeData != null && callerData.callConnectTime != uninitDate && calleeData.callConnectTime != uninitDate)
        //    {
        //        TimeSpan ts = calleeData.callConnectTime - callerData.callConnectTime;
        //        latency = Convert.ToDouble(Math.Abs(ts.TotalSeconds));
        //        return true; 
        //    }
        //    else
        //        return false;
        //}

        /// <summary>
        /// Method to compute and return the call establishment latency for the iteration
        /// </summary>
        /// <returns></returns>
        public bool getCallConnectionLatency(out double latency)
        {
            DateTime uninitDate = new DateTime();
            TimeSpan ts;

            latency = -1;
            // Compute latency only if the objects are valid and call connect timestamps are valid
            // else return false to indicate that latency is not valid
            if (callerData != null && calleeData != null && callerData.makeCallMethodTime != uninitDate && callerData.callConnectTime != uninitDate && calleeData.callConnectTime != uninitDate)
            {
                if (callerData.callConnectTime > calleeData.callConnectTime)
                {
                    ts = callerData.callConnectTime - callerData.makeCallMethodTime;
                }
                else
                {
                    ts = calleeData.callConnectTime - callerData.makeCallMethodTime;
                }
                latency = Convert.ToDouble(Math.Abs(ts.TotalSeconds));
                return true;
            }
            else
                return false;
        }

        ///// <summary>
        ///// Method to compute and return the call hangup latency for the test iteration
        ///// </summary>
        ///// <param name="latency"></param>
        ///// <returns></returns>
        //public bool getCallHangupLatency(out double latency)
        //{
        //    DateTime uninitDate = new DateTime();

        //    latency = -1;

        //    // Compute latency only if the objects are valid and call release timestamps are valid
        //    // else return false to indicate that latency is not valid
        //    if (callerData != null && calleeData != null && callerData.callReleaseTime != uninitDate && calleeData.callReleaseTime != uninitDate)
        //    {
        //        TimeSpan ts = calleeData.callReleaseTime - callerData.callReleaseTime;
        //        latency = Convert.ToDouble(Math.Abs(ts.TotalSeconds));
        //        return true;
        //    }
        //    else
        //        return false;
        //}

        /// <summary>
        /// Method to compute and return the call hangup latency for the test iteration
        /// </summary>
        /// <param name="latency"></param>
        /// <returns></returns>
        public bool getCallHangupLatency(out double latency)
        {
            DateTime uninitDate = new DateTime();
            TimeSpan ts;
            latency = -1;

            // Compute latency only if the objects are valid and call release timestamps are valid
            // else return false to indicate that latency is not valid
            if (callerData != null && calleeData != null && callerData.connClearedMethodTime != uninitDate && callerData.callReleaseTime != uninitDate && calleeData.callReleaseTime != uninitDate)
            {
                if (calleeData.callReleaseTime > callerData.callReleaseTime)
                {
                    ts = calleeData.callReleaseTime - callerData.connClearedMethodTime;
                }
                else
                {
                    ts = callerData.callReleaseTime - callerData.connClearedMethodTime;
                }
                latency = Convert.ToDouble(Math.Abs(ts.TotalSeconds));
                return true;
            }
            else
                return false;
        }
        #endregion
    }
}

