namespace TestApp
{
    partial class Form2
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            button1 = new Button();
            SuspendLayout();
            // 
            // button1
            // 
            button1.BackColor = Color.Red;
            button1.Cursor = Cursors.Hand;
            button1.Dock = DockStyle.Fill;
            button1.FlatAppearance.BorderColor = Color.Red;
            button1.FlatAppearance.MouseDownBackColor = Color.FromArgb(255, 0, 45);
            button1.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 51, 51);
            button1.FlatStyle = FlatStyle.Flat;
            button1.Font = new Font("Rubik", 36F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button1.Location = new Point(0, 0);
            button1.MinimumSize = new Size(100, 50);
            button1.Name = "button1";
            button1.Size = new Size(888, 642);
            button1.TabIndex = 0;
            button1.Text = "ВЕЛИКА ЧЕРВОНА КНОПКА";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // Form2
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(888, 642);
            Controls.Add(button1);
            MinimumSize = new Size(334, 223);
            Name = "Form2";
            Text = "Form2";
            TopMost = true;
            WindowState = FormWindowState.Maximized;
            ResumeLayout(false);
        }

        #endregion

        private Button button1;
    }
}