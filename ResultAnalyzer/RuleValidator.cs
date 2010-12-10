//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.IO;
//using SpeechBargeLibrary;

/**
 * Nirav - 18th Jan 2007. This class is replaced by ResultInterpreter. However, I am leaving the commented file
 * just as a reference to old approach of applying the rules.
 */
//namespace ResultAnalyzer
//{
//    /// <summary>
//    /// Class that applies validation rules to the tokens obtained for one iteration from the caller and callee result logs
//    /// </summary>
//    public class RuleValidator
//    {
//        private WavFileInfo wInfo;                  // WavFileInfo instance
//        private CallerIterationInfo callerData;     // Instance of caller iteration data
//        private CalleeIterationInfo calleeData;     // Instance of callee iteration data

//        /// <summary>
//        /// Class constructor
//        /// </summary>
//        public RuleValidator(WavFileInfo _wInfo)
//        {
//            wInfo = _wInfo;
//            callerData = null;
//            calleeData = null;
//        }

//        /// <summary>
//        /// Method to apply validation rules and return a result object
//        /// </summary>
//        /// <param name="callerInfo"></param>
//        /// <param name="calleeInfo"></param>
//        /// <returns></returns>
//        public IterationResult applyValidationRules(CallerIterationInfo callerInfo, CalleeIterationInfo calleeInfo)
//        {
//            IterationResult result = null;
//            SpeechRecognizerResult speechResult = null;
//            callerData = callerInfo;
//            calleeData = calleeInfo;
//            int ruleOutcome = ValidationErrors.NO_ERROR;
//            int temp = ValidationErrors.NO_ERROR;

//            ruleOutcome = applyScenarioExecutionValidationRule();

//            /**
//             * If execution validation rule fails, don't apply any other rule and return
//             */
//            if (ruleOutcome != ValidationErrors.NO_ERROR)
//            {
//                result = IterationResult.getInstance(ruleOutcome, callerData, calleeData, null);
//                return result;
//            }

//            /**
//             * Apply noise detected rule and if no errors encountered in it, then only apply
//             * echo detection rule.
//             */
//            ruleOutcome = applyNoiseDetectionRule();

//            if (ruleOutcome == ValidationErrors.NO_ERROR)
//            {
//                ruleOutcome = applyEchoDetectionRule(out speechResult);
//            }
//            else
//            {
//                // If noise was detected, we want to bypass echo detection rule and apply speech recognizer rule
//                temp = applySpeechRecognitionRule(out speechResult);
//            }

//            // If no other errors were found, add in the outcome from speech recognizer to the overall iteration result            
//            if (ruleOutcome == ValidationErrors.NO_ERROR)
//                ruleOutcome = ruleOutcome | temp;

//            result = IterationResult.getInstance(ruleOutcome, callerData, calleeData, speechResult);

//            return result;
//        }

//        /// <summary>
//        /// Method to validate if call between caller and callee was established for current iteration
//        /// </summary>
//        /// <returns></returns>
//        private int applyCallFailureValidationRule()
//        {
//            DateTime uninitDate = new DateTime();    // Uninitialized datetime
//            int result = ValidationErrors.NO_ERROR; // Initialize return value to no error

//            if (callerData.callConnectTime == uninitDate || calleeData.callConnectTime == uninitDate)
//            {
//                // Indicate that call could not be established
//                result = result | ValidationErrors.FAILED_CALL;
//            }
//            return result;
//        }

//        /// <summary>
//        /// Method to validate if scenario did not execute correctly
//        /// </summary>
//        /// <returns>An int representing correct/incorrect execution - a member of ValidationErrors class</returns>
//        private int applyScenarioExecutionValidationRule()
//        {
//            DateTime uninitDate = new DateTime();    // Uninitialized datetime
//            int result = ValidationErrors.NO_ERROR; // Initialize return value to no error

//            if (callerData.callConnectTime == uninitDate || calleeData.callConnectTime == uninitDate || callerData.callReleaseTime == uninitDate || calleeData.callReleaseTime == uninitDate)
//            {
//                // Indicate that call could not be established
//                result = result | ValidationErrors.FAILED_CALL;
//            }
//            else
//            if (calleeData.speakTime == uninitDate || callerData.speechDetectionTime == uninitDate || callerData.speakTime == uninitDate || calleeData.speechDetectionTime == uninitDate)
//            {
//                // If speech could not be detected or prompt not played - indicate the following error
//                result = result | ValidationErrors.PROMPT_OR_LISTENER_NOT_STARTED;
//            }
//            return result;
//        }

//        /// <summary>
//        /// Method to apply noise detection validation rule. When speech is detected before any prompts are spoken over the channel,
//        /// then that speech is considered noise detection.
//        /// </summary>
//        /// <returns></returns>
//        private int applyNoiseDetectionRule()
//        {
//            int result = ValidationErrors.NO_ERROR; // Initialize return value to no error

//            TimeSpan refTime = callerData.callConnectTime - callerData.callConnectTime;

//            /// If caller detected speech before callee stared playing prompt, then this means caller detected
//            /// some random noise interference.
//            TimeSpan callerInterferenceLatency = callerData.speechDetectionTime - calleeData.speakTime;

