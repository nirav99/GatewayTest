using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using GatewayTestLibrary;
using System.Diagnostics;

namespace ResultAnalyzer
{
    // Enum that describes the next rule to apply once processing for current rule is completed
    enum NextRule
    {
        None = 0,           // No additional rule
        LatencyRule = 1,    // Next rule is latency rule
        SpeechRecoRule = 2  // Next rule is speech recognition rule
    }

    /// <summary>
    /// Class that interprets the tokens generated from caller and callee log file for one iteration
    /// </summary>
    public class ResultInterpreter
    {
        private WavFileInfo wInfo;                  // WavFileInfo instance
        private CallerIterationInfo callerData;     // Instance of caller iteration data
        private CalleeIterationInfo calleeData;     // Instance of callee iteration data

        /// <summary>
        /// Class constructor
        /// </summary>
        public ResultInterpreter(WavFileInfo _wInfo)
        {
            wInfo = _wInfo;
            callerData = null;
            calleeData = null;
        }

        /// <summary>
        /// Method that applies various validation rules to interpret the result of the iteration
        /// </summary>
        /// <param name="callerInfo"></param>
        /// <param name="calleeInfo"></param>
        /// <returns></returns>
        public IterationResult applyValidationRules(CallerIterationInfo callerInfo, CalleeIterationInfo calleeInfo)
        {
            IterationResult result = null;
            SpeechRecognizerResult callerSpeechResult = null;
            SpeechRecognizerResult calleeSpeechResult = null;
            callerData = callerInfo;
            calleeData = calleeInfo;

            int hangupRuleOutcome = ValidationErrors.NO_ERROR;
            int timestampRuleOutcome = ValidationErrors.NO_ERROR;
            int latencyRuleResult = ValidationErrors.NO_ERROR;
            int speechRecoRuleResult = ValidationErrors.NO_ERROR;
            
            NextRule rule = NextRule.None;

            // First rule - check if call was completed end to end between caller and callee.
            // If not, fail the iteration and return the result
            if (applyCallFailureValidationRule() == ValidationErrors.FAILED_CALL)
            {
                result = IterationResult.getInstance(ValidationErrors.FAILED_CALL, callerData, calleeData, null, null);
                return result;
            }

            hangupRuleOutcome = applyHangupValidationRule();
            timestampRuleOutcome = applyTimeStampValidationRule(out rule);

            // Next apply timestamp validation rule and if we encounter a failure in that, and, we find that we don't need
            // to apply another rule, return result.
            if (rule == NextRule.None)
            {
                result = IterationResult.getInstance(hangupRuleOutcome | timestampRuleOutcome, callerData, calleeData, null, null);
                return result;
            }

            latencyRuleResult = applyLatencyValidationRule();
            
            speechRecoRuleResult = applySpeechRecognitionRule(out calleeSpeechResult, out callerSpeechResult);

            result = IterationResult.getInstance(hangupRuleOutcome | timestampRuleOutcome | latencyRuleResult | speechRecoRuleResult, callerData,
                                                    calleeData,
                                                    callerSpeechResult, calleeSpeechResult);

            return result;
        }

        /// <summary>
        /// Method to validate if call between caller and callee was established for current iteration
        /// </summary>
        /// <returns></returns>
        private int applyCallFailureValidationRule()
        {
            DateTime uninitDate = new DateTime();   // Uninitialized datetime
            int result = ValidationErrors.NO_ERROR; // Initialize return value to no error

            if (callerData.callConnectTime == uninitDate || calleeData.callConnectTime == uninitDate)
            {
                // Indicate that call could not be established
                result = result | ValidationErrors.FAILED_CALL;
            }
            return result;
        }

        /// <summary>
        /// Method to validate if the callee detected hangup from the caller for the current iteration
        /// </summary>
        /// <returns></returns>
        private int applyHangupValidationRule()
        {
            DateTime uninitDate = new DateTime();   // Uninitialized datetime
            int result = ValidationErrors.NO_ERROR; // Initialize return value to no error

            // If callee's connection release timestamp was not set, this implies that the callee did not detect
            // hangup for the specified call.
            if (calleeData.callReleaseTime == uninitDate)
            {
                result = ValidationErrors.MISSING_HANGUP;
            }
            return result;
        }

