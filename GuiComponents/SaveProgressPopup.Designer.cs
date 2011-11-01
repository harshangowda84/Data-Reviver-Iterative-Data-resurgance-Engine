namespace GuiComponents {
    partial class SaveProgressPopup {
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lSaving = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(12, 25);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(450, 23);
            this.progressBar.TabIndex = 0;
            // 
            // lSaving
            // 
            this.lSaving.AutoSize = true;
            this.lSaving.Location = new System.Drawing.Point(12, 9);
            this.lSaving.Name = "lSaving";
            this.lSaving.Size = new System.Drawing.Size(43, 13);
            this.lSaving.TabIndex = 1;
            this.lSaving.Text = "Saving ";
            // 
            // SaveProgressDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(474, 60);
            this.Controls.Add(this.lSaving);
            this.Controls.Add(this.progressBar);
            this.Name = "SaveProgressDialog";
            this.Text = "Recovering...";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lSaving;
    }
}