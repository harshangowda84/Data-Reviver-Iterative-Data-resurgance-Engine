using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GuiComponents {
    public partial class SaveProgressDialog : Form {
        public SaveProgressDialog() {
            InitializeComponent();
        }

        public void SetProgress(string filename, double progress) {
            // progress is between 0 and 1.
            lSaving.Text = string.Concat("Recovering ", filename, "...");
            progressBar.Value = (int)(progress * 100);
        }
    }
}
