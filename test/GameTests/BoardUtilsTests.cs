using Xunit;

using TurnAi.Games.Tictactoe;
using TurnAi.Games.Tictactoe.Utils;

namespace TurnAi.RobotTests {
    public class TictactoeMinimaxTests {
        private class BoardMockup : IBoard {
            public int Size => 10;
            public char GetSymbol(Coords c) => 'x';
            public bool IsOnBoard(Coords c) => true;
        }

        [Fact]
        public void LineEnumerableNumSymbols() {
            Line line = new Line(
                new BoardMockup(),
                new Coords() { X = 0, Y = 0 },
                new Coords() { X = 4, Y = 4 }
            );
            int numFields = 0;
            foreach (var symbol in line) {
                numFields++;
            }
            Assert.Equal(5, numFields);
        }

        [Fact]
        public void EmptyCoords() {
            var gameInfo = new GameInfo(
                'x', 'o', '.', 3, 3,
                new string[] {
                    "xox",
                    ".xo",
                    "xo."
                }
            );
            var board = new Board(gameInfo.Board);
            var empty = BoardUtils.GetEmptyCoords(board, gameInfo);
            Assert.Equal(new Coords[] {
                new Coords() { X = 0, Y = 1 },
                new Coords() { X = 2, Y = 2 },
            }, empty);
        }

        [Fact]
        public void ModifiedBoard() {
            var gameInfo = new GameInfo(
                'x', 'o', '.', 3, 3,
                new string[] {
                    "xox",
                    ".xo",
                    "xo."
                }
            );
            var board = new Board(gameInfo.Board);
            var modified = new ModifiedBoard(board, new Coords() { X = 0, Y = 1 }, 'x');
            Assert.Equal('x', modified.GetSymbol(new Coords() { X = 0, Y = 1 }));
            var applied = (Board)modified;
            Assert.Equal('x', applied.GetSymbol(new Coords() { X = 0, Y = 1 }));
            // make sure the original board did not change
            Assert.Equal('.', board.GetSymbol(new Coords() { X = 0, Y = 1 }));
        }

        [Fact]
        public void IsOnBoard() {
            var board = new Board(new string[] {
                "xox",
                ".xo",
                "xo."
            });
            Assert.True(board.IsOnBoard(new Coords() { X = 0, Y = 0 }));
            Assert.True(board.IsOnBoard(new Coords() { X = 2, Y = 2 }));
            Assert.False(board.IsOnBoard(new Coords() { X = -1, Y = 0 }));
            Assert.False(board.IsOnBoard(new Coords() { X = 0, Y = -1 }));
            Assert.False(board.IsOnBoard(new Coords() { X = 3, Y = 0 }));
            Assert.False(board.IsOnBoard(new Coords() { X = 0, Y = 3 }));
        }
    }
}
