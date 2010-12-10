using System;
using System.Collections.Generic;
using System.Text;

namespace GatewayTestLibrary
{
    /// <summary>
    /// Status code for possible outcomes for iteration
    /// </summary>
    public enum StatusCode
    {
        NOERROR,        // Iteration executed successfully
        ERROR           // Error in iteration execution
    }

    /// <summary>
    /// Represents the enumeration of allowed values when detected speech was recognized or not
    /// </summary>
    public enum RecognitionResult
    {
        RECOGNIZED,
        UNRECOGNIZED
    }

    /// <summary>
    /// Class that encapsulates the information to be captured in one iteration
    /// </summary>
    public abstract class IterationInfo
    {
        protected DateTime _callConnectTime;       // Time when call was connected
        protected DateTime _callReleaseTime;       // Time when call was released
        protected DateTime _speechDetTime;         // Time when speech was detected
        protected DateTime _speakTime;             // Time when prompt started event was received
        protected string _remoteParty;             // URL of remote party connected to
        protected string _separator;               // Token separator string
        protected string _wavFilePlayed;           // Wav file played by the caller/callee in that iteration

        protected List<RecognizerData> recData;    // List of recognized event info from the recognizer
        /// <summary>
        /// Class constructor
        /// </summary>
        public IterationInfo()
        {
            _remoteParty = null;
            _separator = "\t";
            _wavFilePlayed = null;

            recData = new List<RecognizerData>();
        }

        /// <summary>
        /// Method to deserialize the object
        /// </summary>
        /// <param name="connectTime"></param>
        /// <param name="releaseTime"></param>
        /// <param name="speechDetectionTime"></param>
        /// <param name="promptStartedTime"></param>
        /// <param name="remoteParty"></param>
        /// <param name="wavFilePlayed"></param>
        protected void setBaseMembers(string connectTime, string releaseTime, string speechDetectionTime, string promptStartedTime, string remoteParty, string wavFilePlayed, string[] tokens)
        {
            _callConnectTime = Convert.ToDateTime(connectTime);
            _callReleaseTime = Convert.ToDateTime(releaseTime);
            _speechDetTime = Convert.ToDateTime(speechDetectionTime);
            _speakTime = Convert.ToDateTime(promptStartedTime);
            _remoteParty = remoteParty;
            _wavFilePlayed = wavFilePlayed;

            // Deserialize all the RecognizerData objects
            if (tokens != null)
            {
                for (int i = 0; i < tokens.Length; i += 4)
                {
                    RecognizerData d = new RecognizerData(tokens[i] + _separator +
                                                          tokens[i + 1] + _separator +
                                                          tokens[i + 2] + _separator +
                                                          tokens[i + 3]);
                    recData.Add(d);
                }
            }
        }

        #region Properties to get/set values for various members
        public DateTime callConnectTime
        {
            get
            {
                return _callConnectTime;
            }
            set
            {
                _callConnectTime = value;
            }
        }

        public DateTime callReleaseTime
        {
            get
            {
                return _callReleaseTime;
            }
            set
            {
                _callReleaseTime = value;
            }
        }

        public DateTime speechDetectionTime
        {
            get
            {
                return _speechDetTime;
            }
            set
            {
                _speechDetTime = value;
            }
        }

        public DateTime speakTime
        {
            get
            {
                return _speakTime;
            }
            set
            {
                _speakTime = value;
            }
        }

        public string remotePartyExtension
        {
            get
            {
                return _remoteParty;
            }
            set
            {
                _remoteParty = value;
            }
        }

        public string wavFilePlayed
        {
            get
            {
                return _wavFilePlayed;
            }
            set
            {
                _wavFilePlayed = value;
            }
        }

        /// <summary>
        /// Add result from recognizer
        /// </summary>
        /// <param name="_recData"></param>
        public void addRecognizerResult(RecognizerData _recData)
        {
            recData.Add(_recData);
        }

        /// <summary>
        /// Retrieve the list of recognition data objects
        /// </summary>
        /// <returns></returns>
        public List<RecognizerData> getRecognizerResult()
        {
            return recData;
        }

        /// <summary>
        /// Retrieve the list of grammar property names for every recognized event
        /// </summary>
        /// <returns></returns>
        public List<string> getRecognizedGrammarPropertyNames()
        {
            List<string> propNames = new List<string>();

            for (int i = 0; i < recData.Count; i++)
            {
                if (recData[i].recognitionResult == RecognitionResult.RECOGNIZED)
                {
                    propNames.Add(recData[i].grammarPropertyName);
                }
            }
            return propNames;
        }

