using System;
using System.Threading;

namespace SpectrozirRotatorImitator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var stopEvent = new ManualResetEvent(false);
            var server = new RotatorServer();

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                stopEvent.Set();
            };

            server.Start();
            stopEvent.WaitOne();
            server.Stop();
        }
    }
}
