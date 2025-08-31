using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataReviver
{
    public class SuccessDialogForm : Form
    {
        public SuccessDialogForm(string message)
        {
            this.Text = "Success";
            this.Size = new Size(320, 140);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            var icon = new PictureBox
            {
                Image = SystemIcons.Information.ToBitmap(),
                Size = new Size(32, 32),
                Location = new Point(30, 30),
                BackColor = Color.Transparent
            };

            var messageLabel = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                ForeColor = Color.Black,
                Location = new Point(70, 35),
                Size = new Size(200, 30),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var okButton = new Button
            {
                Text = "OK",
                Size = new Size(100, 32),
                Location = new Point(110, 75),
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                TabStop = false
            };
            okButton.FlatAppearance.BorderSize = 0;
            okButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 90, 200);
            okButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 70, 150);
            okButton.Click += (s, e) => { this.DialogResult = DialogResult.OK; this.Close(); };

            this.Controls.Add(icon);
            this.Controls.Add(messageLabel);
            this.Controls.Add(okButton);
        }
    }
}
