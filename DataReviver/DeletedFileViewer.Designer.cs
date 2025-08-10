// Copyright (C) 2017  Joey Scarr, Lukas Korsika
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

namespace DataReviver {
	partial class DeletedFileViewer {
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			this.progressBar = new System.Windows.Forms.ProgressBar();
			this.bScan = new System.Windows.Forms.Button();
			this.UpdateTimer = new System.Windows.Forms.Timer(this.components);
			this.tbFilter = new System.Windows.Forms.TextBox();
			this.bRestoreFiles = new System.Windows.Forms.Button();
			this.cbShowUnknownFiles = new System.Windows.Forms.CheckBox();
			this.fileView = new DataReviver.ListViewNoFlicker();
			this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colSize = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colModified = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colRecovery = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.lblProgress = new System.Windows.Forms.Label();
			this.animationTimer = new System.Windows.Forms.Timer(this.components);
			this.SuspendLayout();
			// 
			// progressBar
			// 
			this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.progressBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
			this.progressBar.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(255)))));
			this.progressBar.Location = new System.Drawing.Point(15, 586);
			this.progressBar.MarqueeAnimationSpeed = 30;
			this.progressBar.Maximum = 10000;
			this.progressBar.Name = "progressBar";
			this.progressBar.Size = new System.Drawing.Size(420, 34);
			this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			this.progressBar.TabIndex = 5;
			// 
			// bScan
			// 
			this.bScan.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.bScan.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(255)))));
			this.bScan.FlatAppearance.BorderSize = 0;
			this.bScan.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(96)))), ((int)(((byte)(210)))));
			this.bScan.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(142)))), ((int)(((byte)(255)))));
			this.bScan.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.bScan.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.bScan.ForeColor = System.Drawing.Color.White;
			this.bScan.Location = new System.Drawing.Point(0, 0);
			this.bScan.Name = "bScan";
			this.bScan.Size = new System.Drawing.Size(702, 80);
			this.bScan.TabIndex = 3;
			this.bScan.Text = "🔍 Start Scan";
			this.bScan.UseVisualStyleBackColor = false;
			this.bScan.Click += new System.EventHandler(this.bScan_Click);
			this.bScan.MouseEnter += new System.EventHandler(this.bScan_MouseEnter);
			this.bScan.MouseLeave += new System.EventHandler(this.bScan_MouseLeave);
			// 
			// UpdateTimer
			// 
			this.UpdateTimer.Interval = 300;
			this.UpdateTimer.Tick += new System.EventHandler(this.UpdateTimer_Tick);
			// 
			// tbFilter
			// 
			this.tbFilter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tbFilter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
			this.tbFilter.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.tbFilter.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.tbFilter.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
			this.tbFilter.Location = new System.Drawing.Point(15, 90);
			this.tbFilter.Margin = new System.Windows.Forms.Padding(15, 5, 5, 5);
			this.tbFilter.Name = "tbFilter";
			this.tbFilter.Size = new System.Drawing.Size(450, 25);
			this.tbFilter.TabIndex = 6;
			this.tbFilter.Text = "🔍 Search files...";
			this.tbFilter.TextChanged += new System.EventHandler(this.tbFilter_TextChanged);
			this.tbFilter.Enter += new System.EventHandler(this.tbFilter_Enter);
			this.tbFilter.Leave += new System.EventHandler(this.tbFilter_Leave);
			// 
			// bRestoreFiles
			// 
			this.bRestoreFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.bRestoreFiles.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(199)))), ((int)(((byte)(89)))));
			this.bRestoreFiles.Enabled = false;
			this.bRestoreFiles.FlatAppearance.BorderSize = 0;
			this.bRestoreFiles.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(167)))), ((int)(((byte)(69)))));
			this.bRestoreFiles.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(72)))), ((int)(((byte)(219)))), ((int)(((byte)(109)))));
			this.bRestoreFiles.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.bRestoreFiles.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.bRestoreFiles.ForeColor = System.Drawing.Color.White;
			this.bRestoreFiles.Location = new System.Drawing.Point(450, 586);
			this.bRestoreFiles.Name = "bRestoreFiles";
			this.bRestoreFiles.Size = new System.Drawing.Size(252, 34);
			this.bRestoreFiles.TabIndex = 5;
			this.bRestoreFiles.Text = "💾 Restore Selected Files";
			this.bRestoreFiles.UseVisualStyleBackColor = false;
			this.bRestoreFiles.Visible = false;
			this.bRestoreFiles.Click += new System.EventHandler(this.bRestoreFiles_Click);
			// 
			// cbShowUnknownFiles
			// 
			this.cbShowUnknownFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.cbShowUnknownFiles.AutoSize = true;
			this.cbShowUnknownFiles.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.cbShowUnknownFiles.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
			this.cbShowUnknownFiles.Location = new System.Drawing.Point(480, 94);
			this.cbShowUnknownFiles.Name = "cbShowUnknownFiles";
			this.cbShowUnknownFiles.Size = new System.Drawing.Size(222, 19);
			this.cbShowUnknownFiles.TabIndex = 7;
			this.cbShowUnknownFiles.Text = "📁 Show system files and unknown types";
			this.cbShowUnknownFiles.UseVisualStyleBackColor = true;
			this.cbShowUnknownFiles.CheckedChanged += new System.EventHandler(this.cbShowUnknownFiles_CheckedChanged);
			// 
			// fileView
			// 
			this.fileView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.fileView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(252)))), ((int)(((byte)(253)))), ((int)(((byte)(255)))));
			this.fileView.CheckBoxes = true;
			this.fileView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colType,
            this.colSize,
            this.colModified,
            this.colPath,
            this.colRecovery});
			this.fileView.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.fileView.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
			this.fileView.FullRowSelect = true;
			this.fileView.GridLines = true;
			this.fileView.HideSelection = false;
			this.fileView.Location = new System.Drawing.Point(15, 125);
			this.fileView.Margin = new System.Windows.Forms.Padding(15, 10, 15, 10);
			this.fileView.Name = "fileView";
			this.fileView.Size = new System.Drawing.Size(672, 448);
			this.fileView.TabIndex = 4;
			this.fileView.UseCompatibleStateImageBehavior = false;
			this.fileView.View = System.Windows.Forms.View.Details;
			this.fileView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.fileView_ColumnClick);
			this.fileView.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.fileView_ItemCheck);
			this.fileView.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.fileView_ItemSelectionChanged);
			this.fileView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.fileView_MouseClick);
			// 
			// colName
			// 
			this.colName.Text = "Name";
			this.colName.Width = 250;
			// 
			// colType
			// 
			this.colType.Text = "Type";
			this.colType.Width = 120;
			// 
			// colSize
			// 
			this.colSize.Text = "Size";
			this.colSize.Width = 80;
			// 
			// colModified
			// 
			this.colModified.Text = "Last Modified";
			this.colModified.Width = 120;
			// 
			// colPath
			// 
			this.colPath.Text = "Path";
			this.colPath.Width = 200;
			// 
			// colRecovery
			// 
			this.colRecovery.Text = "Chance of Recovery";
			this.colRecovery.Width = 160;
			// 
			// lblProgress
			// 
			this.lblProgress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.lblProgress.AutoSize = true;
			this.lblProgress.BackColor = System.Drawing.Color.Transparent;
			this.lblProgress.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblProgress.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(255)))));
			this.lblProgress.Location = new System.Drawing.Point(15, 568);
			this.lblProgress.Name = "lblProgress";
			this.lblProgress.Size = new System.Drawing.Size(0, 15);
			this.lblProgress.TabIndex = 8;
			this.lblProgress.Visible = false;
			// 
			// animationTimer
			// 
			this.animationTimer.Interval = 800;
			this.animationTimer.Tick += new System.EventHandler(this.animationTimer_Tick);
			// 
			// DeletedFileViewer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
			this.Controls.Add(this.lblProgress);
			this.Controls.Add(this.cbShowUnknownFiles);
			this.Controls.Add(this.fileView);
			this.Controls.Add(this.bRestoreFiles);
			this.Controls.Add(this.tbFilter);
			this.Controls.Add(this.progressBar);
			this.Controls.Add(this.bScan);
			this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "DeletedFileViewer";
			this.Size = new System.Drawing.Size(702, 620);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ProgressBar progressBar;
		private System.Windows.Forms.ColumnHeader colName;
		private System.Windows.Forms.ColumnHeader colSize;
		private System.Windows.Forms.Button bScan;
		private System.Windows.Forms.Timer UpdateTimer;
		private System.Windows.Forms.ColumnHeader colType;
		private System.Windows.Forms.TextBox tbFilter;
		private System.Windows.Forms.Button bRestoreFiles;
		private System.Windows.Forms.ColumnHeader colModified;
		private System.Windows.Forms.ColumnHeader colRecovery;
		private System.Windows.Forms.CheckBox cbShowUnknownFiles;
		private System.Windows.Forms.ColumnHeader colPath;
		private ListViewNoFlicker fileView;
		private System.Windows.Forms.Label lblProgress;
		private System.Windows.Forms.Timer animationTimer;

	}
}
