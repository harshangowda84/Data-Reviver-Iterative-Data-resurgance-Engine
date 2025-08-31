using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataReviver
{
    public class PasswordPromptForm : Form
    {
        private TextBox passwordBox;
        private Button confirmButton;
        private Button cancelButton;
        public string EnteredPassword { get; private set; }

        public PasswordPromptForm()
        {
            this.Text = "Authentication Required";
            this.Size = new Size(320, 180);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(250, 250, 250);

            var promptLabel = new Label
            {
                Text = "Enter login password:",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.Black,
                Location = new Point(0, 20),
                Size = new Size(320, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            passwordBox = new TextBox
            {
                Location = new Point(60, 55),
                Size = new Size(200, 28),
                Font = new Font("Segoe UI", 12F),
                UseSystemPasswordChar = true
            };
            passwordBox.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    confirmButton.Focus();
                    confirmButton.PerformClick();
                }
            };

            confirmButton = new Button
            {
                Text = "Confirm",
                Location = new Point(45, 100),
                Size = new Size(100, 36),
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                TabStop = false
            };
            confirmButton.FlatAppearance.BorderSize = 0;
            confirmButton.Click += (s, e) => {
                EnteredPassword = passwordBox.Text;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(170, 100),
                Size = new Size(100, 36),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                TabStop = false
            };
            cancelButton.FlatAppearance.BorderSize = 0;
            cancelButton.Click += (s, e) => {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.Add(promptLabel);
            this.Controls.Add(passwordBox);
            this.Controls.Add(confirmButton);
            this.Controls.Add(cancelButton);
        }
    }
}
