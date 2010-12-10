using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using Microsoft.CSTA;
using GatewayTestLibrary;
using EdbQa.Common.CDSHelper;

namespace GatewayTestCaller
{
    /// <summary>
    /// Class that represents the use case scenario of a gateway test caller
    /// </summary>
    class Caller
    {
        #region CSTA related references
        private Provider p = null;                              // CSTA provider reference
        private VoiceDevice phone = null;                       // VoiceDevice instance
        private Connection conn = null;                         // Stores currently established connection
        private Prompt pm = null;                               // Prompt instance to speak on voice channel
        private Listener ll = null;                             // Listener instance to detect speech on voice channel
        private Recorder recorder = null;                       // CSTA Recorder
        #endregion

        #region Configurable Parameters - whose value can be changed through config file
        private int minSpeechBargeTime = 0;                     // Min time (in sec) to wait before barging in on callee's prompt
        private int maxSpeechBargeTime = 5;                     // Max time (in sec) to wait before barging in on callee's prompt
  //      private int speechBargeTimeRange = 5;                   // Range of time units (in sec) to wait before barging on callee
  //      private int maxWaitTime = 5;                            // Max time (in sec) to wait to barge in after detecting speech
        private int totalIter = 100;                            // Number of iterations in one set
        private int totalSets = 3;                              // Number of sets
        private int maxCallDuration = 70;                       // Max call duration in seconds
        private int waitTimeBetweenCalls = 20;                  // Wait time between calls in seconds
        private int waitTimeBetweenSets = 120;                  // Wait time between sets
        private bool allowRecording = false;                    // whether to allow recording of the calls
        #endregion

        ConfigParameters inputParam = null;                     // Stores configuration parameters

        private Random randomGenerator;                         // Random number generator to generate random wait time before barging

        #region Call related parameters
        private CurrentState currentState;                      // Current state of the Caller
        private System.Windows.Forms.Timer callDurationTimer;   // Timer to hang up call in specified duration
        private System.Windows.Forms.Timer interCallTimer;      // Timer to wait for specified duration between calls
        #endregion

        int numRemainingIter = 1;                               // Indicates number of REMAINING iterations
        int numCallsPlaced = 0;                                 // Indicates the number of calls placed

        CallerIterationInfo callerData = null;                  // Data to collect from caller in one iteration

        private bool isRegistered;                              // Is user agent registered

        #region Event delegates to send state changes and iteration info objects to the driver
        //public delegate void StateChangedEventHandler(object sender, CurrentState state);
        //public event StateChangedEventHandler OnStateChanged;

        public delegate void IterationCompletedEventHandler(object sender, IterationInfo iInfo);
        public event IterationCompletedEventHandler OnIterationCompleted;
        #endregion

        private System.Windows.Forms.Timer bargeTimer;          // Timer to wait for barge duration before playing prompt

        /// <summary>
        /// Default class constructor
        /// </summary>
        public Caller(ConfigParameters _inputParam)
        {
            this.inputParam = _inputParam;
            initializeCaller();
        }

