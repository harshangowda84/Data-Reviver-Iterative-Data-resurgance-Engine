using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataReviver
{
    public class LogoutConfirmDialog : Form
    {
        public bool Confirmed { get; private set; } = false;

        public LogoutConfirmDialog()
        {
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Width = 380;
            this.Height = 180;
            this.BackColor = Color.White;
            this.Text = "Confirm Logout";
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            // Set DR icon if available
            if (MainForm.DRIcon != null)
                this.Icon = MainForm.DRIcon;

            // Header panel
            var headerPanel = new Panel
            {
                BackColor = Color.FromArgb(0, 122, 255),
                Dock = DockStyle.Top,
                Height = 44
            };
            var icon = new PictureBox
            {
                Image = SystemIcons.Question.ToBitmap(),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Size = new Size(32, 32),
                Location = new Point(18, 6)
            };
            var titleLabel = new Label
            {
                Text = "Confirm Logout",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(60, 10),
                AutoSize = true
            };
            headerPanel.Controls.Add(icon);
            headerPanel.Controls.Add(titleLabel);
            this.Controls.Add(headerPanel);

            // Message label
            var messageLabel = new Label
            {
                Text = "Are you sure you want to logout?",
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                Location = new Point(30, 65),
                AutoSize = true
            };
            this.Controls.Add(messageLabel);

            // Yes button
            var yesButton = new Button
            {
                Text = "Yes",
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(90, 36),
                Location = new Point(70, 110),
                DialogResult = DialogResult.Yes
            };
            yesButton.FlatAppearance.BorderSize = 0;
            yesButton.Click += (s, e) => { this.Confirmed = true; this.DialogResult = DialogResult.Yes; this.Close(); };

            // No button
            var noButton = new Button
            {
                Text = "No",
                BackColor = Color.FromArgb(220, 53, 69), // Bootstrap Red
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(90, 36),
                Location = new Point(200, 110),
                DialogResult = DialogResult.No
            };
            noButton.FlatAppearance.BorderSize = 0;
            noButton.Click += (s, e) => { this.Confirmed = false; this.DialogResult = DialogResult.No; this.Close(); };

            this.Controls.Add(yesButton);
            this.Controls.Add(noButton);
        }
    }
}
