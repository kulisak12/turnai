using System;
using System.Collections.Generic;

namespace TurnAi {

    /// <summary>Algorithm for matching players in a tournament.</summary>
    public interface IMatchMaker {

        /// <summary>Add robot that may be placed in a new match.</summary>
        void AddWaitingRobot(int robotId);

        /// <summary>Get the next match as list of robots.</summary>
        /// <returns><c>null</c> if no match is available.</returns>
        int[]? GetNextMatch();

        /// <summary>If all matches have been played.</summary>
        bool IsFinished();
    }

    /// <summary>Ensure that each robot plays against each other robot once.</summary>
    public class AllPairsMatchMaker : IMatchMaker {
        private int numRobots;
        private HashSet<int> waitingRobots = new();
        // for each robot, list of robots that is has not played against yet
        private List<int>[] unplayedMatches;
        // planned matches
        private Queue<int[]> nextMatches = new();

        public AllPairsMatchMaker(int numRobots) {
            this.numRobots = numRobots;
            unplayedMatches = new List<int>[numRobots];

            // initialize all pairs of robots
            for (int i = 0; i < numRobots; i++) {
                unplayedMatches[i] = new List<int>(numRobots - 1);
                for (int j = 0; j < numRobots; j++) {
                    if (i != j) {
                        unplayedMatches[i].Add(j);
                    }
                }
            }
        }

        public void AddWaitingRobot(int robotId) {
            waitingRobots.Add(robotId);
            // invariant: no matches are available between waiting robots
            // it suffices to plan matches for the new robot only
            foreach (int opponentId in unplayedMatches[robotId]) {
                if (waitingRobots.Contains(opponentId)) {
                    // plan the match immediately
                    PlanMatch(robotId, opponentId);
                    return;
                }
            }
        }

        private void PlanMatch(int robotId, int opponentId) {
            unplayedMatches[robotId].Remove(opponentId);
            unplayedMatches[opponentId].Remove(robotId);
            waitingRobots.Remove(robotId);
            waitingRobots.Remove(opponentId);
            nextMatches.Enqueue(new int[] { robotId, opponentId });
        }

        public int[]? GetNextMatch() {
            if (nextMatches.Count == 0) return null;
            return nextMatches.Dequeue();
        }

        public bool IsFinished() {
            return waitingRobots.Count == 0 && nextMatches.Count == numRobots;
        }
    }
}
