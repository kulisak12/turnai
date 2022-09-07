using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace TurnAi {
    /// <summary>
    /// Processing of robots' requests.
    /// Primarily intended for tournament rounds.
    /// </summary>
    public interface IRound {
        /// <remarks>Robots are indexed from 0.</remarks>
        public int NumRobots { get; }

        /// <summary>Get game data for given robot.</summary>
        /// <returns><c>null</c> if no data is available, try again later.</returns>
        JsonNode? RobotGet(int robotId);

        /// <summary>Send turn data for given robot.</summary>
        /// <returns><c>null</c> on success, error JSON otherwise.</returns>
        JsonNode? RobotPost(int robotId, JsonNode turnNode);
    }

    /// <summary>JSON structure used when parsing turn submitted by robot.</summary>
    public class RobotTurnRequest {
        public int Seq { get; set; }
        public JsonNode? Turn { get; set; }
    }

    /// <summary>JSON structure used when sending game data to robots.</summary>
    public class RobotDataResponse {
        public int Seq { get; set; }
        public JsonNode GameInfo { get; set; }

        public RobotDataResponse(int seq, JsonNode gameInfo) {
            Seq = seq;
            GameInfo = gameInfo;
        }
    }

    /// <summary>JSON structure used when reporting errors in submitted turn.</summary>
    public class RobotErrorResponse {
        public string Error { get; set; }

        public RobotErrorResponse(string error) {
            Error = error;
        }
    }

    /// <summary>
    /// One round of the tournament.
    /// New instance is created for each round.
    /// </summary>
    public class Round : IRound {
        /// <summary>Wrapper with game metadata.</summary>
        private class Match {
            // thread safety:
            // Game, Seq, WasFinished: synchronized (this)
            // PlayerIds: immutable
            public IGame Game;
            public int[] Seq;
            public Stopwatch[] Timer;
            public bool WasFinished = false;
            // mapping between robot and player ids
            public readonly Dictionary<int, int> PlayerIds;

            public Match(Dictionary<int, int> playerIds, IGame game) {
                PlayerIds = playerIds;
                Game = game;
                Seq = new int[PlayerIds.Count];
                Timer = new Stopwatch[PlayerIds.Count];

                for (int i = 0; i < PlayerIds.Count; i++) {
                    // initialize sequence numbers randomly
                    Seq[i] = Random.Shared.Next(1, int.MaxValue / 2);
                    Timer[i] = new Stopwatch();
                }
            }
        }

        // thread safety:
        // matches: doesn't need to be synchronized, reads and writes are atomic
        // totalPoints: isolated
        // matchMaker, numRunningMatches: synchronized (matchMaker)
        private IMatchMaker matchMaker;
        private IFactory<IGame> gameFactory;
        private Match?[] matches; // mapping between robot ids and matches
        private int[] totalPoints;
        private int numRunningMatches = 0;
        private TaskCompletionSource tcs = new TaskCompletionSource();

        public int NumRobots { get; }
        public bool IsFinished {
            get {
                lock (matchMaker) {
                    return numRunningMatches == 0 && matchMaker.IsFinished;
                }
            }
        }
        public Task RoundFinished => tcs.Task;

        public Round(int numRobots, IMatchMaker matchMaker, IFactory<IGame> gameFactory) {
            NumRobots = numRobots;
            this.matchMaker = matchMaker;
            this.gameFactory = gameFactory;
            matches = new Match?[NumRobots];
            totalPoints = new int[NumRobots];

            for (int i = 0; i < numRobots; i++) {
                matchMaker.AddWaitingRobot(i);
            }
            StartNewMatches();
        }

        public JsonNode? RobotGet(int robotId) {
            AssertRobotId(robotId);
            Match? match = matches[robotId];
            // if it isn't this robot's turn, return null
            if (match == null) return null;
            int playerId = match.PlayerIds[robotId];

            RobotDataResponse? response = null;
            lock (match) {
                if (!match.Game.MayPlay(playerId)) return null;
                JsonNode gameInfo = match.Game.GetGameInfo(playerId);
                response = new RobotDataResponse(match.Seq[playerId], gameInfo);
            }

            // if this is the first request for the turn, start measuring response time
            if (!match.Timer[playerId].IsRunning) {
                match.Timer[playerId].Start();
            }

            return JsonSerializer.SerializeToNode(response, Config.SerializerOptions);
        }

        public JsonNode? RobotPost(int robotId, JsonNode turnNode) {
            AssertRobotId(robotId);
            Match? match = matches[robotId];
            if (match == null) {
                return Utility.GetErrorNode("Robot is not in a game.");
            }
            int playerId = match.PlayerIds[robotId];
            var request = JsonSerializer.Deserialize<RobotTurnRequest>(
                turnNode, Config.SerializerOptions
            )!;

            string? error = null;
            lock (match) {
                // prevent resubmission of the same turn
                if (request.Seq != match.Seq[playerId]) {
                    return Utility.GetErrorNode(
                        $"Wrong sequence number: expected {match.Seq[playerId]}, got {request.Seq}."
                    );
                }
                // prevent playing out of turn
                if (!match.Game.MayPlay(playerId)) {
                    return Utility.GetErrorNode("It is not your turn.");
                }
                // check response time
                match.Timer[playerId].Stop();
                TimeSpan elapsed = match.Timer[playerId].Elapsed;
                match.Timer[playerId].Reset();
                if (elapsed > Config.ResponseLimit) {
                    // play default turn
                    match.Game.PlayTurn(playerId, null);
                    return Utility.GetErrorNode($"Took too long ({elapsed.TotalMilliseconds} ms).");
                }

                match.Game.PlayTurn(playerId, request.Turn);
                match.Seq[playerId]++;
                error = match.Game.GetError(playerId);
                // match is finished in a task to make the response faster
                if (match.Game.IsFinished) Task.Run(() => FinishMatch(match));
            }
            return (error == null) ? null : Utility.GetErrorNode(error);
        }

        public int GetPoints(int robotId) {
            AssertRobotId(robotId);
            return totalPoints[robotId];
        }

        private void FinishMatch(Match match) {
            // ensure that the match is finished only once
            lock (match) {
                if (match.WasFinished) return;
                match.WasFinished = true;
            }

            lock (matchMaker) {
                foreach (int robotId in match.PlayerIds.Keys) {
                    int playerId = match.PlayerIds[robotId];
                    totalPoints[robotId] += match.Game.GetPoints(playerId);
                    matches[robotId] = null;
                    matchMaker.AddWaitingRobot(robotId);
                }
                numRunningMatches--;
                StartNewMatches();
                // the following will only run once, when the last match finishes
                if (IsFinished) tcs.SetResult();
            }
        }

        // must be called under lock (matchMaker)
        private void StartNewMatches() {
            int[]? robotsInMatch = matchMaker.GetNextMatch();
            while (robotsInMatch != null) {
                StartNewMatch(robotsInMatch);
                robotsInMatch = matchMaker.GetNextMatch();
            }
        }

        // must be called under lock (matchMaker)
        private void StartNewMatch(int[] robotsInMatch) {
            IGame game = gameFactory.Create();
            Dictionary<int, int> playerIds = new();
            for (int i = 0; i < robotsInMatch.Length; i++) {
                playerIds.Add(robotsInMatch[i], i);
            }
            Match match = new(playerIds, game);
            foreach (int robotId in robotsInMatch) {
                matches[robotId] = match;
            }
            numRunningMatches++;
        }

        // throw an exception if robotId is invalid
        private void AssertRobotId(int robotId) {
            if (robotId < 0 || robotId >= NumRobots) {
                throw new ArgumentOutOfRangeException(
                    nameof(robotId), robotId, $"Robot {robotId} does not exist."
                );
            }
        }
    }
}
