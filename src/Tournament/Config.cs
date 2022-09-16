using System;
using System.Text.Json;

namespace TurnAi {

    public static class Config {
        public static JsonSerializerOptions SerializerOptions = new() {
            // parsing
            AllowTrailingCommas = true,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            // PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            // writing
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

        public static string Address = "http://localhost:3000/";
        public static TimeSpan ResponseLimit = TimeSpan.FromSeconds(3);
    }

}
