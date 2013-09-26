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
	public partial class ProgressPopup : Form {
		IProgressable m_Model;
		private ProgressPopup() {
			InitializeComponent();
		}

		/// <summary>
		/// Constructs the popup window.
		/// </summary>
		public ProgressPopup(IProgressable model)
			: this() {
			m_Model = model;

			m_Model.Progress += model_Progress;
			m_Model.Finished += model_Finished;
		}

		void model_Progress(string status, double progress) {
			this.BeginInvoke(new Action(delegate() {
				Show();
				lSaving.Text = status;
				progressBar.Value = (int)(progress * 100);
			}));
		}

		void model_Finished() {
			this.BeginInvoke(new Action(delegate() {
				Hide();
			}));
		}
	}
}
