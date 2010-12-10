using System;
using System.Collections.Generic;
using System.Text;
using GatewayTestLibrary;
using WavFileReader;
using System.IO;

namespace ResultAnalyzer
{
    /// <summary>
    /// This class contains the necessary functionality to compute and present the aggregate test
    /// results.
    /// </summary>
    class AggregateResult
    {
        private List<IterationResult> iterationList;    // List of all the iterations for the test

        #region Test result counters
        private int totalIterations;                    // Total iterations executed
        private int cntPassedIterations;                // Number of passed iterations
        private int cntFailedIterations;                // Number of iterations with bad results
        private int cntInvalidIterations;               // Number of iterations that could not be considered in analysis due to either
                                                        // failure in execution scenario or bad log file format

        #region Error counters
        private int cntCallFailed;                      // Number of times calls failed to connect
        private int cntMissingHangup;                   // Number of times the callee did not receive hangup from the caller
        private int cntEchoDetected;                    // Number of iterations where echo was detected
        private int cntNoiseDetected;                   // Number of iterations where random noise was detected over voice channel
        private int cntCallerSpeechDetFailure;          // When caller could not detect speech from callee
        private int cntCalleeSpeechDetFailure;          // When callee could not detect speech from caller
        #endregion

        #region Spech recognition counters for caller and callee
        private int cntMisrecognizedCaller;             // Number of iterations where caller misrecognized speech
        private int cntRecognizedCaller;                // Number of iterations where caller correctly recognized speech 
        private int cntUnrecognizedCaller;              // Number of iterations where caller did not recognize anything that was heard
        private int cntEchoCaller;                      // Number of iterations where caller's recognizer detected echo

        private int cntMisrecognizedCallee;             // Number of iterations where callee misrecognized speech
        private int cntRecognizedCallee;                // Number of iterations where callee correctly recognized speech 
        private int cntUnrecognizedCallee;              // Number of iterations where callee did not recognize anything that was heard
        private int cntEchoCallee;                      // Number of iterations where callee's recognizer detected echo

        private double avgConfidenceCaller;             // Average confidence for all iterations with speech recognized - caller side
        private double sumConfidenceCaller;             // Sum of confidence for all iterations with speech recognized - caller side
        private double avgConfidenceCallee;             // Average confidence for all iterations with speech recognized - callee side
        private double sumConfidenceCallee;             // Sum of confidence for all iterations with speech recognized - callee side

        private int cntAttenuatedSignal = 0;                // Number of iterations where audio attenuation occurred
        private int cntLowConfidenceAttenuatedSignal = 0;    // Number of iterations where attenuation analyzer had low confidence
        #endregion

        private int cntBadResultLine;                   // Indicates bad token in lines used for parsing

        #region Call connection / release latency counters
        private double connectLatency;                  // Sum of call connection latencies for all established calls
        private double releaseLatency;                  // sum of call release latencies for all established calls
        private double maxConnectLatency;               // Maximum call connection latency
        private double maxReleaseLatency;               // Maximum call release latency
        #endregion

        private Dictionary<string, int> callerIDColl;   // Collection of caller IDs and number of occurrences of that caller ID  

        private Analyzer analyzer;
        #endregion

        /// <summary>
        /// Class constructor
        /// </summary>
        public AggregateResult(Analyzer _analyzer)
        {
            iterationList = new List<IterationResult>();
            callerIDColl = new Dictionary<string,int>();
            this.analyzer = _analyzer;

            cntPassedIterations = 0;
            cntFailedIterations = 0;
            cntInvalidIterations = 0;
            cntCallFailed = 0;
            cntMissingHangup = 0;
            totalIterations = 0;
            cntEchoDetected = 0;
            cntNoiseDetected = 0;
            
            avgConfidenceCaller = 0;
            sumConfidenceCaller = 0;
            avgConfidenceCallee = 0;
            sumConfidenceCallee = 0;
            cntBadResultLine = 0;
          
            cntCallerSpeechDetFailure = 0;
            cntCalleeSpeechDetFailure = 0;

            connectLatency = 0;
            releaseLatency = 0;
            maxConnectLatency = 0;
            maxReleaseLatency = 0;

            cntMisrecognizedCaller = 0;
            cntRecognizedCaller = 0;
            cntUnrecognizedCaller = 0;
            cntMisrecognizedCallee = 0;
            cntRecognizedCallee = 0;
            cntUnrecognizedCallee = 0;
            cntEchoCaller = 0;
            cntEchoCallee = 0;
        }

