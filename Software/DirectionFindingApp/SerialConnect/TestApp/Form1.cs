using SerialConnect;
using SerialMonitor;
using System.Collections.Concurrent;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;

namespace TestApp
{
    public partial class Form1 : Form
    {
        private Form2 _form2;

        private Serial _serial = null!;
        private DeviceSimulator _simulator = null!;
        private bool _useSimulator = false;
        private System.Windows.Forms.Timer _refreshTimer = null!;
        private System.Windows.Forms.Timer _dataRequestTimer = null!;
        private System.Windows.Forms.Timer _scanWaitTimer = null!;
        private System.Windows.Forms.Timer _rotationTimeoutTimer = null!;
        private System.Windows.Forms.Timer _sendQueueTimer = null!;
        private ConcurrentQueue<string> _sendQueue = new ConcurrentQueue<string>();
        private int _sendIntervalMs = 200; // ms between consecutive TX commands
        private bool _isConnected = false;

        // Data request backoff / non-response handling
        private DateTime _lastRxTime = DateTime.MinValue;
        private int _consecutiveNoResponseCount = 0;
        private int _maxNoResponseBeforeBackoff = 3; // number of missed polls before backing off
        private bool _isInDataBackoff = false;
        private int _dataRequestIntervalNormal = 500; // ms
        private int _dataRequestIntervalBackoff = 3000; // ms when backing off
        private int _noResponseThresholdMs = 1500; // consider a miss if no RX within this ms

        // Scanning state
        private bool _isScanning = false;
        private bool _isWaitingForStop = false;
        private int _currentTargetAzimuth = 0;
        private List<int> _scanTargets = new List<int>();
        private int _scanIndex = 0;
        private int _scanStartAzimuth = 0;
        private int _scanEndAzimuth = 0;
        private int _scanStep = 0;
        private int _scanDirection = 1; // 1 for forward, -1 for backward
        private bool _scanPendulumMode = true;
        private int _currentSpeed = 0;
        private int _anAzValue = 0; // Азимут на якому знаходиться кут 180
        private const int DefaultRotationSpeed = 30; // deg/sec used for timeout when unknown
        private bool _rotationTimedOut = false;
        private int _scanRetryIntervalMs = 300; // retry waiting for stop

        private float _lastKnownAzimuth = 0.0f;

        // Map marker and azimuth line
        private GMapOverlay? _markersOverlay = null;
        private GMapMarker? _stationMarker = null;
        private GMapOverlay? _routesOverlay = null;
        private GMapRoute? _azimuthLine = null;
        private GMapPolygon? _azimuthPolygon = null;
        private GMapPolygon? _forbiddenPolygon = null;
        private GMapRoute? _mousePreviewLine = null;
        private ToolTip _mapToolTip = new ToolTip();
        private readonly Pen _mousePreviewPen = new Pen(Color.Blue, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };

        public Form1()
        {
            InitializeComponent();
            InitializeSerial();
            InitializeUI();
            LoadSerialPorts();
            InitializeRefreshTimer();
            InitializeDataRequestTimer();
            InitializeScanTimer();
            InitializeRotationTimeoutTimer();
            InitializeSendQueueTimer();
            InitializeMap();
        }