//            /**
//             * TODO : I am commenting this for now, as I am not sure if such a situation is possible.
//             * Jan 12, 2007.
//             */
//            ///// If callee detected speech before it started playing a prompt, then this means that callee detected
//            ///// some random noise interference.
//            //TimeSpan calleeInterferenceLatency = calleeData.speechDetectionTime - calleeData.speakTime;

//            /// Negative values for either of these variables represents random noise interference
//            if (callerInterferenceLatency < refTime) // || calleeInterferenceLatency < refTime)
//            {
//                result = result | ValidationErrors.NOISE_DETECTED;
//            }
//            return result;
//        }

//        /// <summary>
//        /// Method to apply echo detection rule
//        /// </summary>
//        /// <returns></returns>
//        private int applyEchoDetectionRule(out SpeechRecognizerResult speechResult)
//        {
//            /**
//             * Echo detection rule has been modified to 
//             * 1) Check for negative latency OR
//             * 2) callee hearing its own speech
//             * Either of these conditions is considered an echo.
//             * Assumption: Caller and Callee use a different set of wav files to play.
//             */
//            int result = ValidationErrors.NO_ERROR; // Initialize return value to no error
//            int temp;

//            TimeSpan refDate = calleeData.speechDetectionTime - calleeData.speechDetectionTime; // Initialize a date with zero latency
//            TimeSpan latency = calleeData.speechDetectionTime - callerData.speakTime;           // Calculate latency

//            temp = applySpeechRecognitionRule(out speechResult);
            
//            if (latency < refDate || temp == ValidationErrors.ECHO_DETECTED)
//            {
//                result = result | ValidationErrors.ECHO_DETECTED;
//            }
//            return result;
//        }
        
//        /// <summary>
//        /// Method to apply speech recognition rule
//        /// </summary>
//        /// <param name="speechResult"></param>
//        /// <returns></returns>
//        private int applySpeechRecognitionRule(out SpeechRecognizerResult speechResult)
//        {
//            int result = ValidationErrors.NO_ERROR;
//            string grammarPropertyNameCallerPlayed = null;       // Property name in callee's grammar for file played by caller
//            string grammarPropertyNameCalleePlayed = null;       // Property name in callee's grammar for file played by callee
//            speechResult = null;

//            // Obtain grammar property name for wav file played by caller and callee
//            grammarPropertyNameCallerPlayed = wInfo.getGrammarPropertyName(callerData.wavFilePlayed);
//            grammarPropertyNameCalleePlayed = wInfo.getGrammarPropertyName(calleeData.wavFilePlayed);

//            /**
//             * ASSUMPTIONS:
//             * 1) The wav file played by caller and callee is recognizable by callee (i.e.) it has
//             *      a) Representation in callee's grammar
//             *      b) Representation in mapfile (WavFileName mapped to grammar tag in callee's grammar
//             *      Thus, no validation is required.
//             * 2) Caller and the callee ALWAYS use different set of wav files. Thus, if callee recognizes a file
//             *    played by itself, that situation is considered as an echo.
//             */

//            if (calleeData.speechDetectedStatus == SpeechDetected.RECOGNIZED)
//            {
//                // If callee heard caller's audio, pass the iteration.
//                // If callee heard it's own audio, fail the iteration due to echo.
//                if (calleeData.grammarProperty.Equals(grammarPropertyNameCallerPlayed, StringComparison.CurrentCultureIgnoreCase))
//                {
//                    // Passed
//                    speechResult = new SpeechRecognizerResult();
//                    speechResult.speechOutcome = SpeechRecognizerQuality.RECOGNIZED;
//                    speechResult.msg = "Speech recognized";
//                 }
//                else
//                 if (calleeData.grammarProperty.Equals(grammarPropertyNameCalleePlayed, StringComparison.CurrentCultureIgnoreCase))
//                 {
//                     // Echo
//                     speechResult = new SpeechRecognizerResult();
//                     speechResult.speechOutcome = SpeechRecognizerQuality.RECOGNIZED;
//                     speechResult.msg = "Echo Detected. Callee heard it's own audio.";
//                     result = ValidationErrors.ECHO_DETECTED;
//                 }
//                 else
//                 {
//                     // We consider this mis-recognized
//                     speechResult = new SpeechRecognizerResult();
//                     speechResult.speechOutcome = SpeechRecognizerQuality.MISRECOGNIZED;
//                     speechResult.msg = callerData.wavFilePlayed + " recognized as " + calleeData.textEquivalent;
//                 }
//            }
//            else
//            {
//                // Since audio was not recognized, we can't reliably say there was echo, so we call it passed
//                speechResult = new SpeechRecognizerResult();
//                speechResult.speechOutcome = SpeechRecognizerQuality.UNRECOGNIZED;
//                speechResult.msg = callerData.wavFilePlayed + " wav file played by caller could not be recognized";
//            }
//            return result;
//        }

//        /// <summary>
//        /// Method to apply speech recognition rules
//        /// </summary>
//        /// <param name="speechResult"></param>
//        /// <returns></returns>
//        private int applySpeechRecognitionRuleOLD(out SpeechRecognizerResult speechResult)
//        {
//            int result = ValidationErrors.NO_ERROR;

