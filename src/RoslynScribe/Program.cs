using RoslynScribe.Domain.Models;
using RoslynScribe.Domain.ScribeConsole;
using System.Threading.Tasks;

namespace RoslynScribe
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            return await ScribeConsoleApp.Run(args);
        }

        internal static Task<int> Run(string[] args)
        {
            return ScribeConsoleApp.Run(args);
        }

        internal static Task<ScribeResult> Run()
        {
            return ScribeConsoleApp.RunDefaultAsync();
        }
    }
}
