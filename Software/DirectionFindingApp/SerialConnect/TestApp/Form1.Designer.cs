namespace TestApp
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            groupBox1 = new GroupBox();
            label11 = new Label();
            button8 = new Button();
            label10 = new Label();
            label9 = new Label();
            button7 = new Button();
            numericUpDown1 = new NumericUpDown();
            button6 = new Button();
            label8 = new Label();
            label7 = new Label();
            button5 = new Button();
            button3 = new Button();
            label6 = new Label();
            button4 = new Button();
            label4 = new Label();
            label5 = new Label();
            button2 = new Button();
            label3 = new Label();
            label2 = new Label();
            listBox1 = new ListBox();
            label1 = new Label();
            button1 = new Button();
            groupBox2 = new GroupBox();
            label12 = new Label();
            btnAz = new Button();
            numericUpDownAz = new NumericUpDown();
            groupBox3 = new GroupBox();
            label13 = new Label();
            btnAn = new Button();
            numericUpDownAn = new NumericUpDown();
            richTextBox1 = new RichTextBox();
            label14 = new Label();
            label15 = new Label();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).BeginInit();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDownAz).BeginInit();
            groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDownAn).BeginInit();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(label11);
            groupBox1.Controls.Add(button8);
            groupBox1.Controls.Add(label10);
            groupBox1.Controls.Add(label9);
            groupBox1.Controls.Add(button7);
            groupBox1.Controls.Add(numericUpDown1);
            groupBox1.Controls.Add(button6);
            groupBox1.Controls.Add(label8);
            groupBox1.Controls.Add(label7);
            groupBox1.Controls.Add(button5);
            groupBox1.Controls.Add(button3);
            groupBox1.Controls.Add(label6);
            groupBox1.Controls.Add(button4);
            groupBox1.Controls.Add(label4);
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(button2);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(label2);
            groupBox1.Location = new Point(181, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(622, 234);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Ініціалізація";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(315, 121);
            label11.Name = "label11";
            label11.Size = new Size(47, 15);
            label11.TabIndex = 18;
            label11.Text = "Азимут";
            // 
            // button8
            // 
            button8.Location = new Point(470, 200);
            button8.Name = "button8";
            button8.Size = new Size(128, 23);
            button8.TabIndex = 17;
            button8.Text = "Завершити";
            button8.UseVisualStyleBackColor = true;
            button8.Click += button8_Click;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Font = new Font("Segoe UI", 8F);
            label10.Location = new Point(470, 41);
            label10.MaximumSize = new Size(150, 0);
            label10.Name = "label10";
            label10.Size = new Size(96, 13);
            label10.TabIndex = 16;
            label10.Text = "Початок роботи";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(470, 18);
            label9.Name = "label9";
            label9.Size = new Size(43, 15);
            label9.TabIndex = 15;
            label9.Text = "Крок 4";
            // 
            // button7
            // 
            button7.Location = new Point(315, 171);
            button7.Name = "button7";
            button7.Size = new Size(128, 23);
            button7.TabIndex = 14;
            button7.Text = "Задати";
            button7.UseVisualStyleBackColor = true;
            button7.Click += button7_Click;
            // 
            // numericUpDown1
            // 
            numericUpDown1.Location = new Point(315, 141);
            numericUpDown1.Maximum = new decimal(new int[] { 359, 0, 0, 0 });
            numericUpDown1.Name = "numericUpDown1";
            numericUpDown1.Size = new Size(128, 23);
            numericUpDown1.TabIndex = 13;
            // 
            // button6
            // 
            button6.Location = new Point(315, 200);
            button6.Name = "button6";
            button6.Size = new Size(128, 23);
            button6.TabIndex = 12;
            button6.Text = "Завершити";
            button6.UseVisualStyleBackColor = true;
            button6.Click += button6_Click;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Font = new Font("Segoe UI", 8F);
            label8.Location = new Point(315, 41);
            label8.MaximumSize = new Size(150, 0);
            label8.Name = "label8";
            label8.Size = new Size(140, 39);
            label8.TabIndex = 11;
            label8.Text = "Введення азимуту якому відповідє нульове полеження кута антени";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(315, 18);
            label7.Name = "label7";
            label7.Size = new Size(43, 15);
            label7.TabIndex = 10;
            label7.Text = "Крок 3";
            // 
            // button5
            // 
            button5.Location = new Point(224, 139);
            button5.Name = "button5";
            button5.Size = new Size(56, 23);
            button5.TabIndex = 9;
            button5.Text = "Право";
            button5.UseVisualStyleBackColor = true;
            button5.Click += button5_Click;
            // 
            // button3
            // 
            button3.Location = new Point(162, 200);
            button3.Name = "button3";
            button3.Size = new Size(128, 23);
            button3.TabIndex = 7;
            button3.Text = "Завершити";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 8F);
            label6.Location = new Point(162, 71);
            label6.MaximumSize = new Size(150, 0);
            label6.Name = "label6";
            label6.Size = new Size(149, 65);
            label6.TabIndex = 7;
            label6.Text = "Поправкою необхідно виставити антену в нульове положення якшо після попереднього короку є відхилення";
            // 
            // button4
            // 
            button4.Location = new Point(162, 139);
            button4.Name = "button4";
            button4.Size = new Size(56, 23);
            button4.TabIndex = 8;
            button4.Text = "Ліво";
            button4.UseVisualStyleBackColor = true;
            button4.Click += button4_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 8F);
            label4.Location = new Point(162, 41);
            label4.MaximumSize = new Size(150, 0);
            label4.Name = "label4";
            label4.Size = new Size(113, 26);
            label4.TabIndex = 6;
            label4.Text = "Введення похибки магнітного датчика";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(162, 21);
            label5.Name = "label5";
            label5.Size = new Size(43, 15);
            label5.TabIndex = 5;
            label5.Text = "Крок 2";
            // 
            // button2
            // 
            button2.Location = new Point(9, 200);
            button2.Name = "button2";
            button2.Size = new Size(128, 23);
            button2.TabIndex = 4;
            button2.Text = "Завершити";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 8F);
            label3.Location = new Point(9, 41);
            label3.MaximumSize = new Size(150, 0);
            label3.Name = "label3";
            label3.Size = new Size(128, 26);
            label3.TabIndex = 1;
            label3.Text = "Початок обортання антени в 0 положення";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(9, 21);
            label2.Name = "label2";
            label2.Size = new Size(43, 15);
            label2.TabIndex = 0;
            label2.Text = "Крок 1";
            // 
            // listBox1
            // 
            listBox1.FormattingEnabled = true;
            listBox1.ItemHeight = 15;
            listBox1.Location = new Point(12, 30);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(163, 169);
            listBox1.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 12);
            label1.Name = "label1";
            label1.Size = new Size(163, 15);
            label1.TabIndex = 2;
            label1.Text = "Підключені до USB пристрої";
            // 
            // button1
            // 
            button1.Location = new Point(12, 212);
            button1.Name = "button1";
            button1.Size = new Size(163, 23);
            button1.TabIndex = 3;
            button1.Text = "Підключити";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(label12);
            groupBox2.Controls.Add(btnAz);
            groupBox2.Controls.Add(numericUpDownAz);
            groupBox2.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 204);
            groupBox2.Location = new Point(12, 252);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(387, 184);
            groupBox2.TabIndex = 4;
            groupBox2.TabStop = false;
            groupBox2.Text = "Азимут";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(146, 35);
            label12.Name = "label12";
            label12.Size = new Size(53, 32);
            label12.TabIndex = 2;
            label12.Text = "000";
            // 
            // btnAz
            // 
            btnAz.Location = new Point(12, 126);
            btnAz.Name = "btnAz";
            btnAz.Size = new Size(362, 41);
            btnAz.TabIndex = 1;
            btnAz.Text = "Задати";
            btnAz.UseVisualStyleBackColor = true;
            btnAz.Click += BtnAz_Click;
            // 
            // numericUpDownAz
            // 
            numericUpDownAz.Location = new Point(12, 81);
            numericUpDownAz.Maximum = new decimal(new int[] { 359, 0, 0, 0 });
            numericUpDownAz.Name = "numericUpDownAz";
            numericUpDownAz.Size = new Size(362, 39);
            numericUpDownAz.TabIndex = 0;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(label15);
            groupBox3.Controls.Add(label14);
            groupBox3.Controls.Add(label13);
            groupBox3.Controls.Add(btnAn);
            groupBox3.Controls.Add(numericUpDownAn);
            groupBox3.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 204);
            groupBox3.Location = new Point(405, 252);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(398, 184);
            groupBox3.TabIndex = 5;
            groupBox3.TabStop = false;
            groupBox3.Text = "Кут";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(140, 35);
            label13.Name = "label13";
            label13.Size = new Size(53, 32);
            label13.TabIndex = 3;
            label13.Text = "000";
            // 
            // btnAn
            // 
            btnAn.Location = new Point(12, 126);
            btnAn.Name = "btnAn";
            btnAn.Size = new Size(362, 41);
            btnAn.TabIndex = 2;
            btnAn.Text = "Задати";
            btnAn.UseVisualStyleBackColor = true;
            btnAn.Click += BtnAn_Click;
            // 
            // numericUpDownAn
            // 
            numericUpDownAn.Location = new Point(12, 81);
            numericUpDownAn.Maximum = new decimal(new int[] { 359, 0, 0, 0 });
            numericUpDownAn.Name = "numericUpDownAn";
            numericUpDownAn.Size = new Size(362, 39);
            numericUpDownAn.TabIndex = 1;
            // 
            // richTextBox1
            // 
            richTextBox1.BackColor = SystemColors.WindowText;
            richTextBox1.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            richTextBox1.ForeColor = SystemColors.ButtonShadow;
            richTextBox1.Location = new Point(12, 446);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.ReadOnly = true;
            richTextBox1.Size = new Size(791, 146);
            richTextBox1.TabIndex = 6;
            richTextBox1.Text = "";
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Font = new Font("Segoe UI", 8F);
            label14.ForeColor = Color.Black;
            label14.Location = new Point(51, 46);
            label14.Name = "label14";
            label14.Size = new Size(81, 13);
            label14.TabIndex = 19;
            label14.Text = "< стрілка ліво";
            label14.Click += label14_Click;
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Font = new Font("Segoe UI", 8F);
            label15.ForeColor = Color.Black;
            label15.Location = new Point(199, 46);
            label15.Name = "label15";
            label15.Size = new Size(92, 13);
            label15.TabIndex = 20;
            label15.Text = "стрілка право >";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(818, 604);
            Controls.Add(richTextBox1);
            Controls.Add(groupBox3);
            Controls.Add(groupBox2);
            Controls.Add(button1);
            Controls.Add(label1);
            Controls.Add(listBox1);
            Controls.Add(groupBox1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Name = "Form1";
            Text = "Тестування керування";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).EndInit();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDownAz).EndInit();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDownAn).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private GroupBox groupBox1;
        private Label label2;
        private ListBox listBox1;
        private Label label1;
        private Button button1;
        private Button button2;
        private Label label3;
        private Button button3;
        private Label label4;
        private Label label5;
        private Button button5;
        private Label label6;
        private Button button4;
        private Label label8;
        private Label label7;
        private Button button8;
        private Label label10;
        private Label label9;
        private Button button7;
        private NumericUpDown numericUpDown1;
        private Button button6;
        private Label label11;
        private GroupBox groupBox2;
        private Button btnAz;
        private NumericUpDown numericUpDownAz;
        private GroupBox groupBox3;
        private Button btnAn;
        private NumericUpDown numericUpDownAn;
        private Label label12;
        private Label label13;
        private RichTextBox richTextBox1;
        private Label label14;
        private Label label15;
    }
}