        /// <summary>
        /// Method to preserve the iteration result object
        /// </summary>
        /// <param name="ir"></param>
        public void addIterationResult(IterationResult ir)
        {
            string callerID;
            int temp;
            double tempLatency;

            if (ir != null)
            {
                totalIterations++;
                iterationList.Add(ir);

                if (true == ir.getCallConnectionLatency(out tempLatency))
                {
                    connectLatency += tempLatency;

                    if (tempLatency > maxConnectLatency)
                        maxConnectLatency = tempLatency;
                }

                if (true == ir.getCallHangupLatency(out tempLatency))
                {
                    releaseLatency += tempLatency;

                    if (tempLatency > maxReleaseLatency)
                        maxReleaseLatency = tempLatency;
                }

                if (ir.calleeIterationData != null)
                {
                    callerID = ir.calleeIterationData.remotePartyExtension;

                    if(callerID != null)
                    {
                        if (!callerIDColl.ContainsKey(callerID))
                            callerIDColl.Add(callerID, 1);
                        else
                        {
                            temp = callerIDColl[callerID];
                            callerIDColl[callerID] = ++temp;
                        }
                    }
                }
                #region Code segment to update pass/fail and cause for failure counters

                if (ir.ResultCategory == TestIterationCategory.INVALID)
                {
                    cntInvalidIterations++;
                }
                else
                if (ir.ResultCategory == TestIterationCategory.FAILED)
                {
                    cntFailedIterations++;
                }
                else
                    if (ir.ResultCategory == TestIterationCategory.PASSED)
                    {
                        cntPassedIterations++;
                        string callerFilePlayed = ir.callerIterationData.wavFilePlayed;
                        string calleeFilePlayed = ir.calleeIterationData.wavFilePlayed;
                        string callerFileRecorded = analyzer.resultDir + "\\Caller_RecordedWav_" + (iterationList.Count).ToString() + "_matched.wav";
                        string calleeFileRecorded = analyzer.resultDir + "\\Callee_RecordedWav_" + (iterationList.Count).ToString() + "_matched.wav";


                        if (File.Exists(callerFileRecorded) && File.Exists(calleeFileRecorded))
                        {
                            AudioVolumeConfidence callerVolConf;
                            AudioVolumeConfidence calleeVolConf;
                            AudioVolume callerVolumeResult = isSignalAttenuated(calleeFilePlayed, callerFileRecorded, out callerVolConf);
                            AudioVolume calleeVolumeResult = isSignalAttenuated(callerFilePlayed, calleeFileRecorded, out calleeVolConf);

                            if (calleeVolumeResult == AudioVolume.ATTENUATED || callerVolumeResult == AudioVolume.ATTENUATED)
                            {
                                cntAttenuatedSignal++;
                                ir.audioPowerResult = AudioVolume.ATTENUATED;

                                if (callerVolConf == AudioVolumeConfidence.LOW_CONFIDENCE || calleeVolConf == AudioVolumeConfidence.LOW_CONFIDENCE)
                                {
                                    ir.audioVolConf = AudioVolumeConfidence.LOW_CONFIDENCE;
                                    cntLowConfidenceAttenuatedSignal++;
                                }
                                if (callerVolumeResult == AudioVolume.ATTENUATED)
                                {
                                    Console.WriteLine("Caller : Audio Signal Attenuation");

                                    if (callerVolConf == AudioVolumeConfidence.LOW_CONFIDENCE)
                                    {
                                        Console.WriteLine("Confidence of Signal Analyzer : Low");
                                    }
                                    else
                                    {
                                        Console.WriteLine("Confidence of Signal Analyzer : Good");
                                    }
                                }
                                if (calleeVolumeResult == AudioVolume.ATTENUATED)
                                {
                                    Console.WriteLine("Callee : Audio Signal Attenuation");
                                    if (calleeVolConf == AudioVolumeConfidence.LOW_CONFIDENCE)
                                    {
                                        Console.WriteLine("Confidence of Signal Analyzer : Low");
                                    }
                                    else
                                    {
                                        Console.WriteLine("Confidence of Signal Analyzer : Good");
                                    }
                                }
                            }
                        }
                    }
                if ((ir.ResultCode & ValidationErrors.FAILED_CALL) != 0)
                    cntCallFailed++;
                if ((ir.ResultCode & ValidationErrors.MISSING_HANGUP) != 0)
                    cntMissingHangup++;
                if (((ir.ResultCode & ValidationErrors.CALLEE_NOISE_DETECTED) != 0)
                    || ((ir.ResultCode & ValidationErrors.CALLER_NOISE_DETECTED) != 0))
                    cntNoiseDetected++;
                if ((ir.ResultCode & ValidationErrors.ECHO_DETECTED) != 0)
                    cntEchoDetected++;
                if ((ir.ResultCode & ValidationErrors.CALLEE_NOT_HEARD) != 0)
                    cntCallerSpeechDetFailure++;
                if ((ir.ResultCode & ValidationErrors.CALLER_NOT_HEARD) != 0)
                    cntCalleeSpeechDetFailure++;

                if (ir.callerSpeechRecognizerResult != null)
                {
                    switch (ir.callerSpeechRecognizerResult.speechOutcome)
                    {
                        case SpeechRecognizerQuality.UNRECOGNIZED:
                            cntUnrecognizedCaller++;
                            break;

                        case SpeechRecognizerQuality.MISRECOGNIZED:
                            cntMisrecognizedCaller++;
                            break;

                        case SpeechRecognizerQuality.RECOGNIZED:
                            cntRecognizedCaller++;
                            sumConfidenceCaller = sumConfidenceCaller + ir.callerSpeechRecognizerResult.confidence;
                            break;

                        case SpeechRecognizerQuality.CALLER_ECHO:
                            cntEchoCaller++;
                            break;
                    }
                }

                if (ir.calleeSpeechRecognizerResult != null)
                {
                    switch (ir.calleeSpeechRecognizerResult.speechOutcome)
                    {
                        case SpeechRecognizerQuality.UNRECOGNIZED:
                            cntUnrecognizedCallee++;
                            break;

                        case SpeechRecognizerQuality.MISRECOGNIZED:
                            cntMisrecognizedCallee++;
                            break;

                        case SpeechRecognizerQuality.RECOGNIZED:
                            cntRecognizedCallee++;
                            sumConfidenceCallee = sumConfidenceCallee + ir.calleeSpeechRecognizerResult.confidence;
                            break;

                        case SpeechRecognizerQuality.CALLEE_ECHO:
                            cntEchoCallee++;
                            break;
                    }
                }
                #endregion
            }
        }

