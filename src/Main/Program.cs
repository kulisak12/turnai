using TurnAi.Games.PrisonersDilemma;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace TurnAi {
    class Program {
        static void Main(string[] args) {
            var robotNames = new string[] { "alice", "bob", "charlie" };
            int numRobots = robotNames.Length;
            Server server = new Server(DummyRound.Instance, robotNames);

            var gameFactory = Factory<PrisonersDilemmaGame>.Instance;
            var matchMaker = new AllPairsMatchMaker(numRobots);
            var round = new Round(numRobots, matchMaker, gameFactory);
            RunRound(server, round).Wait();
        }

        static async Task RunRound(Server server, Round round) {
            CancellationTokenSource cts = new CancellationTokenSource();
            server.SetRound(round);
            var serverTask = Task.Run(() => server.Run(Config.Address, cts.Token));
            await round.RoundFinished;
            cts.Cancel();
            await serverTask;
        }

        static void PrintPoints(Round round) {
            for (int i = 0; i < round.NumRobots; i++) {
                Console.WriteLine($"Robot {i} has {round.GetPoints(i)} points.");
            }
        }
    }
}
