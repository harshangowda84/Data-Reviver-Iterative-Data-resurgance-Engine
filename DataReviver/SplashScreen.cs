using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataReviver
{
    public partial class SplashScreen : Form
    {
        private System.Windows.Forms.Timer timer;
        private int progressValue = 0;
        private ProgressBar progressBar;
        private Label lblLoading;
        private Label lblVersion;

        public SplashScreen()
        {
            InitializeComponent();
            SetupSplashScreen();
            StartLoadingAnimation();
        }

        private void SetupSplashScreen()
        {
            // Form Properties
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(500, 300);
            this.BackColor = Color.FromArgb(0, 122, 255);

            // Main Logo
            var logoLabel = new Label
            {
                Text = "🔄 DATA REVIVER",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(80, 50),
                AutoSize = true
            };

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Advanced Forensic Data Recovery Suite",
                Font = new Font("Segoe UI", 12F, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 220, 220),
                Location = new Point(120, 95),
                AutoSize = true
            };

            // Developer info
            var developerLabel = new Label
            {
                Text = "Developed by: Harshan Gowda B B",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(140, 125),
                AutoSize = true
            };

            // Loading label
            lblLoading = new Label
            {
                Text = "Initializing Forensic Modules...",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.White,
                Location = new Point(150, 200),
                AutoSize = true
            };

            // Progress bar
            progressBar = new ProgressBar
            {
                Location = new Point(100, 230),
                Size = new Size(300, 20),
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100
            };

            // Version
            lblVersion = new Label
            {
                Text = "Version 1.5.5 - MCA Final Year Project 2024-25",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(120, 270),
                AutoSize = true
            };

            this.Controls.AddRange(new Control[] { 
                logoLabel, subtitleLabel, developerLabel, lblLoading, progressBar, lblVersion 
            });
        }

        private void StartLoadingAnimation()
        {
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 50; // Update every 50ms
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            progressValue += 2;
            progressBar.Value = Math.Min(progressValue, 100);

            // Update loading text based on progress
            if (progressValue < 20)
                lblLoading.Text = "Loading File System Drivers...";
            else if (progressValue < 40)
                lblLoading.Text = "Initializing NTFS Recovery Engine...";
            else if (progressValue < 60)
                lblLoading.Text = "Loading Forensic Analysis Tools...";
            else if (progressValue < 80)
                lblLoading.Text = "Preparing User Interface...";
            else if (progressValue < 100)
                lblLoading.Text = "Finalizing Startup...";
            else
                lblLoading.Text = "Ready!";

            if (progressValue >= 100)
            {
                timer.Stop();
                System.Threading.Thread.Sleep(500); // Brief pause to show "Ready!"
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(500, 300);
            this.Name = "SplashScreen";
            this.Text = "Data Reviver - Loading";
            this.ResumeLayout(false);
        }
    }
}
