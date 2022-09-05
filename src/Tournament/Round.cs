using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

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

    /// <summary>
    /// One round of the tournament.
    /// New instance is created for each round.
    /// </summary>
    public class Round : IRound {
        /// <summary>Wrapper with game metadata.</summary>
        private class Match {
            public IGame Game;
            public Dictionary<int, int> PlayerIds; // mapping between robot and player ids
            public int[] Seq;

            public Match(Dictionary<int, int> playerIds, IGame game) {
                PlayerIds = playerIds;
                Game = game;
                Seq = new int[PlayerIds.Count];
            }
        }

        /// <summary>JSON structure used when parsing turn submitted by robot.</summary>
        private class RobotTurnRequest {
            public int Seq { get; set; }
            public JsonNode? Turn { get; set; }
        }

        /// <summary>JSON structure used when sending game data to robots.</summary>
        private class RobotDataResponse {
            public int Seq { get; set; }
            public JsonNode GameInfo { get; set; }

            public RobotDataResponse(int seq, JsonNode gameInfo) {
                Seq = seq;
                GameInfo = gameInfo;
            }
        }

        public int NumRobots { get; }
        private IMatchMaker matchMaker;
        private IFactory<IGame> gameFactory;
        private Dictionary<int, Match> matches = new(); // mapping between robot ids and matches
        private int[] totalPoints;

        public Round(int numRobots, IMatchMaker matchMaker, IFactory<IGame> gameFactory) {
            NumRobots = numRobots;
            this.matchMaker = matchMaker;
            this.gameFactory = gameFactory;
            totalPoints = new int[NumRobots];

            for (int i = 0; i < numRobots; i++) {
                matchMaker.AddWaitingRobot(i);
            }
            StartNewMatches();
        }

        public JsonNode? RobotGet(int robotId) {
            AssertRobotId(robotId);
            // if it isn't this robot's turn, return null
            if (!matches.ContainsKey(robotId)) return null;
            Match match = matches[robotId];
            int playerId = match.PlayerIds[robotId];
            if (!match.Game.MayPlay(playerId)) return null;

            JsonNode gameInfo = match.Game.GetGameInfo(playerId);
            RobotDataResponse response = new(match.Seq[playerId], gameInfo);
            return JsonSerializer.SerializeToNode(response);
        }

        public JsonNode? RobotPost(int robotId, JsonNode turnNode) {
            AssertRobotId(robotId);
            if (!matches.ContainsKey(robotId)) {
                return Utility.GetErrorNode("Robot is not in a game.");
            }
            Match match = matches[robotId];
            int playerId = match.PlayerIds[robotId];
            RobotTurnRequest request = JsonSerializer.Deserialize<RobotTurnRequest>(turnNode)!;

            // prevent resubmission of the same turn
            if (request.Seq != match.Seq[playerId]) {
                return Utility.GetErrorNode(
                    $"Wrong sequence number: expected {match.Seq[playerId]}, got {request.Seq}."
                );
            }

            match.Game.PlayTurn(playerId, request.Turn);
            match.Seq[playerId]++;
            string? error = match.Game.GetError(playerId);
            if (match.Game.IsFinished) FinishMatch(match);
            return (error == null) ? null : Utility.GetErrorNode(error);
        }

        private void FinishMatch(Match match) {
            foreach (int robotId in match.PlayerIds.Keys) {
                int playerId = match.PlayerIds[robotId];
                totalPoints[robotId] += match.Game.GetPoints(playerId);
                matches.Remove(robotId);
                matchMaker.AddWaitingRobot(robotId);
            }
            StartNewMatches();
        }

        private void StartNewMatches() {
            int[]? robotsInMatch = matchMaker.GetNextMatch();
            while (robotsInMatch != null) {
                StartNewMatch(robotsInMatch);
                robotsInMatch = matchMaker.GetNextMatch();
            }
        }

        private void StartNewMatch(int[] robotsInMatch) {
                IGame game = gameFactory.Create();
                Dictionary<int, int> playerIds = new();
                for (int i = 0; i < robotsInMatch.Length; i++) {
                    playerIds.Add(robotsInMatch[i], i);
                }
                Match match = new(playerIds, game);
                foreach (int robotId in robotsInMatch) {
                    matches.Add(robotId, match);
                }
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
