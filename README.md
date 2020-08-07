![](doc/images/ggpo_header.png)

## What's GGPO-Sharp?

GGPO-Sharp is a C# port of the C++ version with a network agnostic framework so it can work with any networking library.

### Current progress/to-do

- Finish porting to C# (mostly done, but there's some minor bugs to solve)
- Break out the networking into an interface to make it agnostic and customizable
- Polish to make the code more intuitive and plug-and-play for Unity

## What's GGPO?

Traditional techniques account for network transmission time by adding delay to a players input, resulting in a sluggish, laggy game-feel.  Rollback networking uses input prediction and speculative execution to send player inputs to the game immediately, providing the illusion of a zero-latency network.  Using rollback, the same timings, reactions, visual and audio queues, and muscle memory your players build up playing offline will translate directly online.  The GGPO networking SDK is designed to make incorporating rollback networking into new and existing games as easy as possible.

For more information about the history of GGPO, check out http://ggpo.net/

For the original C++ version, check out https://github.com/pond3r/ggpo

## Building

Building GGPO-Sharp is only available on Windows.

Windows builds requires [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/)

- Open `src/ggpo-sharp.sln` solution for Visual Studio 2019 to compile.

## Sample Application

The C# Vector War application in the source directory contains a simple application which uses GGPO to synchronize the two clients.  The command line arguments are:

```
vectorwar.exe  <localport>  <num players> ('local' | <remote ip>:<remote port>) for each player
```

See the .cmd files in the bin directory for examples on how to start 2, 3, and 4 player games.

## Licensing

GGPO-Sharp is available under The MIT License. This means GGPO-Sharp is free for commercial and non-commercial use. Attribution is not required, but appreciated.