//            string grammarPropertyName = null;
//            speechResult = null;

//            // Obtain grammar property name for wav file played by caller
//            grammarPropertyName = wInfo.getGrammarPropertyName(callerData.wavFilePlayed);

//            if (grammarPropertyName == null || grammarPropertyName.Equals("") == true)
//            {
//                result = result | ValidationErrors.SPOKEN_TEXT_ABSENT_CALLEE_GRAMMAR;
//                return result;
//            }
//            // There were no validation errors - so we continue to apply recognition rules
//            if (calleeData.speechDetectedStatus == SpeechDetected.RECOGNIZED)
//            {
//                if(calleeData.grammarProperty.Equals(grammarPropertyName, StringComparison.CurrentCulture) == true)
//                {
//                    /**
//                    * Speech was recognized and equal to text equivalent for wav file played by caller, speech recognition is considered
//                    * to have recognized the speech.
//                    */
//                    speechResult = new SpeechRecognizerResult();
//                    speechResult.speechOutcome = SpeechRecognizerQuality.RECOGNIZED;
//                    speechResult.msg = "Speech recognized";
//                }
//                else
//                {
//                    /**
//                     * Speech was recognized, however it is not equal to text equivalent of wav file played by caller, so this situation
//                     * is considered as speech misrecognition.
//                     */
//                    speechResult = new SpeechRecognizerResult();
//                    speechResult.speechOutcome = SpeechRecognizerQuality.MISRECOGNIZED;
//                    speechResult.msg = "Speech misrecognized. Wave file played by caller (" + callerData.wavFilePlayed + ") recognized as (\"" + calleeData.textEquivalent + "\")";
//                }
//            }
//            else
//            {
//                /**
//                 * This means that the spoken text was not recognized. 
//                 */
//                speechResult = new SpeechRecognizerResult();
//                speechResult.speechOutcome = SpeechRecognizerQuality.UNRECOGNIZED;
//             //   speechResult.msg = "Valid phrase (\"" + spokenText + "\") that was in grammar could not be recognized";
//                speechResult.msg = callerData.wavFilePlayed + " wav file played by caller could not be recognized";
//            }
//            return result;

//        }

//        /// <summary>
//        /// Method to apply speech recognizer validation rule
//        /// </summary>
//        /// <param name="speechResult"></param>
//        /// <returns></returns>
//        //private int applySpeechValidationRule2(out SpeechRecognizerResult speechResult)
//        //{
//        //    int result = ValidationErrors.NO_ERROR; // Initialize return value to no error

//        //    string spokenText = null;    // Text corresponding to wav file played by caller
//        //    speechResult = null;

//        //    // Obtain the text spoken for wav file
//        //    spokenText = wavValidator.obtainTextForWavFile(callerData.wavFilePlayed);

//        //    if (spokenText == null || spokenText.Equals("") == true)
//        //    {
//        //        result = result | ValidationErrors.TEXT_MAPPING_ABSENT_IN_MAPFILE;
//        //        return result;
//        //    }
//        //    else
//        //    if (wavValidator.matchFoundInGrammar(spokenText) == false)
//        //    {
//        //        result = result | ValidationErrors.SPOKEN_TEXT_ABSENT_CALLEE_GRAMMAR;
//        //        return result;
//        //    }

//        //    // There were no validation errors - so we continue to apply recognition rules
//        //    if (calleeData.speechDetectedStatus == SpeechDetected.RECOGNIZED)
//        //    {
//        //        if (calleeData.textEquivalent.Equals(spokenText, StringComparison.CurrentCultureIgnoreCase) == true)
//        //        {
//        //            /**
//        //            * Speech was recognized and equal to text equivalent for wav file played by caller, speech recognition is considered
//        //            * to have recognized the speech.
//        //            */
//        //            speechResult = new SpeechRecognizerResult();
//        //            speechResult.speechOutcome = SpeechRecognizerQuality.RECOGNIZED;
//        //            speechResult.msg = "Speech recognized";
//        //        }
//        //        else
//        //        {
//        //            /**
//        //             * Speech was recognized, however it is not equal to text equivalent of wav file played by caller, so this situation
//        //             * is considered as speech misrecognition.
//        //             */
//        //            speechResult = new SpeechRecognizerResult();
//        //            speechResult.speechOutcome = SpeechRecognizerQuality.MISRECOGNIZED;
//        //            speechResult.msg = "Speech misrecognized. Spoken text (\"" + spokenText + "\") recognized as (\"" + calleeData.textEquivalent + "\")";
//        //        }
//        //    }
//        //    else
//        //    {
//        //        /**
//        //         * This means that the spoken text was not recognized. 
//        //         */
//        //        speechResult = new SpeechRecognizerResult();
//        //        speechResult.speechOutcome = SpeechRecognizerQuality.UNRECOGNIZED;
//        //        speechResult.msg = "Valid phrase (\"" + spokenText + "\") that was in grammar could not be recognized";
//        //    }
//        //    return result;
//        //}
//    }
//}
