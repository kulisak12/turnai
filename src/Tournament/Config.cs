using System;
using System.Text.Json;

namespace TurnAi {

    public static class Config {
        public static readonly JsonSerializerOptions SerializerOptions = new() {
            // parsing
            AllowTrailingCommas = true,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            // PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            // writing
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

        public static readonly string ApiAddress = "http://localhost:3000/";
        public static readonly string WebAddress = "http://localhost:4000/";
        public static readonly TimeSpan ResponseLimit = TimeSpan.FromSeconds(3);
    }

}
