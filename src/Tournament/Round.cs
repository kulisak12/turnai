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
        string? RobotPost(int robotId, JsonNode turnNode);
    }
}
