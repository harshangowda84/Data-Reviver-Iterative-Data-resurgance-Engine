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
			this.Size = new Size(480, 320);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.BackColor = Color.FromArgb(245, 247, 251);

			var mainPanel = new Panel();
			mainPanel.Dock = DockStyle.Fill;
			mainPanel.BackColor = Color.White;
			mainPanel.Padding = new Padding(24);
			mainPanel.BorderStyle = BorderStyle.FixedSingle;

			var mainLayout = new TableLayoutPanel();
			mainLayout.Dock = DockStyle.Fill;
			mainLayout.RowCount = 4;
			mainLayout.ColumnCount = 1;
			mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); // Icon
			mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Header
			mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Buttons
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

			// Button panel
			var buttonPanel = new TableLayoutPanel();
			buttonPanel.Dock = DockStyle.Fill;
			buttonPanel.RowCount = 2;
			buttonPanel.ColumnCount = 2;
			buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
			buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
			buttonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
			buttonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
			buttonPanel.Padding = new Padding(20, 10, 20, 10);

			var btnCreateCase = new Button();
			btnCreateCase.Text = "Create New Case";
			btnCreateCase.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
			btnCreateCase.BackColor = Color.FromArgb(0, 122, 255);
			btnCreateCase.ForeColor = Color.White;
			btnCreateCase.Dock = DockStyle.Fill;
			btnCreateCase.FlatStyle = FlatStyle.Flat;
			btnCreateCase.FlatAppearance.BorderSize = 0;
			btnCreateCase.FlatAppearance.BorderColor = btnCreateCase.BackColor;
			btnCreateCase.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 90, 200);
			btnCreateCase.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 90, 200);
			btnCreateCase.FlatAppearance.CheckedBackColor = btnCreateCase.BackColor;
			btnCreateCase.Margin = new Padding(10);
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
			btnOpenCase.FlatStyle = FlatStyle.Flat;
			btnOpenCase.FlatAppearance.BorderSize = 0;
			btnOpenCase.Margin = new Padding(10);
			btnOpenCase.Cursor = Cursors.Hand;
			btnOpenCase.Click += (s, e) => { this.DialogResult = DialogResult.No; this.Close(); };
			btnOpenCase.FlatAppearance.MouseDownBackColor = Color.FromArgb(180, 40, 50);
			btnOpenCase.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 40, 50);
			btnOpenCase.MouseEnter += (s, e) => btnOpenCase.BackColor = Color.FromArgb(180, 40, 50);
			btnOpenCase.MouseLeave += (s, e) => btnOpenCase.BackColor = Color.FromArgb(220, 53, 69);

			var descCreate = new Label();
			descCreate.Text = "Start a new investigation and create a fresh case folder.";
			descCreate.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
			descCreate.ForeColor = Color.FromArgb(60, 60, 60);
			descCreate.Dock = DockStyle.Fill;
			descCreate.TextAlign = ContentAlignment.TopCenter;
			descCreate.Margin = new Padding(10, 0, 10, 0);

			var descOpen = new Label();
			descOpen.Text = "Browse and open an existing case for review or updates.";
			descOpen.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
			descOpen.ForeColor = Color.FromArgb(60, 60, 60);
			descOpen.Dock = DockStyle.Fill;
			descOpen.TextAlign = ContentAlignment.TopCenter;
			descOpen.Margin = new Padding(10, 0, 10, 0);

			buttonPanel.Controls.Add(btnCreateCase, 0, 0);
			buttonPanel.Controls.Add(btnOpenCase, 1, 0);
			buttonPanel.Controls.Add(descCreate, 0, 1);
			buttonPanel.Controls.Add(descOpen, 1, 1);
			mainLayout.Controls.Add(buttonPanel, 0, 2);

			var footerLabel = new Label();
			footerLabel.Text = "Tip: You can manage cases later from the File menu.";
			footerLabel.Font = new Font("Segoe UI", 10F, FontStyle.Italic);
			footerLabel.ForeColor = Color.Gray;
			footerLabel.Dock = DockStyle.Fill;
			footerLabel.TextAlign = ContentAlignment.MiddleCenter;
			mainLayout.Controls.Add(footerLabel, 0, 3);

			mainPanel.Controls.Add(mainLayout);
			// Add subtle drop shadow effect
			mainPanel.Paint += (s, e) =>
			{
				var g = e.Graphics;
				var rect = mainPanel.ClientRectangle;
				rect.Inflate(-2, -2);
				using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
				{
					g.FillRectangle(shadowBrush, rect);
				}
			};

			this.Controls.Add(mainPanel);
		}
	}
}
