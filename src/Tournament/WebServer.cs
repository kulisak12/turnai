using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TurnAi {
    public class WebServer {
        private string[] robotNames;
        private List<int[]> points;
        private ReaderWriterLockSlim pointsLock;

        public WebServer(string[] robotNames, List<int[]> points, ReaderWriterLockSlim pointsLock) {
            this.robotNames = robotNames;
            this.points = points;
            this.pointsLock = pointsLock;
        }

        public void Run(string address, CancellationToken token) {
            using var listener = new HttpListener();
            listener.Prefixes.Add(address);
            listener.Start();
            Console.WriteLine("Web server running on " + address);

            while (!token.IsCancellationRequested) {
                HttpListenerContext context = listener.GetContext();
                Task.Run(() => HandleRequest(context));
            }
        }

        private void HandleRequest(HttpListenerContext context) {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            string? path = request.Url?.LocalPath;
            if (path == null) {
                WriteError(response, HttpStatusCode.NotFound);
                return;
            }
            if (request.HttpMethod != "GET") {
                WriteError(response, HttpStatusCode.MethodNotAllowed);
                return;
            }

            HandleGetRequest(request, response, path);
        }

        private void HandleGetRequest(
            HttpListenerRequest request, HttpListenerResponse response, string path
        ) {
            string body = BuildPointsTable();
            string style = "";
            string html = BuildPage("Tournament results", style, body);
            WriteResponse(response, HttpStatusCode.OK, html);
        }

        private string BuildPointsTable() {
            StringBuilder sb = new StringBuilder();
            sb.Append("<table>\n");
            // header
            sb.Append("<tr><th>Round</th>");
            for (int i = 0; i < robotNames.Length; i++) {
                sb.Append("<th>");
                sb.Append(robotNames[i]);
                sb.Append("</th>");
            }
            sb.Append("</tr>\n");

            pointsLock.EnterReadLock();
            try {
                for (int i = 0; i < points.Count; i++) {
                    // one round
                    sb.Append("<tr><td>");
                    sb.Append(i + 1);
                    sb.Append("</td>");
                    for (int j = 0; j < points[i].Length; j++) {
                        sb.Append("<td>");
                        sb.Append(points[i][j]);
                        sb.Append("</td>");
                    }
                    sb.Append("</tr>\n");
                }
            }
            finally {
                pointsLock.ExitReadLock();
            }
            sb.Append("</table>");
            return sb.ToString();
        }

        private string BuildPage(string title, string style, string body) {
            return
$@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>{title}</title>
    <style>
{style}
    </style>
</head>
<body>

{body}

</body>
</html>
";
        }

        private void WriteError(HttpListenerResponse response, HttpStatusCode code) {
            WriteResponse(response, code, null);
        }

        private void WriteResponse(
            HttpListenerResponse response, HttpStatusCode code, string? html
        ) {
            response.StatusCode = (int)code;
            if (html != null) {
                response.Headers.Set("Content-Type", "text/html");
                byte[] buffer = Encoding.UTF8.GetBytes(html);
                response.ContentLength64 = buffer.Length;
                using Stream responseStream = response.OutputStream;
                responseStream.Write(buffer, 0, buffer.Length);
                responseStream.Close();
            }
            response.Close();
        }
    }

}
