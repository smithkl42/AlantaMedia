# Alanta Media Library

## Building
To get the Alanta media client to build:

1. Install Visual Studio 2013 (make sure to include the Silverlight bits).
2. Open the AlantaMedia.sln file in Visual Studio and build.

OK, well, that was maybe a little obvious. Sorry.

## Notes

This project grew out of Alanta's attempt to create a complete realtime media stack 
for its Silverlight client. It consists of several pre-existing open-source C# pieces
(such as the CSpeex and FJCore libraries), our ports to C# of the Speex and WebRTC Acoustic
Echo Cancellors and the WebRTC Voice Activation Detection and Acoustic Gain Control components, 
and several pieces built specifically for Alanta, most notably our 
attempt to build a rough-and-ready video codec that would be
suitable for realtime video communications.

At the moment, this version of the Alanta Media library isn't of much use, for various reasons.
The biggest one is that Microsoft decided to shoot Silverlight in the head, so any reason
for its existence is largely gone. If you need to do video chat in a web client, trust me, 
don't use Silverlight. Use Flash, or better yet, WebRTC, but steer clear of any environment 
(such as, say, Silverlight) that limits you to specific ports or to TCP (instead of UDP).

A significant secondary reason is that much of the code was (excessively) tightly coupled 
to the rest of Alanta's client, to the point where splitting them out became complicated. 
The solution as it stands builds - but it doesn't do anything interesting.
To do something interesting, I'd need to spend some significant time refactoring the tests, 
providing sample code, and including a simple, more-or-less standalone sample application.

A third reason why this doesn't do anything interesting is that the entire client-side
media stack is designed to work with Alanta's media server (using a proprietary and simplified version
of the RTP protocol), and I haven't taken the trouble to push that out to Github. 
If there's any interest whatsoever, I'd be happy to do that - but 
I think I can safely assume there won't be much.