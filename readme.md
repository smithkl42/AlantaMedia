# Alanta Media Library

## Building
To get the Alanta media client to build:

1. Install Visual Studio 2013 (make sure to include the Silverlight bits).
2. Open the AlantaMedia.sln file in Visual Studio and build.

OK, well, that was maybe a little obvious. Sorry.

## Notes

This project grew out of Alanta's attempt to create a complete realtime media stack 
for its Silverlight client. It consists of several pre-existing open-source pieces
(such as the CSpeex and FJCore libraries), along with several pieces 

At the moment, this version of the Alanta Media library isn't of much use, for various reasons.
The biggest one is that Microsoft decided to shoot Silverlight in the head, so any reason
for its existence is largely gone. 

A significant secondary reason is that much of the code was (excessively) tightly coupled 
to the rest of Alanta's client, to the point where splitting them out became complicated. 
The solution as it stands builds - but it doesn't do anything interesting.
To do something interesting, I'd need to spend some significant time refactoring the tests, 
providing sample code, and including a simple, more-or-less standalone sample application.

A third reason why this doesn't do anything interesting is that the entire client-side
media stack is designed to work with Alanta's media server, and I haven't taken the trouble
to push that out to Github. If there's any interest whatsoever, I'd be happy to do that - but 
I think I can be reasonably confident that there won't be any.