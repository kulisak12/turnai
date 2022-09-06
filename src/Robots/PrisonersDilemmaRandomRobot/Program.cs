using System;
using System.Text.Json;
using System.Text.Json.Nodes;

using TurnAi.Games.PrisonersDilemma;

namespace TurnAi.Robots.PrisonersDilemma.Random {
    class Program {
        static void Main(string[] args) {
            if (args.Length != 1) {
                Console.Error.WriteLine("Required argument: robot name");
                Environment.Exit(1);
            }
            string robotName = args[0];
            Client.PlayRound(robotName, Strategy);
        }

        // always pick a random action
        static JsonNode Strategy(JsonNode gameInfoNode) {
            Actions toPlay = (Actions) System.Random.Shared.Next(0, 2);
            Turn turn = new Turn() {Action = toPlay};
            return JsonSerializer.SerializeToNode(turn, Config.SerializerOptions)!;
        }
    }
}
