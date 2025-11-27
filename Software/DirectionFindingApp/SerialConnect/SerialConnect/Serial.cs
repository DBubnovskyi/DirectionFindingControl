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

        /// <summary> Lock object for thread safety. </summary>
        private readonly object _lock = new();
        /// <summary> Task that handles data exchange with Arduino. </summary>
        private Task? _task;
        /// <summary> Cancellation token source for task management. </summary>
        private CancellationTokenSource? _cancellationTokenSource;
        /// <summary> Default baud rate for serial communication. </summary>
        private const int BAUD_RATE = (int)BaudRates.BR_115200;
        /// <summary> Current baud rate for the connection. </summary>
        private int _baudRate = BAUD_RATE;
        /// <summary> Current port name for the connection. </summary>
        private string _portName = string.Empty;
        private string[]? _ports;

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
        /// <summary> Gets the list of available serial ports. </summary>
        public string[] AvailablePorts { 
            get {
                _ports = SerialPort.GetPortNames();
                return _ports;
            } 
        }

        /// <summary> Gets the list of available serial ports as a static method. </summary>
        public static string[] GetAvailablePorts() => SerialPort.GetPortNames();

        /// <summary> Validates if the received message contains valid ASCII characters </summary>
        private bool IsValidMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return false;

            // Check if message contains only printable ASCII characters
            foreach (char c in message)
            {
                if (c < 32 || c > 126) // Outside printable ASCII range
                {
                    // Allow some control characters like tab, but not others
                    if (c != '\t' && c != '\r' && c != '\n')
                        return false;
                }
            }

            // Check if message looks like our expected format
            return message.Contains("AZ,") || message.Contains("AN,") || message.Contains(";");
        }

        /// <summary> Disconnect from the current serial port. </summary>
        public void Disconnect()
        {
            try
            {
                // Cancel the task first
                _cancellationTokenSource?.Cancel();
                
                // Wait for task to complete or timeout
                if (_task != null && !_task.IsCompleted)
                {
                    _task.Wait(TimeSpan.FromMilliseconds(1000)); // Wait max 1 second
                }
                
                // Dispose resources
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _task = null;
                
                OnStateChanged?.Invoke(State.Disconnected, $"Manually disconnected from {_portName}");
            }
            catch (Exception ex)
            {
                OnStateChanged?.Invoke(State.Error, $"Error during disconnect: {ex.Message}");
            }
        }

        /// <summary> Connect to a serial port by index with an optional baud rate (default is 115200). </summary>
        /// <param name="index"></param> <param name="baudRate"></param>
        public void Connect(int index = 0, int baudRate = BAUD_RATE)
        {
            if (_ports == null || _ports.Length == 0)
            {
                _ports = SerialPort.GetPortNames();
            }

            if (_ports?.Length > index && index >= 0)
            {
                _baudRate = baudRate;
                _portName = _ports[index];
                EstablishConnection(new SerialPort(_ports[index], baudRate));
                return;
            }
            OnStateChanged?.Invoke(State.Error, "Invalid port index.");
        }

        /// <summary> Connect to a serial port by name with an optional baud rate (default is 115200). </summary>
        /// <param name="portName"></param> <param name="baudRate"></param>
        public void Connect(string portName, int baudRate = BAUD_RATE)
        {
            if (_ports == null || _ports.Length == 0)
            {
                _ports = SerialPort.GetPortNames();
            }

            if (_ports?.Length > 0 && _ports.Contains(portName))
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
                // Configure serial port settings
                connection.Encoding = System.Text.Encoding.ASCII; // Use ASCII instead of UTF-8
                connection.NewLine = "\n"; // Set newline character
                connection.ReadTimeout = 1000;
                connection.WriteTimeout = 1000;
                
                // Cancel previous task if exists
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                
                // Create new cancellation token
                _cancellationTokenSource = new CancellationTokenSource();
                var token = _cancellationTokenSource.Token;
                
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

                    while (connection.IsOpen && !token.IsCancellationRequested)
                    {
                        try
                        {
                            // Check for cancellation
                            token.ThrowIfCancellationRequested();
                            
                            if (connection.BytesToRead > 0)
                            {
                                try
                                {
                                    OnStateChanged?.Invoke(State.Reading, "Reading data...");
                                    string line = connection.ReadLine();
                                    
                                    // Clean up the received line
                                    line = line.Trim('\r', '\n', '\0');
                                    
                                    // Only process if line is not empty and contains valid characters
                                    if (!string.IsNullOrWhiteSpace(line) && IsValidMessage(line))
                                    {
                                        OnStateChanged?.Invoke(State.Readed, "Data read: " + line);
                                        
                                        lock (_lock)
                                        {
                                            OnMessageReceived?.Invoke(line);
                                        }
                                    }
                                }
                                catch (TimeoutException)
                                {
                                    // Ignore timeout exceptions during reading
                                }
                                catch (Exception readEx)
                                {
                                    OnStateChanged?.Invoke(State.Error, $"Read error: {readEx.Message}");
                                }
                            }

                            lock (_lock)
                            {
                                if (!string.IsNullOrEmpty(Command))
                                {
                                    try
                                    {
                                        OnStateChanged?.Invoke(State.Writing, "Writing data: " + Command);
                                        connection.WriteLine(Command);
                                        Command = string.Empty;
                                        OnStateChanged?.Invoke(State.Written, "Data written.");
                                    }
                                    catch (Exception writeEx)
                                    {
                                        OnStateChanged?.Invoke(State.Error, $"Write error: {writeEx.Message}");
                                        Command = string.Empty;
                                    }
                                }
                            }
                            
                            Thread.Sleep(10);
                        }
                        catch (OperationCanceledException)
                        {
                            // Task was cancelled, exit gracefully
                            break;
                        }
                        catch (Exception e)
                        {
                            if (e is UnauthorizedAccessException || 
                                e.Message.Contains("port does not exist") ||
                                e.Message.Contains("Access is denied"))
                            {
                                OnStateChanged?.Invoke(State.Error, $"Critical error: {e.Message}");
                                try { connection.Close(); } catch { }
                                break;
                            }
                            else
                            {
                                OnStateChanged?.Invoke(State.Error, $"Communication error: {e.Message}");
                                if (!token.IsCancellationRequested)
                                {
                                    Thread.Sleep(100);
                                }
                            }
                        }
                    }
                    
                    // Close connection if still open
                    try { connection.Close(); } catch { }
                    
                    // Only send disconnected event if not cancelled manually
                    if (!token.IsCancellationRequested)
                    {
                        OnStateChanged?.Invoke(State.Disconnected, $"Disconnected from {connection.PortName}");
                    }
                }, token);
            }
        }
    }
}
