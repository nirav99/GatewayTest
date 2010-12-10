using System;
using System.Collections.Generic;
using System.Text;

namespace GatewayTestLibrary
{
    /// <summary>
    /// Enumerations to list all possible states of Caller and Callee
    /// </summary>
    public enum CurrentState
    {
        UNINIT,             // Uninitialized
        READY,              // Ready to make call
        DIALING,            // Invoked make call, waiting for event established
        CALLPENDING,        // Invoked accept call on incoming connection, waiting to receive connection established
        CONNECTED,          // Connected in a call
        LISTENER_STARTED,   // Listener started
        SPEECH_DETECTED,    // Speech detected over audio channel
        PLAYING_PROMPT,     // Playing a prompt
        EXECUTION_COMPLETED // When the specified number of calls are completed
    };
}
