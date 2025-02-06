namespace Bypasser
{
    public static class Utility
    {
        private static readonly string[] _userAgents = {

        // 🖥️ Desktop Browsers
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:119.0) Gecko/20100101 Firefox/119.0",
        "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:115.0) Gecko/20100101 Firefox/115.0",

        // 🌍 Microsoft Edge Variants
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36 Edg/119.0.0.0",
    
        // 🔴 Opera Browser
        "Opera/9.80 (Windows NT 10.0; Win64; x64) Presto/2.12.388 Version/12.16",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 OPR/102.0.0.0",
    
        // 📱 Mobile Browsers (Android, iOS)
        "Mozilla/5.0 (Linux; Android 13; Pixel 6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_1 like Mac OS X) AppleWebKit/537.36 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
        "Mozilla/5.0 (iPad; CPU OS 16_5 like Mac OS X) AppleWebKit/537.36 (KHTML, like Gecko) Version/16.5 Mobile/15E148 Safari/604.1",
    
        // 📱 Samsung Browser (Android)
        "Mozilla/5.0 (Linux; Android 12; Samsung Galaxy S21) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/20.0 Chrome/119.0.0.0 Mobile Safari/537.36",
    
        // 🤖 Search Engine Crawlers
        "Googlebot/2.1 (+http://www.google.com/bot.html)",
        "Bingbot/2.0 (+http://www.bing.com/bingbot.htm)",
        "YandexBot/3.0 (+http://yandex.com/bots)",
        "DuckDuckBot/1.1 (+https://duckduckgo.com/duckduckbot)",
        "Baiduspider/2.0 (+http://www.baidu.com/search/spider.html)",
    
        // ⚙️ API Clients & CLI Tools
        "PostmanRuntime/7.31.1",
        "curl/8.0.1",
        "Wget/1.21.3",
        "python-requests/2.28.1",
        "Java/17.0.1",
        "Go-http-client/2.0",
    
        // 🛰️ Web Scrapers & Bots
        "Scrapy/2.6.1 (+https://scrapy.org)",
        "Apache-HttpClient/4.5.13 (Java/11.0.14)",
        "axios/0.26.1",
    
        };

        private static readonly Random _rnd = new Random();

        public static Dictionary<string, string> CommonIpHeaders(string ip)
        {
            var headers = new Dictionary<string, string>()
            {
                { "X-Forwarded-For", ip },
                { "X-Real-IP", ip },
                { "Forwarded", $"for={ip}" },
                { "Client-IP", ip }
            };

            return headers;
        }

        public static Dictionary<string, string> ObscureIpHeaders(string ip)
        {
            var headers = new Dictionary<string, string>()
            {
                { "Cluster-Client-IP", ip },
                { "True-Client-IP", ip },
                { "CF-Connecting-IP", ip }
            };

            return headers;
        }

        public static Dictionary<string, string> AllIpHeaders(string ip)
        {
            var headers = new Dictionary<string, string>()
            {
                { "X-Forwarded-For", ip },
                { "X-Forwarded-Host", ip },
                { "X-Real-IP", ip },
                { "Forwarded", $"for={ip}" },
                { "Client-IP", ip },
                { "Cluster-Client-IP", ip },
                { "True-Client-IP", ip },
                { "CF-Connecting-IP", ip },
                { "Fastly-Client-IP", ip },
                { "True-Source-IP", ip },
                { "Originating-IP", ip },
                { "X-Originating-IP", ip },
                { "Host", ip }
            };

            return headers;
        }

        public static Dictionary<string, string> AdditionalHeaders(string baseUrl)
        {
            var headers = new Dictionary<string, string>()
            {
                { "Referer", baseUrl },
                //{ "Referer", baseUrl + "/admin" },
                { "X-Rewrite-URL", "/admin" },
                { "X-Original-URL", "/admin" }
            };

            return headers;
        }

        public static List<HttpMethod> HttpMethods()
        {
            var methods = new List<HttpMethod>()
            {
                HttpMethod.Options,
                HttpMethod.Head,
                HttpMethod.Trace,
                HttpMethod.Put,
                HttpMethod.Patch,
                HttpMethod.Delete,
                HttpMethod.Post
            };

            return methods;
        }

        public static KeyValuePair<string, string> GetRandomUserAgent()
        {
            var rndIndex = _rnd.Next(_userAgents.Length);

            return new KeyValuePair<string, string>("User-Agent", _userAgents[rndIndex]);
        }
    }
}
