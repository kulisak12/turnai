using System.Text.Json;

namespace TurnAi {

    public static class Config {
        public static JsonSerializerOptions Options = new() {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        };
    }

}