        /// <summary>
        /// Method to interpret the result based on whether expected speech events occurred or not
        /// </summary>
        /// <param name="rule"></param>
        /// <returns></returns>
        private int applyTimeStampValidationRule(out NextRule rule)
        {
            DateTime uninitDate = new DateTime();       // Un-initialized date

            int result = ValidationErrors.NO_ERROR;

            /**
             * If callee did not play prompt, then entire iteration is practically meaningless. However, there
             * may be noise or echo, and should need further processing.
             * However, such situations seem rare, so I am sending back "Ignored" at this time. If the need arises,
             * this section shall be enhanced to apply further processing rules.
             * Nirav - 16th Jan 2007
             */
            if (calleeData.speakTime == uninitDate)
            {
                Console.WriteLine("OOPS: Should not get here");
                result = ValidationErrors.CALLEE_PROMPT_NOT_PLAYED;
                rule = NextRule.None;
            }
            else
            {
                // For the entire set of conditions, it is always guaranteed that callee prompt was played

                /**
                 * Callee played the prompt - but caller could not detect that. 
                 * Thus, caller did not play prompt - and callee detected no speech.
                 */
                if (callerData.speechDetectionTime == uninitDate &&
                    callerData.speakTime == uninitDate && calleeData.speechDetectionTime == uninitDate)
                {
                    result = ValidationErrors.CALLEE_NOT_HEARD;
                    rule = NextRule.None;
                }
                else
                    if (callerData.speechDetectionTime == uninitDate &&
                        callerData.speakTime == uninitDate && calleeData.speechDetectionTime != uninitDate)
                    {
                        /**
                         * Callee played the prompt - but caller could not detect that.
                         * Caller did not play the prompt - however callee detected some speech.
                         * This typically happens when half-duplex channels with noise/echo are established or callee's audio
                         * underwent significant distortion in the audio channel.
                         */
                        result = ValidationErrors.CALLEE_NOT_HEARD;
                        rule = NextRule.LatencyRule;
                    }
                    else
                        if (callerData.speechDetectionTime != uninitDate && callerData.speakTime != uninitDate &&
                            calleeData.speechDetectionTime == uninitDate)
                        {
                            /**
                             * A very interesting situation.
                             * Callee played the prompt and the caller heard it.
                             * Caller started to play the prompt and callee could not hear it.
                             * This can happen due to
                             * i)  Distortion of caller's speech in the audio channel
                             * ii) Half duplex channel established - i.e. while callee is talking it can't hear anything
                             * So, we consider this failed, and yet apply latency rule to find out if there was any noise or not.
                             */
                            result = ValidationErrors.CALLER_NOT_HEARD;
                            rule = NextRule.LatencyRule;
                        }
                        else
                            if (callerData.speechDetectionTime != uninitDate && callerData.speakTime != uninitDate &&
                                calleeData.speechDetectionTime != uninitDate)
                            {
                                /**
                                 * Now all the events were present.
                                 * Callee played the prompt, caller heard the prompt, and played its own prompt and callee also heard 
                                 * something. So, we consider this iteration to have no error in this rule, and continue to apply more 
                                 * rules.
                                 */
                                result = ValidationErrors.NO_ERROR;
                                rule = NextRule.LatencyRule;
                            }
                            else
                            {
                                // We should not get here. This condition could only be reached if caller detected speech and could not 
                                // play prompt.
                                // For now, we consider this ignored, and apply no more rules for this iteration - Nirav 16th Jan 2007.
                                result = ValidationErrors.BAD_SCENARIO_EXECUTION;
                                rule = NextRule.None;
                            }
            }
            return result;
        }

