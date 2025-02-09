using System.Text;

namespace Bypasser
{
    public static class Logger
    {
        public static async Task Log(string message, string outputPath = "")
        {
            Console.WriteLine(message);

            if (!string.IsNullOrEmpty(outputPath))
            {
                await File.AppendAllTextAsync(outputPath, message + Environment.NewLine);
            }
        }
    }
}
