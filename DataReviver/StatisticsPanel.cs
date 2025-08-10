using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataReviver
{
    public partial class StatisticsPanel : UserControl
    {
        private Label lblTotalFiles;
        private Label lblRecoverableFiles;
        private Label lblScanTime;
        private Label lblStorageScanned;
        private Panel statsContainer;
        
        public StatisticsPanel()
        {
            InitializeComponent();
            SetupStatistics();
        }
        
        private void SetupStatistics()
        {
            this.Size = new Size(702, 100);
            this.BackColor = Color.FromArgb(248, 250, 252);
            
            // Statistics container
            statsContainer = new Panel
            {
                Size = new Size(702, 80),
                Location = new Point(0, 10),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            // Total Files Found
            var totalPanel = CreateStatCard("📊 Files Found", "0", Color.FromArgb(0, 122, 255), 10);
            
            // Recoverable Files
            var recoverablePanel = CreateStatCard("✅ Recoverable", "0", Color.FromArgb(52, 199, 89), 185);
            
            // Scan Time
            var timePanel = CreateStatCard("⏱️ Scan Time", "00:00", Color.FromArgb(255, 152, 0), 360);
            
            // Storage Scanned
            var storagePanel = CreateStatCard("💽 Storage", "0 GB", Color.FromArgb(88, 86, 214), 535);
            
            statsContainer.Controls.AddRange(new Control[] { totalPanel, recoverablePanel, timePanel, storagePanel });
            this.Controls.Add(statsContainer);
        }
        
        private Panel CreateStatCard(string title, string value, Color accentColor, int x)
        {
            var panel = new Panel
            {
                Size = new Size(160, 60),
                Location = new Point(x, 10),
                BackColor = Color.White
            };
            
            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(10, 8),
                AutoSize = true
            };
            
            var valueLabel = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(10, 25),
                AutoSize = true
            };
            
            panel.Controls.AddRange(new Control[] { titleLabel, valueLabel });
            return panel;
        }
        
        public void UpdateStatistics(int totalFiles, int recoverableFiles, TimeSpan scanTime, long storageSize)
        {
            if (statsContainer?.Controls.Count >= 4)
            {
                ((Label)statsContainer.Controls[0].Controls[1]).Text = totalFiles.ToString();
                ((Label)statsContainer.Controls[1].Controls[1]).Text = recoverableFiles.ToString();
                ((Label)statsContainer.Controls[2].Controls[1]).Text = scanTime.ToString(@"mm\:ss");
                ((Label)statsContainer.Controls[3].Controls[1]).Text = $"{storageSize / (1024 * 1024 * 1024)} GB";
            }
        }
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Name = "StatisticsPanel";
            this.ResumeLayout(false);
        }
    }
}
