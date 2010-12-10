=============================================================================================================================
                                            How to execute the Gateway Test
=============================================================================================================================

Recommended Usage Scenario:
(when the test creates the required extensions and configures the server
to forward incoming calls to the callee extension)

GatewayTestDriver /ServerIP value /NumToDial value
        /ServerIP               - Server IP address
        /NumToDial              - Number for caller to dial to reach callee

Alternate Usage Scenario:
(When ATA is pre-configured to forward incoming calls to the callee extension)

GatewayTestDriver /ServerIP value /NumToDial value /CallerExt value /CalleeExt value
        /ServerIP               - Server IP address
        /NumToDial              - Number for caller to dial to reach callee
        /CallerExt              - Caller Extension
        /CalleeExt              - Callee Extension

Note about /NumToDial: This should be the line number the caller uses to reach
the callee. For direct calls between the caller and the callee, set it to the
callee's extension.
When /NumToDial represents a PSTN line through an ATA, add prefix 9 to the number.


Optional Parameters  (can be used with either scenario)
        /AllowCallerRecording   - Enable/disable Caller's recording
                                  Allowed values {true/false}
        /AllowCalleeRecording   - Enable/disable Callee's recording
                                  Allowed values {true/false}
        /InterSetInterval       - Time interval (in sec) to wait between
                                  consecutive calls of different sets
        /InterCallInterval      - Time interval (in sec) to wait between
                                  consecutive calls of same set
        /CallDuration           - Duration of a call (in sec)
                                  Recommended value 60
        /IterationsPerSet       - Number of call iterations per set
        /NumSets                - Number of sets of calls
        /MinSpeechbargeInterval - Minimum Speech barge interval
                                  Min value 0 Max value <= (1/4 * CallDuration)
        /MaxSpeechbargeInterval - Maximum Speech barge interval
                                  Min value >= MinSpeechBargeInterval
                                  Max value <= (1/4 * CallDuration)

------------------------------------------------------------------------------------------------------------------------------

