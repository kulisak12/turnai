using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

using TurnAi.Games.Tictactoe;
using TurnAi.Games.Tictactoe.Utils;

namespace TurnAi.Robots.Tictactoe.Minimax {

    class Program {
        /// <summary>Tuple of turn and assigned score.</summary>
        struct TurnOption {
            public ModifiedBoard Turn { get; init; }
            public int Score { get; init; }

            public TurnOption(ModifiedBoard turn, int score) {
                Turn = turn;
                Score = score;
            }
        }

        /// <summary>Turns with score less than or equal to this will not be considered.</summary>
        // a turn played with no symbols around results in a score of 20
        static readonly int keepThreshold = 20;

        static void Main(string[] args) {
            if (args.Length != 1) {
                Console.Error.WriteLine("Required argument: robot name");
                Environment.Exit(1);
            }
            string robotName = args[0];
            Client.PlayRound(robotName, Strategy);
        }

        static JsonNode Strategy(JsonNode gameInfoNode) {
            GameInfo gameInfo = JsonSerializer.Deserialize<GameInfo>(
                gameInfoNode, Config.SerializerOptions
            )!;
            var options = ConstructNextTurns(new Board(gameInfo.Board), gameInfo.You, 0, gameInfo);
            // out of all possible turns, most just play a symbol in the open
            // only focus on the ones that are more interesting
            // however, always consider at least one turn
            options = SoftFilter(options, keepThreshold);

            // one minimax iteration (more would be too slow)
            int bestScore = int.MinValue;
            Coords bestTurn = new Coords();
            foreach (var option in options) {
                var board = (Board)option.Turn;
                var nextOptions = ConstructNextTurns(
                    board, gameInfo.Opponent, option.Score, gameInfo
                );
                // opponent will try to minimize our score
                int achievableScore = nextOptions.Min(o => o.Score);
                // find the maximum achievable score
                if (achievableScore > bestScore) {
                    bestScore = achievableScore;
                    bestTurn = option.Turn.MoveCoords;
                }
            }

            var turn = new Turn() { X = bestTurn.X, Y = bestTurn.Y };
            return JsonSerializer.SerializeToNode(turn, Config.SerializerOptions)!;
        }

        /// <summary>
        /// Construct all possible turns from the current board.
        /// For each turn, calculate its score.
        /// </summary>
        /// <param name="initialScore">Score for board before the turn.</param>
        static List<TurnOption> ConstructNextTurns(
            Board board, char player, int initialScore, GameInfo gameInfo
        ) {
            List<TurnOption> options = new List<TurnOption>();
            // go through all possible next turns
            foreach (var playCoords in GetEmptyCoords(board, gameInfo)) {
                // find the score increase
                int scoreBefore = PosScore(playCoords, board, gameInfo);
                var modBoard = new ModifiedBoard(board, playCoords, player);
                int scoreAfter = PosScore(playCoords, modBoard, gameInfo);
                options.Add(new TurnOption(modBoard, initialScore + scoreAfter - scoreBefore));
            }
            return options;
        }

        /// <summary>
        /// Filter out turns with score less than or equal to threshold.
        /// If all turns would be filtered out, return the best turns among them.
        /// </summary>
        static List<TurnOption> SoftFilter(List<TurnOption> options, int threshold) {
            var maxScore = options.Max(o => o.Score);
            // filtering will keep some options
            if (maxScore > threshold) {
                options.RemoveAll(o => o.Score < threshold);
                return options;
            }
            // can't filter, so keep the best
            return options.FindAll(o => o.Score == maxScore);
        }

        static List<Coords> GetEmptyCoords(Board board, GameInfo gameInfo) {
            List<Coords> coords = new List<Coords>();
            for (int y = 0; y < board.Size; y++) {
                for (int x = 0; x < board.Size; x++) {
                    Coords c = new Coords() { X = x, Y = y };
                    if (board.GetSymbol(c) == gameInfo.Empty) {
                        coords.Add(c);
                    }
                }
            }
            return coords;
        }

        static int PosScore(Coords pos, IBoard board, GameInfo gameInfo) {
            // aggregate all lines that pass through pos
            int sumScore = 0;
            var dirs = new Move[] {
                Move.Right,
                Move.Down + Move.Right,
                Move.Down,
                Move.Down - Move.Right
            };
            foreach (var dir in dirs) {
                for (int startShift = 0; startShift < gameInfo.WinningLength; startShift++) {
                    Coords start = pos - startShift * dir;
                    Coords end = start + (gameInfo.WinningLength - 1) * dir;
                    if (!board.IsOnBoard(start)) continue;
                    if (!board.IsOnBoard(end)) continue;
                    Line line = new Line(board, start, end);
                    sumScore += LineScore(line, gameInfo);
                }
            }
            return sumScore;
        }

        static int LineScore(Line line, GameInfo gameInfo) {
            int numMe = 0;
            int numOp = 0;
            foreach (char c in line) {
                if (c == gameInfo.You) numMe++;
                if (c == gameInfo.Opponent) numOp++;
            }
            // nobody can win with this line
            if (numMe > 0 && numOp > 0) return 0;

            // the scoring is the same for both players
            // just negative if it's the opponent
            int numSymbols = numMe + numOp;
            // value number of symbols with 3rd power
            // but if the game is won, the score is "infinite"
            const int inf = 1_000_000;
            int score;
            if (numSymbols == gameInfo.WinningLength) {
                score = inf;
            } else {
                score = Utility.IntPow(numSymbols, 3);
            }
            return (numMe > 0) ? score : -score;
        }

    }
}