        /// <summary>
        /// Helper method for constructor to create instance of the caller
        /// </summary>
        /// <param name="inputParam"></param>
        private void initializeCaller()
        {
            readConfigParameters();

            // Total number of calls = total sets * total iterations per set
            this.numRemainingIter = totalSets * totalIter;
            
            display("Config Parameters");
            display("Total Iterations Per Set = " + totalIter);
            display("Total Sets = " + totalSets);
            display("Total Calls = " + numRemainingIter);
            display("Max Call Duration = " + maxCallDuration);
            display("Inter Call Duration = " + waitTimeBetweenCalls);
            display("Inter Set Duration = " + waitTimeBetweenSets);

            if(minSpeechBargeTime > maxSpeechBargeTime)
            {
                display("Value for maxSpeechBargeTime should be greater than or equal to minSpeechBargeTime. you entered Max Speech Barge time = " + maxSpeechBargeTime + " and Min Speech Barge time = " + minSpeechBargeTime);
                Environment.Exit(0);
            }

            if ((maxSpeechBargeTime >=  maxCallDuration / 4.0) || (minSpeechBargeTime >= maxCallDuration / 4.0)) 
            {
                display("Value for maxSpeechBargeTime and minSpeechBargeTime must be <= maxCallDuration / 4. Max Call duration  " + maxCallDuration + " Max Speech Barge time = " + maxSpeechBargeTime + " and Min Speech Barge time = " + minSpeechBargeTime);
                Environment.Exit(0);
            }

            display("Minimum Speech Barge Interval = " + minSpeechBargeTime);
            display("Maximum Speech Barge Interval = " + maxSpeechBargeTime);
        //    display("Speech Barge Interval Range = " + speechBargeTimeRange);
        //    display("Max Barge Duration = " + maxWaitTime);
            display("Extension = " + inputParam.myExtension);

            randomGenerator = new Random(Environment.TickCount);
      
            callDurationTimer = new System.Windows.Forms.Timer();
            callDurationTimer.Interval = maxCallDuration * 1000;
            callDurationTimer.Tick += new System.EventHandler(callDurationTimer_Tick);
            callDurationTimer.Enabled = false;

            isRegistered = false;

            interCallTimer = new System.Windows.Forms.Timer();
            interCallTimer.Interval = waitTimeBetweenCalls * 1000;
            interCallTimer.Tick += new System.EventHandler(interCallTimer_Tick);
            interCallTimer.Enabled = false;

            // Interval for this timer will be set on detecting speech
            bargeTimer = new System.Windows.Forms.Timer();
            bargeTimer.Enabled = false;
            bargeTimer.Tick += new System.EventHandler(bargeTimer_Tick);
            initializeVoiceDevice();
        }
                
