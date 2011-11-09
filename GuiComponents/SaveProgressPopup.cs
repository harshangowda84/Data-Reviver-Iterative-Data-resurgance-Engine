// Copyright (C) 2011  Joey Scarr
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

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
