using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestApp
{
    public class DeviceSimulator
    {
        private readonly object _lock = new();
        private Task? _task;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isConnected = false;

        // Simulated device state
        private int _currentAzimuth = 0;
        private int _currentAngle = 0;
        private int _targetAzimuth = 0;
        private int _currentSpeed = 0;
        private int _anAzValue = 180; // Азимут на якому знаходиться кут 180°
        private bool _isRotating = false;

        // Events
        public Action<string>? OnMessageReceived;
        public Action<SimulatorState, string>? OnStateChanged;

        public enum SimulatorState
        {
            Disconnected,
            Connecting,
            Connected,
            Error
        }

        public string Command
        {
            set
            {
                if (!string.IsNullOrEmpty(value) && _isConnected)
                {
                    ProcessCommand(value);
                }
            }
        }

        public void Connect()
        {
            if (_isConnected)
                return;

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            _task = Task.Run(async () =>
            {
                try
                {
                    OnStateChanged?.Invoke(SimulatorState.Connecting, "Підключення до симулятора...");
                    await Task.Delay(500, token); // Simulate connection delay

                    _isConnected = true;
                    OnStateChanged?.Invoke(SimulatorState.Connected, "Підключено до симулятора");

                    // Start simulation loop
                    while (_isConnected && !token.IsCancellationRequested)
                    {
                        await Task.Delay(100, token);
                        UpdateSimulation();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation
                }
                catch (Exception ex)
                {
                    OnStateChanged?.Invoke(SimulatorState.Error, $"Помилка симулятора: {ex.Message}");
                }
            }, token);
        }

        public void Disconnect()
        {
            try
            {
                _isConnected = false;
                _cancellationTokenSource?.Cancel();

                if (_task != null && !_task.IsCompleted)
                {
                    _task.Wait(TimeSpan.FromMilliseconds(1000));
                }

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _task = null;

                OnStateChanged?.Invoke(SimulatorState.Disconnected, "Відключено від симулятора");
            }
            catch (Exception ex)
            {
                OnStateChanged?.Invoke(SimulatorState.Error, $"Помилка при відключенні: {ex.Message}");
            }
        }

        private void ProcessCommand(string command)
        {
            // Split multiple commands
            string[] commands = command.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (string cmd in commands)
            {
                string trimmedCmd = cmd.Trim();

                if (trimmedCmd.StartsWith("$AZ,"))
                {
                    // Set target azimuth
                    string valueStr = trimmedCmd.Substring(4);
                    if (int.TryParse(valueStr, out int value))
                    {
                        lock (_lock)
                        {
                            _targetAzimuth = value;
                            _isRotating = true;
                            UpdateSpeed();
                        }
                    }
                }
                else if (trimmedCmd.StartsWith("$AN,"))
                {
                    // Set target angle
                    string valueStr = trimmedCmd.Substring(4);
                    if (int.TryParse(valueStr, out int value))
                    {
                        lock (_lock)
                        {
                            _currentAngle = value;
                        }
                        SendMessage($"AN,{_currentAngle};");
                    }
                }
                else if (trimmedCmd.StartsWith("$AN_AZ,"))
                {
                    // Set AN_AZ value
                    string valueStr = trimmedCmd.Substring(7);
                    if (int.TryParse(valueStr, out int value))
                    {
                        lock (_lock)
                        {
                            _anAzValue = value;
                        }
                        SendMessage($"AN_AZ,{_anAzValue};");
                    }
                }
                else if (trimmedCmd.StartsWith("#AZ"))
                {
                    // Query azimuth
                    SendMessage($"AZ,{_currentAzimuth};");
                }
                else if (trimmedCmd.StartsWith("#AN"))
                {
                    // Query angle
                    SendMessage($"AN,{_currentAngle};");
                }
                else if (trimmedCmd.StartsWith("#SP"))
                {
                    // Query speed
                    SendMessage($"SP,{_currentSpeed};");
                }
                else if (trimmedCmd.StartsWith("#AN_AZ"))
                {
                    // Query AN_AZ
                    SendMessage($"AN_AZ,{_anAzValue};");
                }
                else if (trimmedCmd.StartsWith("$ER,L"))
                {
                    // Manual control - Left
                    lock (_lock)
                    {
                        _currentSpeed = -100;
                    }
                }
                else if (trimmedCmd.StartsWith("$ER,R"))
                {
                    // Manual control - Right
                    lock (_lock)
                    {
                        _currentSpeed = 100;
                    }
                }
                else if (trimmedCmd.StartsWith("$IN,"))
                {
                    // Initialization commands - just acknowledge
                    SendMessage($"OK;");
                }
            }
        }

        private void UpdateSimulation()
        {
            lock (_lock)
            {
                if (_isRotating)
                {
                    // Calculate difference
                    int diff = _targetAzimuth - _currentAzimuth;

                    // Handle wrap-around (choose shortest path)
                    if (diff > 180)
                        diff -= 360;
                    else if (diff < -180)
                        diff += 360;

                    // Update azimuth
                    if (Math.Abs(diff) > 0)
                    {
                        int step = Math.Sign(diff) * Math.Min(Math.Abs(diff), 2); // Move 2 degrees per update
                        _currentAzimuth += step;

                        // Normalize to 0-359
                        if (_currentAzimuth < 0)
                            _currentAzimuth += 360;
                        else if (_currentAzimuth >= 360)
                            _currentAzimuth -= 360;

                        UpdateSpeed();
                    }
                    else
                    {
                        // Reached target
                        _isRotating = false;
                        _currentSpeed = 0;
                    }
                }
            }
        }

        private void UpdateSpeed()
        {
            if (!_isRotating)
            {
                _currentSpeed = 0;
                return;
            }

            int diff = _targetAzimuth - _currentAzimuth;

            // Handle wrap-around
            if (diff > 180)
                diff -= 360;
            else if (diff < -180)
                diff += 360;

            if (diff == 0)
            {
                _currentSpeed = 0;
            }
            else
            {
                // Simulate speed based on direction and distance
                int direction = Math.Sign(diff);
                int distance = Math.Abs(diff);

                // Speed ranges from 50 to 150 based on distance
                int speed = Math.Min(50 + distance, 150);
                _currentSpeed = direction * speed;
            }
        }

        private void SendMessage(string message)
        {
            if (_isConnected)
            {
                Task.Run(() =>
                {
                    // Small delay to simulate real device response time
                    Thread.Sleep(10);
                    OnMessageReceived?.Invoke(message);
                });
            }
        }
    }
}
