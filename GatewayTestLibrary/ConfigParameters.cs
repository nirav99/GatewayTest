using System;
using System.Collections.Generic;
using System.Text;

namespace GatewayTestLibrary
{
    /// <summary>
    /// This class represents the configuration parameters passed to caller and callee
    /// </summary>
    public class ConfigParameters
    {
        private string _sipServer;              // IP address of SIP server
        private string _myExt;                  // Own extension
        private string _remoteExt;              // Extension of other entity

        private string _grammarFileName;        // Path and name of grammar file
        private string _outputFileName;         // Path and name of output log file
        private string[] _wavFileName;          // Path and name of list of wav file to play
        private string _resultDir;              // Directory to store results
        private string _configFileName;         // Name of configuration file to read config parameters from
        private string lastWavFilePlayed;       // Name of last wav file played
        private int numIter;                    // Number of iterations
        private Random rn;                      // Used to select a wav file at random

        /// <summary>
        /// Class constructor - for Caller
        /// </summary>
        public ConfigParameters(string sipServer, string myExt, string remoteExt, string grammarFileName, string outputFileName, string dirName, string configFileName, string[] wavFileName)
        {
            initialize(sipServer, myExt, remoteExt, grammarFileName, outputFileName, dirName, configFileName, wavFileName);
        }

        /// <summary>
        /// Class constructor - for Callee
        /// </summary>
        public ConfigParameters(string sipServer, string myExt, string remoteExt, string grammarFileName, string outputFileName, string dirName, string configFileName, string wavFileName)
        {
            string[] wavFileList = new string[1];
            wavFileList[0] = wavFileName;
            initialize(sipServer, myExt, remoteExt, grammarFileName, outputFileName, dirName, configFileName, wavFileList);
        }

        private void initialize(string sipServer, string myExt, string remoteExt, string grammarFileName, string outputFileName, string dirName, string configFileName, string[] wavFileName)
        {
            _sipServer = sipServer;
            _myExt = myExt;
            _remoteExt = remoteExt;
            _grammarFileName = grammarFileName;
            _outputFileName = outputFileName;
            _configFileName = configFileName;
            _wavFileName = wavFileName;
            _resultDir = dirName;
            lastWavFilePlayed = string.Empty;

            rn = new Random(Environment.TickCount);

        }

        #region Get accessor properties to retrieve each of the members
        public string sipServerIP
        {
            get
            {
                return _sipServer;
            }
        }

        public string myExtension
        {
            get
            {
                return _myExt;
            }
        }

        public string remoteExtension
        {
            get
            {
                return _remoteExt;
            }
        }

        public string grammarFileName
        {
            get
            {
                return _grammarFileName;
            }
        }

        public string logFileName
        {
            get
            {
                return _outputFileName;
            }
        }

        public string resultDirName
        {
            get
            {
                return _resultDir;
            }
        }

        /// <summary>
        /// Property to return the name of the current (most recent or last) wav file that was played
        /// </summary>
        public string currentWavFilePlayed
        {
            get
            {
                return lastWavFilePlayed;
            }
        }

        /// <summary>
        /// Property to return the name of the config file to read config parameters from
        /// </summary>
        public string configFile
        {
            get
            {
                return _configFileName;
            }
        }

        /// <summary>
        /// Returns a wav file name that is randomly selected from the list of wav files
        /// </summary>
        /// <returns></returns>
        public string getWavFile()
        {
            int nextFile;
            if (_wavFileName.Length > 1)
                nextFile = rn.Next(0, _wavFileName.Length);
            else
                nextFile = 0;

            lastWavFilePlayed = _wavFileName[nextFile];
            return lastWavFilePlayed;
        }

        //public int numIterations
        //{
        //    get
        //    {
        //        return numIter;
        //    }
        //}
        #endregion
    }
}
