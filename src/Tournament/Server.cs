using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TurnAi {
    public class Server {
        private Dictionary<string, int> robotIds = new() {
            { "alice", 1 },
            { "bob", 2 },
        };
        private IRound round;

        public Server(IRound round) {
            this.round = round;
        }

        public void SetRound(IRound round) => this.round = round;

        public void Run(string address) {
            using var listener = new HttpListener();
            listener.Prefixes.Add(address);
            listener.Start();
            Console.WriteLine("Listening on " + address);

            while (true) {
                HttpListenerContext context = listener.GetContext(); // TODO async
                HandleRequest(context);
            }
        }

        private void HandleRequest(HttpListenerContext context) {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // translate robot name to id
            string? robotName = request.Url?.LocalPath;
            if (robotName == null || !robotIds.ContainsKey(robotName)) {
                WriteError(response, HttpStatusCode.NotFound, "Robot not found");
                return;
            }
            int robotId = robotIds[robotName];

            // get request
            if (request.HttpMethod == "GET") {
                JsonNode? responseNode = round.RobotGet(robotId);
                WriteResponse(response, HttpStatusCode.OK, responseNode);
                return;
            }

            // post request
            if (request.HttpMethod == "POST") {
                HandlePostRequest(request, response, robotId);
                return;
            }

            // ignore other requests
            WriteError(response, HttpStatusCode.MethodNotAllowed, "Method not allowed");
        }

        private void HandlePostRequest(
            HttpListenerRequest request, HttpListenerResponse response, int robotId
        ) {
            using Stream requestStream = request.InputStream;
            JsonNode? requestNode = null;
            try {
                using StreamReader reader = new(requestStream);
                string requestString = reader.ReadToEnd();
                requestNode = JsonNode.Parse(requestString, null, Config.DocumentOptions);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is IOException) {
                WriteError(response, HttpStatusCode.BadRequest, "Error reading request");
            }
            catch (JsonException) {
                WriteError(response, HttpStatusCode.BadRequest, "Invalid JSON");
            }

            if (requestNode == null) return;
            JsonNode? error = round.RobotPost(robotId, requestNode);
            var code = (error == null) ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            WriteResponse(response, code, error);
        }

        private void WriteError(
            HttpListenerResponse response, HttpStatusCode code, string message
        ) {
            WriteResponse(response, code, Utility.GetErrorNode(message));
        }

        private void WriteResponse(
            HttpListenerResponse response, HttpStatusCode code, JsonNode? data
        ) {
            response.StatusCode = (int)code;
            response.Headers.Set("Content-Type", "application/json");
            string json = data?.ToJsonString(Config.SerializerOptions) ?? "{}";
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            using Stream responseStream = response.OutputStream;
            responseStream.Write(buffer, 0, buffer.Length);
            responseStream.Close();
        }
    }

}