        /// <summary>
        /// This rule uses causality of events to detect echoes and noise
        /// If callee hears before caller speaks and after callee speaks, it is considered an echo (case 1)
        /// If caller hears before callee speaks (or callee does not speak), that situation is considered as phantom noise
        /// If callee hears before it speaks, that situation is also considered as phantom noise
        /// If none of the above situations occur, then the iteration is considered to pass as per this rule.
        /// </summary>
        /// <returns></returns>
        private int applyLatencyValidationRule()
        {
            DateTime uninitDate = new DateTime();
            int result = ValidationErrors.NO_ERROR;

            /**
             * if caller heard before callee spoke, that is noise on caller's side
             */
            if (calleeData.speakTime != uninitDate && callerData.speechDetectionTime != uninitDate &&
                callerData.speechDetectionTime < calleeData.speakTime)
            {
                result = result | ValidationErrors.CALLER_NOISE_DETECTED;
            }
           
            /**
             * Noise on callee side is defined as occurrence of speech detection event before both caller and callee
             * have spoken.
             */
            if (calleeData.speechDetectionTime != uninitDate && calleeData.speakTime != uninitDate && callerData.speakTime != uninitDate &&
                calleeData.speechDetectionTime < calleeData.speakTime && calleeData.speechDetectionTime < callerData.speakTime)
            {
                result = result | ValidationErrors.CALLEE_NOISE_DETECTED;
            }
          
            /**
             * If callee heard not before its own speak time, but before caller spoke, that is 
             * echo.
             */
            if (calleeData.speakTime != uninitDate && calleeData.speechDetectionTime != uninitDate &&
                callerData.speakTime != uninitDate &&
                (calleeData.speakTime <= calleeData.speechDetectionTime) &&
                (calleeData.speechDetectionTime < callerData.speakTime))
            {
                result = result | ValidationErrors.ECHO_DETECTED;
            }
            return result;
        }

