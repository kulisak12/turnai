using System;
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
        public int NumRobots { get; }
        private IMatchMaker matchMaker;
        private IFactory<IGame> gameFactory;

        public Round(int numRobots, IMatchMaker matchMaker, IFactory<IGame> gameFactory) {
            NumRobots = numRobots;
            this.matchMaker = matchMaker;
            this.gameFactory = gameFactory;

            for (int i = 0; i < numRobots; i++) {
                matchMaker.AddWaitingRobot(i);
            }
        }

        public JsonNode? RobotGet(int robotId) {
            throw new NotImplementedException();
        }

        public JsonNode? RobotPost(int robotId, JsonNode turnNode) {
            throw new NotImplementedException();
        }
    }
}
