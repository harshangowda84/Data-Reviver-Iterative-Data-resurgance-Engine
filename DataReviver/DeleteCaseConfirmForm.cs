using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataReviver
{
    public class DeleteCaseConfirmForm : Form
    {
        public bool Confirmed { get; private set; } = false;

        public DeleteCaseConfirmForm(string caseName, string caseId)
        {
            this.Text = "Delete Case";
            this.Size = new Size(400, 180);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            var icon = new PictureBox
            {
                Image = SystemIcons.Warning.ToBitmap(),
                Size = new Size(40, 40),
                Location = new Point(20, 30),
                BackColor = Color.Transparent
            };

            var messageLabel = new Label
            {
                Text = $"Are you sure you want to delete case '{caseName}' (ID: {caseId})?\nThis action cannot be undone.",
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                ForeColor = Color.Black,
                Location = new Point(70, 30),
                Size = new Size(300, 50),
                AutoSize = false
            };

            var yesButton = new Button
            {
                Text = "Yes, Delete",
                Size = new Size(120, 36),
                Location = new Point(70, 100),
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                TabStop = false
            };
            yesButton.FlatAppearance.BorderSize = 0;
            yesButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 90, 200);
            yesButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 70, 150);
            yesButton.Click += (s, e) => { Confirmed = true; this.DialogResult = DialogResult.OK; this.Close(); };

            var noButton = new Button
            {
                Text = "Cancel",
                Size = new Size(120, 36),
                Location = new Point(210, 100),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                TabStop = false
            };
            noButton.FlatAppearance.BorderSize = 0;
            noButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 40, 50);
            noButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(150, 30, 40);
            noButton.Click += (s, e) => { Confirmed = false; this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.Add(icon);
            this.Controls.Add(messageLabel);
            this.Controls.Add(yesButton);
            this.Controls.Add(noButton);
        }
    }
}
