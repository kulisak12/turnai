using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TurnAi {

    /// <summary>All-purpose factory.</summary>
    public interface IFactory<out T> {
        T Create();
    }

    /// <remarks>Follows singleton pattern, use the <c>Instance</c> field.</remarks>
    public class Factory<T> : IFactory<T> where T : new() {
        public static readonly IFactory<T> Instance = new Factory<T>();
        private Factory() { }
        public T Create() => new T();
    }

    public static class Utility {
        public static Action Noop = () => { };

        public static JsonNode GetErrorNode(string message) {
            return JsonSerializer.SerializeToNode(
                new RobotErrorResponse(message), Config.SerializerOptions
            )!;
        }

        public static int IntPow(int x, int exp) {
            int result = 1;
            for (int i = 0; i < exp; i++) {
                result *= x;
            }
            return result;
        }
    }
}
