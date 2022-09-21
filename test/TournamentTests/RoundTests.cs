using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace TurnAi.TournamentTests {
    /// <summary>Mockup which allows fields to be configured anytime.</summary>
    class ConfigurableGameMockup : IGame {
        public int NumPlayers { get; init; }
        public bool IsFinished { get; set; }
        public bool[] MayPlayArray;

        public ConfigurableGameMockup(int numPlayers) {
            NumPlayers = numPlayers;
            MayPlayArray = new bool[numPlayers];
        }

        public string? GetError(int playerId) {
            return null;
        }

        public JsonNode GetGameInfo(int playerId) {
            return new JsonObject();
        }

        public int GetPoints(int playerId) {
            return 1;
        }

        public bool MayPlay(int playerId) {
            return MayPlayArray[playerId];
        }

        public void PlayTurn(int playerId, JsonNode? turn) {
            return;
        }
    }

    /// <summary>Mockup which automatically finished once all players have played.</summary>
    class SingleTurnGameMockup : IGame {
        public int NumPlayers { get; init; }
        public bool IsFinished => numPlayersPlayed == NumPlayers;
        private int numPlayersPlayed = 0;

        public SingleTurnGameMockup(int numPlayers) {
            NumPlayers = numPlayers;
        }

        public string? GetError(int playerId) {
            return null;
        }

        public JsonNode GetGameInfo(int playerId) {
            return new JsonObject();
        }

        public int GetPoints(int playerId) {
            return 1;
        }

        public bool MayPlay(int playerId) {
            return !IsFinished;
        }

        public void PlayTurn(int playerId, JsonNode? turn) {
            numPlayersPlayed++;
        }
    }

    class GameMockupFactory : IFactory<IGame> {
        private readonly IGame Instance;

        public GameMockupFactory(IGame instance) {
            Instance = instance;
        }

        public IGame Create() => Instance;
    }

    class SingleTurnGameFactory : IFactory<IGame> {
        private readonly int numPlayers;

        public SingleTurnGameFactory(int numPlayers) {
            this.numPlayers = numPlayers;
        }

        public IGame Create() => new SingleTurnGameMockup(numPlayers);
    }

    public class RoundTests {
        public string? GetError(JsonNode? node) {
            return node?["error"]?.GetValue<string>() ?? null;
        }

        [Fact]
        public void WrongRobotId() {
            int numRobots = 3;
            ConfigurableGameMockup game = new ConfigurableGameMockup(numRobots);
            Round round = new Round(
                numRobots,
                new AllOrderedPairsMatchMaker(numRobots),
                new GameMockupFactory(game)
            );
            Assert.Throws<ArgumentOutOfRangeException>(() => round.RobotGet(numRobots));
        }

        [Fact]
        public void RobotNotInGame() {
            int numRobots = 3;
            ConfigurableGameMockup game = new ConfigurableGameMockup(numRobots);
            Round round = new Round(
                numRobots,
                new AllOrderedPairsMatchMaker(numRobots),
                new GameMockupFactory(game)
            );
            Assert.Null(round.RobotGet(2));

            var turnRequest = new RobotTurnRequest { Seq = 0, Turn = null };
            var error = round.RobotPost(
                2, JsonSerializer.SerializeToNode(turnRequest, Config.SerializerOptions)!
            );
            Assert.Equal("Robot is not in a game.", GetError(error));
        }

        [Fact]
        public void ResubmissionOfTurn() {
            int numRobots = 2;
            ConfigurableGameMockup game = new ConfigurableGameMockup(numRobots);
            game.MayPlayArray[0] = true;
            Round round = new Round(
                numRobots,
                new AllOrderedPairsMatchMaker(numRobots),
                new GameMockupFactory(game)
            );
            var dataResponse = JsonSerializer.Deserialize<RobotDataResponse>(
                round.RobotGet(0)!, Config.SerializerOptions
            );
            int seq = dataResponse!.Seq;
            var turnRequest = new RobotTurnRequest { Seq = seq, Turn = null };
            var serialized = JsonSerializer.SerializeToNode(turnRequest, Config.SerializerOptions);
            var error = round.RobotPost(0, serialized!);
            Assert.Null(error);
            error = round.RobotPost(0, serialized!);
            Assert.StartsWith("Wrong sequence number", GetError(error));
        }

        [Fact]
        public void RobotMayPlay() {
            int numRobots = 2;
            ConfigurableGameMockup game = new ConfigurableGameMockup(numRobots);
            Round round = new Round(
                numRobots,
                new AllOrderedPairsMatchMaker(numRobots),
                new GameMockupFactory(game)
            );
            game.MayPlayArray[0] = true;
            Assert.NotNull(round.RobotGet(0));
            Assert.NotNull(round.RobotGet(0)); // second call should return the same data
            var dataResponse = JsonSerializer.Deserialize<RobotDataResponse>(
                round.RobotGet(0)!, Config.SerializerOptions
            );
            game.MayPlayArray[0] = false;
            Assert.Null(round.RobotGet(0));

            int seq = dataResponse!.Seq;
            var turnRequest = new RobotTurnRequest { Seq = seq, Turn = null };
            var error = round.RobotPost(
                0, JsonSerializer.SerializeToNode(turnRequest, Config.SerializerOptions)!
            );
            Assert.Equal("It is not your turn.", GetError(error));
        }


        [Fact]
        public void LateResponse() {
            int numRobots = 2;
            ConfigurableGameMockup game = new ConfigurableGameMockup(numRobots);
            game.MayPlayArray[0] = true;
            Round round = new Round(
                numRobots,
                new AllOrderedPairsMatchMaker(numRobots),
                new GameMockupFactory(game)
            );
            var dataResponse = JsonSerializer.Deserialize<RobotDataResponse>(
                round.RobotGet(0)!, Config.SerializerOptions
            );
            int seq = dataResponse!.Seq;
            Task.Delay(Config.ResponseLimit + TimeSpan.FromSeconds(1)).Wait();
            var turnRequest = new RobotTurnRequest { Seq = seq, Turn = null };
            var serialized = JsonSerializer.SerializeToNode(turnRequest, Config.SerializerOptions);
            var error = round.RobotPost(0, serialized!);
            Assert.StartsWith("Took too long", GetError(error));
        }

        [Theory]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void PointsTotal(int numRobots) {
            int expectedNumPoints = 2 * (numRobots - 1);

            SingleTurnGameMockup game = new SingleTurnGameMockup(numRobots);
            Round round = new Round(
                numRobots,
                new AllOrderedPairsMatchMaker(numRobots),
                new SingleTurnGameFactory(numRobots)
            );
            while (!round.IsFinished) {
                for (int i = 0; i < numRobots; i++) {
                    var dataResponseNode = round.RobotGet(i);
                    if (dataResponseNode == null) continue;
                    var dataResponse = JsonSerializer.Deserialize<RobotDataResponse>(
                        dataResponseNode, Config.SerializerOptions
                    );
                    int seq = dataResponse!.Seq;
                    var turnRequest = new RobotTurnRequest { Seq = seq, Turn = null };
                    var serialized = JsonSerializer.SerializeToNode(
                        turnRequest, Config.SerializerOptions
                    );
                    round.RobotPost(i, serialized!);
                }
            }

            for (int i = 0; i < numRobots; i++) {
                Assert.Equal(expectedNumPoints, round.GetPoints(i));
            }
        }

        [Fact]
        public void FinishedPromise() {
            int numRobots = 2;
            SingleTurnGameMockup game = new SingleTurnGameMockup(numRobots);
            Round round = new Round(
                numRobots,
                new AllPairsMatchMaker(numRobots),
                new SingleTurnGameFactory(numRobots)
            );

            bool finished = false;
            Task.Run(async () => {
                await round.RoundFinished;
                finished = true;
            });

            for (int i = 0; i < numRobots; i++) {
                var dataResponseNode = round.RobotGet(i);
                if (dataResponseNode == null) continue;
                var dataResponse = JsonSerializer.Deserialize<RobotDataResponse>(
                    dataResponseNode, Config.SerializerOptions
                );
                int seq = dataResponse!.Seq;
                var turnRequest = new RobotTurnRequest { Seq = seq, Turn = null };
                var serialized = JsonSerializer.SerializeToNode(
                    turnRequest, Config.SerializerOptions
                );
                round.RobotPost(i, serialized!);
            }

            Task.Delay(1000).Wait(); // finishing runs in a separate thread, so we need to wait
            Assert.True(round.IsFinished);
            Assert.True(finished);
        }
    }
}
