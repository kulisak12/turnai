using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace TurnAi {

    public class Client {
        /// <summary>Function which takes game data and returns a turn to play.</summary>
        public delegate JsonNode Strategy(JsonNode gameInfo);
        private static readonly HttpClient client = new HttpClient();

        /// <summary>
        /// Continuously get game data and send back turns until the round is over.
        /// </summary>
        public static void PlayRound(string robotName, Strategy strategy) {
            var url = Config.ApiAddress + robotName;

            // run until interrupted with Ctrl+C
            while (true) {
                var getResult = Get(url).Result;
                if (getResult == null) {
                    // no game info yet
                    Task.Delay(500).Wait();
                    continue;
                }

                int seq = getResult.Seq;
                var gameInfo = getResult.GameInfo;
                var turn = strategy(gameInfo);
                var turnRequest = new RobotTurnRequest() {Seq = seq, Turn = turn};
                string? postError = Post(url, turnRequest).Result;
                if (postError == null) continue;
                Console.WriteLine("Tournament error: " + postError);
            }
        }

        /// <summary>Get game info.</summary>
        public static async Task<RobotDataResponse?> Get(string url) {
            try {
                string responseBody = await client.GetStringAsync(url);
                var response = JsonSerializer.Deserialize<RobotDataResponse>(
                    responseBody, Config.SerializerOptions
                );
                // if no game data is available, the response will be empty JSON object
                // deserialzing it will set nullable objects, such as GameInfo, to null
                if (response!.GameInfo == null) return null;
                return response;
            }
            catch (HttpRequestException ex) {
                Console.WriteLine("Get exception: " + ex.Message);
                return null;
            }
        }

        /// <summary>Send turn.</summary>
        /// <returns>
        /// Error which occured while parsing the turn, or <c>null</c> on success.
        /// </returns>
        public static async Task<string?> Post(string url, RobotTurnRequest request) {
            try {
                var content = new StringContent(
                    JsonSerializer.Serialize(request, Config.SerializerOptions)
                );
                var response = await client.PostAsync(url, content);
                if (response.IsSuccessStatusCode) return null; // no error occured
                // get error from response body
                string errorJson = await response.Content.ReadAsStringAsync();
                var postResult = JsonSerializer.Deserialize<RobotErrorResponse>(
                    errorJson, Config.SerializerOptions
                );
                return postResult!.Error;
            }
            catch (HttpRequestException ex) {
                return "Post exception: " + ex.Message;
            }
        }
    }
}
