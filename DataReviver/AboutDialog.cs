using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataReviver
{
    public partial class AboutDialog : Form
    {
        public AboutDialog()
        {
            InitializeComponent();
            SetupAboutDialog();
        }
        
        private void SetupAboutDialog()
        {
            this.Text = "About Data Reviver";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            
            // Header Panel
            var headerPanel = new Panel
            {
                Size = new Size(500, 80),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(0, 122, 255)
            };
            
            var logoLabel = new Label
            {
                Text = "🔄 Data Reviver",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };
            
            var subtitleLabel = new Label
            {
                Text = "Iterative Data Resurgence Engine",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 220, 220),
                Location = new Point(20, 50),
                AutoSize = true
            };
            
            headerPanel.Controls.AddRange(new Control[] { logoLabel, subtitleLabel });
            
            // Info Panel with scrolling
            var infoPanel = new Panel
            {
                Size = new Size(460, 250),
                Location = new Point(20, 100),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };

            var infoText = new Label
            {
                Text = @"🎓 MCA Final Year Project
👨‍💻 Developed by: Harshan Gowda B B
🏫 College: BIT
📅 Academic Year: 2024-25
🆔 Register No: 1BI23MC043

📋 Project: Data Reviver - Advanced File Recovery Tool
🎯 Purpose: Digital Forensics & Data Recovery Solution

📋 Features:
• Advanced NTFS/FAT file system recovery
• Forensic-grade data analysis tools
• Multi-threaded scanning engine
• Professional case management
• Evidence tracking & reporting
• Hash verification & content analysis

🔧 Technologies Used:
• C# .NET Framework 4.0+
• Windows Forms UI
• File System APIs
• Cryptographic Hash Functions
• Advanced Recovery Algorithms",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location = new Point(15, 15),
                AutoSize = true,
                MaximumSize = new Size(430, 0)
            };

            infoPanel.Controls.Add(infoText);
            
            // Close Button
            var closeButton = new Button
            {
                Text = "Close",
                Size = new Size(100, 35),
                Location = new Point(380, 365),
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            closeButton.Click += (s, e) => this.Close();
            
            this.Controls.AddRange(new Control[] { headerPanel, infoPanel, closeButton });
        }
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(484, 411);
            this.Name = "AboutDialog";
            this.Text = "About Data Reviver";
            this.ResumeLayout(false);
        }
    }
}
