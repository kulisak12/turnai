using System.Text.Json.Nodes;

namespace TurnAi {
    class DummyRound : IRound {
        public static readonly DummyRound Instance = new DummyRound();
        public int NumRobots => 0;

        private DummyRound() { }

        public JsonNode? RobotGet(int robotId) => null;

        public JsonNode? RobotPost(int robotId, JsonNode turnNode) {
            return Utility.GetErrorNode("No round is running.");
        }
    }
}
