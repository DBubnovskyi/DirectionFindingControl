using BlazorRotatorServer.Models;

namespace BlazorRotatorServer.Processors
{
    public static class Server
    {
        static Server() { 
        
        }

        public static Rotator Rotator { get; set; } = new Rotator();
        public static Serial Serial { get; set; } = new Serial();
    }
}
