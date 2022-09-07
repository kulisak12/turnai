using TurnAi.Games.PrisonersDilemma;

using System;
using System.Threading;

namespace TurnAi {
    class Program {
        static void Main(string[] args) {
            var robotNames = new string[] { "alice", "bob", "charlie" };
            int numRobots = robotNames.Length;
            var gameFactory = Factory<PrisonersDilemmaGame>.Instance;
            var matchMaker = new AllPairsMatchMaker(numRobots);
            var round = new Round(numRobots, matchMaker, gameFactory);
            round.OnRoundFinished = () => PrintPoints(round);

            CancellationTokenSource cts = new CancellationTokenSource();
            Server server = new Server(round, robotNames);
            server.Run(Config.Address, cts.Token);
        }

        static void PrintPoints(Round round) {
            for (int i = 0; i < round.NumRobots; i++) {
                Console.WriteLine($"Robot {i} has {round.GetPoints(i)} points.");
            }
        }
    }
}
