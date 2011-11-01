using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GuiComponents {
    /// <summary>
    /// A minimal popup window that displays a progress bar.
    /// </summary>
    public partial class SaveProgressPopup : Form {
        /// <summary>
        /// Constructs the popup window.
        /// </summary>
        public SaveProgressPopup() {
            InitializeComponent();
        }

        /// <summary>
        /// Sets the progress of the progress bar.
        /// </summary>
        /// <param name="fileName">The filename of the file being recovered.</param>
        /// <param name="progress">The progress of the operation (between 0 and 1).</param>
        public void SetProgress(string fileName, double progress) {
            // progress is between 0 and 1.
            lSaving.Text = string.Concat("Recovering ", fileName, "...");
            progressBar.Value = (int)(progress * 100);
        }
    }
}
