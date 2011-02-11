Gateway Test:

A project to detect echoes in VoIP calls and evaluate the performance of analog telephony adaptors (ATAs).

The Algorithm:

This application uses time-stamps and speech recognition to detect echoes in VoIP calls (2 patents are pending).

The caller program makes a VoIP call to the callee program. On answering the call, the callee user agent plays some audio and records the time-stamp it played that audio. The caller program on detecting the audio, plays its own audio and records the time-stamp. The callee agent notes the time-stamp when it hears audio. it also applies speech recognition to detect if it is hearing itself or the other side. 

Based on the recorded timestamps and output of speech recognizer, abnormal conditions in a call such as echoes, white noise, bad audio channel, call connection and hang-up latencies are detected.