        private void InitializeUI()
        {
            // Disable groupBox1 controls until connected
            groupBox1.Enabled = false;

            // Set initial selection mode for listBox1
            listBox1.SelectionMode = SelectionMode.One;
            // Set ranges for new numeric controls
            numericUpDownAz.KeyDown += NumericUpDownAz_KeyDown;
            numericUpDownAn.KeyDown += NumericUpDownAn_KeyDown;

            // Setup richTextBox1
            richTextBox1.ReadOnly = true;
            richTextBox1.Font = new Font("Consolas", 9);
            richTextBox1.BackColor = Color.Black;
            richTextBox1.ForeColor = Color.LimeGreen;
            richTextBox1.Text = "Serial Monitor Ready...\n";

            // Add event handlers for new buttons

            numericAzScanStart.ValueChanged += NumericAzScan_ValueChanged;
            numericAzScanEnd.ValueChanged += NumericAzScan_ValueChanged;


            radioButton1.Checked = true; // Pendulum mode by default
            radioButton1.CheckedChanged += RadioButton_CheckedChanged;
            radioButton2.CheckedChanged += RadioButton_CheckedChanged;

            buttonScan.Click += ButtonScan_Click;

            // Connect settings buttons
            buttonSettigsGet.Click += ButtonSettingsGet_Click;
            buttonSettingsSet.Click += ButtonSettingsSet_Click;

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

        private void BtnAzSet(int value)
        {
            if (_isConnected)
            {
                // Stop scanning if active
                if (_isScanning)
                {
                    StopScanning();
                }

                if(float.TryParse(label12.Text, out float az))
                {
                    int azimuth = (int)az + value;
                    azimuth = azimuth > 359 ? 0 : azimuth < 0 ? 359 : azimuth;
                    SendCommand($"$AZ,{azimuth};");
                }
            }
        }

        private void ButtonAzL_Click(object? sender, EventArgs e)
        {
            if (_isConnected)
            {
                // Зменшуємо азимут на 1 градус від поточного відображеного значення
                int currentAz = (int)numericUpDownAz.Value;
                int newAz = currentAz - 1;

                // Перевірка на діапазон 0-359
                if (newAz < 0)
                {
                    newAz = 359;
                }

                numericUpDownAz.Value = newAz;
                SendCommand($"$AZ,{newAz};");
            }
        }

        private void ButtonAzR_Click(object? sender, EventArgs e)
        {
            if (_isConnected)
            {
                // Збільшуємо азимут на 1 градус від поточного відображеного значення
                int currentAz = (int)numericUpDownAz.Value;
                int newAz = currentAz + 1;

                // Перевірка на діапазон 0-359
                if (newAz > 359)
                {
                    newAz = 0;
                }

                numericUpDownAz.Value = newAz;
                SendCommand($"$AZ,{newAz};");
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

        private void NumericUpDownAz_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (_isConnected)
                {
                    btnAz.PerformClick();
                }
            }
        }

        private void NumericUpDownAn_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (_isConnected)
                {
                    btnAn.PerformClick();
                }
            }
        }

