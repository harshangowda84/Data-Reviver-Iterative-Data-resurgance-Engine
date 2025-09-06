using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataReviver
{
    public class ReportViewerDialog : Form
    {
        public ReportViewerDialog(string reportText, string reportPath)
        {
            this.Text = $"Case Report - {System.IO.Path.GetFileName(reportPath)}";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.BackColor = Color.White;

            var label = new Label
            {
                Text = "Forensic Case Report:",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 255),
                Location = new Point(20, 18),
                Size = new Size(400, 32)
            };

            var textBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 11F, FontStyle.Regular),
                Location = new Point(20, 60),
                Size = new Size(740, 440),
                Text = reportText,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                WordWrap = false
            };

            var openButton = new Button
            {
                Text = "Open in Notepad",
                Size = new Size(140, 32),
                Location = new Point(20, 520),
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                TabStop = false
            };
            openButton.FlatAppearance.BorderSize = 0;
            openButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 90, 200);
            openButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 70, 150);
            openButton.Click += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start("notepad.exe", reportPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open report in Notepad:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var closeButton = new Button
            {
                Text = "Close",
                Size = new Size(100, 32),
                Location = new Point(180, 520),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                TabStop = false
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 40, 50);
            closeButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(180, 40, 50);
            closeButton.Click += (s, e) => this.Close();

            this.Controls.Add(label);
            this.Controls.Add(textBox);
            this.Controls.Add(openButton);
            this.Controls.Add(closeButton);
        }
    }
}