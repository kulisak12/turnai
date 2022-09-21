using TurnAi.Games.Tictactoe;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TurnAi {
    class Program {
        static void Main(string[] args) {
            var robotNames = new string[] { "alice", "bob" };
            int numRobots = robotNames.Length;
            List<int[]> points = new List<int[]>();
            var pointsLock = new ReaderWriterLockSlim();
            var server = new Server(DummyRound.Instance, robotNames);
            var webServer = new WebServer(robotNames, points, pointsLock);
            int numRounds = 2;
            TimeSpan timeBetweenRounds = TimeSpan.FromMinutes(0.5);
            var gameFactory = new TictactoeFactory(15);

            var webServerTask = Task.Run(
                () => webServer.Run(Config.WebAddress, CancellationToken.None)
            );
            for (int i = 0; i < numRounds; i++) {
                if (i > 0) Task.Delay(timeBetweenRounds).Wait();
                Console.WriteLine($"Starting round {i + 1} of {numRounds}.");

                var matchMaker = new AllPairsMatchMaker(numRobots);
                var round = new Round(numRobots, matchMaker, gameFactory);
                RunRound(server, round).Wait();
                var roundPoints = GetRoundPoints(round);

                pointsLock.EnterWriteLock();
                try {
                    points.Add(roundPoints);
                } finally {
                    pointsLock.ExitWriteLock();
                }
                Console.WriteLine($"Ending round {i + 1}.");
            }
            webServerTask.Wait(); // leave web server running, stop with Ctrl+C
        }

        static async Task RunRound(Server server, Round round) {
            CancellationTokenSource cts = new CancellationTokenSource();
            server.SetRound(round);
            var serverTask = Task.Run(() => server.Run(Config.ApiAddress, cts.Token));
            await round.RoundFinished;
            cts.Cancel();
            await serverTask;
        }

        static int[] GetRoundPoints(Round round) {
            int[] points = new int[round.NumRobots];
            for (int i = 0; i < round.NumRobots; i++) {
                points[i] = round.GetPoints(i);
            }
            return points;
        }
    }
}
