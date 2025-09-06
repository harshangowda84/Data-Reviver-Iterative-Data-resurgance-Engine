using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataReviver
{
	public class CasePromptForm : Form
	{
		public CasePromptForm()
		{
			this.Text = "Case Selection";
			this.Size = new Size(540, 500);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.BackColor = Color.FromArgb(245, 247, 251);
			if (MainForm.DRIcon != null)
				this.Icon = MainForm.DRIcon;

			// Removed duplicate mainPanel declaration from previous layout
			// --- Redesigned layout ---
			this.Controls.Clear();
			this.BackColor = Color.FromArgb(245, 247, 251);

			// Back button (top left, outside main panel)
			var btnBack = new Button();
			btnBack.Text = "← Back";
			btnBack.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
			btnBack.BackColor = Color.FromArgb(220, 53, 69);
			btnBack.ForeColor = Color.White;
			btnBack.FlatStyle = FlatStyle.Flat;
			btnBack.FlatAppearance.BorderSize = 0;
			btnBack.FlatAppearance.MouseDownBackColor = Color.FromArgb(180, 40, 50);
			btnBack.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 40, 50);
			btnBack.Cursor = Cursors.Hand;
			btnBack.Size = new Size(90, 32);
			btnBack.Location = new Point(20, 10);
			btnBack.Anchor = AnchorStyles.Top | AnchorStyles.Left;
			btnBack.TabStop = false;
			btnBack.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
			this.Controls.Add(btnBack);

			// Main content panel
			var mainPanel = new Panel();
			mainPanel.Size = new Size(480, 440);
			mainPanel.Location = new Point(30, 50);
			mainPanel.BackColor = Color.White;
			mainPanel.Padding = new Padding(24);
			mainPanel.BorderStyle = BorderStyle.None;
			mainPanel.Paint += (s, e) => {
				var g = e.Graphics;
				var rect = mainPanel.ClientRectangle;
				using (var pen = new Pen(Color.FromArgb(160, 160, 160), 1))
				{
					g.DrawRectangle(pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
				}
			};
			mainPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

			var mainLayout = new TableLayoutPanel();
			mainLayout.Dock = DockStyle.Fill;
			mainLayout.RowCount = 4;
			mainLayout.ColumnCount = 1;
			mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); // Icon
			mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Header
			mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120)); // Buttons
			mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Footer

			// Large icon
			var iconLabel = new Label();
			iconLabel.Text = "🗂️";
			iconLabel.Font = new Font("Segoe UI Emoji", 54F, FontStyle.Bold);
			iconLabel.Dock = DockStyle.Fill;
			iconLabel.TextAlign = ContentAlignment.MiddleCenter;
			mainLayout.Controls.Add(iconLabel, 0, 0);

			// Header
			var headerLabel = new Label();
			headerLabel.Text = "Select a Case Option";
			headerLabel.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
			headerLabel.ForeColor = Color.FromArgb(40, 40, 40);
			headerLabel.Dock = DockStyle.Fill;
			headerLabel.TextAlign = ContentAlignment.MiddleCenter;
			mainLayout.Controls.Add(headerLabel, 0, 1);

			// Button panel (vertical stack)
			var buttonPanel = new TableLayoutPanel();
			buttonPanel.Dock = DockStyle.Fill;
			buttonPanel.RowCount = 2;
			buttonPanel.ColumnCount = 1;
			buttonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
			buttonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
			buttonPanel.Padding = new Padding(10, 10, 10, 10);

			var btnCreateCase = new Button();
			btnCreateCase.Text = "Create New Case";
			btnCreateCase.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
			btnCreateCase.BackColor = Color.FromArgb(0, 122, 255);
			btnCreateCase.ForeColor = Color.White;
			btnCreateCase.Dock = DockStyle.Fill;
			btnCreateCase.FlatStyle = FlatStyle.Flat;
			btnCreateCase.FlatAppearance.BorderSize = 0;
			btnCreateCase.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 90, 200);
			btnCreateCase.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 90, 200);
			btnCreateCase.Margin = new Padding(10, 5, 10, 5);
			btnCreateCase.Cursor = Cursors.Hand;
			btnCreateCase.TabStop = false;
			btnCreateCase.Click += (s, e) => { this.DialogResult = DialogResult.Yes; this.Close(); };
			btnCreateCase.MouseEnter += (s, e) => btnCreateCase.BackColor = Color.FromArgb(0, 90, 200);
			btnCreateCase.MouseLeave += (s, e) => btnCreateCase.BackColor = Color.FromArgb(0, 122, 255);

			var btnOpenCase = new Button();
			btnOpenCase.Text = "Open Existing Case";
			btnOpenCase.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
			btnOpenCase.BackColor = Color.FromArgb(220, 53, 69); // Red
			btnOpenCase.ForeColor = Color.White;
			btnOpenCase.Dock = DockStyle.Fill;
			btnOpenCase.TabStop = false;
			btnOpenCase.FlatStyle = FlatStyle.Flat;
			btnOpenCase.FlatAppearance.BorderSize = 0;
			btnOpenCase.Margin = new Padding(10, 5, 10, 5);
			btnOpenCase.Cursor = Cursors.Hand;
			btnOpenCase.Click += (s, e) => { this.DialogResult = DialogResult.No; this.Close(); };
			btnOpenCase.FlatAppearance.MouseDownBackColor = Color.FromArgb(180, 40, 50);
			btnOpenCase.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 40, 50);
			btnOpenCase.MouseEnter += (s, e) => btnOpenCase.BackColor = Color.FromArgb(180, 40, 50);
			btnOpenCase.MouseLeave += (s, e) => btnOpenCase.BackColor = Color.FromArgb(220, 53, 69);

			buttonPanel.Controls.Add(btnCreateCase, 0, 0);
			buttonPanel.Controls.Add(btnOpenCase, 0, 1);
			mainLayout.Controls.Add(buttonPanel, 0, 2);

			var footerLabel = new Label();
			footerLabel.Text = "Tip: You can manage cases later from the File menu.";
			footerLabel.Font = new Font("Segoe UI", 10F, FontStyle.Italic);
			footerLabel.ForeColor = Color.Gray;
			footerLabel.Dock = DockStyle.Fill;
			footerLabel.TextAlign = ContentAlignment.MiddleCenter;
			mainLayout.Controls.Add(footerLabel, 0, 3);

			mainPanel.Controls.Add(mainLayout);
			this.Controls.Add(mainPanel);
		}
	}
	}
