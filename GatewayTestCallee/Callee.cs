using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Microsoft.CSTA;
using System.Timers;
using GatewayTestLibrary;
using System.Xml;
using EdbQa.Common.CDSHelper;

namespace GatewayTestCallee
{
    enum MessageType
    {
        INFO,
        WARNING,
        ERROR
    };

    /// <summary>
    /// Class that represents the use case scenario of a gateway test callee
    /// </summary>
    class Callee
    {
        #region CSTA related references
        private Provider p;                                     // CSTA provider reference
        private VoiceDevice phone;                              // VoiceDevice instance
        private Connection conn;                                // Stores currently established connection
        private Prompt pm;                                      // Prompt instance to speak on voice channel
        private Listener ll;                                    // Listener instance to detect speech on voice channel
        private Recorder recorder;                              // CSTA Recorder
        #endregion

        ConfigParameters inputParam;                            // Stores the configuration parameters

        private CalleeIterationInfo calleeData;                // Contains the info of one callee iteration   
        #region Call related parameters
        private CurrentState currentState;                      // Current state of the Callee
    
        private System.Windows.Forms.Timer promptTimer;         // Indicates the duration to wait for before speaking on prompt to 
                                                                // ensure that listener on caller will be started
    //    private int numIterations;
        private int currIter = 0;                               // Current call iteration

        private bool isRegistered;                              // Is user agent registered

        #region Configurable Parameters - whose value can be changed through config file
        private int totalIter = 100;                            // Number of iterations in one set
        private int totalSets = 3;                              // Number of sets
        private bool allowRecording = false;                    // whether to allow recording of the calls
        private int promptWaitDuration = 3;                     // Indicates time to wait before firing prompt after getting event established
        private int maxCallDuration = 70;                       // Max call duration in seconds
        private int waitTimeBetweenCalls = 20;                  // Wait time between calls in seconds
        #endregion
        #endregion

        // Timer to detect missing hangup - initialized to 3 times maxCallDuration
        private System.Windows.Forms.Timer missingHangupDetectionTimer;
        private volatile bool missingHangupDetected = false;    // Flag to keep track of when missing hangup is detected

        #region Event delegates to send iteration info objects to the driver
        public delegate void IterationCompletedEventHandler(object sender, IterationInfo iInfo);
        public event IterationCompletedEventHandler OnIterationCompleted;
        #endregion

        /// <summary>
        /// Class constructor
        /// </summary>
        public Callee(ConfigParameters _inputParam)
        {
            this.inputParam = _inputParam;
            initializeCallee();
        }

        /// <summary>
        /// Private helper method for the constructors
        /// </summary>
        private void initializeCallee()
        {
            conn = null;
            phone = null;
            pm = null;
            ll = null;
            recorder = null;
            calleeData = null;
            isRegistered = false;

            /**
             * Read the configuration params file and populate suitable parameters
             */
            readConfigParams();
            display("Configration file = " + inputParam.configFile);

 //           this.numIterations = totalIter * totalSets;

            display("Extension = " + inputParam.myExtension);
            
            /**
             * Create an instance of prompt wait timer
             */
            promptTimer = new System.Windows.Forms.Timer();
            promptTimer.Enabled = false;
            promptTimer.Interval = promptWaitDuration * 1000;
            promptTimer.Tick += new System.EventHandler(promptTimer_Tick);

            missingHangupDetectionTimer = new System.Windows.Forms.Timer();
            missingHangupDetectionTimer.Enabled = false;

            // Missing hangup detection timer is set to 5 times maximum call duration and wait time between calls
            missingHangupDetectionTimer.Interval = 5 * (maxCallDuration + waitTimeBetweenCalls) * 1000;
            missingHangupDetectionTimer.Enabled = false;
            missingHangupDetectionTimer.Tick += new System.EventHandler(missingHangupDetectionTimer_Tick);
            initializeVoiceDevice();
        }

        #region Private helper methods to log messages to console and tracelog
        private void display(string displayString)
        {
            display(displayString, MessageType.INFO);
        }

        private void display(string displayString, MessageType msgType)
        {
            Console.WriteLine(displayString);
            switch (msgType)
            {
                case MessageType.INFO:
                    Trace.WriteLine(displayString + " at time = " + DateTime.Now);
                    break;

                case MessageType.WARNING:
                    Trace.TraceWarning(displayString + " at time = " + DateTime.Now);
                    break;

                case MessageType.ERROR:
                    Trace.TraceError(displayString + " at time = " + DateTime.Now);
                    break;
            }
        }
        #endregion

