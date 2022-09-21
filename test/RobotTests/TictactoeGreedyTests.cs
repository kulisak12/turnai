using Xunit;

using TurnAi.Games.Tictactoe;
using TurnAi.Games.Tictactoe.Utils;
using TurnAi.Robots.Tictactoe.Greedy;

namespace TurnAi.RobotTests {
    public class TictactoeGreedyTests {

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
    }
}
