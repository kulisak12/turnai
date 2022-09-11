using System;
using System.Collections;
using System.Collections.Generic;

namespace TurnAi.Games.Tictactoe.Utils {

    public interface IBoard {
        int Size { get; }
        bool IsOnBoard(Coords c);
        char GetSymbol(Coords c);
    }

    public class Line : IEnumerable<char> {
        private readonly IBoard board;
        private readonly Coords start;
        private readonly Coords end;

        public Line(IBoard board, Coords start, Coords end) {
            this.board = board;
            this.start = start;
            this.end = end;
        }

        public IEnumerator<char> GetEnumerator() {
            Move fullMove = end - start;
            Move unitMove = new Move() { Dx = Math.Sign(fullMove.Dx), Dy = Math.Sign(fullMove.Dy) };
            Coords current = start;
            do {
                yield return board.GetSymbol(current);
                current = current + unitMove;
            } while (current != end);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct Board : IBoard {
        private readonly string[] board;

        public Board(string[] board) {
            this.board = board;
        }

        public int Size => board.Length;
        public bool IsOnBoard(Coords pos) {
            return pos.X >= 0 && pos.X < Size && pos.Y >= 0 && pos.Y < Size;
        }
        public char GetSymbol(Coords c) => board[c.Y][c.X];

        public Board WithSymbol(Coords c, char symbol) {
            string[] newBoard = new string[Size];
            Array.Copy(board, newBoard, Size);
            var row = newBoard[c.X].ToCharArray();
            row[c.Y] = symbol;
            newBoard[c.X] = new string(row);
            return new Board(newBoard);
        }
    }

    public struct ModifiedBoard : IBoard {
        public Board Board { get; init; }
        public Coords MoveCoords { get; init; }
        public char MoveSymbol { get; init; }

        public ModifiedBoard(Board board, Coords moveCoords, char moveSymbol) {
            Board = board;
            MoveCoords = moveCoords;
            MoveSymbol = moveSymbol;
        }

        public int Size => Board.Size;
        public bool IsOnBoard(Coords pos) => Board.IsOnBoard(pos);

        public char GetSymbol(Coords c) {
            if (c == MoveCoords) return MoveSymbol;
            return Board.GetSymbol(c);
        }

        public static explicit operator Board(ModifiedBoard board) {
            return board.Board.WithSymbol(board.MoveCoords, board.MoveSymbol);
        }
    }
}
