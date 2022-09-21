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
    }
}
