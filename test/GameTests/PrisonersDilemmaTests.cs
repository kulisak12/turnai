using System.Linq;
using System.Text.Json.Nodes;
using Xunit;

using TurnAi.Games.PrisonersDilemma;

namespace TurnAi.GameTests {

    public class PrisonersDilemmaTests {
        private static JsonNode GetActionNode(int action) {
            return new JsonObject() {
                { "action", action }
            };
        }

        private static int[] ArrayFromNode(JsonArray? array) {
            return array!.Select(node => node!.GetValue<int>()).ToArray();
        }

        [Fact]
        public void NumberOfTurns() {
            var game = new PrisonersDilemmaGame() { NumTotalTurns = 5 };
            for (int i = 0; i < 5; i++) {
                game.PlayTurn(0, GetActionNode(0));
                game.PlayTurn(1, GetActionNode(0));
            }
            Assert.True(game.IsFinished);
            Assert.False(game.MayPlay(0));
            Assert.False(game.MayPlay(1));
        }

        [Fact]
        public void PointTotal() {
            const int bothSilent = 3;
            const int betrayed = 0;
            const int betraying = 4;
            const int bothBetray = 1;

            var game = new PrisonersDilemmaGame() { NumTotalTurns = 3 };
            game.PlayTurn(0, GetActionNode(0));
            game.PlayTurn(1, GetActionNode(0));
            game.PlayTurn(0, GetActionNode(0));
            game.PlayTurn(1, GetActionNode(1));
            game.PlayTurn(0, GetActionNode(1));
            game.PlayTurn(1, GetActionNode(1));

            Assert.Equal(bothSilent + betrayed + bothBetray, game.GetPoints(0));
            Assert.Equal(bothSilent + betraying + bothBetray, game.GetPoints(1));
        }

        [Fact]
        public void MayPlay() {
            var game = new PrisonersDilemmaGame() { NumTotalTurns = 5 };
            Assert.True(game.MayPlay(0));
            Assert.True(game.MayPlay(1));

            game.PlayTurn(0, GetActionNode(0));
            Assert.False(game.MayPlay(0));
            Assert.True(game.MayPlay(1));

            game.PlayTurn(1, GetActionNode(0));
            Assert.True(game.MayPlay(0));
            Assert.True(game.MayPlay(1));

            game.PlayTurn(1, GetActionNode(0));
            Assert.True(game.MayPlay(0));
            Assert.False(game.MayPlay(1));

            game.PlayTurn(1, GetActionNode(0));
            Assert.True(game.MayPlay(0));
            Assert.False(game.MayPlay(1));

            game.PlayTurn(0, GetActionNode(0));
            Assert.True(game.MayPlay(0));
            Assert.False(game.MayPlay(1));

            game.PlayTurn(0, GetActionNode(0));
            Assert.True(game.MayPlay(0));
            Assert.True(game.MayPlay(1));
        }

        [Fact]
        public void GameInfo() {
            var game = new PrisonersDilemmaGame() { NumTotalTurns = 5 };
            game.PlayTurn(0, GetActionNode(0));
            game.PlayTurn(0, GetActionNode(1));
            game.PlayTurn(0, GetActionNode(1));
            game.PlayTurn(0, GetActionNode(0));
            game.PlayTurn(1, GetActionNode(1));
            game.PlayTurn(1, GetActionNode(0));
            game.PlayTurn(1, GetActionNode(0));

            var p0Info = game.GetGameInfo(0);
            Assert.Equal(1, p0Info["turnsLeft"]!.GetValue<int>());
            Assert.Equal(
                new int[] {0, 1, 1, 0},
                ArrayFromNode(p0Info["yourActions"] as JsonArray)
            );
            Assert.Equal(
                new int[] {1, 0, 0},
                ArrayFromNode(p0Info["opponentsActions"] as JsonArray)
            );

            var p1Info = game.GetGameInfo(1);
            Assert.Equal(2, p1Info["turnsLeft"]!.GetValue<int>());
            Assert.Equal(
                new int[] {1, 0, 0},
                ArrayFromNode(p1Info["yourActions"] as JsonArray)
            );
            Assert.Equal(
                new int[] {0, 1, 1, 0},
                ArrayFromNode(p1Info["opponentsActions"] as JsonArray)
            );
        }

        [Fact]
        public void Errors() {
            var game = new PrisonersDilemmaGame() { NumTotalTurns = 5 };
            game.PlayTurn(0, null);
            Assert.Equal("No turn provided.", game.GetError(0));
            game.PlayTurn(0, new JsonObject() {
                { "act", 1 }
            });
            Assert.Equal("No action provided.", game.GetError(0));
            game.PlayTurn(0, GetActionNode(0));
            Assert.Null(game.GetError(0));
        }
    }

}
