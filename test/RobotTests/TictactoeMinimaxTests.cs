using Xunit;

using TurnAi.Games.Tictactoe;
using TurnAi.Games.Tictactoe.Utils;
using TurnAi.Robots.Tictactoe.Minimax;

namespace TurnAi.RobotTests {
    public class TictactoeMinimaxTests {
        private class BoardMockup : IBoard {
            public int Size => 10;
            public char GetSymbol(Coords c) => 'x';
            public bool IsOnBoard(Coords c) => true;
        }

        [Fact]
        public void WinningTurn() {
            var gameInfo = new GameInfo(
                'x', 'o', '.', 10, 5,
                new string[] {
                    "..........",
                    "..........",
                    "..........",
                    "....xox...",
                    "....ox....",
                    "...ox.o...",
                    "...x......",
                    "..........",
                    "..........",
                    "..........",
                }
            );
            var turn = Program.FindBestTurn(gameInfo);
            var expected1 = new Coords() { X = 7, Y = 2 };
            var expected2 = new Coords() { X = 2, Y = 7 };
            Assert.True(turn.Equals(expected1) || turn.Equals(expected2));
        }

        [Fact]
        public void PreventOpponentWin() {
            var gameInfo = new GameInfo(
                'x', 'o', '.', 10, 5,
                new string[] {
                    "..........",
                    "..........",
                    "..........",
                    "....oxo...",
                    "....xox...",
                    "....o.x...",
                    "..........",
                    "..........",
                    "..........",
                    "..........",
                }
            );
            var turn = Program.FindBestTurn(gameInfo);
            var expected1 = new Coords() { X = 7, Y = 2 };
            var expected2 = new Coords() { X = 3, Y = 6 };
            Assert.True(turn.Equals(expected1) || turn.Equals(expected2));
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
    }
}
