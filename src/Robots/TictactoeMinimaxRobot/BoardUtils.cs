using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

using TurnAi.Games.Tictactoe;

namespace TurnAi.Robots.PrisonersDilemma.Mirror {

    public interface IBoard {
        int Size { get; }
        string[] GetBoard();
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
        private string[] board;

        public Board(string[] board) {
            this.board = board;
        }

        public int Size => board.Length;
        public string[] GetBoard() => board;
        public char GetSymbol(Coords c) => board[c.Y][c.X];
    }

    public struct ModifiedBoard : IBoard {
        public string[] Board { get; init; }
        public Coords MoveCoords { get; init; }
        public char MoveSymbol { get; init; }

        public ModifiedBoard(string[] board, Coords moveCoords, char moveSymbol) {
            Board = board;
            MoveCoords = moveCoords;
            MoveSymbol = moveSymbol;
        }

        public int Size => Board.Length;

        public string[] GetBoard() {
            string[] newBoard = new string[Board.Length];
            Array.Copy(Board, newBoard, Board.Length);
            BoardUtils.PlayOnBoard(Board, MoveCoords, MoveSymbol);
            return newBoard;
        }

        public char GetSymbol(Coords c) {
            if (c == MoveCoords) return MoveSymbol;
            return Board[c.Y][c.X];
        }
    }


    public static class BoardUtils {
        public static bool IsOnBoard(Coords pos, int size) {
            return pos.X >= 0 && pos.X < size && pos.Y >= 0 && pos.Y < size;
        }

        public static void PlayOnBoard(string[] board, Coords coords, char symbol) {
            var row = board[coords.X].ToCharArray();
            row[coords.Y] = symbol;
            board[coords.X] = new string(row);
        }
    }

}
