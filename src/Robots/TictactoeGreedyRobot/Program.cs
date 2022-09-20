using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

using TurnAi.Games.Tictactoe;
using TurnAi.Games.Tictactoe.Utils;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RobotTests")]
namespace TurnAi.Robots.Tictactoe.Greedy {

    class Program {
        /// <summary>Tuple of turn and assigned score.</summary>
        public struct TurnOption {
            public ModifiedBoard Turn { get; init; }
            public int Score { get; init; }

            public TurnOption(ModifiedBoard turn, int score) {
                Turn = turn;
                Score = score;
            }
        }

        static void Main(string[] args) {
            if (args.Length != 1) {
                Console.Error.WriteLine("Required argument: robot name");
                Environment.Exit(1);
            }
            string robotName = args[0];
            Client.PlayRound(robotName, Strategy);
        }

        public static JsonNode Strategy(JsonNode gameInfoNode) {
            GameInfo gameInfo = JsonSerializer.Deserialize<GameInfo>(
                gameInfoNode, Config.SerializerOptions
            )!;
            var bestTurn = FindBestTurn(gameInfo);
            var turn = new Turn() { X = bestTurn.X, Y = bestTurn.Y };
            return JsonSerializer.SerializeToNode(turn, Config.SerializerOptions)!;
        }

        /// <summary>Extend the longest line.</summary>
        public static Coords FindBestTurn(GameInfo gameInfo) {
            int longestStreak = -1;
            Coords bestTurn = new Coords();
            var board = new Board(gameInfo.Board);
            foreach (var coords in BoardUtils.GetEmptyCoords(board, gameInfo)) {
                int streak = LongestStreakHere(coords, board, gameInfo);
                if (streak > longestStreak) {
                    longestStreak = streak;
                    bestTurn = coords;
                }
            }
            return bestTurn;
        }

        public static int LongestStreakHere(Coords pos, IBoard board, GameInfo gameInfo) {
            int longestStreak = 0;
            var dirs = new Move[] {
                Move.Right,
                Move.Down + Move.Right,
                Move.Down,
                Move.Down - Move.Right
            };
            foreach (var dir in dirs) {
                Coords end = pos + (gameInfo.WinningLength - 1) * dir;
                if (!board.IsOnBoard(pos)) continue;
                if (!board.IsOnBoard(end)) continue;
                Line line = new Line(board, pos, end);
                int streak = LineStreak(line, gameInfo);
                longestStreak = Math.Max(longestStreak, streak);
            }
            return longestStreak;
        }

        public static int LineStreak(Line line, GameInfo gameInfo) {
            int myStreak = 0;
            foreach (char c in line) {
                if (c == gameInfo.You) myStreak++;
                else if (c == gameInfo.Opponent) return -1; // can't win this line
                else break;
            }
            return myStreak;
        }

    }
}
