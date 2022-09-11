using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TurnAi.Games.Tictactoe {

    /// <summary>Template for deserializing Json provided by the player.</summary>
    /// <remarks>All fields are nullable to distinguish missing values.</remarks>
    public class Turn {
        public int? X { get; set; }
        public int? Y { get; set; }
    }

    /// <summary>Template for serializing Json to be sent to the player.</summary>
    public class GameInfo {
        public char You { get; set; }
        public char Opponent { get; set; }
        public char Empty { get; set; }
        public int Size { get; set; }
        public int WinningLength { get; set; }
        public string[] Board { get; set; }

        public GameInfo(
            char you, char opponent, char empty,
            int size, int winningLength, string[] board
        ) {
            You = you;
            Opponent = opponent;
            Empty = empty;
            Size = size;
            WinningLength = winningLength;
            Board = board;
        }
    }

    public record Coords {
        public int X;
        public int Y;

        public static Move operator-(Coords a, Coords b) {
            return new Move() { Dx = a.X - b.X, Dy = a.Y - b.Y };
        }
    }

    public record Move {
        public int Dx;
        public int Dy;

        public static readonly Move Down = new() { Dx = 0, Dy = 1 };
        public static readonly Move Right = new() { Dx = 1, Dy = 0 };

        public static Move operator+(Move a, Move b) {
            return new() { Dx = a.Dx + b.Dx, Dy = a.Dy + b.Dy };
        }

        public static Move operator-(Move a, Move b) {
            return a + -b;
        }

        public static Move operator*(int scale, Move a) {
            return new() { Dx = scale * a.Dx, Dy = scale * a.Dy };
        }

        public static Move operator-(Move a) {
            return -1 * a;
        }

        public static Coords operator+(Coords a, Move b) {
            return new() { X = a.X + b.Dx, Y = a.Y + b.Dy };
        }

        public static Coords operator-(Coords a, Move b) {
            return a + -b;
        }
    }


    public class TictactoeGame : IGame {
        private enum Symbol {
            Empty = '.',
            P0 = 'x',
            P1 = 'o'
        }

        private int nextPlayer = 0;
        private string?[] errors = new string?[NumPlayers];
        private int boardSize;
        private int fieldsLeft;
        private string[] board;
        private Coords firstFreeField = new Coords { X = 0, Y = 0 };

        public static int NumPlayers { get => 2; }
        public int WinningLength { get; init; } = 5;
        int IGame.NumPlayers => NumPlayers;
        private int winner = -1; // -1 = no winner yet, -2 = draw
        public bool IsFinished => winner != -1;

        public TictactoeGame(int boardSize) {
            this.boardSize = boardSize;
            board = new string[boardSize];
            for (int i = 0; i < boardSize; i++) {
                board[i] = new string((char)Symbol.Empty, boardSize);
            }
            fieldsLeft = boardSize * boardSize;
        }

        private Symbol this[Coords c] {
            get => (Symbol)board[c.Y][c.X];
            set {
                var row = board[c.Y].ToCharArray();
                row[c.X] = (char)value;
                board[c.Y] = new string(row);
            }
        }

        public void PlayTurn(int playerId, JsonNode? turn) {
            AssertPlayerId(playerId);
            AssertNotFinished();
            (Coords coords, string? error) = ParseTurn(turn);
            errors[playerId] = error; // stores null if no error
            this[coords] = PlayerToSymbol(playerId);
            fieldsLeft--;
            nextPlayer = 1 - nextPlayer;
            EvaluateTurn(coords, playerId);
            PrintBoard();
            if (!FindFirstFreeField()) winner = -2; // draw
        }

        private void PrintBoard() {
            Console.WriteLine();
            foreach (var line in board) {
                Console.WriteLine(line);
            }
        }

        public bool MayPlay(int playerId) {
            return nextPlayer == playerId;
        }

        public JsonNode GetGameInfo(int playerId) {
            AssertPlayerId(playerId);
            int otherPlayerId = 1 - playerId;
            var gameInfo = new GameInfo(
                (char)PlayerToSymbol(playerId),
                (char)PlayerToSymbol(otherPlayerId),
                (char)Symbol.Empty,
                boardSize,
                WinningLength,
                board
            );
            return JsonSerializer.SerializeToNode(gameInfo, Config.SerializerOptions)!;
        }

        public string? GetError(int playerId) {
            AssertPlayerId(playerId);
            return errors[playerId];
        }

        public int GetPoints(int playerId) {
            AssertPlayerId(playerId);
            if (!IsFinished) throw new GameStateException("Game is not finished yet.");
            if (winner == -2) return 0;
            if (winner == playerId) return 1;
            return -1;
        }

        /// <returns>Parsed coords and error message.</returns>
        private ValueTuple<Coords, string?> ParseTurn(JsonNode? turn) {
            if (turn == null) {
                return (firstFreeField, "No turn provided.");
            }
            var deserialized = JsonSerializer.Deserialize<Turn>(turn, Config.SerializerOptions);
            var x = deserialized!.X;
            var y = deserialized!.Y;
            if (x == null || y == null) {
                return (firstFreeField, "Coordinates not provided.");
            }
            Coords coords = new Coords { X = x.Value, Y = y.Value };
            if (!IsOnBoard(coords)) {
                return (firstFreeField, "Coordinates out of bounds.");
            }
            if (this[coords] != Symbol.Empty) {
                return (firstFreeField, "Field is already occupied.");
            }
            return (coords, null);
        }

        private void EvaluateTurn(Coords coords, int playerId) {
            // look for a winning line
            var dirs = new Move[] {
                Move.Right,
                Move.Down + Move.Right,
                Move.Down,
                Move.Down - Move.Right
            };
            foreach (var dir in dirs) {
                if (LineLength(coords, dir) >= WinningLength) {
                    winner = playerId;
                    return;
                }
            }
        }

        // calculate length of line in along dir (and backwards)
        private int LineLength(Coords coords, Move dir) {
            int length = 1;
            Symbol symbol = this[coords];
            var field = coords + dir;
            while (IsOnBoard(field) && this[field] == symbol) {
                length++;
                field += dir;
            }
            field = coords - dir;
            while (IsOnBoard(field) && this[field] == symbol) {
                length++;
                field -= dir;
            }

            return length;
        }

        /// <returns><c>true</c> if a free field exists.</returns>
        private bool FindFirstFreeField() {
            while (this[firstFreeField] != Symbol.Empty) {
                firstFreeField.Y++;
                if (firstFreeField.Y == boardSize) {
                    firstFreeField.Y = 0;
                    firstFreeField.X++;
                }
                if (firstFreeField.X == boardSize) {
                    return false;
                }
            }
            return true;
        }

        private Symbol PlayerToSymbol(int playerId) {
            return playerId == 0 ? Symbol.P0 : Symbol.P1;
        }

        private bool IsOnBoard(Coords c) {
            return c.X >= 0 && c.X < boardSize && c.Y >= 0 && c.Y < boardSize;
        }

        // throw an exception if playerId is invalid
        private void AssertPlayerId(int playerId) {
            if (playerId < 0 || playerId >= NumPlayers) {
                throw new ArgumentOutOfRangeException(
                    nameof(playerId),
                    playerId,
                    $"Player {playerId} does not exist."
                );
            }
        }

        // throw an exception if game is finished
        private void AssertNotFinished() {
            if (IsFinished) throw new GameStateException("Game is finished.");
        }
    }

    public class TictactoeFactory : IFactory<IGame> {
        public int BoardSize { get; init; }

        public TictactoeFactory(int boardSize) {
            BoardSize = boardSize;
        }

        public IGame Create() => new TictactoeGame(BoardSize);
    }

}
