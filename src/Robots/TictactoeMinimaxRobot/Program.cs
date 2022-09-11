using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

using TurnAi.Games.Tictactoe;
using TurnAi.Games.Tictactoe.Utils;

namespace TurnAi.Robots.Tictactoe.Minimax {

    class Program {
        struct TurnOption {
            public ModifiedBoard Turn { get; init; }
            public int Score { get; init; }

            public TurnOption(ModifiedBoard turn, int score) {
                Turn = turn;
                Score = score;
            }
        }

        static readonly int topK = 500;
        static readonly int maxDepth = 10;

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
            options = TopK(options, topK);

            // one minimax iteration (more would be too slow)
            int bestScore = int.MinValue;
            Coords bestTurn = new Coords();
            foreach (var option in options) {
                var board = (Board) option.Turn;
                var nextOptions = ConstructNextTurns(
                    board, gameInfo.Opponent, option.Score, gameInfo
                );
                int score = nextOptions.Min(o => o.Score);
                if (score > bestScore) {
                    bestScore = score;
                    bestTurn = option.Turn.MoveCoords;
                }
            }

            var turn = new Turn() { X = bestTurn.X, Y = bestTurn.Y };
            return JsonSerializer.SerializeToNode(turn, Config.SerializerOptions)!;
        }

        static List<TurnOption> ConstructNextTurns(
            Board board, char player, int initialScore, GameInfo gameInfo
        ) {
            List<TurnOption> options = new List<TurnOption>();
            // go through all possible next turns
            foreach (var playCoords in GetEmptyCoords(board)) {
                int scoreBefore = PosScore(playCoords, board, gameInfo);
                var modBoard = new ModifiedBoard(board, playCoords, player);
                int scoreAfter = PosScore(playCoords, modBoard, gameInfo);
                options.Add(new TurnOption(modBoard, initialScore + scoreAfter - scoreBefore));
            }
            return options;
        }

        static List<TurnOption> TopK(List<TurnOption> options, int k) {
            options.Sort((a, b) => b.Score - a.Score);
            if (options.Count > k) {
                options.RemoveRange(k, options.Count - k);
            }
            return options;
        }

        static List<Coords> GetEmptyCoords(Board board) {
            List<Coords> coords = new List<Coords>();
            for (int y = 0; y < board.Size; y++) {
                for (int x = 0; x < board.Size; x++) {
                    if (board.GetSymbol(new Coords() { X = x, Y = y }) == ' ') {
                        coords.Add(new Coords() { X = x, Y = y });
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
            // value number of symbols with 3rd power
            if (numMe > 0) return Utility.IntPow(numMe, 3);
            return -Utility.IntPow(numOp, 3);
        }





    }
}