        /// <summary>
        /// Method to apply speech recognition rule. This method can 
        /// 1) detect caller side echoes as long as the recognizer on the caller recognizes its own speech.
        /// 2) detect callee side echoes if the callee recognizer recognizes its own speech
        /// 3) identifies whether caller or callee recognized/mis-recognized or did not recognize speech
        /// </summary>
        /// <param name="calleeSpeechResult">Callee's speech recognizer result</param>
        /// <param name="callerSpeechResult">Caller's speech recognizer result</param>
        /// <returns></returns>
        private int applySpeechRecognitionRule(out SpeechRecognizerResult calleeSpeechResult, out SpeechRecognizerResult callerSpeechResult)
        {
            DateTime uninitDate = new DateTime();
            int result = ValidationErrors.NO_ERROR;
            string grammarPropertyNameCallerPlayed = null;       // Property name in callee's grammar for file played by caller
            string grammarPropertyNameCalleePlayed = null;       // Property name in callee's grammar for file played by callee
            List<string> callerRecognizedPropNames = null;       // List of property names recognized by caller
            List<string> calleeRecognizedPropNames = null;       // List of property names recognized by callee

            calleeSpeechResult = null;
            callerSpeechResult = null;

            // Obtain grammar property name for wav file played by caller and callee
            if (callerData.speakTime != uninitDate)
                grammarPropertyNameCallerPlayed = wInfo.getGrammarPropertyName(callerData.wavFilePlayed);

            if (calleeData.speakTime != uninitDate)
                grammarPropertyNameCalleePlayed = wInfo.getGrammarPropertyName(calleeData.wavFilePlayed);

           /**
            * ASSUMPTIONS:
            * 1) The wav file played by caller and callee is recognizable by other party (i.e.) it has
            *      a) Representation in each other's grammar
            *      b) Representation in mapfile (WavFileName mapped to grammar tag in caller and callee's grammar
            *      Thus, no validation is required.
            * 2) Caller and the callee ALWAYS use different set of wav files. Thus, if callee recognizes a file
            *    played by itself, that situation is considered as an echo on callee side. If a caller recognizes
            *    a file played by itself, that is considered as an echo on the caller side.
            */
            callerRecognizedPropNames = callerData.getRecognizedGrammarPropertyNames();

            /**
             * We apply caller's speech recognition rule only if the caller detected speech from the callee
             */
            if (callerData.speechDetectionTime != uninitDate)
            {
                if (callerRecognizedPropNames.Count == 0)
                {
                    // This means that caller could not recognize anything.
                    callerSpeechResult = new SpeechRecognizerResult();
                    callerSpeechResult.speechOutcome = SpeechRecognizerQuality.UNRECOGNIZED;
                    callerSpeechResult.msg = "Caller did not recognize speech";
                }
                else
                if (grammarPropertyNameCallerPlayed != null && callerRecognizedPropNames.Contains(grammarPropertyNameCallerPlayed))
                {
                    // This means that the caller heard self. This is an echo on the caller side
                    result = ValidationErrors.ECHO_DETECTED;
                    callerSpeechResult = new SpeechRecognizerResult();
                    callerSpeechResult.speechOutcome = SpeechRecognizerQuality.CALLER_ECHO;
                    callerSpeechResult.msg = "Caller heard its own audio";
                }
                else
                if (grammarPropertyNameCalleePlayed != null && callerRecognizedPropNames.Contains(grammarPropertyNameCalleePlayed))
                {
                    // This means that the caller correctly heard the callee. Thus, speech is considered to be "recognized" here.
                    callerSpeechResult = new SpeechRecognizerResult();
                    callerSpeechResult.speechOutcome = SpeechRecognizerQuality.RECOGNIZED;
                    callerSpeechResult.msg = "Caller recognized the callee";
                    callerSpeechResult.property = grammarPropertyNameCalleePlayed;
                    callerSpeechResult.confidence = callerData.getConfidence(grammarPropertyNameCalleePlayed);
                }
                else
                {
                    // Caller recognized something other than its own audio and audio from the callee. We treat this "misrecognized".
                    callerSpeechResult = new SpeechRecognizerResult();
                    callerSpeechResult.speechOutcome = SpeechRecognizerQuality.MISRECOGNIZED;
                    callerSpeechResult.msg = "Caller mis-recognized speech.";
                }
            }
            calleeRecognizedPropNames = calleeData.getRecognizedGrammarPropertyNames();

            /**
             * We apply callee's speech recognition rule only if the callee detected speech from the caller
             */
            if (calleeData.speechDetectionTime != uninitDate)
            {
                if (calleeRecognizedPropNames.Count == 0)
                {
                    // Callee did not recognize anything
                    calleeSpeechResult = new SpeechRecognizerResult();
                    calleeSpeechResult.speechOutcome = SpeechRecognizerQuality.UNRECOGNIZED;
                    calleeSpeechResult.msg = "Callee did not recognize speech";
                }
                else
                if (grammarPropertyNameCalleePlayed != null && calleeRecognizedPropNames.Contains(grammarPropertyNameCalleePlayed))
                {
                    // Callee heard self. This is an echo.
                    calleeSpeechResult = new SpeechRecognizerResult();
                    result = ValidationErrors.ECHO_DETECTED;
                    calleeSpeechResult.speechOutcome = SpeechRecognizerQuality.CALLEE_ECHO;
                    calleeSpeechResult.msg = "Callee heard its own audio";
                }
                else
                if (grammarPropertyNameCallerPlayed != null && calleeRecognizedPropNames.Contains(grammarPropertyNameCallerPlayed))
                {
                    // Callee recognized the callee. This is considered as "speech recognized".
                    calleeSpeechResult = new SpeechRecognizerResult();
                    calleeSpeechResult.speechOutcome = SpeechRecognizerQuality.RECOGNIZED;
                    calleeSpeechResult.msg = "Callee recognized the caller";
                    calleeSpeechResult.property = grammarPropertyNameCallerPlayed;
                    calleeSpeechResult.confidence = calleeData.getConfidence(grammarPropertyNameCallerPlayed);
                }
                else
                {
                    // Callee heard something besides its own speech or caller's speech. This is considered "mis-recognized".
                    calleeSpeechResult = new SpeechRecognizerResult();
                    calleeSpeechResult.speechOutcome = SpeechRecognizerQuality.MISRECOGNIZED;
                    calleeSpeechResult.msg = "Callee mis-recognized speech";
                }
            }
            return result;
        }
        ///// <summary>
        ///// Method to apply speech recognition rule. If an echo is delayed to pass the timestamp rule, this rull will catch it
        ///// as long as callee can recognize the speech being played as its own.
        ///// </summary>
        ///// <param name="speechResult"></param>
        ///// <returns></returns>
        //private int applySpeechRecognitionRule(out SpeechRecognizerResult speechResult)
        //{
        //    DateTime uninitDate = new DateTime();
        //    int result = ValidationErrors.NO_ERROR;
        //    string grammarPropertyNameCallerPlayed = null;       // Property name in callee's grammar for file played by caller
        //    string grammarPropertyNameCalleePlayed = null;       // Property name in callee's grammar for file played by callee
        //    speechResult = null;

