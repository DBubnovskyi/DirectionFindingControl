using SerialConnect;

namespace SerialMonitor
{
    public class SerialWrapper
    {
        private readonly Serial _serial;
        private bool _isConnected;
        private bool _isRunning;
        private Timer? _portCheckTimer;
        private readonly object _consoleLock = new();
        
        public SerialWrapper()
        {
            _serial = new Serial();
            _serial.OnMessageReceived += OnMessageReceived;
            _serial.OnStateChanged += OnStateChanged;
        }

        public async Task StartAsync()
        {
            _isRunning = true;
            Console.WriteLine("=== Serial Monitor Started ===");
            
            while (_isRunning)
            {
                if (!_isConnected)
                {
                    await ShowPortSelectionMenuAsync();
                }
                else
                {
                    await HandleConnectedModeAsync();
                    if (_isConnected)
                    {
                        await Task.Delay(100);
                    }
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _portCheckTimer?.Dispose();
            Console.WriteLine("\n=== Serial Monitor Stopped ===");
        }

        private async Task ShowPortSelectionMenuAsync()
        {
            var portInfos = SerialPortDetector.GetDetailedPortInfo();
            var availablePorts = portInfos.Where(p => p.IsAvailable).ToList();
            
            if (availablePorts.Count == 0)
            {
                lock (_consoleLock)
                {
                    Console.Clear();
                    Console.WriteLine("No serial ports found. Checking again in 2 seconds...");
                    Console.WriteLine("Press 'q' to quit");
                }
                
                await WaitWithKeyCheck(2000);
                return;
            }

            lock (_consoleLock)
            {
                Console.Clear();
                Console.WriteLine("Available Serial Ports:");
                Console.WriteLine("======================");
                
                for (int i = 0; i < availablePorts.Count; i++)
                {
                    var port = availablePorts[i];
                    Console.WriteLine($"{i + 1}. {port}");
                }
                
                Console.WriteLine($"\nEnter port number (1-{availablePorts.Count}) or 'q' to quit:");
            }

            while (!_isConnected && _isRunning)
            {
                var input = await ReadLineAsync();
                
                if (!_isRunning || _isConnected)
                    return;
                
                if (input?.ToLower() == "q")
                {
                    Stop();
                    return;
                }

                if (int.TryParse(input, out int portIndex) && portIndex >= 1 && portIndex <= availablePorts.Count)
                {
                    var selectedPort = availablePorts[portIndex - 1];
                    Console.WriteLine($"Connecting to {selectedPort.PortName}...");
                    Console.WriteLine($"Device: {selectedPort.Description}");
                    
                    _serial.Connect(selectedPort.PortName);
                    
                    while (!_isConnected && _isRunning)
                    {
                        await Task.Delay(100);
                    }
                    return;
                }
                else
                {
                    if (!_isConnected)
                        Console.WriteLine("Invalid selection. Try again:");
                }
            }
        }

        private async Task HandleConnectedModeAsync()
        {
            var input = await ReadLineAsync();
            
            if (input?.ToLower() == "disconnect" || input?.ToLower() == "quit")
            {
                _serial.Disconnect();
                return;
            }
            
            if (!string.IsNullOrEmpty(input))
            {
                _serial.Command = input;
            }
        }

        private void OnMessageReceived(string message)
        {
            lock (_consoleLock)
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                Console.WriteLine($"[{timestamp}] RX: {message}");
            }
        }

        private void OnStateChanged(Serial.State state, string message)
        {
            lock (_consoleLock)
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                
                switch (state)
                {
                    case Serial.State.Connected:
                        _isConnected = true;
                        Console.WriteLine($"[{timestamp}] {message}");
                        break;
                        
                    case Serial.State.Disconnected:
                        _isConnected = false;
                        Console.WriteLine($"[{timestamp}] {message}");
                        Console.WriteLine("Press any key to return to port selection...");
                        break;
                        
                    case Serial.State.Error:
                        if (message.Contains("Critical error"))
                        {
                            _isConnected = false;
                            Console.WriteLine($"[{timestamp}] CRITICAL ERROR: {message}");
                            Console.WriteLine("Connection lost. Press any key to return to port selection...");
                        }
                        else
                        {
                            Console.WriteLine($"[{timestamp}] ERROR: {message}");
                        }
                        break;
                        
                    case Serial.State.Writing:
                        Console.WriteLine($"[{timestamp}] TX: {message.Replace("Writing data: ", "")}");
                        break;
                        
                    case Serial.State.Reading:
                        break;
                        
                    case Serial.State.Readed:
                        break;
                        
                    case Serial.State.Written:
                        break;
                }
            }
        }

        private async Task<string?> ReadLineAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    return Console.ReadLine();
                }
                catch
                {
                    return null;
                }
            });
        }

        private async Task WaitWithKeyCheck(int milliseconds)
        {
            var endTime = DateTime.Now.AddMilliseconds(milliseconds);
            
            while (DateTime.Now < endTime && _isRunning)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                    {
                        Stop();
                        return;
                    }
                }
                await Task.Delay(100);
            }
        }
    }
}
