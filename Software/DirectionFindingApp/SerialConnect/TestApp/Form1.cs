using SerialConnect;
using SerialMonitor;

namespace TestApp
{
    public partial class Form1 : Form
    {
        private Serial _serial = null!;
        private DeviceSimulator _simulator = null!;
        private bool _useSimulator = false;
        private System.Windows.Forms.Timer _refreshTimer = null!;
        private System.Windows.Forms.Timer _dataRequestTimer = null!;
        private System.Windows.Forms.Timer _scanWaitTimer = null!;
        private bool _isConnected = false;

        // Scanning state
        private bool _isScanning = false;
        private bool _isWaitingForStop = false;
        private int _currentTargetAzimuth = 0;
        private int _scanStartAzimuth = 0;
        private int _scanEndAzimuth = 0;
        private int _scanStep = 0;
        private int _scanDirection = 1; // 1 for forward, -1 for backward
        private bool _scanPendulumMode = true;
        private int _currentSpeed = 0;
        private int _anAzValue = 0; // Азимут на якому знаходиться кут 180

        public Form1()
        {
            InitializeComponent();
            InitializeSerial();
            InitializeUI();
            LoadSerialPorts();
            InitializeRefreshTimer();
            InitializeDataRequestTimer();
            InitializeScanTimer();
        }

        private void InitializeUI()
        {
            // Disable groupBox1 controls until connected
            groupBox1.Enabled = false;

            // Set initial selection mode for listBox1
            listBox1.SelectionMode = SelectionMode.One;

            // Set azimuth range
            numericUpDown1.Minimum = 0;
            numericUpDown1.Maximum = 359;
            numericUpDown1.Value = 0;

            // Set ranges for new numeric controls
            numericUpDownAz.Minimum = 0;
            numericUpDownAz.Maximum = 359;
            numericUpDownAz.Value = 0;

            numericUpDownAn.Minimum = 0;
            numericUpDownAn.Maximum = 359;
            numericUpDownAn.Value = 0;

            // Set initial label values
            label12.Text = "000";
            label13.Text = "000";

            // Setup richTextBox1
            richTextBox1.ReadOnly = true;
            richTextBox1.Font = new Font("Consolas", 9);
            richTextBox1.BackColor = Color.Black;
            richTextBox1.ForeColor = Color.LimeGreen;
            richTextBox1.Text = "Serial Monitor Ready...\n";

            // Add event handlers for new buttons
            btnAz.Click += BtnAz_Click;
            btnAn.Click += BtnAn_Click;

            // Initialize scan controls
            numericAzScanStart.Minimum = 0;
            numericAzScanStart.Maximum = 359;
            numericAzScanStart.Value = 0;
            numericAzScanStart.ValueChanged += NumericAzScan_ValueChanged;

            numericAzScanEnd.Minimum = 0;
            numericAzScanEnd.Maximum = 359;
            numericAzScanEnd.Value = 0;
            numericAzScanEnd.ValueChanged += NumericAzScan_ValueChanged;

            numericScanStep.Minimum = 1;
            numericScanStep.Maximum = 180;
            numericScanStep.Value = 10;

            numericScanTime.Minimum = 1;
            numericScanTime.Maximum = 3600;
            numericScanTime.Value = 5;

            radioButton1.Checked = true; // Pendulum mode by default
            radioButton1.CheckedChanged += RadioButton_CheckedChanged;
            radioButton2.CheckedChanged += RadioButton_CheckedChanged;

            buttonScan.Click += ButtonScan_Click;
            labelAN_AZ.Text = "000";

            // Handle form closing
            this.FormClosing += Form1_FormClosing;
        }

        private void BtnAz_Click(object? sender, EventArgs e)
        {
            if (_isConnected)
            {
                // Stop scanning if active
                if (_isScanning)
                {
                    StopScanning();
                }

                int azimuth = (int)numericUpDownAz.Value;
                SendCommand($"$AZ,{azimuth};");
            }
        }

        private void BtnAn_Click(object? sender, EventArgs e)
        {
            if (_isConnected)
            {
                int angle = (int)numericUpDownAn.Value;
                SendCommand($"$AN,{angle};");
            }
        }







        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
                _dataRequestTimer?.Stop();
                _dataRequestTimer?.Dispose();
                _scanWaitTimer?.Stop();
                _scanWaitTimer?.Dispose();

