namespace TestApp
{
    partial class MapForm
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
            gMapControl = new GMap.NET.WindowsForms.GMapControl();
            buttonSetCoords = new Button();
            buttonSaveAz = new Button();
            buttonClearAz = new Button();
            SuspendLayout();
            // 
            // gMapControl
            // 
            gMapControl.Bearing = 0F;
            gMapControl.CanDragMap = true;
            gMapControl.Dock = DockStyle.Fill;
            gMapControl.EmptyTileColor = Color.Navy;
            gMapControl.GrayScaleMode = false;
            gMapControl.HelperLineOption = GMap.NET.WindowsForms.HelperLineOptions.DontShow;
            gMapControl.LevelsKeepInMemory = 5;
            gMapControl.Location = new Point(0, 0);
            gMapControl.MarkersEnabled = true;
            gMapControl.MaxZoom = 18;
            gMapControl.MinZoom = 2;
            gMapControl.MouseWheelZoomEnabled = true;
            gMapControl.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter;
            gMapControl.Name = "gMapControl";
            gMapControl.NegativeMode = false;
            gMapControl.PolygonsEnabled = true;
            gMapControl.RetryLoadTile = 0;
            gMapControl.RoutesEnabled = true;
            gMapControl.ScaleMode = GMap.NET.WindowsForms.ScaleModes.Integer;
            gMapControl.SelectedAreaFillColor = Color.FromArgb(33, 65, 105, 225);
            gMapControl.ShowTileGridLines = false;
            gMapControl.Size = new Size(800, 600);
            gMapControl.TabIndex = 0;
            gMapControl.Zoom = 5D;
            // 
            // buttonSetCoords
            // 
            buttonSetCoords.FlatStyle = FlatStyle.Flat;
            buttonSetCoords.Location = new Point(0, 0);
            buttonSetCoords.Name = "buttonSetCoords";
            buttonSetCoords.Size = new Size(132, 26);
            buttonSetCoords.TabIndex = 5;
            buttonSetCoords.Text = "Задати координати";
            buttonSetCoords.UseVisualStyleBackColor = true;
            // 
            // buttonSaveAz
            // 
            buttonSaveAz.BackColor = SystemColors.ActiveCaption;
            buttonSaveAz.FlatStyle = FlatStyle.Flat;
            buttonSaveAz.Location = new Point(0, 32);
            buttonSaveAz.Name = "buttonSaveAz";
            buttonSaveAz.Size = new Size(132, 26);
            buttonSaveAz.TabIndex = 5;
            buttonSaveAz.Text = "Зберегти пеленг";
            buttonSaveAz.UseVisualStyleBackColor = false;
            // 
            // buttonClearAz
            // 
            buttonClearAz.BackColor = Color.SandyBrown;
            buttonClearAz.FlatStyle = FlatStyle.Flat;
            buttonClearAz.Location = new Point(0, 64);
            buttonClearAz.Name = "buttonClearAz";
            buttonClearAz.Size = new Size(132, 28);
            buttonClearAz.TabIndex = 5;
            buttonClearAz.Text = "Очистити пеленги";
            buttonClearAz.UseVisualStyleBackColor = false;
            // 
            // MapForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 600);
            Controls.Add(buttonClearAz);
            Controls.Add(buttonSaveAz);
            Controls.Add(buttonSetCoords);
            Controls.Add(gMapControl);
            Name = "MapForm";
            Text = "Мапа";
            ResumeLayout(false);
        }

        #endregion

        private GMap.NET.WindowsForms.GMapControl gMapControl;
        private Button buttonSetCoords;
        private Button buttonSaveAz;
        private Button buttonClearAz;
    }
}
