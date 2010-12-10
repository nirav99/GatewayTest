using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Xml;
using System.IO;

namespace ResultAnalyzer
{
    /// <summary>
    /// This class checks if the specified wav file has representation in callee's grammar and also in mapfile. 
    /// It also contains the "propname" information for the selected wav files. ("Propname" is the rule containing
    /// all allowed text for this wav file in the grammar.
    /// </summary>
    public class WavFileInfo
    {
        private Hashtable ht;                       // Hashtable that contains "propname" as key
        private Hashtable ht2;                      // Hashtable that contains wav file name as key and propname as value

        private WavFileInfo(string calleeGrammarFile, string mapFile)
        {
            XmlDocument calleeGrammar;
            XmlNodeList nodeList;
            XmlAttributeCollection ac;

            ht = new Hashtable();
            ht2 = new Hashtable();

#region Code to read Callee's grammar and obtain all the property values
            calleeGrammar = new XmlDocument();
            calleeGrammar.Load(calleeGrammarFile);

            nodeList = calleeGrammar.GetElementsByTagName("l");

            foreach (XmlNode n in nodeList)
            {
               ac = n.Attributes;

               foreach (XmlAttribute xAtt in ac)
               {
                   if(!xAtt.Value.Equals(string.Empty))
                    ht.Add(xAtt.Value, null);
       //            Console.WriteLine("Attribute Name = " + xAtt.Name + " Value = " + xAtt.Value);
               }
           }
#endregion
           readMapFileAndBuildList(mapFile);
        }

        /// <summary>
        /// Method to obtain a new instance of WavFileInfo class
        /// </summary>
        /// <param name="_calleeGrammar"></param>
        /// <param name="_mapFile"></param>
        /// <returns></returns>
        public static WavFileInfo getInstance(string _calleeGrammar, string _mapFile)
        {
            WavFileInfo vfInfo = null;

            try
            {
                vfInfo = new WavFileInfo(_calleeGrammar, _mapFile);
            }
            catch (Exception e)
            {
                Console.WriteLine("WavFileInfo.WavFileInfo encountered an exception.\nMessage = " + e.Message);
                vfInfo = null;
            }
            return vfInfo;
        }

        /// <summary>
        /// Helper method to read the map file and build the info about wav file names and their corresponding 
        /// property names in map file.
        /// </summary>
        /// <param name="mapFile"></param>
        private void readMapFileAndBuildList(string mapFile)
        {
            string line;        
            string fileName;
            string propertyName;
            char[] delimiter = { '\t' };
            string[] tokens;

            StreamReader mapFileHandle = new StreamReader(mapFile);

            while ((line = mapFileHandle.ReadLine()) != null)
            {
                tokens = line.Split(delimiter);

                if (tokens.Length != 2)
                {
                    throw new Exception("Bad format of Map File " + mapFile);
                }

                //Discard the file path, preserve only the file name
                fileName = extractFileName(tokens[0].Trim());
                propertyName = tokens[1].Trim();

                if (ht.ContainsKey(propertyName))
                {
                    ht2.Add(fileName.ToLower(), propertyName);
                }
            }
        }

        /// <summary>
        /// Method that returns true if the specified wav file will be recognized by callee's grammar, false otherwise
        /// </summary>
        /// <param name="wavFile"></param>
        /// <returns></returns>
        public bool wavFileRecognizedByGrammar(string wavFile)
        {
            string fileName = extractFileName(wavFile).Trim().ToLower();

            if (ht2.ContainsKey(fileName))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Method that returns grammar property name corresponding to the specified file name
        /// </summary>
        /// <param name="wavFile"></param>
        /// <returns></returns>
        public string getGrammarPropertyName(string wavFile)
        {
            string fileName = extractFileName(wavFile).Trim().ToLower();

            if(ht2.ContainsKey(fileName))
                return ht2[fileName].ToString();
            else
                return null;
        }

        /// <summary>
        /// Method to extract file name from a full file path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string extractFileName(string path)
        {
            int idx;
            string result = null;

            if (path == null || path.Equals("") == true)
                return null;

            idx = path.LastIndexOf('\\');

            if (idx < 0)
                return path;
            else
            {
                result = path.Substring(idx + 1);
                return result;
            }
        }
     }
}