        private void NumericUpDownAz_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                btnAz.PerformClick();
            }
        }

        private void NumericUpDownAn_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                btnAn.PerformClick();
            }
        }







        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                // Закриваємо Form2 якщо вона відкрита
                if (_form2 != null && !_form2.IsDisposed)
                {
                    _form2.Close();
                }

                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
                _dataRequestTimer?.Stop();
                _dataRequestTimer?.Dispose();
                _scanWaitTimer?.Stop();
                _scanWaitTimer?.Dispose();
                _sendQueueTimer?.Stop();
                _sendQueueTimer?.Dispose();
                _rotationTimeoutTimer?.Stop();
                _rotationTimeoutTimer?.Dispose();

                // clear pending queued commands
                while (_sendQueue.TryDequeue(out _)) { }

                if (_serial != null && _isConnected)
                {
                    _serial.Disconnect();
                }

                // Очищення ресурсів карти
                if (_azimuthLine != null)
                {
                    _routesOverlay?.Routes.Remove(_azimuthLine);
                    _azimuthLine = null;
                }

                if (_azimuthPolygon != null)
                {
                    _routesOverlay?.Polygons.Remove(_azimuthPolygon);
                    _azimuthPolygon = null;
                }

                if (_forbiddenPolygon != null)
                {
                    _routesOverlay?.Polygons.Remove(_forbiddenPolygon);
                    _forbiddenPolygon = null;
                }

                if (_stationMarker != null)
                {
                    _markersOverlay?.Markers.Remove(_stationMarker);
                    _stationMarker = null;
                }

                if (_routesOverlay != null)
                {
                    gMapControl1?.Overlays.Remove(_routesOverlay);
                    _routesOverlay = null;
                }

                if (_markersOverlay != null)
                {
                    gMapControl1?.Overlays.Remove(_markersOverlay);
                    _markersOverlay = null;
                }

                // Dispose GMapControl
                gMapControl1?.Dispose();
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
            _dataRequestTimer.Interval = _dataRequestIntervalNormal; // Request data every 500ms (0.5 seconds)
            _dataRequestTimer.Tick += DataRequestTimer_Tick;
        }

        private void InitializeRotationTimeoutTimer()
        {
            _rotationTimeoutTimer = new System.Windows.Forms.Timer();
            // will be configured per-rotation when starting
            _rotationTimeoutTimer.Tick += RotationTimeoutTimer_Tick;
        }

        private void InitializeSendQueueTimer()
        {
            _sendQueueTimer = new System.Windows.Forms.Timer();
            _sendQueueTimer.Interval = _sendIntervalMs;
            _sendQueueTimer.Tick += SendQueueTimer_Tick;
            _sendQueueTimer.Start();
        }

        private void InitializeMap()
        {
            // Налаштування GMap.NET
            gMapControl1.MapProvider = GMapProviders.OpenStreetMap;
            GMaps.Instance.Mode = AccessMode.ServerAndCache;

            // Встановлюємо центр карти (Київ, Україна як приклад)
            gMapControl1.Position = new PointLatLng(50.4501, 30.5234);
            gMapControl1.MinZoom = 2;
            gMapControl1.MaxZoom = 18;
            gMapControl1.Zoom = 10;

            // Налаштування відображення
            gMapControl1.ShowCenter = true; // Показуємо хрестик по центру
            gMapControl1.DragButton = MouseButtons.Left;

            // Створення оверлеїв для маркерів та ліній
            _markersOverlay = new GMapOverlay("markers");
            _routesOverlay = new GMapOverlay("routes");
            gMapControl1.Overlays.Add(_routesOverlay);
            gMapControl1.Overlays.Add(_markersOverlay);

            // Додати обробник для оновлення лінії при зміні азимуту
            label12.TextChanged += Label12_TextChanged;

            // Обробник для перетягування маркера
            gMapControl1.OnMarkerEnter += (marker) =>
            {
                if (marker?.Tag?.ToString() == "station")
                {
                    gMapControl1.Cursor = Cursors.Hand;
                }
            };

            gMapControl1.OnMarkerLeave += (marker) =>
            {
                gMapControl1.Cursor = Cursors.Default;
            };

            // Обробник для оновлення лінії після переміщення маркера
            gMapControl1.MouseUp += (sender, e) =>
            {
                if (_stationMarker != null && gMapControl1.IsMouseOverMarker)
                {
                    UpdateAzimuthLine();
                }
            };

            // Обробник для малювання пунктирної лінії до миші
            gMapControl1.MouseMove += GMapControl1_MouseMove;
            gMapControl1.MouseLeave += GMapControl1_MouseLeave;
            gMapControl1.MouseClick += GMapControl1_MouseClick;

            buttonSetCoords_Click(new object(), new EventArgs());
            ButtonSettingsGet_Click(new object(), new EventArgs());
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
                    var now = DateTime.Now;

                    // Check for recent responses
                    if (_lastRxTime != DateTime.MinValue && (now - _lastRxTime).TotalMilliseconds > _noResponseThresholdMs)
                    {
                        _consecutiveNoResponseCount++;
                    }
                    else
                    {
                        _consecutiveNoResponseCount = 0;
                    }

                    // Enter backoff if we've missed a few polls
                    if (!_isInDataBackoff && _consecutiveNoResponseCount >= _maxNoResponseBeforeBackoff)
                    {
                        _isInDataBackoff = true;
                        try { _dataRequestTimer.Interval = _dataRequestIntervalBackoff; } catch { }
                        Console.WriteLine($"DataRequest: entering backoff after {_consecutiveNoResponseCount} missed responses; interval={_dataRequestTimer.Interval}ms");
                    }

                    // If in backoff, we'll still send but less frequently (timer interval increased).
                    // If you prefer to stop entirely while silent, we could `_dataRequestTimer.Stop()` here instead.
                    SendCommand("#AZ;#AN;#SP;");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DataRequestTimer_Tick: {ex.Message}");
            }
        }

        private void RotationTimeoutTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Timeout: device didn't report stop in time
                _rotationTimeoutTimer?.Stop();
                _rotationTimedOut = true;
                Console.WriteLine("Rotation timeout triggered - device did not report stop. Will wait for SP==0 before advancing.");
                // Do NOT call OnRotationStopped here - we must wait for a confirmed SP==0
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RotationTimeoutTimer_Tick: {ex.Message}");
            }
        }

        private void SendQueueTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (_sendQueue.TryDequeue(out var cmd))
                {
                    string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    string log = $"[{timestamp}] TX: {cmd}\n";
                    try
                    {
                        if (_useSimulator)
                        {
                            _simulator.Command = cmd;
                        }
                        else
                        {
                            _serial.Command = cmd;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending command: {ex.Message}");
                    }

                    // Log TX to console and UI
                    Console.WriteLine(log.TrimEnd());
                    try
                    {
                        if (!richTextBox1.IsDisposed && !this.IsDisposed)
                        {
                            richTextBox1.AppendText(log);
                            richTextBox1.ScrollToCaret();
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendQueueTimer_Tick: {ex.Message}");
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

                // Update last-received time and clear any data-request backoff state
                _lastRxTime = DateTime.Now;
                _consecutiveNoResponseCount = 0;
                if (_isInDataBackoff)
                {
                    _isInDataBackoff = false;
                    try { _dataRequestTimer.Interval = _dataRequestIntervalNormal; } catch { }
                    Console.WriteLine("DataRequest: responses resumed, exiting backoff; interval restored.");
                }

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

                int? receivedAz = null;
                int? receivedAn = null;

                foreach (string part in parts)
                {
                    string trimmedPart = part.Trim();

                    if (trimmedPart.StartsWith("AZ,"))
                    {
                        string angleStr = trimmedPart.Substring(3).Trim();
                        if (float.TryParse(angleStr, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float azimuthFloat))
                        {
                            int azimuth = (int)Math.Round(azimuthFloat);
                            receivedAz = azimuth;
                            _lastKnownAzimuth = azimuthFloat;
                            label12.Text = azimuthFloat.ToString("000.0");
                        }
                    }
                    else if (trimmedPart.StartsWith("AN,"))
                    {
                        string angleStr = trimmedPart.Substring(3).Trim();
                        if (float.TryParse(angleStr, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float angleFloat))
                        {
                            int angle = (int)Math.Round(angleFloat);
                            receivedAn = angle;
                            label13.Text = angleFloat.ToString("000.0");
                        }
                    }
                    else if (trimmedPart.StartsWith("SP,"))
                    {
                        string speedStr = trimmedPart.Substring(3).Trim();
                        if (float.TryParse(speedStr, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float speedFloat))
                        {
                            int speed = (int)Math.Round(speedFloat);
                            _currentSpeed = speed;
                            label15.Text = speed.ToString("000");

                            // Check if rotation stopped during scanning
                            if (_isScanning && _isWaitingForStop && speed == 0)
                            {
                                // stop rotation timeout and handle stopped
                                try { _rotationTimeoutTimer?.Stop(); } catch { }
                                OnRotationStopped();
                            }
                        }
                    }
                    else if (trimmedPart.StartsWith("TOL,"))
                    {
                        string tolStr = trimmedPart.Substring(4).Trim();
                        if (float.TryParse(tolStr, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float tol))
                        {
                            numericTolerance.Value = (decimal)Math.Max(0.1f, Math.Min(5.0f, tol));
                        }
                    }
                    else if (trimmedPart.StartsWith("MINS,"))
                    {
                        string minsStr = trimmedPart.Substring(5).Trim();
                        if (int.TryParse(minsStr, out int mins))
                        {
                            numericMinSpeed.Value = Math.Max(0, Math.Min(255, mins));
                        }
                    }
                    else if (trimmedPart.StartsWith("MAXS,"))
                    {
                        string maxsStr = trimmedPart.Substring(5).Trim();
                        if (int.TryParse(maxsStr, out int maxs))
                        {
                            numericMaxSpeed.Value = Math.Max(0, Math.Min(255, maxs));
                        }
                    }
                    else if (trimmedPart.StartsWith("BRK,"))
                    {
                        string brkStr = trimmedPart.Substring(4).Trim();
                        if (float.TryParse(brkStr, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float brk))
                        {
                            numericBreackAngle.Value = (decimal)Math.Max(1.0f, Math.Min(90.0f, brk));
                        }
                    }
                }

                // Calculate AN_AZ from received AZ and AN values
                // AN_AZ is the azimuth where the antenna angle is 180 degrees
                if (receivedAz.HasValue && receivedAn.HasValue)
                {
                    // AN_AZ = AZ - AN + 180
                    int calculatedAnAz = (receivedAz.Value - receivedAn.Value + 180 + 360) % 360;
                    _anAzValue = calculatedAnAz;
                    labelAN_AZ.Text = calculatedAnAz.ToString("000");

                    // Оновлюємо полігони на карті при зміні AN_AZ
                    if (_stationMarker != null)
                    {
                        UpdateAzimuthLine();
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

                        ButtonSettingsGet_Click(new object(), new EventArgs());
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
            // Build scan target list before starting to avoid relying on live sensor values
            _scanTargets = GenerateScanTargets(_scanStartAzimuth, _scanEndAzimuth, _scanStep, _scanPendulumMode);
            if (_scanTargets == null || _scanTargets.Count == 0)
            {
                MessageBox.Show("Не вдалося сформувати масив точок сканування", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_scanStartAzimuth == _scanEndAzimuth)
            {
                MessageBox.Show("Початковий та кінцевий азимут не можуть бути однаковими",
                    "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _isScanning = true;
            _scanIndex = 0;
            _currentTargetAzimuth = _scanTargets[_scanIndex];

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
            btnAn.Enabled = false;
            numericUpDownAz.Enabled = false;
            numericUpDownAn.Enabled = false;

            // Start rotation to first position
            RotateToAzimuth(_currentTargetAzimuth);
        }

        private void StopScanning()
        {
            _isScanning = false;
            _isWaitingForStop = false;
            _scanWaitTimer?.Stop();
            _scanTargets?.Clear();
            _scanIndex = 0;

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
            btnAn.Enabled = true;
            numericUpDownAz.Enabled = true;
            numericUpDownAn.Enabled = true;
        }

        private int DetermineScanDirection(int start, int end, int anAz)
        {
            // Check if the shorter arc (direct path) crosses AN_AZ (180° forbidden zone)
            int shortDiff = end - start;
            if (shortDiff < 0) shortDiff += 360;

            bool shortestIsForward = shortDiff <= 180;

            // Check if short path crosses AN_AZ
            bool shortPathCrossesAnAz = false;
            if (shortestIsForward)
            {
                // Forward: from start to end
                if (start < end)
                {
                    // Simple case: start < end, check if anAz is between
                    shortPathCrossesAnAz = (anAz > start && anAz < end);
                }
                else
                {
                    // Wraps around 0: start=350, end=10
                    shortPathCrossesAnAz = (anAz > start || anAz < end);
                }
            }
            else
            {
                // Backward: from start to end going backwards
                if (start > end)
                {
                    // Simple case: start > end, going backwards
                    shortPathCrossesAnAz = (anAz < start && anAz > end);
                }
                else
                {
                    // Wraps around 0 backwards: start=10, end=350
                    shortPathCrossesAnAz = (anAz < start || anAz > end);
                }
            }

            // If short path crosses AN_AZ, we must go the long way
            if (shortPathCrossesAnAz)
            {
                // Reverse direction to take the long path
                return shortestIsForward ? -1 : 1;
            }

            // Otherwise use the shortest path
            return shortestIsForward ? 1 : -1;
        }

        private List<int> GenerateScanTargets(int start, int end, int step, bool pendulum)
        {
            var list = new List<int>();
            if (step <= 0) step = 1;

            int dir = DetermineScanDirection(start, end, _anAzValue);

            // Build forward or backward base sequence from start to end inclusive
            int current = start;
            list.Add(current);
            if (current != end)
            {
                while (true)
                {
                    if (dir > 0)
                    {
                        int distToEnd = (end - current + 360) % 360;
                        if (distToEnd == 0) break;
                        if (step >= distToEnd)
                        {
                            list.Add(end);
                            break;
                        }
                        current = (current + step) % 360;
                        list.Add(current);
                    }
                    else
                    {
                        int distToEnd = (current - end + 360) % 360;
                        if (distToEnd == 0) break;
                        if (step >= distToEnd)
                        {
                            list.Add(end);
                            break;
                        }
                        current = (current - step) % 360;
                        if (current < 0) current += 360;
                        list.Add(current);
                    }
                }
            }

            if (pendulum)
            {
                // append reversed sequence excluding the last element to avoid duplicate end
                if (list.Count > 1)
                {
                    var rev = new List<int>(list);
                    rev.RemoveAt(rev.Count - 1);
                    rev.Reverse();
                    list.AddRange(rev);
                }
            }
            else
            {
                // return-to-start mode: append start so sequence returns
                if (list.Count > 0 && list[list.Count - 1] != start)
                {
                    list.Add(start);
                }
            }

            return list;
        }

        private void RotateToAzimuth(int azimuth)
        {
            _isWaitingForStop = true;
            SendCommand($"$AZ,{azimuth};");

            // Start rotation timeout based on estimated rotation duration
            try
            {
                int fromAz = _currentTargetAzimuth;
                int toAz = azimuth;
                int diff = Math.Abs(((toAz - fromAz) + 540) % 360 - 180); // minimal angular difference
                int speed = _currentSpeed > 0 ? _currentSpeed : DefaultRotationSpeed;
                int timeoutMs = Math.Max(2000, (int)(diff / (double)speed * 1000.0) + 1500);
                _rotationTimeoutTimer.Interval = timeoutMs;
                _rotationTimeoutTimer.Start();
                Console.WriteLine($"Rotation timeout set to {timeoutMs} ms (diff {diff} deg, speed {speed})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scheduling rotation timeout: {ex.Message}");
            }
        }

        private void OnRotationStopped()
        {
            _isWaitingForStop = false;

            // Clear timeout flag on confirmed stop
            _rotationTimedOut = false;

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

            // Only advance when we have a confirmed stop (speed == 0)
            if (_currentSpeed != 0)
            {
                // Not stopped yet - wait a short retry interval and check again
                Console.WriteLine($"ScanWait: waiting for stop, current speed={_currentSpeed}. Retrying in {_scanRetryIntervalMs}ms");
                _scanWaitTimer.Interval = _scanRetryIntervalMs;
                _scanWaitTimer.Start();
                return;
            }

            // Advance index in precomputed target list
            _scanIndex++;
            if (_scanIndex >= _scanTargets.Count)
            {
                // wrap around to continue scanning
                _scanIndex = 0;
            }

            _currentTargetAzimuth = _scanTargets[_scanIndex];
            RotateToAzimuth(_currentTargetAzimuth);
        }

        private int CalculateNextAzimuth()
        {
            // Determine next target by moving _scanStep toward _scanEndAzimuth in _scanDirection
            // This computes remaining distance to end along the chosen direction and clamps to end.
            if (_scanDirection > 0)
            {
                // Forward direction: distance from current to end
                int distToEnd = (_scanEndAzimuth - _currentTargetAzimuth + 360) % 360;
                if (distToEnd == 0) return -1; // already at end
                if (_scanStep >= distToEnd)
                {
                    return _scanEndAzimuth;
                }
                else
                {
                    int next = (_currentTargetAzimuth + _scanStep) % 360;
                    return next;
                }
            }
            else
            {
                // Backward direction: distance from current to end going negative
                int distToEnd = (_currentTargetAzimuth - _scanEndAzimuth + 360) % 360;
                if (distToEnd == 0) return -1; // already at end
                if (_scanStep >= distToEnd)
                {
                    return _scanEndAzimuth;
                }
                else
                {
                    int next = (_currentTargetAzimuth - _scanStep) % 360;
                    if (next < 0) next += 360;
                    return next;
                }
            }
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
            // Enqueue command for paced sending to avoid bursts and ensure stable intervals
            if (string.IsNullOrWhiteSpace(command)) return;
            _sendQueue.Enqueue(command);
            Console.WriteLine($"[ENQUEUE] {DateTime.Now:HH:mm:ss.fff} {command}");
        }

        private void ButtonSettingsGet_Click(object? sender, EventArgs e)
        {
            if (_isConnected)
            {
                SendCommand("#TOL;#MINS;#MAXS;#BRK;");
            }
        }

        private void ButtonSettingsSet_Click(object? sender, EventArgs e)
        {
            if (_isConnected)
            {
                // Send settings with InvariantCulture to ensure decimal point format
                string tol = ((float)numericTolerance.Value).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                int mins = (int)numericMinSpeed.Value;
                int maxs = (int)numericMaxSpeed.Value;
                string brk = ((float)numericBreackAngle.Value).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

                SendCommand($"$TOL,{tol};$MINS,{mins};$MAXS,{maxs};$BRK,{brk};");
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

        private void buttonSetCoords_Click(object sender, EventArgs e)
        {
            if (_markersOverlay == null) return;

            // Отримуємо поточну позицію центру карти
            PointLatLng centerPosition = gMapControl1.Position;

            if (_stationMarker != null)
            {
                // Видаляємо старий маркер
                _markersOverlay.Markers.Remove(_stationMarker);
                _stationMarker = null;
            }

            // Створюємо новий маркер
            _stationMarker = new GMarkerGoogle(centerPosition, GMarkerGoogleType.red_small);
            _stationMarker.ToolTipText = "Станція пеленгації";
            _stationMarker.IsHitTestVisible = true;

            // Дозволяємо перетягування маркера
            _stationMarker.Tag = "station";
            _markersOverlay.Markers.Add(_stationMarker);

            // Малюємо початкову лінію азимуту
            UpdateAzimuthLine();

            gMapControl1.Refresh();
        }

        private void Label12_TextChanged(object? sender, EventArgs e)
        {
            // При зміні азимуту оновлюємо лінію
            if (_stationMarker != null)
            {
                UpdateAzimuthLine();
            }
        }

        private void UpdateAzimuthLine()
        {
            if (_stationMarker == null || _routesOverlay == null) return;

            try
            {
                // Видаляємо стару лінію
                if (_azimuthLine != null)
                {
                    _routesOverlay.Routes.Remove(_azimuthLine);
                    _azimuthLine = null;
                }

                // Видаляємо старий полігон
                if (_azimuthPolygon != null)
                {
                    _routesOverlay.Polygons.Remove(_azimuthPolygon);
                    _azimuthPolygon = null;
                }

                // Обчислюємо кінцеву точку лінії на відстані 150 км
                PointLatLng startPoint = _stationMarker.Position;
                PointLatLng endPoint = CalculateDestinationPoint(startPoint, _lastKnownAzimuth, 150.0);

                // Створюємо нову лінію
                List<PointLatLng> points = new List<PointLatLng> { startPoint, endPoint };
                _azimuthLine = new GMapRoute(points, "azimuth");
                _azimuthLine.Stroke = new Pen(Color.BlueViolet, 3);

                _routesOverlay.Routes.Add(_azimuthLine);

                // Створюємо полігон: центр, ліва точка (-7°), права точка (+7°)
                double leftAzimuth = (_lastKnownAzimuth - 7 + 360) % 360;
                double rightAzimuth = (_lastKnownAzimuth + 7) % 360;

                PointLatLng leftPoint = CalculateDestinationPoint(startPoint, leftAzimuth, 150.0);
                PointLatLng rightPoint = CalculateDestinationPoint(startPoint, rightAzimuth, 150.0);

                List<PointLatLng> polygonPoints = new List<PointLatLng>
                {
                    startPoint,
                    leftPoint,
                    rightPoint
                };

                _azimuthPolygon = new GMapPolygon(polygonPoints, "azimuthPolygon");
                _azimuthPolygon.Fill = new SolidBrush(Color.FromArgb(102, Color.BlueViolet)); // 0.4 opacity = 102/255
                _azimuthPolygon.Stroke = new Pen(Color.BlueViolet, 1);

                _routesOverlay.Polygons.Add(_azimuthPolygon);

                // Видаляємо старий заборонений полігон
                if (_forbiddenPolygon != null)
                {
                    _routesOverlay.Polygons.Remove(_forbiddenPolygon);
                    _forbiddenPolygon = null;
                }

                // Створюємо заборонений полігон за кутом AN_AZ на 50 км
                double forbiddenLeftAzimuth = (_anAzValue - 7 + 360) % 360;
                double forbiddenRightAzimuth = (_anAzValue + 7) % 360;

                PointLatLng forbiddenLeftPoint = CalculateDestinationPoint(startPoint, forbiddenLeftAzimuth, 50.0);
                PointLatLng forbiddenRightPoint = CalculateDestinationPoint(startPoint, forbiddenRightAzimuth, 50.0);

                List<PointLatLng> forbiddenPolygonPoints = new List<PointLatLng>
                {
                    startPoint,
                    forbiddenLeftPoint,
                    forbiddenRightPoint
                };

                _forbiddenPolygon = new GMapPolygon(forbiddenPolygonPoints, "forbiddenPolygon");
                _forbiddenPolygon.Fill = new SolidBrush(Color.FromArgb(102, Color.Red)); // 0.4 opacity = 102/255
                _forbiddenPolygon.Stroke = new Pen(Color.Transparent, 0); // Без бордера

                _routesOverlay.Polygons.Add(_forbiddenPolygon);

                if (gMapControl1 != null && !gMapControl1.IsDisposed)
                {
                    gMapControl1.Refresh();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateAzimuthLine: {ex.Message}");
            }
        }

        private PointLatLng CalculateDestinationPoint(PointLatLng start, double bearing, double distanceKm)
        {
            // Радіус Землі в км
            const double earthRadius = 6371.0;

            // Переводимо в радіани
            double lat1 = start.Lat * Math.PI / 180.0;
            double lon1 = start.Lng * Math.PI / 180.0;
            double bearingRad = bearing * Math.PI / 180.0;
            double distRad = distanceKm / earthRadius;

            // Обчислюємо нову широту
            double lat2 = Math.Asin(
                Math.Sin(lat1) * Math.Cos(distRad) +
                Math.Cos(lat1) * Math.Sin(distRad) * Math.Cos(bearingRad)
            );

            // Обчислюємо нову довготу
            double lon2 = lon1 + Math.Atan2(
                Math.Sin(bearingRad) * Math.Sin(distRad) * Math.Cos(lat1),
                Math.Cos(distRad) - Math.Sin(lat1) * Math.Sin(lat2)
            );

            // Переводимо назад у градуси
            double lat2Deg = lat2 * 180.0 / Math.PI;
            double lon2Deg = lon2 * 180.0 / Math.PI;

            return new PointLatLng(lat2Deg, lon2Deg);
        }

        private double CalculateAzimuth(PointLatLng from, PointLatLng to)
        {
            // Обчислює азимут від точки from до точки to
            double lat1 = from.Lat * Math.PI / 180.0;
            double lon1 = from.Lng * Math.PI / 180.0;
            double lat2 = to.Lat * Math.PI / 180.0;
            double lon2 = to.Lng * Math.PI / 180.0;

            double dLon = lon2 - lon1;

            double y = Math.Sin(dLon) * Math.Cos(lat2);
            double x = Math.Cos(lat1) * Math.Sin(lat2) -
                       Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

            double azimuthRad = Math.Atan2(y, x);
            double azimuthDeg = azimuthRad * 180.0 / Math.PI;

            // Нормалізуємо до діапазону 0-360
            azimuthDeg = (azimuthDeg + 360.0) % 360.0;

            return azimuthDeg;
        }

        private double CalculateDistance(PointLatLng from, PointLatLng to)
        {
            // Обчислює відстань між двома точками в км (формула Haversine)
            const double earthRadius = 6371.0;

            double lat1 = from.Lat * Math.PI / 180.0;
            double lon1 = from.Lng * Math.PI / 180.0;
            double lat2 = to.Lat * Math.PI / 180.0;
            double lon2 = to.Lng * Math.PI / 180.0;

            double dLat = lat2 - lat1;
            double dLon = lon2 - lon1;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = earthRadius * c;

            return distance;
        }

        private void GMapControl1_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_stationMarker == null || _routesOverlay == null) return;

            try
            {
                // Отримуємо координати миші на мапі
                PointLatLng mousePosition = gMapControl1.FromLocalToLatLng(e.X, e.Y);

                // Обчислюємо азимут від маркера до миші
                double azimuth = CalculateAzimuth(_stationMarker.Position, mousePosition);

                // Обчислюємо відстань від маркера до миші
                double distance = CalculateDistance(_stationMarker.Position, mousePosition);

                // Видаляємо стару лінію попереднього перегляду
                if (_mousePreviewLine != null)
                {
                    _routesOverlay.Routes.Remove(_mousePreviewLine);
                }

                // Створюємо нову пунктирну лінію з підказкою азимуту
                List<PointLatLng> points = new List<PointLatLng>
                {
                    _stationMarker.Position,
                    mousePosition
                };

                _mousePreviewLine = new GMapRoute(points, "mousePreview")
                {
                    Stroke = _mousePreviewPen
                };

                _routesOverlay.Routes.Add(_mousePreviewLine);

                // Встановлюємо підказку з азимутом та відстанню
                string tooltipText = $"аз {azimuth:000.0}°\nд {distance:0.0}км";
                _mapToolTip.SetToolTip(gMapControl1, tooltipText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GMapControl1_MouseMove: {ex.Message}");
            }
        }

        private void GMapControl1_MouseLeave(object? sender, EventArgs e)
        {
            try
            {
                // Видаляємо лінію попереднього перегляду коли мишка покидає мапу
                if (_mousePreviewLine != null && _routesOverlay != null)
                {
                    _routesOverlay.Routes.Remove(_mousePreviewLine);
                    _mousePreviewLine = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GMapControl1_MouseLeave: {ex.Message}");
            }
        }

        private void GMapControl1_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && _stationMarker != null)
            {
                try
                {
                    // Отримуємо координати кліка на карті
                    PointLatLng clickPosition = gMapControl1.FromLocalToLatLng(e.X, e.Y);

                    // Обчислюємо азимут від маркера до точки кліка
                    double azimuth = CalculateAzimuth(_stationMarker.Position, clickPosition);

                    // Округлюємо до цілих
                    int azimuthInt = (int)Math.Round(azimuth);
                    azimuthInt = azimuthInt == 360 ? 0 : azimuthInt;
                    // Встановлюємо значення в numericUpDownAz
                    numericUpDownAz.Value = azimuthInt;
                    BtnAz_Click(sender, e);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in GMapControl1_MouseClick: {ex.Message}");
                }
            }
        }

        private void btnAz_Click_1(object sender, EventArgs e)
        {
            BtnAz_Click(sender, e);
        }

        private void btnAn_Click_1(object sender, EventArgs e)
        {
            BtnAn_Click(sender, e);
        }

        private void buttonSettigsGet_Click(object sender, EventArgs e)
        {

        }

        private void buttonRed_Click(object sender, EventArgs e)
        {
            if (_form2 == null || _form2.IsDisposed)
            {
                _form2 = new Form2();
                _form2.OnButtonClick += () =>
                {
                    if (float.TryParse(label12.Text, out float result))
                    {
                        numericUpDownAz.Value = (int)result;
                        BtnAz_Click(sender, e);
                    }
                };
            }
            _form2.Show();
        }

        private void label27_Click(object sender, EventArgs e)
        {

        }

        private void buttonAzRight_Click(object sender, EventArgs e)
        {
            BtnAzSet(1);
        }

        private void button9_Click(object sender, EventArgs e)
        {

            BtnAzSet(-1);
        }

        private void label13_Click(object sender, EventArgs e)
        {

        }
    }
}
