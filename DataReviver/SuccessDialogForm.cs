using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataReviver
{
    public class SuccessDialogForm : Form
    {
        public SuccessDialogForm(string message, string filePath = null)
        {
            this.Text = "Success";
            this.Size = new Size(370, 160);
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
                Size = new Size(260, 30),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            var okButton = new Button
            {
                Text = "OK",
                Size = new Size(100, 32),
                Location = new Point(70, 90),
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

            Button openButton = null;
            if (!string.IsNullOrEmpty(filePath))
            {
                openButton = new Button
                {
                    Text = "Open",
                    Size = new Size(100, 32),
                    Location = new Point(180, 90),
                    BackColor = Color.FromArgb(0, 122, 255),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    TabStop = false
                };
                openButton.FlatAppearance.BorderSize = 0;
                openButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 90, 200);
                openButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 70, 150);
                openButton.Click += (s, e) => {
                    try
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Could not open file location:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
            }

            this.Controls.Add(icon);
            this.Controls.Add(messageLabel);
            this.Controls.Add(okButton);
            if (openButton != null)
                this.Controls.Add(openButton);
        }
    }
}
