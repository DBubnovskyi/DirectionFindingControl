using System.Text;

namespace SerialMonitor
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Set console encoding to UTF-8 for Cyrillic support
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            
            Console.Title = "Serial Monitor - Direction Finding Control";
            Console.WriteLine("Starting Serial Monitor...\n");
            
            var wrapper = new SerialWrapper();
            
            // Handle Ctrl+C gracefully
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\nShutting down gracefully...");
                wrapper.Stop();
            };

            try
            {
                await wrapper.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }
    }
}