        /// <summary>
        /// Private helper method to analyze if the recorded wav file has audio signal volume loss by comparing it with the
        /// played audio wav file.
        /// </summary>
        /// <param name="filePlayed"></param>
        /// <param name="fileRecorded"></param>
        /// <param name="audioVolConf"></param>
        /// <returns></returns>
        private AudioVolume isSignalAttenuated(string filePlayed, string fileRecorded, out AudioVolumeConfidence audioVolConf)
        {
            AudioVolume result = AudioVolume.NOT_ATTENUATED;
            audioVolConf = AudioVolumeConfidence.GOOD_CONFIDENCE;

            // The tolerance represents the % difference between the number of frames used to compute
            // average power between the played and the recorded files.
            // If the number of frames used to compute the average power differs more than the threshold, 
            // then it implies that the audio waveform could be different, and hence, should not be analyzed
            // for signal attenuation, since the answer would not be reliable.
            double tolerance = 0.1;

            // Represents the minimum difference in power levels between the recorded and the played files that should be
            // considered to flag audio signal attenuation warning
            double powerThreshold = 6.0;

            WavDataCharacteristics wdRec = WavFile.getInstance(fileRecorded).analyzeData();
            WavDataCharacteristics wdPlayed = WavFile.getInstance(filePlayed).analyzeData();

            double pRec = wdRec.AveragePower;
            double fRec = Convert.ToDouble(wdRec.NumFramesForAvgPower);

            double pPlayed = wdPlayed.AveragePower;
            double fPlayed = Convert.ToDouble(wdPlayed.NumFramesForAvgPower);

            /**
             * If the difference in the power levels between the played file and the recorded exceeds the power threshold,
             * it implies signal attenuation. Thus, in this case, we determine the confidence level of the algorithm.
             * If the number of frames used to compute the average power in the recorded file is more than the number of frames used to 
             * compute the average power from the played file, it implies distortion of the recorded file. Thus, we set the confidence level
             * of the algorithm to "LOW". To determine the confidence level, if the number of frames used to compute avg. power in the recorded
             * file exceeds the number of frames used to compute avg. power in the played file by a threshold percentage, we set the confidence
             * level to "low", or set the confidence level to "Good" otherwise.
             */
            if (pPlayed - pRec >= powerThreshold)
            {
                Console.WriteLine("Power Level of Played File = " + pPlayed + " Power Level of Recorded File = " + pRec + " . Flagging Signal Attenuation Warning");
                result = AudioVolume.ATTENUATED;

                if (fRec > fPlayed + tolerance * fPlayed)
                {
                    audioVolConf = AudioVolumeConfidence.LOW_CONFIDENCE;
                }
                else
                {
                    audioVolConf = AudioVolumeConfidence.GOOD_CONFIDENCE;
                }
            }
            else
            {
                result = AudioVolume.NOT_ATTENUATED;
            }
            return result;
        }
        
