using System.Threading.Tasks;

namespace RoslynScribe.Printer.Mermaid
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var options = new PrintingOptions()
            {
                Title = "Hello World!"
            };

            await Printer.Print(options);
        }
    }
}
