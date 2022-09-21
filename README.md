# TurnAi

Run a server to host tournaments. The games are played by robots that connect to
the server and play their turns automatically. It is intended as a programming
contest between teams of students, each team trying to code the smartest robot.

## Tournament

The main part of this project is a library for running tournaments. It contains
a server that works as an API endpoint and forwards requests to the ongoing
rounds. Another part is a web server which provides simple points overview of
the finished rounds.

### Rounds

A tournament can consist of multiple rounds. In each round, robots are paired
and play matches against each other. The pairing algorithm, known as a
`MatchMaker`, can be customized. Once all matches in a round are played, the
points are tallied.

The game played can be any turn-based game. To create a new game, create a new
implementation of the `IGame` interface.

## Robots

The robots are the players in the tournament. They are completely independent of
the tournament program and can be written in any programming language. They only
need to conform to the API.

Each robot is assigned a unique name. The robot then sends all requests to the
URL `http://<server>/<name>`.

A `GET` request returns the state of the game in JSON format. If the robot
should wait for its turn, the returned body contains an empty object. If the
robot may play, the response contains all neccessary information to play the
turn under the key `GameInfo`. The structure of this object is game-specific. In
addition, the response contains the key `Seq` which uniquely identifies the turn
and must be included when submitting a turn.

A `POST` request submits a turn. The body of the request must be a JSON object
with the `Seq` from the last `GET` request and the turn under the key `Turn`.
The structer of the turn object is once again game-specific.

For robots written in C#, it is possible to use the `TurnAi.Client` utility
which handles the communication with the server.

## Examples

### Prisoner's Dilemma

A simple example suitable for getting familiar with the system. This two-player
game only has two possible actions: to stay silent or to betray the other
player. Each combination of actions is rewarded with a certain number of points.
The game ends after a fixed number of turns.

The rewards are chosen such that the optimal strategy is to betray the other
player. However, if both players betray each other, they get no points at all.
Therefore, cooperation is the best strategy in the long run.

The game state sent to robots consist of the number of turns remaining and the
complete history of the previous turns.

Two example robots are provided. One always plays randomly. The other is silent
at first and then always mirrors the other player's last action.

### Tic-Tac-Toe

A more complex example. Both the grid size and the winning line length required
can be configured.

The game state sent to robots contains the current grid and metadata such as
symbol descriptions and game configuration.

Two example robots are provided. One is greedy and always tries to extend its
longest line. The other is more advanced and uses a minimax algorithm with
static scoring after the first two moves. This algorithm provides a reasonable
challenge even for a human opponent.

## License

&copy; 2022 David Klement, [MIT License](https://github.com/kulisak12/turnai/blob/main/LICENSE).

## Documentation

The developer documentation is available [here](https://github.com/kulisak12/turnai/blob/main/devdoc.md).
