using System;
using System.Text.Json.Nodes;

namespace TurnAi {

    /// <summary>The game played. A new instance is created for each match.</summary>
    public interface IGame {

        /// <summary>For how many players the game is intented.</summary>
        int NumPlayers { get; }

        /// <remarks>
        /// Once the game is finished, no more turns can be played and points are calculated.
        /// </remarks>
        bool IsFinished { get; }

        /// <summary>Take actions provided by the player and play them.</summary>
        /// <param name="turn">
        /// Json provided by the player. Format correctness is checked, errors can be retrieved with
        /// <c>GetError</c>. Can be <c>null</c> if the player didn't provide a turn in time.
        /// If, for any reason, the turn provided is invalid, the default turn is played.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">If player does not exist.</exception>
        /// <exception cref="GameStateException">If game is finished.</exception>
        void PlayTurn(int playerId, JsonNode? turn);

        /// <summary>If it is this player's turn.</summary>
        /// <exception cref="ArgumentOutOfRangeException">If player does not exist.</exception>
        bool MayPlay(int playerId);

        /// <summary>
        /// Get game state to be sent to the player.
        /// This should include all information the player needs to know.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If player does not exist.</exception>
        JsonNode GetGameInfo(int playerId);

        /// <summary>
        /// Get error that occured when turn was parsed or played.
        /// </summary>
        /// <returns><c>null</c> if turn was played successfully.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If player does not exist.</exception>
        string? GetError(int playerId);

        /// <summary>Get the player's score.</summary>
        /// <exception cref="ArgumentOutOfRangeException">If player does not exist.</exception>
        /// <exception cref="GameStateException">If results aren't known yet.</exception>
        int GetPoints(int playerId);
    }

    /// <summary>
    /// Thrown when the action is unsupported in the current state of the game.
    /// </summary>
    public class GameStateException : Exception {
        public GameStateException() { }

        public GameStateException(string? message) : base(message) { }

        public GameStateException(string? message, Exception? innerException)
            : base(message, innerException) { }
    }

}
