using System.Net;
using System.Text.Json;

namespace TurnAi {

    public static class Config {
        public static JsonSerializerOptions SerializerOptions = new() {
            PropertyNameCaseInsensitive = true,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        };

        public static JsonDocumentOptions DocumentOptions = new() {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
            MaxDepth = 64,
        };

        public static string Address = "http://localhost:3000/";
    }

}