                if (_serial != null && _isConnected)
                {
                    _serial.Disconnect();
                }
            }
            catch (Exception ex)
            {
                // Ignore errors during cleanup
                Console.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }

        private void InitializeSerial()
        {
            _serial = new Serial();
            _serial.OnMessageReceived += OnSerialMessageReceived;
            _serial.OnStateChanged += OnSerialStateChanged;

            _simulator = new DeviceSimulator();
            _simulator.OnMessageReceived += OnSerialMessageReceived;
            _simulator.OnStateChanged += OnSimulatorStateChanged;
        }

        private void InitializeRefreshTimer()
        {
            //_refreshTimer = new System.Windows.Forms.Timer();
            //_refreshTimer.Interval = 2000; // Refresh every 2 seconds
            //_refreshTimer.Tick += RefreshTimer_Tick;
            //_refreshTimer.Start();
        }

        private void InitializeDataRequestTimer()
        {
            _dataRequestTimer = new System.Windows.Forms.Timer();
            _dataRequestTimer.Interval = 500; // Request data every 500ms (0.5 seconds)
            _dataRequestTimer.Tick += DataRequestTimer_Tick;
        }





        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (!_isConnected && !this.IsDisposed)
                {
                    LoadSerialPorts();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RefreshTimer_Tick: {ex.Message}");
            }
        }

        private void DataRequestTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (_isConnected && !this.IsDisposed)
                {
                    SendCommand("#AZ;#AN;#SP;#AN_AZ;");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DataRequestTimer_Tick: {ex.Message}");
            }
        }

        private void LoadSerialPorts()
        {
            try
            {
                var portInfos = SerialPortDetector.GetDetailedPortInfo();
                var selectedItem = listBox1.SelectedItem;

                listBox1.Items.Clear();

                // Add simulator as first item
                var simulatorInfo = new SerialPortInfo
                {
                    PortName = "SIMULATOR",
                    Description = "Симулятор пристрою",
                    IsAvailable = true
                };
                listBox1.Items.Add(simulatorInfo);

                foreach (var port in portInfos)
                {
                    listBox1.Items.Add(port);
                }

                // Try to restore selection if the port is still available
                if (selectedItem != null)
                {
                    foreach (var item in listBox1.Items)
                    {
                        if (item.ToString() == selectedItem.ToString())
                        {
                            listBox1.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Auto-select first item if nothing is selected and items are available
                if (listBox1.SelectedItem == null && listBox1.Items.Count > 0)
                {
                    listBox1.SelectedIndex = 0;
                }

                // Update button state
                button1.Enabled = listBox1.Items.Count > 0 && !_isConnected;

                if (listBox1.Items.Count == 0 && !_isConnected)
                {
                    listBox1.Items.Add("Серійні пристрої не знайдено");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при завантаженні портів: {ex.Message}", "Помилка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnSerialMessageReceived(string message)
        {
            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action<string>(OnSerialMessageReceived), message);
                }
                catch (ObjectDisposedException)
                {
                    // Form is being disposed, ignore
                }
                return;
            }

            try
            {
                // Add message to richTextBox1
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                string logMessage = $"[{timestamp}] RX: {message}\n";
                richTextBox1.AppendText(logMessage);
                richTextBox1.ScrollToCaret();

                // Keep only last 1000 lines
                var lines = richTextBox1.Lines;
                if (lines.Length > 1000)
                {
                    richTextBox1.Lines = lines.Skip(lines.Length - 1000).ToArray();
                    richTextBox1.ScrollToCaret();
                }

                // Parse AZ and AN commands - handle combined messages like "AZ,358;AN,268;"
                string cleanMessage = message.Trim().TrimEnd(';');

                // Split by semicolon to handle multiple commands in one message
                string[] parts = cleanMessage.Split(';', StringSplitOptions.RemoveEmptyEntries);

                foreach (string part in parts)
                {
                    string trimmedPart = part.Trim();

                    if (trimmedPart.StartsWith("AZ,"))
                    {
                        string angleStr = trimmedPart.Substring(3).Trim();
                        if (int.TryParse(angleStr, out int azimuth))
                        {
                            label12.Text = azimuth.ToString("000");
                        }
                    }
                    else if (trimmedPart.StartsWith("AN,"))
                    {
                        string angleStr = trimmedPart.Substring(3).Trim();
                        if (int.TryParse(angleStr, out int angle))
                        {
                            label13.Text = angle.ToString("000");
                        }
                    }
                    else if (trimmedPart.StartsWith("SP,"))
                    {
                        string speedStr = trimmedPart.Substring(3).Trim();
                        if (int.TryParse(speedStr, out int speed))
                        {
                            _currentSpeed = speed;
                            label14.Text = speed.ToString("000");

                            // Check if rotation stopped during scanning
                            if (_isScanning && _isWaitingForStop && speed == 0)
                            {
                                OnRotationStopped();
                            }
                        }
                    }
                    else if (trimmedPart.StartsWith("AN_AZ,"))
                    {
                        string anAzStr = trimmedPart.Substring(6).Trim();
                        if (int.TryParse(anAzStr, out int anAz))
                        {
                            _anAzValue = anAz;
                            labelAN_AZ.Text = anAz.ToString("000");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnSerialMessageReceived: {ex.Message}");
            }
        }

        private void OnSerialStateChanged(Serial.State state, string message)
        {
            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action<Serial.State, string>(OnSerialStateChanged), state, message);
                }
                catch (ObjectDisposedException)
                {
                    // Form is being disposed, ignore
                    return;
                }
                return;
            }

            try
            {
                switch (state)
                {
                    case Serial.State.Connected:
                        _isConnected = true;
                        button1.Text = "Відключити";
                        button1.Enabled = true;
                        _refreshTimer?.Stop();
                        _dataRequestTimer?.Start();

                        // Enable controls in groupBox1 when connected
                        groupBox1.Enabled = true;
                        break;

                    case Serial.State.Disconnected:
                        _isConnected = false;
                        button1.Text = "Підключити";
                        button1.Enabled = true;
                        _refreshTimer?.Start();
                        _dataRequestTimer?.Stop();

                        // Stop scanning if active
                        if (_isScanning)
                        {
                            StopScanning();
                        }

                        // Disable controls in groupBox1 when disconnected
                        groupBox1.Enabled = false;

                        LoadSerialPorts();
                        break;

                    case Serial.State.Error:
                        _isConnected = false;
                        button1.Text = "Підключити";
                        button1.Enabled = true;
                        _refreshTimer?.Start();
                        _dataRequestTimer?.Stop();

                        groupBox1.Enabled = false;

                        LoadSerialPorts();
                        break;
                }
            }
            catch (ObjectDisposedException)
            {
                // Form is being disposed, ignore
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnSerialStateChanged: {ex.Message}");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_isConnected)
            {
                // Disconnect
                if (_useSimulator)
                {
                    _simulator.Disconnect();
                }
                else
                {
                    _serial.Disconnect();
                }
            }
            else
            {
                // Connect
                if (listBox1.SelectedItem is SerialPortInfo selectedPort)
                {
                    button1.Enabled = false;
                    button1.Text = "Підключення...";

                    try
                    {
                        if (selectedPort.PortName == "SIMULATOR")
                        {
                            _useSimulator = true;
                            _simulator.Connect();
                        }
                        else
                        {
                            _useSimulator = false;
                            _serial.Connect(selectedPort.PortName);
                        }
                    }
                    catch (Exception)
                    {
                        button1.Enabled = true;
                        button1.Text = "Підключити";
                    }
                }
                else
                {
                    // Auto-select first item if nothing selected
                    if (listBox1.Items.Count > 0)
                    {
                        listBox1.SelectedIndex = 0;
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Крок 1: Початок оборотання антени в 0 положення
            if (_isConnected)
            {
                SendCommand("$IN,1;#IN");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Крок 2: Введення похибки магнітного датчика  
            if (_isConnected)
            {
                SendCommand("$IN,3;#IN;");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Ручне керування - Ліво
            if (_isConnected)
            {
                SendCommand("$ER,L;");
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            // Ручне керування - Право
            if (_isConnected)
            {
                SendCommand("$ER,R;");
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // Крок 3: Завершити
            if (_isConnected)
            {
                SendCommand("$IN,4;#IN;");
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            // Задати азимут
            if (_isConnected)
            {
                int azimuth = (int)numericUpDown1.Value;
                SendCommand($"$AN_AZ,{azimuth};");
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            // Крок 4: Початок роботи
            if (_isConnected)
            {
                SendCommand("START_OPERATION");
            }
        }

        private void label14_Click(object sender, EventArgs e)
        {

        }

        private void InitializeScanTimer()
        {
            _scanWaitTimer = new System.Windows.Forms.Timer();
            _scanWaitTimer.Tick += ScanWaitTimer_Tick;
        }

        private void NumericAzScan_ValueChanged(object? sender, EventArgs e)
        {
            ValidateScanRange();
        }

        private void ValidateScanRange()
        {
            int start = (int)numericAzScanStart.Value;
            int end = (int)numericAzScanEnd.Value;

            // Check if range crosses 180 degree angle
            if (CrossesAN_AZ(start, end, _anAzValue))
            {
                numericAzScanStart.Value = 0;
                numericAzScanEnd.Value = 0;
                MessageBox.Show($"Діапазон сканування не може перетинати кут 180° (азимут {_anAzValue}°)",
                    "Помилка діапазону", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private bool CrossesAN_AZ(int start, int end, int anAz)
        {
            if (start == end) return false;

            if (start < end)
            {
                // Normal range
                return anAz > start && anAz < end;
            }
            else
            {
                // Range crosses 0/360
                return anAz > start || anAz < end;
            }
        }

        private void RadioButton_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender == radioButton1 && radioButton1.Checked)
            {
                _scanPendulumMode = true;
            }
            else if (sender == radioButton2 && radioButton2.Checked)
            {
                _scanPendulumMode = false;
            }
        }

        private void ButtonScan_Click(object? sender, EventArgs e)
        {
            if (!_isConnected)
            {
                MessageBox.Show("Підключіться до пристрою перед початком сканування",
                    "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_isScanning)
            {
                StopScanning();
            }
            else
            {
                StartScanning();
            }
        }

        private void StartScanning()
        {
            _scanStartAzimuth = (int)numericAzScanStart.Value;
            _scanEndAzimuth = (int)numericAzScanEnd.Value;
            _scanStep = (int)numericScanStep.Value;

            if (_scanStartAzimuth == _scanEndAzimuth)
            {
                MessageBox.Show("Початковий та кінцевий азимут не можуть бути однаковими",
                    "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _isScanning = true;
            _currentTargetAzimuth = _scanStartAzimuth;
            _scanDirection = DetermineScanDirection(_scanStartAzimuth, _scanEndAzimuth);

            // Update UI
            buttonScan.Text = "Зупинити сканування";
            buttonScan.BackColor = Color.LimeGreen;

            // Disable manual controls during scan
            numericAzScanStart.Enabled = false;
            numericAzScanEnd.Enabled = false;
            numericScanStep.Enabled = false;
            numericScanTime.Enabled = false;
            radioButton1.Enabled = false;
            radioButton2.Enabled = false;
            btnAz.Enabled = false;
            numericUpDownAz.Enabled = false;

            // Start rotation to first position
            RotateToAzimuth(_currentTargetAzimuth);
        }

        private void StopScanning()
        {
            _isScanning = false;
            _isWaitingForStop = false;
            _scanWaitTimer?.Stop();

            // Update UI
            buttonScan.Text = "Почати сканування";
            buttonScan.BackColor = SystemColors.Control;
            buttonScan.UseVisualStyleBackColor = true;

            // Enable manual controls
            numericAzScanStart.Enabled = true;
            numericAzScanEnd.Enabled = true;
            numericScanStep.Enabled = true;
            numericScanTime.Enabled = true;
            radioButton1.Enabled = true;
            radioButton2.Enabled = true;
            btnAz.Enabled = true;
            numericUpDownAz.Enabled = true;
        }

        private int DetermineScanDirection(int start, int end)
        {
            // Determine shortest rotation direction
            int diff = end - start;
            if (diff < 0) diff += 360;

            return diff <= 180 ? 1 : -1;
        }

        private void RotateToAzimuth(int azimuth)
        {
            _isWaitingForStop = true;
            SendCommand($"$AZ,{azimuth};");
        }

        private void OnRotationStopped()
        {
            _isWaitingForStop = false;

            if (!_isScanning)
            {
                return;
            }

            // Start wait timer before next movement
            int waitTime = (int)(numericScanTime.Value * 1000); // Convert seconds to milliseconds
            _scanWaitTimer.Interval = waitTime;
            _scanWaitTimer.Start();
        }

        private void ScanWaitTimer_Tick(object? sender, EventArgs e)
        {
            _scanWaitTimer.Stop();

            if (!_isScanning)
            {
                return;
            }

            // Calculate next azimuth
            int nextAzimuth = CalculateNextAzimuth();

            if (nextAzimuth == -1)
            {
                // Reached end of scan range
                HandleScanRangeEnd();
            }
            else
            {
                _currentTargetAzimuth = nextAzimuth;
                RotateToAzimuth(_currentTargetAzimuth);
            }
        }

        private int CalculateNextAzimuth()
        {
            int next = _currentTargetAzimuth + (_scanStep * _scanDirection);

            // Normalize to 0-359
            if (next < 0) next += 360;
            if (next >= 360) next -= 360;

            // Check if we've crossed the end point
            if (_scanDirection > 0)
            {
                // Moving forward
                if (_scanStartAzimuth < _scanEndAzimuth)
                {
                    if (next > _scanEndAzimuth)
                        return -1;
                }
                else
                {
                    // Range crosses 0/360
                    if (_currentTargetAzimuth < _scanStartAzimuth && next > _scanEndAzimuth && next < _scanStartAzimuth)
                        return -1;
                }
            }
            else
            {
                // Moving backward
                if (_scanStartAzimuth > _scanEndAzimuth)
                {
                    if (next < _scanEndAzimuth)
                        return -1;
                }
                else
                {
                    // Range crosses 0/360
                    if (_currentTargetAzimuth > _scanStartAzimuth && next < _scanEndAzimuth && next > _scanStartAzimuth)
                        return -1;
                }
            }

            return next;
        }

        private void HandleScanRangeEnd()
        {
            if (_scanPendulumMode)
            {
                // Pendulum mode: reverse direction
                _scanDirection *= -1;
                int temp = _scanStartAzimuth;
                _scanStartAzimuth = _scanEndAzimuth;
                _scanEndAzimuth = temp;

                // Continue scanning from current position
                int nextAzimuth = CalculateNextAzimuth();
                if (nextAzimuth != -1)
                {
                    _currentTargetAzimuth = nextAzimuth;
                    RotateToAzimuth(_currentTargetAzimuth);
                }
            }
            else
            {
                // Return to start mode: go back to start position
                _currentTargetAzimuth = _scanStartAzimuth;
                RotateToAzimuth(_currentTargetAzimuth);
            }
        }

        private void SendCommand(string command)
        {
            if (_useSimulator)
            {
                _simulator.Command = command;
            }
            else
            {
                _serial.Command = command;
            }
        }

        private void OnSimulatorStateChanged(DeviceSimulator.SimulatorState state, string message)
        {
            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action<DeviceSimulator.SimulatorState, string>(OnSimulatorStateChanged), state, message);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                return;
            }

            try
            {
                switch (state)
                {
                    case DeviceSimulator.SimulatorState.Connected:
                        _isConnected = true;
                        button1.Text = "Відключити";
                        button1.Enabled = true;
                        _refreshTimer?.Stop();
                        _dataRequestTimer?.Start();
                        groupBox1.Enabled = true;
                        break;

                    case DeviceSimulator.SimulatorState.Disconnected:
                        _isConnected = false;
                        button1.Text = "Підключити";
                        button1.Enabled = true;
                        _refreshTimer?.Start();
                        _dataRequestTimer?.Stop();

                        if (_isScanning)
                        {
                            StopScanning();
                        }

                        groupBox1.Enabled = false;
                        LoadSerialPorts();
                        break;

                    case DeviceSimulator.SimulatorState.Error:
                        _isConnected = false;
                        button1.Text = "Підключити";
                        button1.Enabled = true;
                        _refreshTimer?.Start();
                        _dataRequestTimer?.Stop();
                        groupBox1.Enabled = false;
                        LoadSerialPorts();
                        break;
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnSimulatorStateChanged: {ex.Message}");
            }
        }
    }
}