        /// <summary>
        /// Read configuration parameters (if present) from the caller's configuration file
        /// </summary>
        private void readConfigParameters()
        {
            StreamReader reader;        // File handle
            string line;
            int temp;

            try
            {
                display("Configuration file = " + inputParam.configFile);
                reader = new StreamReader(inputParam.configFile);

                // Locate the lines for expected parameters and use those values only if they are positive
                // integers
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Equals("CALLER_RECORD=YES", StringComparison.CurrentCultureIgnoreCase) == true)
                    {
                        allowRecording = true;
                    }
                    if (line.Contains("MIN_SPEECH_BARGE_INTERVAL=") && ((temp = Helper.parseValue(line)) >= 0))
                    {
                        minSpeechBargeTime = temp;
                    }
                    if (line.Contains("MAX_SPEECH_BARGE_INTERVAL=") && ((temp = Helper.parseValue(line)) > 0))
                    {
                        maxSpeechBargeTime = temp;
                    }
                    if (line.Contains("CALL_DURATION=") && ((temp = Helper.parseValue(line)) > 30))
                    {
                        maxCallDuration = temp;
                    }
                    if (line.Contains("INTER_CALL_INTERVAL=") && ((temp = Helper.parseValue(line)) > 3))
                    {
                        waitTimeBetweenCalls = temp;
                    }
                    if (line.Contains("COUNT_ITERATIONS=") && ((temp = Helper.parseValue(line)) > 0))
                    {
                        totalIter = temp;
                    }
                    if (line.Contains("COUNT_SETS=") && ((temp = Helper.parseValue(line)) > 0))
                    {
                        totalSets = temp;
                    }
                    if (line.Contains("INTER_SET_INTERVAL=") && ((temp = Helper.parseValue(line)) > 0))
                    {
                        waitTimeBetweenSets = temp;
                    }
                }
                reader.Close();
            }
            catch(Exception e)
            {
                Trace.TraceWarning("Exception in reading custom configuration Parameters. Using Default Config Values", "Info");
                Trace.TraceWarning("Exception message = " + e.Message);
                Trace.TraceWarning("Exception stack trace = " + e.StackTrace);
            }
        }
        
        /// <summary>
        /// Method that places a call to the callee and sets caller state to dialing
        /// <returns>true if call has been attempted, false otherwise</returns>
        /// </summary>
        public bool placeCall()
        {
            int nextCallIter; // Local temp variable
            /**
             * Place a call only if no other call was in progress and caller is ready.
             */

            CurrentState localState = getState();

            if (localState == CurrentState.READY)
            {
                lock (this)
                {
                   numRemainingIter--;
                   nextCallIter = ++numCallsPlaced;
                }

                conn = null;

                /**
                 * Create a new instance of caller iteration info while attempting to place a call.
                 */
                callerData = null;
                callerData = new CallerIterationInfo();

                display("Starting iteration " + nextCallIter);

                phone.MakeCall("SIP:" + inputParam.remoteExtension + "@" + inputParam.sipServerIP);
                // Store the current time
                callerData.makeCallMethodTime = DateTime.Now;

                setState(CurrentState.DIALING);
                return true;
            }
            else
                return false;
        }

        #region Helper method to create voice device with a prompt and listener
        /// <summary>
        /// Private helper method to initialize the CSTA voice device
        /// </summary>
        private void initializeVoiceDevice()
        {
            p = new Provider(inputParam.sipServerIP, new CredentialManager(inputParam.sipServerIP));

            Phone cdsPhone = Utilities.GetPhone(
                inputParam.sipServerIP,
                Utilities.LocalMacAddresses[0], 
                inputParam.myExtension);

            /**
             * To create an instance of VoiceDevice, use GenericInteractiveVoice as the flag.
             */
            phone = (VoiceDevice) p.GetDevice(inputParam.myExtension, 
                Provider.DeviceCategory.GenericInteractiveVoice, 
                cdsPhone.FunctionId);

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
            pm = phone.AllocatePrompt("CallerPrompt");
            pm.Started += new VoiceEventHandler(pm_Started);
            pm.Completed += new VoiceEventHandler(pm_Completed);
            pm.VoiceErrorOccurred += new VoiceEventHandler(pm_VoiceErrorOccurred);
            prepareListener();

            // Create a recorder if recording was enabled
            try
            {
                if (allowRecording == true)
                {
                    recorder = phone.AllocateRecorder();
                    recorder.Started += new VoiceEventHandler(recorder_Started);
                    recorder.Completed += new VoiceEventHandler(recorder_Completed);
                    recorder.VoiceErrorOccurred += new VoiceEventHandler(recorder_VoiceErrorOccurred);
                }
            }
            catch(Exception e)
            {
                Trace.WriteLine("Exception in creating recorder. Message: " + e.Message, "Info");
                recorder = null;
            }
        }

        /// <summary>
        /// Helper method that creates a new listener and prepares sets its event handlers
        /// </summary>
        private void prepareListener()
        {
            ll = null;

            ll = phone.AllocateListener("CallerListener");
            ll.Mode = Listener.ModeType.Multiple;
            
            ll.LoadGrammar("generic", inputParam.grammarFileName);

            ll.ListenerReady += new ListenerReadyEventHandler(ll_ListenerReady);
            ll.SpeechDetected += new VoiceEventHandler(ll_SpeechDetected);
            ll.Recognized += new RecognizedEventHandler(ll_Recognized);
            ll.NotRecognized += new NotRecognizedEventHandler(ll_NotRecognized);
            ll.Started += new VoiceEventHandler(ll_Started);
            ll.VoiceErrorOccurred += new VoiceEventHandler(ll_VoiceErrorOccurred);
            ll.SilenceTimeoutExpired += new VoiceEventHandler(ll_SilenceTimeoutExpired);
            ll.Activate("GatewayTestCaller");
        }
        #endregion

        #region Telephony related event handlers

        /// <summary>
        /// Event handler fired in response to change in registration state
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
                // Enable intercall timer
                display("Enabling intercall wait timer");
                interCallTimer.Enabled = true;
            }
            if (isRegistered ==true && (args.NewState == RegistrationStateChangedEventArgs.State.Error ||
                args.NewState == RegistrationStateChangedEventArgs.State.Rejected ||
                args.NewState == RegistrationStateChangedEventArgs.State.NotRegistered))
            {
                Trace.WriteLine("Registration Failed", "Error");
                // If the caller was previously registered, and it received failed registration message, exit with appropriate error code
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
        /// This event is fired in response to an incoming call. Since caller should not accept any incoming
        /// call, we simply refuse this connection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void phone_Delivered(object sender, DeliveredEventArgs args)
        {
            // Reject all incoming calls
            args.AlertingConnection.ClearConnection();
            Trace.TraceInformation("Rejecting incoming call from " + args.CallingDevice + " at time " + DateTime.Now);
        }

        void phone_Held(object sender, HeldEventArgs args)
        {
            Trace.TraceError("Held Event Received");
        }

        void phone_Transferred(object sender, TransferredEventArgs args)
        {
            Trace.TraceError("Transferred Event Received", "Info"); ;
        }

        void phone_Originated(object sender, OriginatedEventArgs args)
        {
            /**
             * conn must be set to originated connection here so that if the callee does not answer the call,
             * that condition can be detected with the current implementation of connection cleared.
             */
            conn = args.OriginatedConnection;

            // Enable call timer and re-initialize its timeout value 
            callDurationTimer.Interval = maxCallDuration * 1000;
            callDurationTimer.Enabled = true;
         }

        void phone_InfoEventReceived(object sender, InfoEventArgs args)
        {
            Trace.WriteLine("Info Event Received " + args.info, "Info");
        }

        /// <summary>
        /// This method is fired when call is hungup.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void phone_ConnectionCleared(object sender, ConnectionClearedEventArgs args)
        {
            DateTime callReleaseTimeStamp = DateTime.Now;
            Connection droppedConn = args.DroppedConnection;

            /**
             * If connection cleared was received in response to active or dialing connection, take appropriate action
             * or else do nothing.
             */
            if (conn != null && conn.Equals(droppedConn) == true)
            {
                conn = args.DroppedConnection;

                // Store the timestamp when call was disconnected with callerData
                callerData.callReleaseTime = callReleaseTimeStamp;

                // Disable all timers to prevent them from accidentally firing in next iteration
                callDurationTimer.Enabled = false;
                bargeTimer.Enabled = false;

                try
                {
                    if (ll != null)
                        ll.Stop();
                    if (recorder != null)
                        recorder.Stop();
                }
                catch (Exception e)
                {
                    Trace.TraceError("Exception in phone_ConnectionCleared. Message: " + e.Message + "Current time = " + DateTime.Now + "\r\nStack Trace : \r\n" + e.StackTrace, "Warning");
                }

                // After processing a connection cleared, send the notification of call state for the caller to the observer.
                // Make sure to set the callerData to null - this is a safety mechanism to prevent same iteration being logged
                // multiple times (if multiple connection cleared) are received in response to makecall
                if (OnIterationCompleted != null && callerData != null)
                {
                    OnIterationCompleted(this, callerData);
                    callerData = null;
                }

                int temp;

                lock (this)
                {
                    temp = numRemainingIter;
                }
                if (temp > 0)
                {
                    setState(CurrentState.READY);
                    display("Enabling inter-call wait timer");
                    // Start the timer to place the next call
                    interCallTimer.Enabled = true;
                }
                else
                {
                    setState(CurrentState.EXECUTION_COMPLETED);
                }
            }
            else
            {
                /**
                 * If connection cleared was ignored, print out the time that happened, and also the reason.
                 * If conn was null say that. If conn was not equivalent to dropped connection, say that, otherwise 
                 * set the reason as "Unknown".
                 */
                StringBuilder message = new StringBuilder();
                message.Append("Ignoring connection cleared event at time = " + DateTime.Now + " Reason: ");
               
                if (conn == null)
                {
                    message.Append("Preserved connection was null");
                }
                else
                {
                    message.Append("Preserved connection is not equivalent to dropped connection");
                }
                
                display(message.ToString());
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
             * A new instance of callerIterationInfo (callerData) is created while calling placeCall method. 
             * Initialize it with call connect time.
             */
            callerData.callConnectTime = connEstablishedTimeStamp;

             // Preserve the connection object
            conn = args.EstablishedConnection;

            setState(CurrentState.CONNECTED);
        
            try
            {
              ll.Start();
            }
            catch(Exception e1)
            {
                Console.WriteLine("Exception while starting listener. Message: " + e1.Message + " Stack Trace: " + e1.StackTrace);
               Trace.TraceError("Exception while starting listener. Message: " + e1.Message + " Stack Trace: " + e1.StackTrace + " Current Time = " + DateTime.Now);
            }
            try
            {
                if (recorder != null)
                {
                    int temp;

                    lock (this)
                    {
                        temp = numCallsPlaced;
                    }
                    recorder.Filename = inputParam.resultDirName + "\\Caller_RecordedWav_" + (temp).ToString() + ".wav";
                    display("Starting recording to file: " + recorder.Filename);
                    recorder.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in starting recorder. Message : " + e.Message);
                Trace.TraceError("Exception in starting recorder. Message: " + e.Message + " Stack Trace: " + e.StackTrace + " Current time = " + DateTime.Now + "\r\nStack Trace : \r\n" + e.StackTrace, "Warning");
            }
        }
        #endregion

        #region Prompt related event handlers and method
        /// <summary>
        /// Method invoked when prompt is being played. Store the time when prompt was started and change status.
        /// Also send result of this iteration.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        void pm_Started(object o, VoiceEventArgs args)
        {
            callerData.speakTime = DateTime.Now;
            ///**
            // * Once prompt has started to be played, we disable the timer used to release the call and enable only
            // * if voice error event is received for the prompt.
            // */
            //callDurationTimer.Enabled = false;
            setState(CurrentState.PLAYING_PROMPT);
        }

        /// <summary>
        /// Event handler fired when prompt playing is completed.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        void pm_Completed(object o, VoiceEventArgs args)
        {
            // Since prompt playing is completed, we re-enable the call duration timer so that the call will be 
            // released eventually
        //    callDurationTimer.Interval = 20000;
            callDurationTimer.Enabled = true;
        }

        /// <summary>
        /// Event handler fired when some error occurs in prompt playing
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        void pm_VoiceErrorOccurred(object o, VoiceEventArgs args)
        {
            /**
             * Some error occurred in playing the prompt. Thus, we enable the timer so that the call can
             * be disconnected in the duration specified with the timer.
             */
            callDurationTimer.Enabled = true;

            int temp;

            lock (this)
            {
                temp = numCallsPlaced;
            }
            Trace.TraceWarning("Prompt: Voice Error Occurred in Iteration " + temp + " Current time = " + DateTime.Now);
        }

        /// <summary>
        /// Method for playing the prompt after selecting a suitable wav file
        /// </summary>
        private void playPrompt()
        {
            // Select a wav file to play at random and then play it asynchronously
            string wavFileToPlay = inputParam.getWavFile();
            pm.PlayAsync(wavFileToPlay);
            callerData.wavFilePlayed = wavFileToPlay;
        }

        #endregion

        #region Listener event handlers

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
           Trace.WriteLine("Listener: Voice Error Occurred. REASON = " + args.Cause.ToString() + " Current time = " + DateTime.Now, "Info");
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
        /// This method is fired when listener detects speech. It waits for random amount of time and 
        /// then plays the selected prompt.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        void ll_SpeechDetected(object o, VoiceEventArgs args)
        {
            DateTime spDetTime = DateTime.Now;
            int sleepTime;
            DateTime uninitDate = new DateTime();
                    
            // Once speech is detected, we disable the call duration timer. In case prompt can't be played (VoiceEvent error)
            // is received or when prompt playing gets completed, we enable the timer to ensure that the call gets disconnected.
            callDurationTimer.Enabled = false;

            /*
             * On detecting speech, the caller must barge-in (i.e. play a prompt after waiting a random time).
             * Since caller can detect its own echoes, it is necessary to ensure that the caller only barges in the first time
             * This is why, the caller's speak time is checked to see if the caller played a prompt once or not.
             */
            if (callerData.speechDetectionTime == uninitDate)
            {
                callerData.speechDetectionTime = DateTime.Now;
               
                // Wait for random number of seconds and then speak on the channel
                sleepTime = randomGenerator.Next(minSpeechBargeTime, maxSpeechBargeTime);
               
                if (sleepTime > 0)
                {
                    bargeTimer.Interval = sleepTime * 1000; // To convert to milli-sec
                    bargeTimer.Enabled = true;
                }
                else
                {
                    playPrompt();
                }
            }
            setState(CurrentState.SPEECH_DETECTED);
        }

        void ll_NotRecognized(object sender, NotRecognizedEventArgs args)
        {
            ;
        }

        void ll_Recognized(object sender, RecognizedEventArgs args)
        {
        //    Console.WriteLine("Property Name = " + args.Result.FirstChild.FirstChild.Name);
            RecognizerData rData = new RecognizerData(args.Confidence, args.Result.FirstChild.FirstChild.Name, args.Text);
            callerData.addRecognizerResult(rData);
       //     calleeData.speechRecognized(args.Confidence, args.Text, args.Result.FirstChild.FirstChild.Name); ;
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

        #region Recorder event handlers
        void recorder_VoiceErrorOccurred(object sender, VoiceEventArgs args)
        {
            display("Voice Error occurred in recorder " + args.Cause.ToString());
        }

        void recorder_Completed(object sender, VoiceEventArgs args)
        {
            RecorderEventArgs recorderArgs = args as RecorderEventArgs;
            display("Recorder completed event received. Duration of recorded file = " + recorderArgs.Duration.ToString());
        }

        void recorder_Started(object sender, VoiceEventArgs args)
        {
            display("Recording started");
        }
        #endregion

        #region Timer event handlers
        /// <summary>
        /// This event handler is fired when required barge-in interval elapses on detecting speech.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void bargeTimer_Tick(object sender, System.EventArgs e)
        {
            bargeTimer.Enabled = false;
            playPrompt();
        }

        /// <summary>
        /// This method is fired when call duration timeout is reached. If the call is in progress, it is released.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void callDurationTimer_Tick(object sender, System.EventArgs e)
        {
            /**
             * Disable call duration timer now
             */
            callDurationTimer.Enabled = false;


            if (conn != null)
            {
                if (conn.ConnectionState == Connection.State.Connected || conn.ConnectionState == Connection.State.Initiated ||
                    conn.ConnectionState == Connection.State.Hold)
                {
                    // Store current time as the time when we attempt to disconnect the call
                    callerData.connClearedMethodTime = DateTime.Now;
                    display("Going to release call");
                    try
                    {
                        conn.ClearConnection();
                    }
                    catch (Exception fail)
                    {
                        display("Exception in releasing call: " + fail.Message + " " + fail.StackTrace);
                    }
                }
                else
                {
                    display("ERROR: Could Not Release Call: Connection State : " + conn.ConnectionState.ToString());
                }
            }
            else
            {
                display("ERROR: Could not release call: Connection was null");
            }
        }

        /// <summary>
        /// Event handler to place next call when inter call wait timer fires
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void interCallTimer_Tick(object sender, System.EventArgs e)
        {
            interCallTimer.Enabled = false;
            if (numCallsPlaced == 0 || numCallsPlaced % totalSets != 0)
            {
                interCallTimer.Interval = waitTimeBetweenCalls * 1000;
            }
            else
            {
                interCallTimer.Interval = waitTimeBetweenSets * 1000;
            }
            placeCall();
        }
        #endregion

        #region Methods to atomically get and set value of currentState variable
        private void setState(CurrentState newState)
        {
            lock (this)
            {
                currentState = newState;
            }
            // For simplicity, I am displaying current state on console rather than sending event to driver program
            // and have the program print the state
            //if (OnStateChanged != null)
            //    OnStateChanged(this, newState);
            display("State = " + newState.ToString());

            if (newState == CurrentState.EXECUTION_COMPLETED)
            {
                display("Execution completed.. Exiting");
                Environment.Exit(ReturnCode.SUCCESS);
            }
        }

        private void display(string dispStr)
        {
            Console.WriteLine(dispStr);
            Trace.WriteLine(dispStr + " Current Time = " + DateTime.Now);
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