        /// <summary>
        /// Method to read the configuration parameters and use those values if they pass the validation rules, 
        /// else use default values.
        /// </summary>
        private void readConfigParams()
        {
            StreamReader reader = null;     // Temp file handle
            string line = null;             // Temp variable
            int temp;

            try
            {
                reader = new StreamReader(inputParam.configFile);

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("CALL_DURATION=") && ((temp = Helper.parseValue(line)) > 30))
                    {
                        maxCallDuration = temp;
                    }
                    if (line.Contains("INTER_CALL_INTERVAL=") && ((temp = Helper.parseValue(line)) > 3))
                    {
                        waitTimeBetweenCalls = temp;
                    }
                    if (line.Equals("CALLEE_RECORD=YES", StringComparison.CurrentCultureIgnoreCase) == true)
                    {
                        allowRecording = true;
                    }
                    if (line.Contains("MAX_CALLEE_PROMPT_WAIT_INTERVAL=") && ((temp = Helper.parseValue(line)) > 0))
                    {
                        promptWaitDuration = temp;
                    }
                    if (line.Contains("COUNT_ITERATIONS=") && ((temp = Helper.parseValue(line)) > 0))
                    {
                        totalIter = temp;
                    }
                    if (line.Contains("COUNT_SETS=") && ((temp = Helper.parseValue(line)) > 0))
                    {
                        totalSets = temp;
                    }
                }
                reader.Close();
            }
            catch (Exception e)
            {
                display("Exception in reading custom configuration Parameters. Using Default Config Values", MessageType.WARNING);
            }
        }

        #region Helper method to create voice device with a prompt and listener
        /// <summary>
        /// Private helper method to initialize the CSTA voice device
        /// </summary>
        private void initializeVoiceDevice()
        {
            p = new Provider(inputParam.sipServerIP, new CredentialManager(inputParam.sipServerIP));

            Phone cdsPhone = Utilities.GetPhone(inputParam.sipServerIP, Utilities.LocalMacAddresses[0], inputParam.myExtension);
            if (cdsPhone == null)
            {
                throw new ApplicationException("Cannot find phone owned by " + inputParam.myExtension + " on server");
            }

            /**
             * To create an instance of VoiceDevice, use GenericInteractiveVoice as the flag.
             */
            phone = p.GetDevice(inputParam.myExtension, Provider.DeviceCategory.GenericInteractiveVoice, cdsPhone.FunctionId) as VoiceDevice;

            /**
             * Setup event handler methods
             */
            phone.Established += new EstablishedEventHandler(phone_Established);
            phone.ConnectionCleared += new ConnectionClearedEventHandler(phone_ConnectionCleared);
            phone.InfoEventReceived += new InfoEventHandler(phone_InfoEventReceived);
            phone.Originated += new OriginatedEventHandler(phone_Originated);
            phone.Delivered += new DeliveredEventHandler(phone_Delivered);
            phone.RegistrationStateChanged += new RegistrationStateChangedEventHandler(phone_RegistrationStateChanged);
            /**
             * Allocate a prompt
             */
            pm = phone.AllocatePrompt("CalleePrompt");
            pm.Started += new VoiceEventHandler(pm_Started);
            pm.Completed += new VoiceEventHandler(pm_Completed);
            pm.InterruptionDetected += new VoiceEventHandler(pm_InterruptionDetected);
            pm.VoiceErrorOccurred += new VoiceEventHandler(pm_VoiceErrorOccurred);
            prepareListener();   

            // Now prepare the recorder
            if (allowRecording == true)
                prepareRecorder();
        }

        /// <summary>
        /// Helper method that creates a new listener and prepares sets its event handlers
        /// </summary>
        private void prepareListener()
        {
            ll = null;
            ll = phone.AllocateListener("CalleeListener");
            ll.Mode = Listener.ModeType.Multiple;
            ll.LoadGrammar("generic", inputParam.grammarFileName);

            ll.ListenerReady += new ListenerReadyEventHandler(ll_ListenerReady);
            ll.SpeechDetected += new VoiceEventHandler(ll_SpeechDetected);
            ll.Started += new VoiceEventHandler(ll_Started);
            ll.VoiceErrorOccurred += new VoiceEventHandler(ll_VoiceErrorOccurred);
            ll.SilenceTimeoutExpired += new VoiceEventHandler(ll_SilenceTimeoutExpired);
            ll.Recognized += new RecognizedEventHandler(ll_Recognized);
            ll.NotRecognized += new NotRecognizedEventHandler(ll_NotRecognized);
            ll.Activate("GatewayTestCallee");
        }
        #endregion

        /// <summary>
        /// Helper method that creates a new recorder and sets its event handlers
        /// </summary>
        private void prepareRecorder()
        {
            recorder = null;

            try
            {
                recorder = phone.AllocateRecorder();
                recorder.Completed += new VoiceEventHandler(recorder_Completed);
            }
            catch (Exception e)
            {
                display("Exception in creating recorder. Message: " + e.Message + "\r\n" + e.StackTrace, MessageType.WARNING);
                recorder = null;
            }
        }

        void recorder_Completed(object sender, VoiceEventArgs args)
        {
            RecorderEventArgs recorderArgs = args as RecorderEventArgs;
            display("Recorder completed event received. Duration of recorded file = " + recorderArgs.Duration.ToString());
        }

        /// <summary>
        /// Helper method to stop listener and recorder
        /// </summary>
        private void stopListenerAndRecorder()
        {
            try
            {
                if (ll != null)
                    ll.Stop();
                if (recorder != null)
                {
                    display("stopListenerAndRecorder : Stopping recorder");
                    recorder.Stop();
                }
            }
            catch (Exception e)
            {
                display("Exception in stopListenerAndRecorder. Message: " + e.Message + " \r\nStack Trace = " + e.StackTrace, MessageType.WARNING);
            }
        }

        /// <summary>
        /// Helper method to process connection cleared when hangup is received or re-initialize callee if it detects that
        /// hangup for last iteration was missing.
        /// </summary>
        /// <param name="connClrTime">Timestamp of when connection cleared was received or non-initialized timestamp if it was missing</param>
        private void processHangup(DateTime connClrTime)
        {
            calleeData.callReleaseTime = connClrTime;
            stopListenerAndRecorder();
            sendCalleeDataToWatcher();
        }

        /// <summary>
        /// Event handler invoked when missing hangup is detected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void missingHangupDetectionTimer_Tick(object sender, System.EventArgs e)
        {
            missingHangupDetectionTimer.Enabled = false;
            missingHangupDetected = true;

            display("Missing hangup detection timer fired");
            if (conn != null)
            {
                display("Clearing the connection as a result of missingHangupDetectionTimer");
                conn.ClearConnection();
            }
        }

        #region Telephony related event handlers
        /// <summary>
        /// This event is fired in response to change in registration state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void phone_RegistrationStateChanged(object sender, RegistrationStateChangedEventArgs args)
        {
            display("Registration State = " + args.NewState);
            
            if (args.NewState == RegistrationStateChangedEventArgs.State.Registered)
            {
                lock (this)
                {
                    isRegistered = true;
                    sendRegistrationEvent();
                }
                setState(CurrentState.READY);
            }
            if (isRegistered == true && (args.NewState == RegistrationStateChangedEventArgs.State.Error ||
                args.NewState == RegistrationStateChangedEventArgs.State.Rejected ||
                args.NewState == RegistrationStateChangedEventArgs.State.NotRegistered))
            {
                // If callee was already registered, and now it's registration failed, exit
                display("Registration Failed", MessageType.ERROR);
                Environment.Exit(ReturnCode.FAILED_REGISTRATION);
            }
        }

        /// <summary>
        /// Private method that sends registration event to the driver process
        /// </summary>
        private void sendRegistrationEvent()
        {
            IntPtr handle;
            string eventName = this.inputParam.myExtension + " REGISTERED";
            handle = Win32CustomEvent.openCustomEvent(eventName);
            Win32CustomEvent.sendCustomEvent(handle);
        }

        /// <summary>
        /// This event is fired in response to an incoming call. This must be accepted if no other call is in progress
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void phone_Delivered(object sender, DeliveredEventArgs args)
        {
            DateTime currentTime = DateTime.Now;
            CurrentState localState = getState();
            
            if (localState == CurrentState.READY)
            {
                setState(CurrentState.CALLPENDING);
                conn = args.AlertingConnection;
                display("Going to answer incoming call from " + conn.CallerURI);
                conn.AnswerCall();
            }
            else
  //          if (localState != CurrentState.READY)
            {
                if (calleeData != null && (currentTime.Subtract(calleeData.callConnectTime) >= new TimeSpan(0, 0, maxCallDuration + waitTimeBetweenCalls)))
           //     if (currentTime.Subtract(calleeData.callConnectTime) >= new TimeSpan(0, 0, maxCallDuration + waitTimeBetweenCalls))
                {
                    // If a call comes in after the expected time, this implies that the hangup was not
                    // detected for the previous call. Thus, we log the previous iteration, clean up after it
                    // and accept this new call.
                    // The expected time >= max call interval + inter call duration

                    display("Accepting this call as hangup might not have been detected for the previous call");
                    
                    missingHangupDetectionTimer.Enabled = false;
                    // This method sets calleeData.callReleaseTime to uninitialized value, which gets reported as a missing hangup
                    processHangup(new DateTime());
                    setState(CurrentState.CALLPENDING);
                    conn = args.AlertingConnection;
                    conn.AnswerCall();
                }
                else
                {
                    /**
                     * If a call arrives when not expected, reject it.
                     */
                    display("Rejecting a call from " + args.CallingDevice, MessageType.WARNING);
                    args.AlertingConnection.ClearConnection();
                }
            }
        }

        void phone_Held(object sender, HeldEventArgs args)
        {
            Trace.WriteLine("Held Event Received", "Info");
        }

        void phone_Transferred(object sender, TransferredEventArgs args)
        {
            Trace.WriteLine("Transferred Event Received", "Info");
        }

        /// <summary>
        /// This is invoked when call is initiated. It will never be invoked for callee
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void phone_Originated(object sender, OriginatedEventArgs args)
        {
            Trace.WriteLine("Originated Event Received - Not allowed for Callee", "Warning");    
        }

        void phone_InfoEventReceived(object sender, InfoEventArgs args)
        {
            Trace.WriteLine("Info Event Received " + args.info, "Info"); ;
        }

        /// <summary>
        /// This method is fired when call is disconnected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void phone_ConnectionCleared(object sender, ConnectionClearedEventArgs args)
        {
            DateTime callReleaseTime = DateTime.Now;
            Connection droppedConn = args.DroppedConnection;
       
            /**
             * If connection cleared was received in response to active or pending connection, take appropriate action
             * or else do nothing.
             */
            if (conn != null && conn.Equals(droppedConn) == true)
            {
                missingHangupDetectionTimer.Enabled = false;
                conn = args.DroppedConnection;

                if (missingHangupDetected == false)
                {
                    display("Received connection cleared for Iteration " + currIter);
                    processHangup(callReleaseTime);
                }
                else
                {
                    display("Received connection cleared in response to callee clearing call on not detecting a hangup");
                    processHangup(new DateTime());
                    missingHangupDetected = false; // This is set to false to prepare callee for next call iteration
                }
                setState(CurrentState.READY);

                /**
                 * If callee does not detect hangup and releases the ongoing call, we need to ensure that if an actual hangup is received
                 * after this call is released, it should not be processed. Thus, we set conn to null so that it will not pass through the
                 * previous if block and not get processed.
                 */
                conn = null;
            }
            else
            {
                display("Ignored connection cleared event because it was not in a response to an established call. Connection object details = " + droppedConn.ToString());
            }
        }

        /// <summary>
        /// Method invoked when call is established
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void phone_Established(object sender, EstablishedEventArgs args)
        {
            DateTime connEstablishedTimeStamp = DateTime.Now;

            /**
             * When call is connected, create a new instance of calleeIterationInfo
             * and set the time stamp of the instant when call was connected.
             * Also set the caller ID
             */
            calleeData = null;
            calleeData = new CalleeIterationInfo();
            calleeData.callConnectTime = connEstablishedTimeStamp;
            calleeData.remotePartyExtension = args.CallingDevice;
            
            // Preserve the connection object
            conn = args.EstablishedConnection;
            ++currIter; // Increment the iteration number

            try
            {
              ll.Start();
            }
            catch (Exception e)
            {
                display("Exception in phone_Established.Message = " + e.Message + " StackTrace = " + e.StackTrace, MessageType.WARNING);
            }
            try
            {
                if (recorder != null)
                {
                    recorder.Filename = inputParam.resultDirName + "\\Callee_RecordedWav_" + currIter.ToString() + ".wav";
                    recorder.Start();
                }
            }
            catch(Exception e2)
            {
                display("Exception in phone_Established.Message = " + e2.Message + " StackTrace = " + e2.StackTrace, MessageType.WARNING);
            }
            promptTimer.Enabled = true;
            setState(CurrentState.CONNECTED);
            missingHangupDetectionTimer.Enabled = true;
        }
        #endregion

        /// <summary>
        /// This method is fired when specified duration elapses after connection is established. A prompt is played
        /// after the specified timer duration expires. This latency is necessary to ensure that Caller side listener
        /// will start before the prompt is played.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void promptTimer_Tick(object sender, System.EventArgs e)
        {
            calleeData.wavFilePlayed = inputParam.getWavFile();
            promptTimer.Enabled = false;
            pm.PlayAsync(calleeData.wavFilePlayed);
        }

        #region Listener event handlers

        /// <summary>
        /// This method is fired when listener detects speech.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        void ll_SpeechDetected(object o, VoiceEventArgs args)
        {
            calleeData.speechDetectionTime = DateTime.Now;
            setState(CurrentState.SPEECH_DETECTED);
        }

        /// <summary>
        /// Event handler invoked when detected speech is not recognized.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        void ll_NotRecognized(object o, NotRecognizedEventArgs args)
        {
        //      Trace.WriteLine(args.Result.InnerXml);
        }

        /// <summary>
        /// Event handler invoked when detected speech is recognized.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        void ll_Recognized(object o, RecognizedEventArgs args)
        {
            // Store confidence, Recognized text, Grammar property
            RecognizerData rData = new RecognizerData(args.Confidence, args.Result.FirstChild.FirstChild.Name, args.Text);
            calleeData.addRecognizerResult(rData);
        }

        void ll_SilenceTimeoutExpired(object o, VoiceEventArgs args)
        {
            Trace.WriteLine("Silence timeout expired. Restarting Listener", "Info");
            ll.Start();
        }

        /// <summary>
        /// Method fired if some voice error occurs
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        void ll_VoiceErrorOccurred(object o, VoiceEventArgs args)
        {
           display("Listener: Voice Error Occurred. REASON = " + args.Cause.ToString(), MessageType.WARNING);
           ll.Start();
        }

        /// <summary>
        /// Method fired when listener is started
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        void ll_Started(object o, VoiceEventArgs args)
        {
            setState(CurrentState.LISTENER_STARTED);
        }

        /// <summary>
        /// Method fired when listener is ready
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        void ll_ListenerReady(object o, System.EventArgs args)
        {
           ;
        }
        #endregion

        /// <summary>
        /// A private method that sends the iteration result (calleeData in this case) to the watcher.
        /// </summary>
        private void sendCalleeDataToWatcher()
        {
            if (calleeData != null && OnIterationCompleted != null)
                OnIterationCompleted(this, calleeData);

            // Resetting calleeData to null because if a duplicate connection cleared is received, calleeData instance should 
            // not be written again to the calleelog.
            calleeData = null;
        }

        #region Event handlers for prompt
        void pm_VoiceErrorOccurred(object o, VoiceEventArgs args)
        {
            display("Voice error occurred in prompt", MessageType.WARNING);
        }

        void pm_InterruptionDetected(object o, VoiceEventArgs args)
        {
            display("Interruption detected in playing prompt", MessageType.WARNING);
        }

        /// <summary>
        /// This method is invoked when prompt is completely played.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        void pm_Completed(object o, VoiceEventArgs args)
        {
          ;
        }

        /// <summary>
        /// This method is invoked when prompt just starts to be played. At this time, we store
        /// the current time as the time when prompt was being played.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        void pm_Started(object o, VoiceEventArgs args)
        {
            calleeData.speakTime = DateTime.Now;
            setState(CurrentState.PLAYING_PROMPT);
        }
        #endregion

        #region Methods to atomically get and set value of currentState variable
        private void setState(CurrentState newState)
        {
            lock (this)
            {
                currentState = newState;
            }
       
            display("State = " + newState.ToString());

            if (newState == CurrentState.EXECUTION_COMPLETED)
            {
                display("Execution completed.. Exiting");
                Environment.Exit(0);
            }
        }

        public CurrentState getState()
        {
            CurrentState localState;
            lock (this)
            {
                localState = currentState;
            }
            return localState;
        }
        #endregion
    }
}