        /// <summary>
        /// Return confidence for specified grammar property. Find maximum confidence if the same property is found 
        /// multiple times.
        /// </summary>
        /// <param name="propName"></param>
        /// <returns></returns>
        public double getConfidence(string propName)
        {
            double conf = 0;

            if(propName != null)
            {
            for (int i = 0; i < recData.Count; i++)
            {
                if (recData[i].grammarPropertyName != null && recData[i].grammarPropertyName.Equals(propName, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (recData[i].confidence > conf)
                    {
                        conf = recData[i].confidence;
                    }
                }
            }
            }
            return conf;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            result.Append(_callConnectTime.ToString() +
                     _separator +
                     _callReleaseTime.ToString() +
                     _separator +
                     _speechDetTime.ToString() +
                     _separator +
                     _speakTime.ToString() +
                     _separator);

            if (_remoteParty == null)
                result.Append("null");
            else
                result.Append(_remoteParty);

            result.Append(_separator);

            if (_wavFilePlayed == null)
                result.Append("null");
            else
                result.Append(_wavFilePlayed);

            foreach (RecognizerData r in recData)
                result.Append(_separator + r.ToString());
            return result.ToString();
        }
        #endregion
    }

    /// <summary>
    /// Class that encapsulates additional info collected from caller
    /// </summary>
    public class CallerIterationInfo : IterationInfo
    {
        protected DateTime _makeCallTime;          // Time when caller invoked the MakeCall method
        protected DateTime _connClearedTime;       // Time when caller invoked connection released method
        /// <summary>
        /// Class constructor
        /// </summary>
        public CallerIterationInfo()
            : base()
        {
            _makeCallTime = new DateTime();
            _connClearedTime = new DateTime();
        }

        /// <summary>
        /// Class constructor that deserializes the object
        /// </summary>
        /// <param name="callerInfo"></param>
        public CallerIterationInfo(string callerInfo)
            : base()
        {
            string[] tokens = callerInfo.Split(_separator.ToCharArray());

            if (tokens == null || tokens.Length < 8 || (tokens.Length > 8 && ((tokens.Length - 8) % 4 != 0)))
                throw new Exception("CallerIterationInfo : Cannot instantiate object. Error in input string");

            try
            {
                _makeCallTime = Convert.ToDateTime(tokens[2]);
                _connClearedTime = Convert.ToDateTime(tokens[3]);
                if (tokens.Length == 8)
                    setBaseMembers(tokens[0], tokens[1], tokens[4], tokens[5], tokens[6], tokens[7], null);
                else
                {
                    string[] temp = new string[tokens.Length - 8];

                    for (int i = 8; i < tokens.Length; i++)
                        temp[i - 8] = tokens[i];

                    setBaseMembers(tokens[0], tokens[1], tokens[4], tokens[5], tokens[6], tokens[7], temp);
                }
            }
            catch (Exception e)
            {
                throw new Exception("CallerIterationInfo : Bad format of input string. Cannot instantiate the object");
            }
        }

        public DateTime makeCallMethodTime
        {
            set
            {
                _makeCallTime = value;
            }
            get
            {
                return _makeCallTime;
            }
        }

        public DateTime connClearedMethodTime
        {
            set
            {
                _connClearedTime = value;
            }
            get
            {
                return _connClearedTime;
            }
        }

        /// <summary>
        /// Method that serializes the object by converting it to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            result.Append(_callConnectTime.ToString() +
                      _separator +
                      _callReleaseTime.ToString() +
                      _separator +
                      _makeCallTime.ToString() +
                      _separator +
                      _connClearedTime.ToString() +
                      _separator +
                      _speechDetTime.ToString() +
                      _separator +
                      _speakTime.ToString() +
                      _separator);

            if (_remoteParty == null)
                result.Append("null");
            else
                result.Append(_remoteParty);
            if (_wavFilePlayed != null && _wavFilePlayed.Equals("") == false)
                result.Append(_separator + _wavFilePlayed);
            else
                result.Append(_separator + "null");

            foreach (RecognizerData r in recData)
                result.Append(_separator + r.ToString());
            return result.ToString();
        }
    }

    /// <summary>
    /// Class that encapsulates additional info from callee
    /// </summary>
    public class CalleeIterationInfo : IterationInfo
    {
        /// <summary>
        /// Class constructor
        /// </summary>
        public CalleeIterationInfo()
            : base()
        {

        }

        /// <summary>
        /// Class constructor that deserializes the object
        /// </summary>
        public CalleeIterationInfo(string calleeInfo)
            : base()
        {
            string[] tokens = calleeInfo.Split(_separator.ToCharArray());

            if (tokens.Length < 6 || (tokens.Length > 6 && (tokens.Length - 6) % 4 != 0))
                throw new Exception("CalleeIterationInfo : Bad format of input string. Cannot instantiate the object");

            try
            {
                if (tokens.Length == 6)
                    setBaseMembers(tokens[0], tokens[1], tokens[2], tokens[3], tokens[4], tokens[5], null);
                else
                {
                    string[] temp = new string[tokens.Length - 6];

                    for (int i = 6; i < tokens.Length; i++)
                        temp[i - 6] = tokens[i];
                    setBaseMembers(tokens[0], tokens[1], tokens[2], tokens[3], tokens[4], tokens[5], temp);
                }
            }
            catch (Exception e)
            {
                throw new Exception("CalleeIterationInfo : Bad format of input string. Cannot instantiate the object");
            }
        }

        /// <summary>
        /// Method that serializes the object by converting it to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return base.ToString();
        }
    }
}
