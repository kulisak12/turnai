using System;
using System.Text.Json;
using System.Text.Json.Nodes;

using TurnAi.Games.PrisonersDilemma;

namespace TurnAi.Robots.PrisonersDilemma.Mirror {
    class Program {
        static void Main(string[] args) {
            if (args.Length != 1) {
                Console.Error.WriteLine("Required argument: robot name");
                Environment.Exit(1);
            }
            string robotName = args[0];
            Client.PlayRound(robotName, Strategy);
        }

        // mirror opponent's last move
        // if it is the first round, stay silent
        static JsonNode Strategy(JsonNode gameInfoNode) {
            var gameInfo = JsonSerializer.Deserialize<GameInfo>(
                gameInfoNode, Config.SerializerOptions
            );
            var opponentsActions = gameInfo!.OpponentsActions;
            int numActions = opponentsActions.Count;
            Actions toPlay = numActions == 0 ? Actions.Silent : opponentsActions[numActions - 1];
            Turn turn = new Turn() {Action = toPlay};
            return JsonSerializer.SerializeToNode(turn, Config.SerializerOptions)!;
        }
    }
}