        private void compute()
        {
            if (cntRecognizedCaller > 0)
            {
                avgConfidenceCaller = sumConfidenceCaller / cntRecognizedCaller;
            }
            else
            {
                avgConfidenceCaller = 0;
            }

            if (cntRecognizedCallee > 0)
            {
                avgConfidenceCallee = sumConfidenceCallee / cntRecognizedCallee;
            }
            else
            {
                avgConfidenceCallee = 0;
            }
        }

        /// <summary>
        /// Method to truncate double values to 3 decimals
        /// </summary>
        /// <param name="temp"></param>
        /// <returns></returns>
        private string truncate(string temp)
        {
            int idx = temp.IndexOf('.');

            if (idx >= 0 && temp.Length - idx > 3)
            {
                return temp.Substring(0, idx + 4);
            }
            else
                return temp;
        }

        /// <summary>
        /// Method to generate a string representation of the object's state for display and logging purposes
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            string temp;

            compute();

            temp = truncate(Convert.ToString((totalIterations > 0 ? cntPassedIterations * 1.0 / totalIterations * 100.0 : 0)));

            result.Append("\r\n\t\t\tTEST SUMMARY\r\n");
            result.Append("****************************************************\r\n");
            result.Append("\r\n");
            result.Append("Pass Percentage = " + temp + "%\r\n\r\n");
            result.Append("Total Iterations = " + totalIterations + "\r\n");
            result.Append("Passed Iterations = " + cntPassedIterations + "\r\n");
            result.Append("Failed Iterations = " + cntFailedIterations + "\r\n");

            if (cntInvalidIterations > 0)
            {
                result.Append("Ignored Iterations = " + cntInvalidIterations + "\r\n");
            }

            result.Append("\r\nAudio Quality:\r\n");
            result.Append("\tNum. calls having echo = " + cntEchoDetected + "\r\n");
            result.Append("\tNum. calls having phantom noise = " + cntNoiseDetected + "\r\n");
            result.Append("\tNum. calls where callee did not detect caller's audio = " + cntCalleeSpeechDetFailure + "\r\n");
            result.Append("\tNum. calls where caller did not detect callee's audio = " + cntCallerSpeechDetFailure + "\r\n");

            result.Append("\r\n\r\n");

            result.Append("Call Characteristics\r\n");
            result.Append("\tNum. call establishment failures = " + cntCallFailed + "\r\n");
            result.Append("\tNum. calls where callee failed to detect hangup = " + cntMissingHangup + "\r\n");

