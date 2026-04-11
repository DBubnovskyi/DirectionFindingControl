namespace TestApp
{
    public partial class Form2 : Form
    {
        public Action? OnButtonClick;

        public Form2()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OnButtonClick?.Invoke();
        }

        private void stopAutoInit_Click(object sender, EventArgs e)
        {
            SettingsManager.Current.Initialization.IsAutoInit = false;
        }
    }
}