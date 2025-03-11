using Bypasser.RequestHelpers;
using CommandLine;
using System.Net;

namespace Bypasser
{
    public class Program
    {
        static bool randomAgent = false;
        static int _timeout = 0;
        static string _outputPath = "";

        static bool success = false;
        static Dictionary<string, string> successRequests = new Dictionary<string, string>();

        static async Task Main(string[] args)
        {
            args = ["-u", "https://connect.oppo.com/js/rem.js", "-s", "-r" ];

            PrintBanner();

            var parseResult = Parser.Default.ParseArguments<Options>(args);

            await parseResult.WithParsedAsync(async options =>
            {
                var uri = new Uri(options.Url);

                if (!string.IsNullOrEmpty(options.Output))
                {
                    _outputPath = options.Output;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                await Logger.Log("Generating payloads...", _outputPath);

                var payloads = new List<string>();

                //if (string.IsNullOrEmpty(options.Payloads))
                //{
                //    payloads.AddRange(PayloadGenerator.PathTraversal(uri.PathAndQuery));
                //    payloads.AddRange(PayloadGenerator.WordCase(uri.PathAndQuery));
                //    payloads.AddRange(PayloadGenerator.Encode(uri.PathAndQuery));
                //}
                //else
                //{
                //    payloads.AddRange(await PayloadGenerator.CustomPayload(options.Payloads, uri.PathAndQuery));
                //}

                if (options.DryRun)
                {
                    await Logger.Log($"Generated {payloads.Count} payloads", _outputPath);
                    payloads.ForEach(async v => await Logger.Log($"    {v}", _outputPath));
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

                await Logger.Log($"Probing {uri.AbsoluteUri} with payloads...", _outputPath);

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

                    await Logger.Log(Environment.NewLine, _outputPath);

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

                    await Logger.Log($"Status: {statusMessage}", _outputPath);

                    foreach (var item in successRequests)
                    {
                        await Logger.Log(item.Key, _outputPath);
                        await Logger.Log(item.Value, _outputPath);
                        await Logger.Log(Environment.NewLine, _outputPath);
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

                    await Logger.Log($"    {displayHost}{payload} {response.StatusCode} {response.StatusMessage}", _outputPath);
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
                    await Logger.Log($"{payload} - {e.Message}", _outputPath);
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

                    await Logger.Log($"    {url} {(int)res.StatusCode} {res.StatusCode} ({header.Key}: {header.Value})", _outputPath);
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

                await Logger.Log($"    {url} {(int)res.StatusCode} {res.StatusCode} ({header.Key}: {header.Value})", _outputPath);
                Console.ResetColor();

                if (res.StatusCode == HttpStatusCode.OK)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    await Logger.Log($"Successfully bypassed with: {header.Key}: {header.Value}", _outputPath);
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

                await Logger.Log($"    {url} {(int)res.StatusCode} {res.StatusCode} ({method})", _outputPath);
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
            Console.WriteLine("|                      403 Bypasser                      |");
            Console.WriteLine("|                                                        |");
            Console.WriteLine("==========================================================");
            Console.WriteLine();
        }
    }
}