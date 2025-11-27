using SerialConnect;
using SerialMonitor;

namespace TestApp
{
    public partial class Form1 : Form
    {
        private Serial _serial = null!;
        private System.Windows.Forms.Timer _refreshTimer = null!;
        private System.Windows.Forms.Timer _dataRequestTimer = null!;
        private bool _isConnected = false;

        public Form1()
        {
            InitializeComponent();
            InitializeSerial();
            InitializeUI();
            LoadSerialPorts();
            InitializeRefreshTimer();
            InitializeDataRequestTimer();
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

            // Handle form closing
            this.FormClosing += Form1_FormClosing;
        }

        private void BtnAz_Click(object? sender, EventArgs e)
        {
            if (_isConnected)
            {
                int azimuth = (int)numericUpDownAz.Value;
                _serial.Command = $"$AZ,{azimuth};";
            }
        }

        private void BtnAn_Click(object? sender, EventArgs e)
        {
            if (_isConnected)
            {
                int angle = (int)numericUpDownAn.Value;
                _serial.Command = $"$AN,{angle};";
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
        }

        private void InitializeRefreshTimer()
        {
            _refreshTimer = new System.Windows.Forms.Timer();
            _refreshTimer.Interval = 2000; // Refresh every 2 seconds
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();
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
                    _serial.Command = "#AZ;#AN;";
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
                _serial.Disconnect();
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
                        _serial.Connect(selectedPort.PortName);
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
                _serial.Command = "$IN,1";
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Крок 2: Введення похибки магнітного датчика  
            if (_isConnected)
            {
                _serial.Command = "$IN,3;";
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Ручне керування - Ліво
            if (_isConnected)
            {
                _serial.Command = "$ER,L;";
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            // Ручне керування - Право
            if (_isConnected)
            {
                _serial.Command = "$ER,R;";
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // Крок 3: Завершити
            if (_isConnected)
            {
                _serial.Command = "$IN,4;";
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            // Задати азимут
            if (_isConnected)
            {
                int azimuth = (int)numericUpDown1.Value;
                _serial.Command = $"$AN_AZ,{azimuth};";
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            // Крок 4: Початок роботи
            if (_isConnected)
            {
                _serial.Command = "START_OPERATION";
            }
        }

        private void label14_Click(object sender, EventArgs e)
        {

        }
    }
}
