using System;
using System.Collections.Generic;
using System.Text;

namespace GatewayTestLibrary
{
    /// <summary>
    /// Class that encapsulates the information obtained from the speech recognizer
    /// Currently this class is to be used inside CallerIterationInfo itself - Nirav 14th Feb 2007
    /// </summary>
    public class RecognizerData
    {
        private RecognitionResult _spRec;   // Enumeration containing whether speech was recognized or not
        private double _confidence;         // Confidence if speech was recognized else zero
        private string _propName;           // Property name for rule in grammar if speech was recognized, else null string
        private string _textEqt;            // If speech was recognized, then contains corresponding text, else null string
        private string _separator = "\t";   // For use in serializing and deserializing objects

        #region Class constructors
        public RecognizerData()
        {
            _spRec = RecognitionResult.UNRECOGNIZED;
            _confidence = 0;
            _propName = null;
            _textEqt = null;
        }

        public RecognizerData(double _conf, string _pName, string _txtEqt)
        {
            _spRec = RecognitionResult.RECOGNIZED;
            _confidence = _conf;
            _propName = _pName;
            _textEqt = _txtEqt;
        }

        public RecognizerData(string _data)
        {
            string[] tokens = _data.Split(_separator.ToCharArray());

            if (tokens.Length != 4)
                throw new Exception("RecognizerData: Cannot instantiate object. Error in input string");

            try
            {
                if (tokens[0].Equals("recognized", StringComparison.CurrentCultureIgnoreCase))
                    _spRec = RecognitionResult.RECOGNIZED;
                else
                    _spRec = RecognitionResult.UNRECOGNIZED;

                _confidence = Convert.ToDouble(tokens[1]);

                if (tokens[2].Equals("null"))
                    _propName = null;
                else
                    _propName = tokens[2];

                if (tokens[3].Equals("null"))
                    _textEqt = null;
                else
                    _textEqt = tokens[3];
            }
            catch(Exception e)
            {
                throw new Exception("RecognizerData: Cannot instantiate object. Error in input string");
            }
        }

        #endregion

        public RecognitionResult recognitionResult
        {
            get
            {
                return _spRec;
            }
        }

        public double confidence
        {
            get
            {
                return _confidence;
            }
        }

        public string grammarPropertyName
        {
            get
            {
                return _propName;
            }
        }

        public string text
        {
            get
            {
                return _textEqt;
            }
        }

        /// <summary>
        /// Get current state of object as a string - used for serialization while logging
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            result.Append(recognitionResult + _separator + _confidence + _separator);

            if (grammarPropertyName == null)
                result.Append("null");
            else
                result.Append(grammarPropertyName);
            result.Append(_separator);

            if (text == null)
                result.Append("null");
            else
                result.Append(text);

            return result.ToString();
        }
    }
}
