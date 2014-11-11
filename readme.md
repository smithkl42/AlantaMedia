# Alanta Application Website

## Building
To get the Alanta application website to build, follow these steps:

1. Open the AlantaServer.sln file in Visual Studio and build.

OK, well, that was maybe a little obvious. Sorry.

## Rolling Out the Website

Right-click on the Alanta.Web.App project in Visual Studio, and choose "Publish".

You may need to configure some options before you do. They will look something like this:

![Publish step 1](//bitbucket.org/smithkl42/alanta/raw/default/AlantaClient/Images/Publish1.png "Publish step 1")

![Publish step 2](//bitbucket.org/smithkl42/alanta/raw/default/AlantaClient/Images/Publish2.png "Publish step 2")

![Publish step 3](//bitbucket.org/smithkl42/alanta/raw/default/AlantaClient/Images/Publish3.png "Publish step 3")

![Publish step 4](//bitbucket.org/smithkl42/alanta/raw/default/AlantaClient/Images/Publish4.png "Publish step 4")

I've tried in the past to automate some of this through scripts, 
but MS has made it astonishingly difficult to get the syntax right, 
and I eventually gave up, concluding my efforts would be more useful elsewhere. 
You're welcome to give it another shot :-).

For the database associated with this application, see the 
[readme.md](//bitbucket.org/smithkl42/alanta/src/default/AlantaClient/AlantaDB/readme.md)
in the [AlantaDB](//bitbucket.org/smithkl42/alanta/src/default/AlantaClient/AlantaDB/) folder.