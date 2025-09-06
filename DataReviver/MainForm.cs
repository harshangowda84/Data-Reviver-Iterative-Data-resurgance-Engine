// Copyright (C) 2017  Joey Scarr, Lukas Korsika
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

using KFS.Disks;
using KFS.FileSystems;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DataReviver;

namespace DataReviver {
	/// <summary>
	/// The main form of Kickass Undelete.
	/// </summary>
	public partial class MainForm : Form {
		// Static DR icon for use throughout the app
		private static Icon _drIcon;
		public static Icon DRIcon {
			get {
				return _drIcon != null ? (Icon)_drIcon.Clone() : null;
			}
			private set {
				_drIcon = value;
			}
		}

		// Forensic Report menu item and scan results storage
		private ToolStripMenuItem _generateReportMenuItem;
		private IList<INodeMetadata> _lastScanResults;
		// Call this at app startup to ensure icon is ready for all forms
		public static void GenerateDRIcon() {
			try {
				Bitmap iconBitmap = new Bitmap(32, 32);
				using (Graphics g = Graphics.FromImage(iconBitmap)) {
					g.Clear(Color.FromArgb(0, 122, 255));
					g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
					Font font = new Font("Segoe UI", 12, FontStyle.Bold);
					SizeF textSize = g.MeasureString("DR", font);
					g.DrawString("DR", font, Brushes.White, (32 - textSize.Width) / 2, (32 - textSize.Height) / 2);
				}
				using (var ms = new System.IO.MemoryStream())
				{
					iconBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
					using (var bmp = new Bitmap(ms))
					{
						IntPtr hIcon = bmp.GetHicon();
						DRIcon = Icon.FromHandle(hIcon);
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error creating DR icon: {ex.Message}");
			}
		}
		IFileSystem _fileSystem;
		Dictionary<IFileSystem, Scanner> _scanners = new Dictionary<IFileSystem, Scanner>();
		Dictionary<IFileSystem, DeletedFileViewer> _deletedViewers = new Dictionary<IFileSystem, DeletedFileViewer>();
	private CaseManager _caseManager;
	private UserSession _currentUser;
	private ForensicCase _currentCase;
	// Refresh Drives button animation state
	private bool _isRefreshingDrives = false;
	private Timer _refreshDrivesTimer;
	private int _refreshAnimationStep = 0;

		/// <summary>
		/// Constructs the main form.
		/// </summary>
		public MainForm(UserSession userSession = null, ForensicCase selectedCase = null) {
			_currentUser = userSession;
			_currentCase = selectedCase;
			InitializeComponent();
			// Icon is now generated at app startup
			if (DRIcon != null)
				this.Icon = DRIcon;
			SetupMenuBar();
			_caseManager = new CaseManager();
			ApplyRoleBasedAccess();
			UpdateUserInterface();
			// Show case name and ID in title if available
			if (_currentCase != null)
			{
				this.Text = $"Data Reviver - Forensic Analysis Tool - {_currentCase.CaseName} (Case ID: {_currentCase.CaseId})";
			}
			// Setup Refresh Drives animation timer
			_refreshDrivesTimer = new Timer();
			_refreshDrivesTimer.Interval = 120;
			_refreshDrivesTimer.Tick += RefreshDrivesTimer_Tick;

			// Subscribe to scan finished events for all scanners (if any are created later)
			// This will be handled in SetFileSystem when a new scanner is created
		}

		/// <summary>
		/// Creates a custom "DR" icon for the application
		/// </summary>
		private void CreateDRIcon() {
			try {
				Bitmap iconBitmap = new Bitmap(32, 32);
				using (Graphics g = Graphics.FromImage(iconBitmap)) {
					g.Clear(Color.FromArgb(0, 122, 255));
					g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
					Font font = new Font("Segoe UI", 12, FontStyle.Bold);
					SizeF textSize = g.MeasureString("DR", font);
					g.DrawString("DR", font, Brushes.White, (32 - textSize.Width) / 2, (32 - textSize.Height) / 2);
				}
				using (var ms = new System.IO.MemoryStream())
				{
					iconBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
					using (var bmp = new Bitmap(ms))
					{
						IntPtr hIcon = bmp.GetHicon();
						DRIcon = Icon.FromHandle(hIcon);
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error creating DR icon: {ex.Message}");
			}
		}

		private void LogoutUser()
		{
			using (var confirmDialog = new LogoutConfirmDialog())
			{
				var confirmResult = confirmDialog.ShowDialog(this);
				if (confirmResult == DialogResult.Yes)
				{
					// Hide the main form
					this.Hide();
					// Show the login form modally
					using (var loginForm = new NewLoginForm())
					{
						var result = loginForm.ShowDialog();
						if (result == DialogResult.OK && loginForm.DialogResult == DialogResult.OK)
						{
							// Optionally, you can re-initialize the main form with the new user
							Application.Restart();
						}
						else
						{
							// Exit if login is cancelled
							Application.Exit();
						}
					}
				}
			}
		}
		private void SetupMenuBar()
		{
			var menuStrip = new MenuStrip();
			menuStrip.BackColor = Color.FromArgb(0, 122, 255);
			menuStrip.ForeColor = Color.White;
			menuStrip.Font = new Font("Segoe UI", 13, FontStyle.Bold);
			menuStrip.Padding = new Padding(14, 10, 14, 10);

			// File Menu (with icon)
			var fileMenu = new ToolStripMenuItem("  File");
			fileMenu.Image = new Bitmap(SystemIcons.Application.ToBitmap(), new Size(28, 28));
			fileMenu.ImageScaling = ToolStripItemImageScaling.None;
			fileMenu.Padding = new Padding(12, 0, 12, 0);
			fileMenu.ToolTipText = "File operations";
			// Removed 'New Case' and 'Open Case' from File menu as requested
			fileMenu.DropDownItems.Add(new ToolStripSeparator());
			fileMenu.DropDownItems.Add("Logout", null, (s, e) => LogoutUser());

			// Tools Menu (with icon)
			var toolsMenu = new ToolStripMenuItem("  Tools");
			toolsMenu.Image = new Bitmap(SystemIcons.Shield.ToBitmap(), new Size(28, 28));
			toolsMenu.ImageScaling = ToolStripItemImageScaling.None;
			toolsMenu.Padding = new Padding(12, 0, 12, 0);
			toolsMenu.ToolTipText = "Forensic tools";
			toolsMenu.Click += (s, e) => OpenForensicTools();

			// Analysis Menu (with icon)
			var analysisMenu = new ToolStripMenuItem("  Analysis");
			analysisMenu.Image = new Bitmap(SystemIcons.Information.ToBitmap(), new Size(28, 28));
			analysisMenu.ImageScaling = ToolStripItemImageScaling.None;
			analysisMenu.Padding = new Padding(12, 0, 12, 0);
			analysisMenu.ToolTipText = "Analysis features";
			analysisMenu.DropDownItems.Add(new ToolStripSeparator());
			_generateReportMenuItem = new ToolStripMenuItem("📊 Generate Forensic Report", null, (s, e) => GenerateForensicReport());
			_generateReportMenuItem.Enabled = false; // Disabled by default
			analysisMenu.DropDownItems.Add(_generateReportMenuItem);

			// Help Menu (with icon)
			var helpMenu = new ToolStripMenuItem("  Help");
			helpMenu.Image = new Bitmap(SystemIcons.Question.ToBitmap(), new Size(28, 28));
			helpMenu.ImageScaling = ToolStripItemImageScaling.None;
			helpMenu.Padding = new Padding(12, 0, 12, 0);
			helpMenu.ToolTipText = "Help and documentation";
			helpMenu.DropDownItems.Add("🧰 Forensic Tools Help", null, (s, e) => OpenForensicHelp());
			helpMenu.DropDownItems.Add("ℹ️ About", null, (s, e) => new AboutDialog().ShowDialog(this));

			menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, toolsMenu, analysisMenu, helpMenu });
			this.MainMenuStrip = menuStrip;
			this.Controls.Add(menuStrip);
		}

		// Enhanced renderer for modern menu bar
private class EnhancedMenuRenderer : ToolStripProfessionalRenderer
{
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        Rectangle rect = new Rectangle(Point.Empty, e.Item.Size);
        if (e.ToolStrip is MenuStrip)
        {
            // Top bar: blue and hover effect
            if (e.Item.Selected)
            {
                using (Brush b = new SolidBrush(Color.FromArgb(0, 90, 200)))
                    e.Graphics.FillRectangle(b, rect);
            }
            else
            {
                using (Brush b = new SolidBrush(Color.FromArgb(0, 122, 255)))
                    e.Graphics.FillRectangle(b, rect);
            }
        }
        else
        {
            // Dropdown: default light background
            if (e.Item.Selected)
            {
                using (Brush b = new SolidBrush(Color.FromArgb(230, 240, 255)))
                    e.Graphics.FillRectangle(b, rect);
            }
            else
            {
                using (Brush b = new SolidBrush(SystemColors.Menu))
                    e.Graphics.FillRectangle(b, rect);
            }
        }
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        if (e.ToolStrip is MenuStrip)
        {
            // Draw bottom border for top bar only
            using (Pen p = new Pen(Color.FromArgb(0, 90, 200), 3))
            {
                e.Graphics.DrawLine(p, 0, e.ToolStrip.Height - 2, e.ToolStrip.Width, e.ToolStrip.Height - 2);
            }
        }
        else
        {
            base.OnRenderToolStripBorder(e);
        }
    }
}

		private void ApplyRoleBasedAccess()
		{
			if (_currentUser == null) return;

			// Get the menu strip
			var menuStrip = this.Controls.OfType<MenuStrip>().FirstOrDefault();
			if (menuStrip == null) return;

			// Apply role-based restrictions to menu items
			foreach (ToolStripMenuItem topMenu in menuStrip.Items)
			{
				switch (topMenu.Text)
				{
					case "&File":
						ApplyFileMenuRestrictions(topMenu);
						break;
					case "&Tools":
						ApplyToolsMenuRestrictions(topMenu);
						break;
					case "&Analysis":
						ApplyAnalysisMenuRestrictions(topMenu);
						break;
				}
			}
		}

		private void ApplyFileMenuRestrictions(ToolStripMenuItem fileMenu)
		{
			foreach (ToolStripItem item in fileMenu.DropDownItems)
			{
				if (item.Text.Contains("New Case") && !_currentUser.CanCreateCases)
				{
					item.Enabled = false;
					item.ToolTipText = "Access Denied: Insufficient privileges";
				}
				else if (item.Text.Contains("Export") && !_currentUser.CanExportData)
				{
					item.Enabled = false;
					item.ToolTipText = "Access Denied: Export restricted for your role";
				}
			}
		}

		private void ApplyToolsMenuRestrictions(ToolStripMenuItem toolsMenu)
		{
			foreach (ToolStripItem item in toolsMenu.DropDownItems)
			{
				if (item.Text.Contains("Forensic Tools") && !_currentUser.CanAccessForensicTools)
				{
					item.Enabled = false;
					item.ToolTipText = "Access Denied: Forensic tools restricted";
				}
				else if ((item.Text.Contains("Disk Imager") || item.Text.Contains("File Carver")) && !_currentUser.CanRecoverFiles)
				{
					item.Enabled = false;
					item.ToolTipText = "Access Denied: Recovery tools restricted";
				}
			}
		}

		private void ApplyAnalysisMenuRestrictions(ToolStripMenuItem analysisMenu)
		{
			foreach (ToolStripItem item in analysisMenu.DropDownItems)
			{
				if ((item.Text.Contains("Deep Scan") || item.Text.Contains("Signature Analysis")) && !_currentUser.CanViewSensitiveData)
				{
					item.Enabled = false;
					item.ToolTipText = "Access Denied: Advanced analysis restricted";
				}
			}
		}

		private void UpdateUserInterface()
		{
			if (_currentUser != null)
			{
				// Update the window title to show user info
				this.Text = $"Data Reviver - Forensic Recovery Suite | User: {_currentUser.FullName} ({_currentUser.GetRoleDisplayName()})";
				
				// Add a status bar to show user role
				var statusStrip = new StatusStrip();
				statusStrip.BackColor = Color.FromArgb(70, 130, 180);
				statusStrip.ForeColor = Color.White;
				
				var roleLabel = new ToolStripStatusLabel($"Role: {_currentUser.GetRoleDisplayName()}");
				roleLabel.ForeColor = Color.White;
				
				var permissionsLabel = new ToolStripStatusLabel($"Permissions: {_currentUser.GetPermissionSummary()}");
				permissionsLabel.ForeColor = Color.LightGray;
				
				var loginTimeLabel = new ToolStripStatusLabel($"Login: {_currentUser.LoginTime:HH:mm:ss}");
				loginTimeLabel.ForeColor = Color.LightGray;
				
				statusStrip.Items.AddRange(new ToolStripItem[] { roleLabel, permissionsLabel, loginTimeLabel });
				this.Controls.Add(statusStrip);
			}
		}

		private void MainForm_Load(object sender, EventArgs e) {
			LoadLogicalDisks();
			// Ensure button is enabled on load
			btnRefreshDrives.Enabled = true;
			btnRefreshDrives.BackColor = Color.FromArgb(0, 122, 255);
			btnRefreshDrives.Text = "⟳ Refresh Drives";
		}

		private void LoadLogicalDisks() {
			foreach (Disk disk in DiskLoader.LoadLogicalVolumes()) {
				TreeNode node = new TreeNode(disk.ToString());
				Console.WriteLine("Added disk: " + disk.ToString());
				node.Tag = disk;
				node.ImageKey = "HDD";
				if (((IFileSystemStore)disk).FS == null) {
					node.ForeColor = Color.Gray;
				}
				diskTree.Nodes.Add(node);
			}
			// If called from refresh, re-enable button
			if (_isRefreshingDrives)
			{
				_isRefreshingDrives = false;
				_refreshDrivesTimer.Stop();
				btnRefreshDrives.Enabled = true;
				btnRefreshDrives.BackColor = Color.FromArgb(0, 122, 255);
				btnRefreshDrives.Text = "⟳ Refresh Drives";
			}
		}

		private void diskTree_AfterSelect(object sender, TreeViewEventArgs e) {
			SetFileSystem((IFileSystemStore)e.Node.Tag);
		}

		private void SetFileSystem(IFileSystemStore logicalDisk) {
			if (logicalDisk.FS != null) {
				if (!_scanners.ContainsKey(logicalDisk.FS)) {
					_scanners[logicalDisk.FS] = new Scanner(logicalDisk.ToString(), logicalDisk.FS);
					_deletedViewers[logicalDisk.FS] = new DeletedFileViewer(_scanners[logicalDisk.FS]);
					AddDeletedFileViewer(_deletedViewers[logicalDisk.FS]);

					// Subscribe to scan finished event to enable report button and store results
					_scanners[logicalDisk.FS].ScanFinished += (sender, e) =>
					{
						// Store the latest scan results
						_lastScanResults = _scanners[logicalDisk.FS].GetDeletedFiles();
						// Enable the report button
						if (_generateReportMenuItem != null)
						{
							_generateReportMenuItem.Enabled = _lastScanResults != null && _lastScanResults.Count > 0;
						}
					};
				}
				if (_fileSystem != null && _scanners.ContainsKey(_fileSystem)) {
					_deletedViewers[_fileSystem].Hide();
				}
				_fileSystem = logicalDisk.FS;
				_deletedViewers[logicalDisk.FS].Show();
			}
		}

		private void AddDeletedFileViewer(DeletedFileViewer viewer) {
			int MARGIN = 12;
			splitContainer1.Panel2.Controls.Add(viewer);
			viewer.Top = viewer.Left = MARGIN;
			viewer.Width = splitContainer1.Panel2.Width - MARGIN * 2;
			viewer.Height = splitContainer1.Panel2.Height - MARGIN * 2;
			viewer.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
		}

		private void MainForm_FormClosed(object sender, FormClosedEventArgs e) {
			foreach (Scanner state in _scanners.Values) {
				state.CancelScan();
			}
			// Default close behavior: exit application
			Application.Exit();
		}

		private void diskTree_BeforeSelect(object sender, TreeViewCancelEventArgs e) {
			if (((IFileSystemStore)e.Node.Tag).FS == null) {
				e.Cancel = true;
			}
		}

		private void diskTree_DrawNode(object sender, DrawTreeNodeEventArgs e) {
			// Custom drawing to maintain full highlight color
			if ((e.State & TreeNodeStates.Selected) != 0) {
				// Draw selected node with full blue background
				using (Brush brush = new SolidBrush(Color.FromArgb(0, 122, 255))) {
					e.Graphics.FillRectangle(brush, e.Bounds);
				}
				TextRenderer.DrawText(e.Graphics, e.Node.Text, diskTree.Font, e.Bounds, Color.White, TextFormatFlags.VerticalCenter);
			}
			else {
				// Draw normal node
				e.DrawDefault = true;
			}
		}

		// Animated Refresh Drives button logic
		private void btnRefreshDrives_Click(object sender, EventArgs e)
		{
			if (_isRefreshingDrives) return;
			_isRefreshingDrives = true;
			btnRefreshDrives.Enabled = false;
			btnRefreshDrives.BackColor = Color.Orange;
			btnRefreshDrives.Text = "⏳ Refreshing...";
			_refreshAnimationStep = 0;
			_refreshDrivesTimer.Start();

			// Clear and reload drives
			diskTree.Nodes.Clear();
			// Simulate delay for animation (can be replaced with async if needed)
			LoadLogicalDisks();
		}

		private void RefreshDrivesTimer_Tick(object sender, EventArgs e)
		{
			// Animate button text with dots
			string[] dots = { "", ".", "..", "..." };
			btnRefreshDrives.Text = $"⏳ Refreshing{dots[_refreshAnimationStep % dots.Length]}";
			_refreshAnimationStep++;
		}

		// Forensic Tools Menu Handlers
		private void OpenForensicTools()
		{
			try
			{
				var forensicForm = new RealForensicTools();
				forensicForm.ShowDialog(this);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error opening forensic tools: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OpenFileSignatureAnalysis()
		{
			try
			{
				var forensicForm = new RealForensicTools();
				forensicForm.ShowDialog(this);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error opening file signature analysis: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OpenHashCalculator()
		{
			try
			{
				var forensicForm = new RealForensicTools();
				forensicForm.ShowDialog(this);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error opening hash calculator: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OpenTimelineAnalyzer()
		{
			try
			{
				var forensicForm = new RealForensicTools();
				forensicForm.ShowDialog(this);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error opening timeline analyzer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OpenDiskImager()
		{
			try
			{
				var forensicForm = new RealForensicTools();
				forensicForm.ShowDialog(this);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error opening forensic tools: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void GenerateForensicReport()
		{
			var reportBuilder = new System.Text.StringBuilder();
			reportBuilder.AppendLine("=== DATA REVIVER FORENSIC ANALYSIS REPORT ===");
			reportBuilder.AppendLine($"Generated: {DateTime.Now}");
			reportBuilder.AppendLine($"Examiner: {Environment.UserName}");
			reportBuilder.AppendLine($"Tool Version: Data Reviver v1.0");
			reportBuilder.AppendLine();
			reportBuilder.AppendLine("SCAN SUMMARY:");

			if (_lastScanResults != null && _lastScanResults.Count > 0)
			{
				reportBuilder.AppendLine($"Total Files Analyzed: {_lastScanResults.Count}");
				int recoverable = _lastScanResults.Count(md => md.ChanceOfRecovery == FileRecoveryStatus.Recoverable || md.ChanceOfRecovery == FileRecoveryStatus.Resident);
				reportBuilder.AppendLine($"Recoverable Files: {recoverable}");
				// Optionally, add a table of files
				reportBuilder.AppendLine();
				reportBuilder.AppendLine("DELETED FILES:");
				reportBuilder.AppendLine("Name\tType\tSize\tLast Modified\tPath\tRecovery Status");
				foreach (var md in _lastScanResults)
				{
					var node = md.GetFileSystemNode();
					reportBuilder.AppendLine($"{md.Name}\t{System.IO.Path.GetExtension(md.Name)}\t{node.Size}\t{md.LastModified}\t{node.Path}\t{md.ChanceOfRecovery}");
				}
			}
			else
			{
				reportBuilder.AppendLine($"Total Files Analyzed: 0 (Scan a drive to see results)");
				reportBuilder.AppendLine($"Recoverable Files: 0 (Scan a drive to see results)");
			}

			reportBuilder.AppendLine();
			reportBuilder.AppendLine("This report demonstrates comprehensive forensic capabilities");
			reportBuilder.AppendLine("suitable for digital investigation and evidence preservation.");

				// Automatically save the report in the current case folder
				if (_currentCase != null && !string.IsNullOrEmpty(_currentCase.CaseFolderPath))
				{
					string reportPath = System.IO.Path.Combine(_currentCase.CaseFolderPath, $"ForensicReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
					System.IO.File.WriteAllText(reportPath, reportBuilder.ToString());
					// Use a custom dialog for consistent UI
					using (var dialog = new SuccessDialogForm($"Forensic report generated and saved to:\n{reportPath}"))
					{
						dialog.Text = "Report Generated";
						dialog.ShowDialog();
					}
				}
				else
				{
					MessageBox.Show("No case folder found. Cannot save report.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
		}

		private void OpenForensicHelp()
		{
			var helpText = @"=== DATA REVIVER FORENSIC TOOLS HELP ===

🔧 FORENSIC TOOLS SUITE:
Complete integrated forensic analysis environment

🔍 FILE SIGNATURE ANALYSIS:
- Detects 20+ file types by binary signatures
- Identifies encrypted files through entropy analysis
- Extracts metadata from images, documents, executables
- Provides confidence scoring for file type identification

🔐 HASH CALCULATOR:
- Calculates MD5, SHA1, SHA256, SHA512 hashes
- Verifies file integrity against known databases
- Detects malware signatures
- Supports batch processing

🕒 TIMELINE ANALYZER:
- Reconstructs file system activity timeline
- Detects suspicious patterns (mass deletion, late-night activity)
- Exports timeline in CSV, JSON, XML formats
- Identifies key forensic events

💾 DISK IMAGER:
- Creates forensic disk images in multiple formats (RAW, DD, E01, AFF, VHD)
- Maintains chain of custody with hash verification
- Supports compression and metadata preservation
- Progress monitoring and error handling

ACADEMIC PROJECT FEATURES:
This tool demonstrates advanced forensic concepts suitable for
MCA final year project presentation, including:
- Digital evidence preservation
- Forensic file analysis
- Timeline reconstruction
- Hash-based integrity verification
- Professional reporting capabilities";

			MessageBox.Show(helpText, "Forensic Tools Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CreateNewCase()
		{
			try
			{
				var newCase = _caseManager.CreateNewCase();
				if (newCase != null)
				{
					UpdateCaseStatus($"Case Created: {newCase.CaseName} (ID: {newCase.CaseId})");
					
					// Show success message
					var result = MessageBox.Show($"New forensic case created successfully!\n\nCase ID: {newCase.CaseId}\nCase Name: {newCase.CaseName}\nInvestigator: {newCase.InvestigatorName}\n\nWould you like to open the Advanced Case Manager?", 
						"Case Created", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
					
					// Open advanced case manager if user wants
					if (result == DialogResult.Yes)
					{
						var advancedManager = new AdvancedCaseManager(newCase);
						advancedManager.ShowDialog(this);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error creating new case: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OpenExistingCase()
		{
			try
			{
				var existingCase = _caseManager.OpenExistingCase();
				if (existingCase != null)
				{
					UpdateCaseStatus($"Case Opened: {existingCase.CaseName} (ID: {existingCase.CaseId})");
					
					// Show case info and option to open advanced manager
					var result = MessageBox.Show($"Case opened successfully!\n\nCase ID: {existingCase.CaseId}\nCase Name: {existingCase.CaseName}\nEvidence Items: {existingCase.EvidenceItems.Count}\nLast Modified: {existingCase.LastModified}\n\nWould you like to open the Advanced Case Manager?", 
						"Case Opened", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
					
					// Open advanced case manager if user wants
					if (result == DialogResult.Yes)
					{
						var advancedManager = new AdvancedCaseManager(existingCase);
						advancedManager.ShowDialog(this);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error opening case: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void UpdateCaseStatus(string status)
		{
			// Update the main form title to show current case
			if (_caseManager.CurrentCase != null)
			{
				this.Text = $"Data Reviver - Forensic Analysis Tool - {_caseManager.CurrentCase.CaseName} (Case ID: {_caseManager.CurrentCase.CaseId})";
			}
			else
			{
				this.Text = "Data Reviver - Forensic Analysis Tool";
			}
		}

		public void AddCurrentFileToCase(string filePath)
		{
			try
			{
				_caseManager.AddEvidence(filePath);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error adding file to case: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void ExportCaseReport()
		{
			try
			{
				_caseManager.GenerateCaseReport();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error exporting case report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}
}
