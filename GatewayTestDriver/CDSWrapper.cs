using System;
using System.Collections.Generic;
using System.Text;
using EdbQa.Common.CDSHelper;

namespace GatewayTestDriver
{
    /// <summary>
    /// Class that is used to create / delete extensions anbd phones as the test requires
    /// </summary>
    public class CDSWrapper
    {
        private PhoneExtension callerExt = null;       // Caller's extension
        private PhoneExtension calleeExt = null;       // Callee's extension
        private Phone CallerPhone = null;              // Phone for caller extension
        private Phone CalleePhone = null;              // Phone for callee extension
        private string serverIP = null;                // IP address of edinburgh
        private bool callDistPlanChanged = false;      // If call distribution plan was changed

        public CDSWrapper(string _serverIP)
        {
            this.serverIP = _serverIP;
        }

        /// <summary>
        /// Method that creates a phone with 2 extensions. If they were created, it returns true. False otherwise
        /// </summary>
        /// <param name="callerExt"></param>
        /// <param name="calleeExt"></param>
        /// <returns></returns>
        public bool createExtensionsAndPhone(string callerExtension, string calleeExtension, out string callerExtAcct, out string calleeExtAcct)
        {
            bool result = true;;

            callerExtAcct = null;
            calleeExtAcct = null;

            if (callerExtension != null && calleeExtension != null)
            {
                result = createExtension(callerExtension, out callerExt);
                result = result && createExtension(calleeExtension, out calleeExt);
            }
            else
            {
                result = createExtension(null, out callerExt);
                result = result && createExtension(null, out calleeExt);
            }
            if (result == false)
            {
                if (callerExt != null)
                {
                    removeExtension(callerExt);
                }
                else
                {
                    removeExtension(calleeExt);
                }
            }
            else
            {
                // Since both extensions were created, create the phones
                Console.WriteLine("Creating Caller Phone");
                CallerPhone = new Phone("CallerTestPhone_" + callerExt.Account , "CSTA-Phone", Utilities.LocalMacAddresses[0], Utilities.ErrorHandling.IGNORE);
                CallerPhone.AddExt(callerExt.Account);
                CallerPhone.Owner = callerExt.Account;
                Console.WriteLine("Extension : " + CallerPhone.Owner);
                CallerPhone.ToServer(serverIP, Utilities.UpdateType.BLIND_APPEND, Utilities.ErrorHandling.IGNORE);

                Console.WriteLine("Creating Callee Phone");
                CalleePhone = new Phone("CalleeTestPhone_" + calleeExt.Account, "CSTA-Phone", Utilities.LocalMacAddresses[0], Utilities.ErrorHandling.IGNORE);
                CalleePhone.AddExt(calleeExt.Account);
                CalleePhone.Owner = calleeExt.Account;
                Console.WriteLine("Extension : " + CalleePhone.Owner);
                CalleePhone.ToServer(serverIP, Utilities.UpdateType.BLIND_APPEND, Utilities.ErrorHandling.IGNORE);
                
                callerExtAcct = callerExt.Account;
                calleeExtAcct = calleeExt.Account;

                //// Modify the dial plan so that incoming calls are routed to the callee's extension
                //CallDistroPlan.ReceptionistAccount = calleeExtAcct;
                //CallDistroPlan.Plan = CallDistroPlan.PlanStyle.RECEPTIONIST;
                //CallDistroPlan.ApplySettings(serverIP);

            }
            return result;
        }

        /// <summary>
        /// Method to change server's call distribution plan to forward all calls to the specified extension
        /// </summary>
        /// <param name="ext"></param>
        /// <returns></returns>
        public bool changeCallDistributionPlan(string ext)
        {
            try
            {
                CallDistroPlan.RetrieveSettings(serverIP);
                CallDistroPlan.SaveSettings("original settings");

                // Modify the dial plan so that incoming calls are routed to the callee's extension
                CallDistroPlan.ReceptionistAccount = ext;
                CallDistroPlan.Plan = CallDistroPlan.PlanStyle.RECEPTIONIST;
                CallDistroPlan.AutoTransfer = false;
                CallDistroPlan.ApplySettings(serverIP);
                callDistPlanChanged = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception encountered in changing Call Distribution Plan. Message = " + e.Message + "\nStack Trace: " + e.StackTrace);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Remove the extensions after the test is over
        /// </summary>
        public void cleanupTest()
        {
            removeExtension(callerExt);
            removeExtension(calleeExt);

            if (callDistPlanChanged)
            {
                CallDistroPlan.RestoreSettings("original settings");
                CallDistroPlan.ApplySettings(serverIP);
            }

            if (CallerPhone != null)
            {
                try
                {
                    CallerPhone.ToServer(serverIP, Utilities.UpdateType.DELETE, Utilities.ErrorHandling.IGNORE);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception while removing caller phone : " + e.Message + e.StackTrace);
                }
                CallerPhone = null;
            }

            if (CalleePhone != null)
            {
                try
                {
                    CalleePhone.ToServer(serverIP, Utilities.UpdateType.DELETE, Utilities.ErrorHandling.IGNORE);
                }
                catch (Exception e2)
                {
                    Console.WriteLine("Exception while removing callee phone : " + e2.Message + e2.StackTrace);
                }
                CalleePhone = null;
            }
        }

        /// <summary>
        /// Method to create an extension. Try to find a unique extension within 600 tries, if an extension number is not supplied
        /// </summary>
        /// <param name="pe"></param>
        /// <returns></returns>
        private bool createExtension(string extensionNumber, out PhoneExtension pe)
        {
            pe = null;
            bool created = false;
            int extNum;

            try
            {
                // If extension number was not supplied, find a free extension and use it
                if (extensionNumber == null)
                {
                    Random rn = new Random(Environment.TickCount);
                    for (int numTries = 0; numTries < 600 && created == false; numTries++)
                    {
                        extNum = rn.Next(100, 799);
                        pe = new PhoneExtension("GatewayTestExtension_" + extNum, extNum.ToString());

                        if (!pe.ExistsOn(serverIP))
                        {
                            pe.ToServer(serverIP, Utilities.UpdateType.ADD, Utilities.ErrorHandling.HANDLE);
                            created = true;
                        }
                    }
                }
                else
                {
                    // If extension number was supplied, use that. If that extension already exists, fail and return
                    pe = new PhoneExtension("GatewayTextExtension_" + extensionNumber, extensionNumber);

                    if (pe.ExistsOn(serverIP))
                    {
                        Console.WriteLine("Error: Specified extension \"{0}\" already exists on the server.", extensionNumber);
                    }
                    else
                    {
                        pe.ToServer(serverIP, Utilities.UpdateType.ADD, Utilities.ErrorHandling.HANDLE);
                        created = true;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in CDSWrapper.createExtension: " + e.Message + "\nStack Trace: " + e.StackTrace);
                created = false;
                pe = null;
            }

            if (created == false)
            {
                pe = null;
            }
            return created;
        }

        /// <summary>
        /// Method to delete an extension
        /// </summary>
        /// <param name="pe"></param>
        private void removeExtension(PhoneExtension pe)
        {
            if (pe != null)
            {
                pe.ToServer(serverIP, Utilities.UpdateType.DELETE, Utilities.ErrorHandling.IGNORE);
            }
        }

        #region Properties to return caller and callee extensions
        public string callerExtension
        {
            get
            {
                return callerExt.Account;
            }
        }

        public string calleeExtension
        {
            get
            {
                return calleeExt.Account;
            }
        }
        #endregion
    }
}
