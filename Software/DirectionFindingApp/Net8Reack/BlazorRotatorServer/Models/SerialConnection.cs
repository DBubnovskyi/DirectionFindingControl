namespace BlazorRotatorServer.Models
{
    public class SerialConnection
    {
        public bool IsConnected { get; set; }
        public State State { get; set; }
        public BaudRates BaudRate { get; set; } = BaudRates.BR_115200;
        public string? PortName { get; set; }
        public List<string> PortLog { get; set; } = [];
    }

    /// <summary> Serial connection states </summary>
    public enum State
    {
        /// <summary> The connection is not established. </summary> 
        Disconnected,
        /// <summary> The connection is in the process of being established. </summary>
        Connecting,
        /// <summary> The connection is established. </summary>
        Connected,
        /// <summary> The connection is reading data. </summary>
        Reading,
        /// <summary> The data has been read. </summary>
        Readed,
        /// <summary> The connection is writing data. </summary>
        Writing,
        /// <summary> The data has been written. </summary>
        Written,
        /// <summary> An error has occurred. </summary>
        Error
    }

    /// <summary> Standard baud rates for serial communication. </summary>
    public enum BaudRates
    {
        BR_9600 = 9600,
        BR_14400 = 14400,
        BR_19200 = 19200,
        BR_38400 = 38400,
        BR_57600 = 57600,
        BR_115200 = 115200,
        BR_128000 = 128000,
        BR_256000 = 256000,
        BR_512000 = 512000,
        BR_921600 = 921600
    }
}
