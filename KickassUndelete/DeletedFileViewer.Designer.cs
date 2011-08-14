namespace KickassUndelete {
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
            this.fileView = new System.Windows.Forms.ListView();
            this.colName = new System.Windows.Forms.ColumnHeader();
            this.colSize = new System.Windows.Forms.ColumnHeader();
            this.bScan = new System.Windows.Forms.Button();
            this.UpdateTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(4, 593);
            this.progressBar.Maximum = 10000;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(695, 23);
            this.progressBar.TabIndex = 5;
            // 
            // fileView
            // 
            this.fileView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.fileView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colSize});
            this.fileView.Location = new System.Drawing.Point(4, 103);
            this.fileView.Margin = new System.Windows.Forms.Padding(10);
            this.fileView.Name = "fileView";
            this.fileView.Size = new System.Drawing.Size(695, 477);
            this.fileView.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.fileView.TabIndex = 4;
            this.fileView.UseCompatibleStateImageBehavior = false;
            this.fileView.View = System.Windows.Forms.View.Details;
            this.fileView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.fileView_MouseClick);
            // 
            // colName
            // 
            this.colName.Text = "Name";
            this.colName.Width = 500;
            // 
            // colSize
            // 
            this.colSize.Text = "Size";
            this.colSize.Width = 100;
            // 
            // bScan
            // 
            this.bScan.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.bScan.Font = new System.Drawing.Font("Microsoft Sans Serif", 48F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bScan.Location = new System.Drawing.Point(3, 3);
            this.bScan.Name = "bScan";
            this.bScan.Size = new System.Drawing.Size(696, 87);
            this.bScan.TabIndex = 3;
            this.bScan.Text = "Scan";
            this.bScan.UseVisualStyleBackColor = true;
            this.bScan.Click += new System.EventHandler(this.bScan_Click);
            // 
            // UpdateTimer
            // 
            this.UpdateTimer.Interval = 200;
            this.UpdateTimer.Tick += new System.EventHandler(this.UpdateTimer_Tick);
            // 
            // DeletedFileViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.fileView);
            this.Controls.Add(this.bScan);
            this.Name = "DeletedFileViewer";
            this.Size = new System.Drawing.Size(702, 620);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.ListView fileView;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colSize;
        private System.Windows.Forms.Button bScan;
        private System.Windows.Forms.Timer UpdateTimer;

    }
}
