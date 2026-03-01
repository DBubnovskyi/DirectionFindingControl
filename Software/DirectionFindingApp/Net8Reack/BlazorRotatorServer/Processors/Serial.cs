using System.IO.Ports;
using System.Management;
using System.Text.RegularExpressions;
using BlazorRotatorServer.Models;

namespace BlazorRotatorServer.Processors
{
    public class Serial
    {
        public Serial()
        {
            OnMessageReceived += (message) =>
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    Connection.PortLog.Add($"[{DateTime.Now:HH:mm:ss.fff}] RX: {message}");
                    if (Connection.PortLog.Count > 100)
                    {
                        Connection.PortLog.RemoveAt(0);
                    }
                }
            };
            OnStateChanged += (state, message) =>
            {
                Connection.State = state;
                if (!string.IsNullOrWhiteSpace(message))
                {
                    Connection.PortLog.Add($"[{DateTime.Now:HH:mm:ss.fff}] [{state}] {message}");
                    if (Connection.PortLog.Count > 100)
                    {
                        Connection.PortLog.RemoveAt(0);
                    }
                }
            };
        }

        public SerialConnection Connection { get; set; } = new SerialConnection();

        /// <summary> Lock object for thread safety. </summary>
        private readonly object _lock = new();
        /// <summary>  A compiled, case-insensitive regular expression that matches and captures COM port names in the format'(COMn)'.</summary>
        private static readonly Regex PortRegex = new(@"\((COM\d+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary> Task that handles data exchange with Arduino. </summary>
        private Task? _task;
        /// <summary> Cancellation token source for task management. </summary>
        private CancellationTokenSource? _cancellationTokenSource;
        /// <summary> Current port name for the connection. </summary>
        private string _portName = string.Empty;

        /// <summary> Event for handling received messages. </summary>
        public Action<string>? OnMessageReceived;
        /// <summary> Event for handling connection state changes. </summary>
        public Action<State, string>? OnStateChanged;
        /// <summary> Command to send to the device. </summary>
        public string Command = string.Empty;

        /// <summary> Gets extended serial port details using WMI on Windows. </summary>
        public List<PortDetails> GetAvailablePorts()
        {
            var detailsByPort = new List<PortDetails>();

            if (!OperatingSystem.IsWindows())
            {
                return new List<PortDetails>();
            }

            try
            {
                using var pnpEntitySearcher = new ManagementObjectSearcher("SELECT Name, Manufacturer, Status, PNPDeviceID FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");
                var pnpPorts = pnpEntitySearcher.Get();
                foreach (ManagementObject entity in pnpPorts)
                {
                    var name = entity["Name"]?.ToString() ?? string.Empty;
                    var portName = ExtractPortName(name);
                    if (string.IsNullOrWhiteSpace(portName))
                        continue;

                    var pnpDeviceId = entity["PNPDeviceID"]?.ToString() ?? string.Empty;

                    detailsByPort.Add(new PortDetails
                    {
                        PortName = portName,
                        FriendlyName = name,
                        Manufacturer = entity["Manufacturer"]?.ToString() ?? string.Empty,
                        Status = entity["Status"]?.ToString() ?? string.Empty,
                        PnpDeviceId = pnpDeviceId,
                    });
                }
            }
            catch
            {
                return new List<PortDetails>();
            }

            return detailsByPort;
        }

        private static string ExtractPortName(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var match = PortRegex.Match(text);
            return match.Success ? match.Groups[1].Value.ToUpperInvariant() : string.Empty;
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
        public void Connect(string portName, BaudRates baudRate = BaudRates.BR_115200)
        {
            var currentPort = GetAvailablePorts().FirstOrDefault(x => x.PortName == portName);
            if (!string.IsNullOrWhiteSpace(portName) && currentPort != null)
            {
                Connection.BaudRate = baudRate;
                EstablishConnection(new SerialPort(portName, (int)baudRate));
                return;
            }
            OnStateChanged?.Invoke(State.Error, "Invalid port name.");
        }

        /// <summary> Establishes a connection to the specified serial port. </summary>
        /// <param name="connection"></param>
        private void EstablishConnection(SerialPort? connection, int timeout = 200)
        {
            if (connection != null)
            {
                // Configure serial port settings
                connection.Encoding = System.Text.Encoding.ASCII; // Use ASCII instead of UTF-8
                connection.NewLine = "\n"; // Set newline character
                connection.ReadTimeout = timeout;
                connection.WriteTimeout = timeout;

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
                                    if (!string.IsNullOrWhiteSpace(line))
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
