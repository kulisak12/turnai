using TurnAi.Games.PrisonersDilemma;

using System;

namespace TurnAi {
    class Program {
        static void Main(string[] args) {
            int numPlayers = 3;
            var gameFactory = Factory<PrisonersDilemmaGame>.Instance;
            var matchMaker = new AllPairsMatchMaker(numPlayers);
            var round = new Round(numPlayers, matchMaker, gameFactory);
            round.OnRoundFinished = () => PrintPoints(round);

            Server server = new Server(round);
            server.Run(Config.Address);
        }

        static void PrintPoints(Round round) {
            for (int i = 0; i < round.NumRobots; i++) {
                Console.WriteLine($"Robot {i} has {round.GetPoints(i)} points.");
            }
        }
    }
}
