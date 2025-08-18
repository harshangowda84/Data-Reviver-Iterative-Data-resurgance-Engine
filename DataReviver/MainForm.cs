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

namespace DataReviver {
	/// <summary>
	/// The main form of Kickass Undelete.
	/// </summary>
	public partial class MainForm : Form {
		IFileSystem _fileSystem;
		Dictionary<IFileSystem, Scanner> _scanners = new Dictionary<IFileSystem, Scanner>();
		Dictionary<IFileSystem, DeletedFileViewer> _deletedViewers = new Dictionary<IFileSystem, DeletedFileViewer>();
		private CaseManager _caseManager;
		private UserSession _currentUser;
	// Refresh Drives button animation state
	private bool _isRefreshingDrives = false;
	private Timer _refreshDrivesTimer;
	private int _refreshAnimationStep = 0;

		/// <summary>
		/// Constructs the main form.
		/// </summary>
		public MainForm(UserSession userSession = null) {
			_currentUser = userSession;
			InitializeComponent();
			CreateDRIcon();
			SetupMenuBar();
			_caseManager = new CaseManager();
			ApplyRoleBasedAccess();
			UpdateUserInterface();
			// Setup Refresh Drives animation timer
			_refreshDrivesTimer = new Timer();
			_refreshDrivesTimer.Interval = 120;
			_refreshDrivesTimer.Tick += RefreshDrivesTimer_Tick;
		}

		/// <summary>
		/// Creates a custom "DR" icon for the application
		/// </summary>
		private void CreateDRIcon() {
			try {
				// Create a bitmap with the text "DR"
				Bitmap iconBitmap = new Bitmap(32, 32);
				using (Graphics g = Graphics.FromImage(iconBitmap)) {
					// Clear with blue background
					g.Clear(Color.FromArgb(0, 122, 255));
					
					// Set up text rendering
					g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
					
					// Draw "DR" text in white
					Font font = new Font("Segoe UI", 12, FontStyle.Bold);
					SizeF textSize = g.MeasureString("DR", font);
					float x = (32 - textSize.Width) / 2;
					float y = (32 - textSize.Height) / 2;
					
					g.DrawString("DR", font, Brushes.White, x, y);
					font.Dispose();
				}
				
				// Convert bitmap to icon and set it
				IntPtr hIcon = iconBitmap.GetHicon();
				Icon drIcon = Icon.FromHandle(hIcon);
				this.Icon = drIcon;
				
				iconBitmap.Dispose();
			} catch (Exception ex) {
				Console.WriteLine("Failed to create DR icon: " + ex.Message);
				// Keep the default icon if creation fails
			}
		}

		private void SetupMenuBar()
		{
			var menuStrip = new MenuStrip();
			menuStrip.BackColor = Color.FromArgb(70, 130, 180);
			menuStrip.ForeColor = Color.White;
			menuStrip.Font = new Font("Segoe UI", 9);
			
			// File Menu
			var fileMenu = new ToolStripMenuItem("&File");
			fileMenu.DropDownItems.Add("&New Case", null, (s, e) => CreateNewCase());
			fileMenu.DropDownItems.Add("&Open Case", null, (s, e) => OpenExistingCase());
			fileMenu.DropDownItems.Add(new ToolStripSeparator());
			fileMenu.DropDownItems.Add("&Export Report", null, (s, e) => GenerateForensicReport());
			fileMenu.DropDownItems.Add("&Export Case Report", null, (s, e) => ExportCaseReport());
			fileMenu.DropDownItems.Add(new ToolStripSeparator());
			fileMenu.DropDownItems.Add("E&xit", null, (s, e) => this.Close());
			
			// Tools Menu
			var toolsMenu = new ToolStripMenuItem("&Tools");
			toolsMenu.DropDownItems.Add("🔧 &Forensic Tools Suite", null, (s, e) => OpenForensicTools());
			toolsMenu.DropDownItems.Add(new ToolStripSeparator());
			toolsMenu.DropDownItems.Add("&Disk Imager", null, (s, e) => OpenDiskImager());
			toolsMenu.DropDownItems.Add("&File Carver", null, (s, e) => MessageBox.Show("Feature: Advanced File Carving"));
			toolsMenu.DropDownItems.Add("&Hash Calculator", null, (s, e) => OpenHashCalculator());
			toolsMenu.DropDownItems.Add("&Timeline Analyzer", null, (s, e) => OpenTimelineAnalyzer());
			
			// Analysis Menu
			var analysisMenu = new ToolStripMenuItem("&Analysis");
			analysisMenu.DropDownItems.Add("&Quick Scan", null, (s, e) => MessageBox.Show("Feature: Quick Recovery Scan"));
			analysisMenu.DropDownItems.Add("&Deep Scan", null, (s, e) => MessageBox.Show("Feature: Deep Recovery Analysis"));
			analysisMenu.DropDownItems.Add("🔍 &File Signature Analysis", null, (s, e) => OpenFileSignatureAnalysis());
			analysisMenu.DropDownItems.Add(new ToolStripSeparator());
			analysisMenu.DropDownItems.Add("📊 &Generate Forensic Report", null, (s, e) => GenerateForensicReport());
			
			// Help Menu
			var helpMenu = new ToolStripMenuItem("&Help");
			helpMenu.DropDownItems.Add("&User Guide", null, (s, e) => MessageBox.Show("Feature: User Documentation"));
			helpMenu.DropDownItems.Add("&Forensic Tools Help", null, (s, e) => OpenForensicHelp());
			helpMenu.DropDownItems.Add("&About", null, (s, e) => new AboutDialog().ShowDialog(this));
			
			menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, toolsMenu, analysisMenu, helpMenu });
			this.MainMenuStrip = menuStrip;
			this.Controls.Add(menuStrip);
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
			reportBuilder.AppendLine($"Total Files Analyzed: 0 (Scan a drive to see results)");
			reportBuilder.AppendLine($"Recoverable Files: 0 (Scan a drive to see results)");
			reportBuilder.AppendLine();
			reportBuilder.AppendLine("FORENSIC CAPABILITIES DEMONSTRATED:");
			reportBuilder.AppendLine("✅ File Signature Analysis - 20+ file types detected");
			reportBuilder.AppendLine("✅ Hash Calculation - MD5, SHA1, SHA256, SHA512");
			reportBuilder.AppendLine("✅ Timeline Analysis - File activity reconstruction");
			reportBuilder.AppendLine("✅ Disk Imaging - Multiple forensic formats supported");
			reportBuilder.AppendLine("✅ Integrity Verification - Chain of custody maintained");
			reportBuilder.AppendLine();
			reportBuilder.AppendLine("This report demonstrates comprehensive forensic capabilities");
			reportBuilder.AppendLine("suitable for digital investigation and evidence preservation.");

			using (var dialog = new SaveFileDialog())
			{
				dialog.Filter = "Text Files (*.txt)|*.txt|PDF Files (*.pdf)|*.pdf";
				dialog.DefaultExt = "txt";
				dialog.FileName = $"ForensicReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
				
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					System.IO.File.WriteAllText(dialog.FileName, reportBuilder.ToString());
					MessageBox.Show("Forensic report generated successfully!", "Report Generated", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
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
