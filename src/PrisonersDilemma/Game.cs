using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TurnAi.PrisonnersDilemma {

    class PrisonnersDilemma : IGame {
        private enum Actions {
            Silent = 0,
            Betray = 1
        }

        /// <summary>Template for deserializing Json provided by the player.</summary>
        /// <remarks>All fields are nullable to distinguish missing values.</remarks>
        private class Turn {
            public Actions? Action { get; set; }
        }

        /// <summary>Template for serializing Json to be sent to the player.</summary>
        private class GameInfo {
            public int TurnsLeft { get; set; }
            public List<Actions>? YourActions { get; set; }
            public List<Actions>? OpponentActions { get; set; }
        }

        // first index = my action, second index = opponent's action
        private static readonly int[,] payoffs = new int[,] {
            {3, 0},
            {4, 1}
        };

        private static readonly Actions defaultAction = Actions.Silent;
        private static readonly int numTotalTurns = 10;
        private int turnsPlayed = 0;
        private int[] points = new int[NumPlayers];
        private List<Actions>[] history = new List<Actions>[NumPlayers];
        private string?[] errors = new string?[NumPlayers];

        public static int NumPlayers { get => 2; }
        int IGame.NumPlayers => NumPlayers;
        public bool IsFinished { get; private set; } = false;

        public void PlayTurn(int playerId, JsonNode? turn) {
            AssertPlayerId(playerId);
            AssertNotFinished();
            (Actions action, string? error) = ParseTurn(turn);
            errors[playerId] = error; // stores null if no error
            history[playerId].Add(action);
        }

        public bool MayPlay(int playerId) {
            throw new System.NotImplementedException();
        }

        public JsonNode GetGameInfo(int playerId) {
            throw new System.NotImplementedException();
        }

        public string? GetError(int playerId) {
            AssertPlayerId(playerId);
            return errors[playerId];
        }

        public int GetPoints(int playerId) {
            AssertPlayerId(playerId);
            if (!IsFinished) throw new GameStateException("Game is not finished yet.");
            return points[playerId];
        }

        /// <returns>Parsed action and error message.</returns>
        private ValueTuple<Actions, string?> ParseTurn(JsonNode? turn) {
            if (turn == null) {
                return (defaultAction, "No turn provided.");
            }
            var deserialized = JsonSerializer.Deserialize<Turn>(turn, Config.Options);
            var action = deserialized!.Action;
            if (action == null) {
                return (defaultAction, "No action provided.");
            }
            return (action.Value, null);
        }

        private void UpdatePoints(Actions[] actions) {
            points[0] += payoffs[(int)actions[0], (int)actions[1]];
            points[1] += payoffs[(int)actions[1], (int)actions[0]];
        }

        // throw an exception if playerId is invalid
        private void AssertPlayerId(int playerId) {
            if (playerId < 0 || playerId >= NumPlayers) {
                throw new ArgumentOutOfRangeException(
                    nameof(playerId),
                    playerId,
                    $"Player {playerId} does not exist."
                );
            }
        }

        // throw an exception if game is finished
        private void AssertNotFinished() {
            if (IsFinished) throw new GameStateException("Game is finished.");
        }
    }

}
