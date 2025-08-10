using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;


namespace DataReviver
{
    public partial class ForensicToolsForm : Form
    {
        public ForensicToolsForm()
        {
            this.Text = "Data Reviver - Forensic Analysis Tools";
            this.Size = new Size(800, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(240, 248, 255);
            var label = new Label
            {
                Text = "Forensic features are unavailable in this build.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 16, FontStyle.Bold)
            };
            this.Controls.Add(label);
        }
    }
}
