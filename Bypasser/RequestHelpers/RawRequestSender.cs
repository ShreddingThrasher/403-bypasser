using System.Net.Sockets;
using System.Text;

namespace Bypasser.RequestHelpers
{
    public class RawRequestSender
    {
        public static async Task<string> SendAsync(string targetHost, int port, string payload, Dictionary<string, string>? headers = null)
        {
            try
            {
                using TcpClient client = new TcpClient(targetHost, port);
                using NetworkStream stream = client.GetStream();

                // Build raw HTTP request
                string rawRequest = BuildRawRequest(targetHost, payload, headers);

                byte[] requestData = Encoding.ASCII.GetBytes(rawRequest);
                await stream.WriteAsync(requestData, 0, requestData.Length);

                // Read response
                byte[] buffer = new byte[4096];
                int bytesRead;
                StringBuilder responseBuilder = new StringBuilder();

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    responseBuilder.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
                }

                return responseBuilder.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending request for {payload}: {ex.Message}");
                return string.Empty;
            }
        }

        private static string BuildRawRequest(string targetHost, string payload, Dictionary<string, string>? headers)
        {
            // Build raw HTTP request
            string rawRequest = $"GET {payload} HTTP/1.1\r\n" +
                                $"Host: {targetHost}\r\n";

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    rawRequest += $"{header.Key}: {header.Value}\r\n";
                }
            }

            rawRequest += $"Connection: close\r\n\r\n";

            return rawRequest;
        }
    }
}
