using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataReviver
{
    public class AdminWarningDialogForm : Form
    {
        public bool UserAccepted { get; private set; } = false;

        public AdminWarningDialogForm()
        {
            this.Text = "Administrator Access Recommended";
            this.Size = new Size(500, 340);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 251);

            var icon = new PictureBox
            {
                Image = SystemIcons.Warning.ToBitmap(),
                Size = new Size(48, 48),
                Location = new Point((this.Width - 48) / 2, 16), // Top center
                SizeMode = PictureBoxSizeMode.StretchImage
            };

            var headline = new Label
            {
                Text = "Administrator Access Recommended",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 53, 69),
                Location = new Point(40, 70), // Top center, below icon
                Size = new Size(420, 28),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var message = new Label
            {
                Text = "You are not currently using an administrator account. This means you will only be able to recover files from external drives, such as flash drives and SD cards. Would you like to run as administrator now? You will need the password for your administrator account.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location = new Point(24, 110), // More vertical space below headline
                Size = new Size(440, 120)
            };

            var btnYes = new Button
            {
                Text = "Yes",
                Size = new Size(90, 36),
                Location = new Point(150, 250),
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnYes.FlatAppearance.BorderSize = 0;
            btnYes.Click += (s, e) => { UserAccepted = true; this.DialogResult = DialogResult.OK; this.Close(); };

            var btnNo = new Button
            {
                Text = "No",
                Size = new Size(90, 36),
                Location = new Point(270, 250),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnNo.FlatAppearance.BorderSize = 0;
            btnNo.Click += (s, e) => { UserAccepted = false; this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.Add(icon);
            this.Controls.Add(headline);
            this.Controls.Add(message);
            this.Controls.Add(btnYes);
            this.Controls.Add(btnNo);
        }
    }
}
