using Xunit;

namespace TurnAi.TournamentTests {
    public class AllPairsMatchMakerTests {

        [Theory]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void TotalNumberOfMatches(int numRobots) {
            int expectedNumMatches = numRobots * (numRobots - 1) / 2;
            var matchMaker = new AllPairsMatchMaker(numRobots);
            for (int i = 0; i < numRobots; i++) {
                matchMaker.AddWaitingRobot(i);
            }

            int numMatches = 0;
            while (!matchMaker.IsFinished) {
                var match = matchMaker.GetNextMatch();
                numMatches++;
                Assert.NotNull(match);
                foreach (int robotId in match!) {
                    matchMaker.AddWaitingRobot(robotId);
                }
            }
            Assert.Equal(expectedNumMatches, numMatches);
        }

        [Theory]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void NumberOfMatchesForOnePlayer(int numRobots) {
            int expectedNumMatches = numRobots - 1;
            int trackedRobotId = 2;
            var matchMaker = new AllPairsMatchMaker(numRobots);
            for (int i = 0; i < numRobots; i++) {
                matchMaker.AddWaitingRobot(i);
            }

            int numMatches = 0;
            while (!matchMaker.IsFinished) {
                var match = matchMaker.GetNextMatch();
                foreach (int robotId in match!) {
                    if (robotId == trackedRobotId) numMatches++;
                    matchMaker.AddWaitingRobot(robotId);
                }
            }
            Assert.Equal(expectedNumMatches, numMatches);
        }

        [Fact]
        public void NoAvailableMatch() {
            var matchMaker = new AllPairsMatchMaker(2);
            matchMaker.AddWaitingRobot(0);
            matchMaker.AddWaitingRobot(1);
            Assert.NotNull(matchMaker.GetNextMatch());
            Assert.Null(matchMaker.GetNextMatch());
        }
    }
}
