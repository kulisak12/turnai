# TurnAi, Developer Documentation

## Customizing

The entire project is built in a way which allows any game to be played. This is
done using the `IGame` and `IMatchMaker` interfaces. These interfaces are
heavily documented, since it is expected that the user will create their own
implementations.

Currently, the `IGame` interface is limited to turn-based games.

## Overview

The project is split into two main parts: the `Tournament` directory contains
the core logic for running a tournament. The `Games` and `Robots` directories
contain examples of games and robots that can be implemented.

### Round

The tournament logic mostly resides withing the `Round` instances. A round keeps
track of robots and matches. Whenever a match is finished, the robots are passed
to `IMatchMaker`, which returns the ids of robots that should be matched up
next. The round then creates a new game instance for these robots. Since the
`IGame` implementation only holds the state of the game, the round needs to
store additional information. This is done using the `Match` class.

The `Round` instance receives robot requests forwarded by the API server. First,
it looks for the match that the given robot is in. It then communicates with the
`IGame` instance to get the game state or to play the move.

The entire round logic is designed to be thread-safe. It is assumed that
requests from the server can come in parallel, even from a single robot. Since
individual matches are independent, it suffices to ensure thread safety within a
single match. In addition, any operations that create new matches and end old
ones are synchronized.

### Server

The server is a simple HTTP server that forwards requests to the `Round`
instance. It is run asynchronously and can be cancelled with a
`CancellationToken` when a round is finished.

The webserver isn't cancellable, it is expected that the program will be
interrupted with `Ctrl+C`.

## Building

The entire application can be built and executed using the `dotnet` command in
the `src/Main` directory.

## Code documentation

The most important code parts are documented with doccomments. The documentation
can be generated using `doxygen Doxyfile` in the `src/main` directory.
