using System.IO.Ports;

namespace SerialConnect
{
    public class Serial
    {
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

        /// <summary> Lock object for thread safety. </summary>
        private readonly object _lock = new();
        /// <summary> Task that handles data exchange with Arduino. </summary>
        private Task? _task;
        /// <summary> Default baud rate for serial communication. </summary>
        private const int BAUD_RATE = 115200;
        /// <summary> Current baud rate for the connection. </summary>
        private int _baudRate = BAUD_RATE;
        /// <summary> Current port name for the connection. </summary>
        private string _portName = string.Empty;

        /// <summary> Event for handling received messages. </summary>
        public Action<string>? OnMessageReceived;
        /// <summary> Event for handling connection state changes. </summary>
        public Action<State, string>? OnStateChanged;
        /// <summary> Command to send to the device. </summary>
        public string Command = string.Empty; 
        /// <summary> Gets the current baud rate. </summary> 
        public int BaudRate { get => _baudRate; }
        /// <summary> Gets the current port name. </summary>
        public string PortName { get => _portName; }

        /// <summary> Connect to a serial port by index with an optional baud rate (default is 115200). </summary>
        /// <param name="index"></param> <param name="baudRate"></param>
        public void Connect(int index = 0, int baudRate = BAUD_RATE)
        {
            string[] ports = SerialPort.GetPortNames();
            if (ports?.Length > index && index >= 0)
            {
                _baudRate = baudRate;
                _portName = ports[index];
                EstablishConnection(new SerialPort(ports[index], baudRate));
                return;
            }
            OnStateChanged?.Invoke(State.Error, "Invalid port index.");
        }

        /// <summary> Connect to a serial port by name with an optional baud rate (default is 115200). </summary>
        /// <param name="portName"></param> <param name="baudRate"></param>
        public void Connect(string portName, int baudRate = BAUD_RATE)
        {
            string[] ports = SerialPort.GetPortNames();
            if (ports?.Length > 0 && ports.Contains(portName))
            {
                _baudRate = baudRate;
                _portName = portName;
                EstablishConnection(new SerialPort(PortName, BaudRate));
                return;
            }
            OnStateChanged?.Invoke(State.Error, "Invalid port name.");
        }

        /// <summary> Establishes a connection to the specified serial port. </summary>
        /// <param name="connection"></param>
        private void EstablishConnection(SerialPort? connection)
        {
            if (connection != null)
            {
                _task?.Dispose();
                _task = Task.Run(() =>
                {
                    try
                    {
                        OnStateChanged?.Invoke(State.Connecting, $"Connecting to {connection.PortName}...");
                        connection.Open();
                        OnStateChanged?.Invoke(State.Connected, $"Connected to {connection.PortName}");
                    }
                    catch (Exception e)
                    {
                        OnStateChanged?.Invoke(State.Error, $"Error connecting to {connection.PortName}: {e.Message}");
                    }

                    while (connection.IsOpen)
                    {
                        try
                        {
                            if (connection.BytesToRead > 0)
                            {
                                OnStateChanged?.Invoke(State.Reading, "Reading data...");
                                string line = connection.ReadLine();
                                OnStateChanged?.Invoke(State.Readed, "Data read: " + line);

                                lock (_lock)
                                {
                                    OnMessageReceived?.Invoke(line);
                                }
                            }

                            lock (_lock)
                            {
                                if (!string.IsNullOrEmpty(Command))
                                {
                                    OnStateChanged?.Invoke(State.Writing, "Writing data: " + Command);
                                    connection.WriteLine(Command);
                                    Command = string.Empty;
                                    OnStateChanged?.Invoke(State.Written, "Data written.");
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            OnStateChanged?.Invoke(State.Error, $"Error: {e.Message}");
                        }
                    }
                    OnStateChanged?.Invoke(State.Disconnected, $"Disconnected from {connection.PortName}");
                });
            }
        }
    }
}