            if (totalIterations - cntCallFailed > 0)
            {
                result.Append("\tCall Connection Latency: Avg = " + truncate(Convert.ToString(connectLatency * 1.0 / (totalIterations - cntCallFailed))) + " sec. Max = " + maxConnectLatency + " sec\r\n");
                result.Append("\tCall Hangup Latency: Avg = " + truncate(Convert.ToString(releaseLatency * 1.0 / (totalIterations - cntCallFailed))) + " sec. Max = " + maxReleaseLatency + " sec\r\n");
            }
            result.Append("\r\n");

            // If caller IDs are available, check for inconsistencies in caller IDs
            if (callerIDColl.Count > 0)
            {
                result.Append("Caller ID Detection: ");

                if (callerIDColl.Count == 1)
                {
                    result.Append("Consistent\r\n");
                }
                else
                {
                    result.Append("Inconsistent\r\n\tDifferent calls showed different caller IDs\r\n");
                }

                foreach (KeyValuePair<string, int> kvp in callerIDColl)
                {
                    result.Append("\tCaller ID " + kvp.Key + " was found in " + kvp.Value + " calls\r\n");
                }
            }

            result.Append("\r\nCaller's Speech Recognizer's Outcome\r\n");
            result.Append("\tIterations with correctly recognized speech = " + cntRecognizedCaller + "\r\n");
            result.Append("\tIterations with misrecognized speech = " + cntMisrecognizedCaller + "\r\n");
            result.Append("\tIterations with unrecognized speech = " + cntUnrecognizedCaller + "\r\n");
            result.Append("\tIterations where recognizer deteced echo = " + cntEchoCaller + "\r\n");
            result.Append("\tAvg. Confidence of recognized speech = " + truncate(Convert.ToString(avgConfidenceCaller)) + "\r\n\r\n");

            result.Append("\r\nCallee's Speech Recognizer's Outcome\r\n");
            result.Append("\tIterations with correctly recognized speech = " + cntRecognizedCallee + "\r\n");
            result.Append("\tIterations with misrecognized speech = " + cntMisrecognizedCallee + "\r\n");
            result.Append("\tIterations with unrecognized speech = " + cntUnrecognizedCallee + "\r\n");
            result.Append("\tIterations where recognizer detected echo = " + cntEchoCallee + "\r\n");
            result.Append("\tAvg. Confidence of recognized speech = " + truncate(Convert.ToString(avgConfidenceCallee)) + "\r\n");


            if (cntLowConfidenceAttenuatedSignal > 0 || cntAttenuatedSignal > 0)
            {
                result.Append("\r\nAudio Signal Strength Analysis\r\n");
                if (Math.Max(0, cntAttenuatedSignal - cntLowConfidenceAttenuatedSignal) > 0)
                {
                    result.Append("\tIterations with Reduced Volume (Good Confidence) = " + Math.Max(0, (cntAttenuatedSignal - cntLowConfidenceAttenuatedSignal)) + "\r\n");
                }

                if (cntLowConfidenceAttenuatedSignal > 0)
                {
                    result.Append("\tIterations with Reduced Volume (Low Confidence) = " + cntLowConfidenceAttenuatedSignal + "\r\n");
                }
            }
            result.Append("****************************************************");
            return result.ToString();
        }

        /// <summary>
        /// Method to display results to the console and to a specified result file
        /// </summary>
        /// <param name="logFile"></param>
        public void displayResult(string logFile)
        {
            Logger logger = null;

            try
            {
                logger = Logger.getInstance(logFile);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in AggregateResult.computeResult. Message : " + e.Message);
            }

            Console.WriteLine(this.ToString());

            if (logger != null)
            {
                logger.writeLog(this.ToString());

                logger.writeLog("\r\n");
                logger.writeLog("Individual Iteration Results\n");

                for (int i = 0; i < iterationList.Count; i++)
                {
                    logger.writeLog("Iteration " + (i + 1));
                    logger.writeLog(iterationList[i].ToString());
                    logger.writeLog("\r\n");
                }
                logger.closeFile();
            }
        }
    }
}
