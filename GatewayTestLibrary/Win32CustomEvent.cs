using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace GatewayTestLibrary
{
    public class Win32CustomEvent
    {
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr CreateEvent(IntPtr lpEventAttributes, Boolean bManualReset, Boolean bInitialState, String lpName);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr OpenEvent(Int32 dwDesiredAccess, Boolean bInheritHandle, String lpName);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern Int32 WaitForSingleObject(IntPtr Handle, Int32 dwTimeout);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool SetEvent(IntPtr handle);

        /// <summary>
        /// Managed code method to create a custom event
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IntPtr createCustomEvent(string name)
        {
            IntPtr p = new IntPtr();
            return CreateEvent(p, true, false, name);
        }

        /// <summary>
        /// Managed code method to wait for a custom event
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static Int32 waitForCustomEvent(IntPtr handle, int timeout)
        {
            return WaitForSingleObject(handle, timeout);
        }

        /// <summary>
        /// Managed code method to open (instantiate) a custom event
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IntPtr openCustomEvent(string name)
        {
            // Here 2 represents EVENT_MODIFY_STATE
            return OpenEvent(2, false, name);
        }

        /// <summary>
        /// Managed code method to send the opened event
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static bool sendCustomEvent(IntPtr handle)
        {
            return SetEvent(handle);
        }
    }
}

