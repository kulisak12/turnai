using System;
using System.Collections.Generic;

namespace TurnAi {

    /// <summary>Algorithm for matching players in a tournament.</summary>
    public interface IMatchMaker {

        /// <summary>If all matches have been played.</summary>
        bool IsFinished { get; }

        /// <summary>
        /// Add robot that may be placed in a new match.
        /// No robots are waiting when the match maker is created, they must be added.
        /// </summary>
        void AddWaitingRobot(int robotId);

        /// <summary>Get the next match as list of robots.</summary>
        /// <returns><c>null</c> if no match is available.</returns>
        int[]? GetNextMatch();
    }

    /// <summary>Ensure that each robot plays against each other robot once.</summary>
    public class AllPairsMatchMaker : IMatchMaker {
        private int numRobots;
        private HashSet<int> waitingRobots = new();
        // for each robot, list of robots that is has not played against yet
        private List<int>[] unplayedMatches;
        // planned matches
        private Queue<int[]> nextMatches = new();

        public bool IsFinished {
            get => waitingRobots.Count == numRobots && nextMatches.Count == 0;
        }

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
    }

    /// <summary>
    /// Ensure that each robot plays against each other robot twice.
    /// In the second match, the robots are switched.
    /// </summary>
    public class AllOrderedPairsMatchMaker : IMatchMaker {
        private IMatchMaker unorderedMatchMaker;
        // planned matches
        private Queue<int[]> nextMatches = new();
        private int?[] nextOpponents;
        private HashSet<int> waitingForSecondMatch = new();

        public AllOrderedPairsMatchMaker(int numRobots) {
            unorderedMatchMaker = new AllPairsMatchMaker(numRobots);
            nextOpponents = new int?[numRobots];
        }

        public bool IsFinished => nextMatches.Count == 0 && unorderedMatchMaker.IsFinished;

        public void AddWaitingRobot(int robotId) {
            int? opponentId = nextOpponents[robotId];
            // find an entirely new pair
            if (opponentId == null) {
                unorderedMatchMaker.AddWaitingRobot(robotId);
                return;
            }
            // add the other match to queue, if possible
            waitingForSecondMatch.Add(robotId);
            if (waitingForSecondMatch.Contains(opponentId.Value)) {
                EnqueueSecondMatch(robotId, opponentId.Value);
            }
        }

        public int[]? GetNextMatch() {
            // first take from local queue
            if (nextMatches.Count > 0) return nextMatches.Dequeue();

            int[]? match = unorderedMatchMaker.GetNextMatch();
            if (match == null) return null;

            // plan the second match
            nextOpponents[match[0]] = match[1];
            nextOpponents[match[1]] = match[0];
            // return the first match in ascending order
            if (match[0] < match[1]) return match;
            return new int[] { match[1], match[0] };
        }

        private void EnqueueSecondMatch(int robotId, int opponentId) {
            nextOpponents[robotId] = null;
            nextOpponents[opponentId] = null;
            waitingForSecondMatch.Remove(robotId);
            waitingForSecondMatch.Remove(opponentId);
            // add the match in descending order
            if (robotId > opponentId) {
                nextMatches.Enqueue(new int[] { robotId, opponentId });
            } else {
                nextMatches.Enqueue(new int[] { opponentId, robotId });
            }
        }
    }
}