        //    // Obtain grammar property name for wav file played by caller and callee
        //    if (callerData.speakTime != uninitDate)
        //        grammarPropertyNameCallerPlayed = wInfo.getGrammarPropertyName(callerData.wavFilePlayed);

        //    if (calleeData.speakTime != uninitDate)
        //        grammarPropertyNameCalleePlayed = wInfo.getGrammarPropertyName(calleeData.wavFilePlayed);

        //    /**
        //     * ASSUMPTIONS:
        //     * 1) The wav file played by caller and callee is recognizable by callee (i.e.) it has
        //     *      a) Representation in callee's grammar
        //     *      b) Representation in mapfile (WavFileName mapped to grammar tag in callee's grammar
        //     *      Thus, no validation is required.
        //     * 2) Caller and the callee ALWAYS use different set of wav files. Thus, if callee recognizes a file
        //     *    played by itself, that situation is considered as an echo.
        //     */
        //    if (calleeData.speechDetectedStatus == RecognitionResult.RECOGNIZED)
        //    {
        //        // If callee heard caller's audio, pass the iteration.
        //        // If callee heard it's own audio, fail the iteration due to echo.
        //        if (grammarPropertyNameCalleePlayed != null)
        //        {
        //            if (calleeData.grammarProperty.Equals(grammarPropertyNameCalleePlayed, StringComparison.CurrentCultureIgnoreCase))
        //            {
        //                // Echo
        //                speechResult = new SpeechRecognizerResult();
        //                speechResult.speechOutcome = SpeechRecognizerQuality.ECHODETECTED;
        //                speechResult.msg = "Echo Detected. Callee heard it's own audio.";
        //                result = ValidationErrors.ECHO_DETECTED;
        //            }
        //            else
        //                if (grammarPropertyNameCallerPlayed != null)
        //                {
        //                    if (calleeData.grammarProperty.Equals(grammarPropertyNameCallerPlayed, StringComparison.CurrentCultureIgnoreCase))
        //                    {
        //                        // Passed
        //                        speechResult = new SpeechRecognizerResult();
        //                        speechResult.speechOutcome = SpeechRecognizerQuality.RECOGNIZED;
        //                        speechResult.msg = "Speech recognized";
        //                    }
        //                    else
        //                    {
        //                        // We consider this mis-recognized
        //                        speechResult = new SpeechRecognizerResult();
        //                        speechResult.speechOutcome = SpeechRecognizerQuality.MISRECOGNIZED;
        //                        speechResult.msg = callerData.wavFilePlayed + " recognized as " + calleeData.textEquivalent;
        //                    }
        //                }
        //        }
        //    }
        //    else
        //    {
        //        // Since audio was not recognized, we can't reliably say there was echo, so we call it passed
        //        speechResult = new SpeechRecognizerResult();
        //        speechResult.speechOutcome = SpeechRecognizerQuality.UNRECOGNIZED;

        //        // Check if caller actually played audio before constructing a message
        //        if (callerData.wavFilePlayed.Equals("null", StringComparison.CurrentCultureIgnoreCase) == false)
        //            speechResult.msg = callerData.wavFilePlayed + " wav file played by caller could not be recognized";
        //        else
        //            speechResult.msg = "callee unable to recognize audio";
        //    }
        //    return result;
        //}
    }
}

