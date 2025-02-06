using CommandLine;

namespace Bypasser
{
    public class Options
    {
        [Option('u', "url", Required = true, HelpText = "403 forbidden URL to test")]
        public string Url { get; set; } = null!;

        [Option('s', "spoof", Required = false, HelpText = "Spoof IP origin headers with known trusted local IP addresses")]
        public bool SpoofIp { get; set; }

        [Option('p', "proxy", Required = false, HelpText = "Use proxy - <ip:port>")]
        public string UseProxy { get; set; } = null!;

        [Option('d', "dry", Required = false, HelpText = "Dry run. Only display generated urls with payloads")]
        public bool DryRun { get; set; }

        [Option('o', "output", Required = false, HelpText = "Output file for the result")]
        public string Output { get; set; }

        [Option('r', "random", Required = false, HelpText = "Random user-agent for each request")]
        public bool RandomUserAgent { get; set; }

        [Option('t', "timeout", Required = false, HelpText = "Timeout between requests in miliseconds.")]
        public int? Timeout { get; set; }
    }
}
