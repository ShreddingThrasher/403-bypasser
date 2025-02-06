namespace Bypasser.RequestHelpers
{
    public class CustomHttpReponse
    {
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; } = null!;
        public Dictionary<string, string> Headers { get; set; } = new();
        public string Body { get; set; } = null!;

        /// <summary>
        /// Parse raw response text into CustomHttpResponse
        /// </summary>
        /// <param name="rawResponse">Raw http response text</param>
        /// <returns></returns>
        public static CustomHttpReponse Parse(string rawResponse)
        {
            var response = new CustomHttpReponse();
            var lines = rawResponse.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            int index = 0;

            // Parse status line
            var statusLine = lines[index++].Split(' ', 3);
            response.StatusCode = int.Parse(statusLine[1]);
            response.StatusMessage = statusLine[2];

            // Parse headers
            while (index < lines.Length && !string.IsNullOrWhiteSpace(lines[index]))
            {
                var headerParts = lines[index].Split(new[] { ": " }, 2, StringSplitOptions.None);
                if (headerParts.Length == 2)
                {
                    response.Headers[headerParts[0]] = headerParts[1];
                }
                index++;
            }

            // Skip empty line between headers and body
            index++;

            // Parse body
            response.Body = string.Join("\n", lines.Skip(index));

            return response;
        }
    }
}
