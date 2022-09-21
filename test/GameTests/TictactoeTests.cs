using System.Text.Json.Nodes;
using Xunit;

using TurnAi.Games.Tictactoe;
using System.Text.Json;

namespace TurnAi.GameTests {

    public class TictactoeTests {
        private static JsonNode GetCoordsNode(int x, int y) {
            return new JsonObject() {
                { "x", x },
                { "y", y },
            };
        }

        [Fact]
        public void MayPlay() {
            var game = new TictactoeGame(10);
            for (int i = 0; i < 4; i++) {
                Assert.True(game.MayPlay(0));
                Assert.False(game.MayPlay(1));
                game.PlayTurn(0, GetCoordsNode(i, 0));

                Assert.True(game.MayPlay(1));
                Assert.False(game.MayPlay(0));
                game.PlayTurn(1, GetCoordsNode(i, 1));
            }
        }

        [Fact]
        public void Victory() {
            var game = new TictactoeGame(10);
            for (int i = 0; i < 4; i++) {
                game.PlayTurn(0, GetCoordsNode(i, 0));
                game.PlayTurn(1, GetCoordsNode(i, 1));
            }
            game.PlayTurn(0, GetCoordsNode(4, 0));
            Assert.True(game.IsFinished);
            Assert.Equal(1, game.GetPoints(0));
            Assert.Equal(-1, game.GetPoints(1));
        }

        [Fact]
        public void Draw() {
            var game = new TictactoeGame(5);
            for (int i = 0; i < 4; i++) {
                for (int j = 0; j < 5; j++) {
                    game.PlayTurn(j % 2, GetCoordsNode(i, j));
                }
            }
            for (int j = 0; j < 5; j++) {
                game.PlayTurn((j + 1) % 2, GetCoordsNode(4, j));
            }
            Assert.True(game.IsFinished);
            Assert.Equal(0, game.GetPoints(0));
            Assert.Equal(0, game.GetPoints(0));
        }

        [Fact]
        public void GameInfo() {
            var game = new TictactoeGame(10);
            game.PlayTurn(0, GetCoordsNode(0, 2));
            game.PlayTurn(1, GetCoordsNode(1, 2));
            game.PlayTurn(0, GetCoordsNode(2, 2));
            game.PlayTurn(1, GetCoordsNode(3, 2));
            game.PlayTurn(0, GetCoordsNode(4, 2));

            GameInfo p1Info = JsonSerializer.Deserialize<GameInfo>(
                game.GetGameInfo(1), Config.SerializerOptions
            )!;
            Assert.Equal('o', p1Info.You);
            Assert.Equal('x', p1Info.Opponent);
            Assert.Equal('.', p1Info.Empty);
            Assert.Equal(10, p1Info.Size);
            Assert.Equal(5, p1Info.WinningLength);
            var board = p1Info.Board;

            for (int i = 0; i < 10; i++) {
                if (i == 2) {
                    Assert.Equal("xoxox.....", board[i]);
                }
                else {
                    Assert.Equal("..........", board[i]);
                }
            }

            GameInfo p0Info = JsonSerializer.Deserialize<GameInfo>(
                game.GetGameInfo(0), Config.SerializerOptions
            )!;
            Assert.Equal('x', p0Info.You);
            Assert.Equal('o', p0Info.Opponent);
            Assert.Equal('.', p0Info.Empty);
            Assert.Equal(10, p0Info.Size);
            Assert.Equal(5, p0Info.WinningLength);

            for (int i = 0; i < 10; i++) {
                Assert.Equal(board[i], p0Info.Board[i]);
            }
        }

        [Fact]
        public void Errors() {
            var game = new TictactoeGame(10);
            game.PlayTurn(0, null);
            Assert.Equal("No turn provided.", game.GetError(0));
            game.PlayTurn(0, new JsonObject() {
                { "x", 1 }
            });
            Assert.Equal("Coordinates not provided.", game.GetError(0));
            game.PlayTurn(0, GetCoordsNode(10, 0));
            Assert.Equal("Coordinates out of bounds.", game.GetError(0));
            game.PlayTurn(0, GetCoordsNode(-1, 0));
            Assert.Equal("Coordinates out of bounds.", game.GetError(0));

            game.PlayTurn(0, GetCoordsNode(5, 5));
            Assert.Null(game.GetError(0));
            game.PlayTurn(1, GetCoordsNode(5, 5));
            Assert.Equal("Field is already occupied.", game.GetError(1));
        }
    }

}
