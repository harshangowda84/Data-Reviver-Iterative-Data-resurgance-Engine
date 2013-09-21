// Copyright (C) 2011  Joey Scarr, Lukas Korsika
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
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FileSystems.FileSystem;
using System.Threading;
using KFA.DataStream;
using System.IO;
using GuiComponents;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Collections;
using System.Diagnostics;

namespace KickassUndelete {
	/// <summary>
	/// A custom GUI control for viewing a list of deleted files.
	/// Acts as the View to a ScanState object.
	/// </summary>
	public partial class DeletedFileViewer : UserControl {
		private const string EMPTY_FILTER_TEXT = "Enter filter text here...";
		private Dictionary<FileRecoveryStatus, string> m_RecoveryDescriptions =
				new Dictionary<FileRecoveryStatus, string>() {
                {FileRecoveryStatus.Unknown,"Unknown"},
                {FileRecoveryStatus.Overwritten,"Impossible"},
                {FileRecoveryStatus.Recoverable,"Recoverable"},
                {FileRecoveryStatus.ProbablyRecoverable,"Possibly recoverable"},
                {FileRecoveryStatus.PartiallyRecoverable,"Partially recoverable (may be corrupt)"}};
		private Dictionary<FileRecoveryStatus, Color> m_RecoveryColors =
				new Dictionary<FileRecoveryStatus, Color>() {
                {FileRecoveryStatus.Unknown,Color.FromArgb(255,222,168)}, // Orange
                {FileRecoveryStatus.Overwritten,Color.FromArgb(255,130,130)}, // Red
                {FileRecoveryStatus.Recoverable,Color.FromArgb(190,255,180)}, // Green
                {FileRecoveryStatus.ProbablyRecoverable,Color.FromArgb(255,222,168)}, // Green
                {FileRecoveryStatus.PartiallyRecoverable,Color.FromArgb(255,222,168)}}; // Orange

		private HashSet<string> m_SystemFileExtensions =
				new HashSet<string>() { ".DLL", ".TMP", ".CAB", ".LNK", ".LOG", ".EXE", ".XML", ".INI" };

		private ScanState m_ScanState;
		private FileSavingQueue m_FileSavingQueue;
		private ProgressPopup m_ProgressPopup;
		private string m_MostRecentlySavedFile;
		private string m_Filter = "";
		private bool m_MatchUnknownFileTypes = false;
		private bool m_Scanning;

		private List<ListViewItem> m_Files = new List<ListViewItem>();

		private ListViewColumnSorter lvwColumnSorter;

		private Dictionary<string, ExtensionInfo> m_ExtensionMap;
		private ImageList m_ImageList;

		/// <summary>
		/// Constructs a DeletedFileViewer, using a given ScanState.
		/// </summary>
		/// <param name="state">The ScanState that will be the model for this DeletedFileViewer.</param>
		public DeletedFileViewer(ScanState state) {
			InitializeComponent();

			lvwColumnSorter = new ListViewColumnSorter();
			fileView.ListViewItemSorter = lvwColumnSorter;
			m_ExtensionMap = new Dictionary<string, ExtensionInfo>();
			m_ImageList = new ImageList();
			fileView.SmallImageList = m_ImageList;

			m_ScanState = state;
			state.ProgressUpdated += new EventHandler(state_ProgressUpdated);
			state.ScanStarted += new EventHandler(state_ScanStarted);
			state.ScanFinished += new EventHandler(state_ScanFinished);

			m_FileSavingQueue = new FileSavingQueue();
			m_FileSavingQueue.Finished += m_FileSavingQueue_Finished;
			m_ProgressPopup = new ProgressPopup(m_FileSavingQueue);

			UpdateFilterTextBox();
		}

		/// <summary>
		/// Updates the filter textbox to show the "Enter filter text" message.
		/// </summary>
		private void UpdateFilterTextBox() {
			if (tbFilter.Text.Length == 0 || tbFilter.Text == EMPTY_FILTER_TEXT) {
				tbFilter.Text = EMPTY_FILTER_TEXT;
				tbFilter.ForeColor = Color.Gray;
				tbFilter.Font = new Font(tbFilter.Font, FontStyle.Italic);
			} else {
				tbFilter.ForeColor = Color.Black;
				tbFilter.Font = new Font(tbFilter.Font, FontStyle.Regular);
			}
		}

		/// <summary>
		/// Handles a scan starting.
		/// </summary>
		void state_ScanStarted(object sender, EventArgs ea) {
			m_Scanning = true;
			try {
				this.Invoke(new Action(() => {
					SetScanButtonScanning();
					UpdateTimer.Start();
				}));
			} catch (InvalidOperationException exc) { Console.WriteLine(exc); }
		}

		/// <summary>
		/// Handles a scan finishing.
		/// </summary>
		void state_ScanFinished(object sender, EventArgs ea) {
			try {
				this.Invoke(new Action(() =>
				{
					foreach (ListViewItem item in m_Files) {
						item.SubItems[4].Text = ((INodeMetadata)item.Tag).GetFileSystemNode().Path;
					}

					SetScanButtonFinished();
					UpdateTimer.Stop();
					UpdateTimer_Tick(null, null);
					fileView.BeginUpdate();
					fileView.Items.Clear();
					fileView.Items.AddRange(m_Files.Where(FilterMatches).ToArray());
					fileView.EndUpdate();
				}));
				m_Scanning = false;
			} catch (InvalidOperationException exc) { Console.WriteLine(exc); }
		}

		/// <summary>
		/// Handles a progress report from the underlying ScanState.
		/// </summary>
		void state_ProgressUpdated(object sender, EventArgs ea) {
			try {
				this.BeginInvoke(new Action(() => {
					SetProgress(m_ScanState.Progress);
				}));
			} catch (InvalidOperationException exc) { Console.WriteLine(exc); }
		}

		/// <summary>
		/// Disables the scan button and shows the progress bar.
		/// </summary>
		public void SetScanButtonScanning() {
			bScan.Enabled = false;
			bScan.Text = "Scanning...";
			progressBar.Show();
		}

		/// <summary>
		/// Hides the progress bar and sets the scan button to "Finished".
		/// </summary>
		public void SetScanButtonFinished() {
			bScan.Enabled = false;
			bScan.Text = "Finished Scanning!";
			progressBar.Hide();
			bRestoreFiles.Show();
		}

		private void bScan_Click(object sender, EventArgs e) {
			m_ScanState.StartScan();
		}

		/// <summary>
		/// Constructs a list of ListViewItems based on the files retrieved by the
		/// underlying ScanState.
		/// </summary>
		/// <param name="metadatas">A list of the metadata for each deleted file found.</param>
		/// <returns>An array of ListViewItems.</returns>
		private List<ListViewItem> MakeListItems(IList<INodeMetadata> metadatas) {
			List<ListViewItem> items = new List<ListViewItem>(metadatas.Count);
			for (int i = 0; i < metadatas.Count; i++) {
				ListViewItem item = MakeListItem(metadatas[i]);
				items.Add(item);
			}
			return items;
		}

		/// <summary>
		/// Constructs a ListViewItem from an underlying INodeMetadata model.
		/// </summary>
		/// <param name="metadata">The metadata to create a view for.</param>
		/// <returns>The constructed ListViewItem.</returns>
		private ListViewItem MakeListItem(INodeMetadata metadata) {
			FileSystemNode node = metadata.GetFileSystemNode();
			string ext = "";
			try {
				ext = Path.GetExtension(metadata.Name);
			} catch (ArgumentException exc) { Console.WriteLine(exc); }
			if (!m_ExtensionMap.ContainsKey(ext)) {
				m_ExtensionMap[ext] = new ExtensionInfo(ext);
			}
			ExtensionInfo extInfo = m_ExtensionMap[ext];
			if (extInfo.Image != null) {
				if (!m_ImageList.Images.ContainsKey(ext)) {
					m_ImageList.Images.Add(ext, extInfo.Image);
				}
			}
			ListViewItem lvi = new ListViewItem(new string[] {
                metadata.Name,
                extInfo.FriendlyName,
                Util.ByteFormat(node.Size),
                metadata.LastModified.ToString(CultureInfo.CurrentCulture),
				node.Path,
                m_RecoveryDescriptions[metadata.GetChanceOfRecovery()]
            });
			lvi.BackColor = m_RecoveryColors[metadata.GetChanceOfRecovery()];

			lvi.ImageKey = ext;
			lvi.Tag = metadata;
			return lvi;
		}

		/// <summary>
		/// Sets the progress of the scan progress bar.
		/// </summary>
		/// <param name="progress"></param>
		private void SetProgress(double progress) {
			progressBar.Value = (int)(progress * progressBar.Maximum);
		}

		/// <summary>
		/// Filter the ListView by a filter string.
		/// </summary>
		/// <param name="filter">The string to filter by.</param>
		/// <param name="showUnknownFileTypes">Whether to show unknown file types.</param>
		private void FilterBy(string filter, bool showUnknownFileTypes) {
			string upperFilter = filter.ToUpperInvariant();
			if (m_Filter != upperFilter
					|| showUnknownFileTypes != m_MatchUnknownFileTypes) {
				// Check whether the new filter is more restrictive than the old filter.
				// If so, only iterate over the displayed list items and remove the ones that don't match.
				if (upperFilter.StartsWith(m_Filter) && (showUnknownFileTypes == m_MatchUnknownFileTypes || !showUnknownFileTypes)) {
					m_Filter = upperFilter;
					m_MatchUnknownFileTypes = showUnknownFileTypes;

					fileView.BeginUpdate();
					// THis is premature optimization
					for (int i = 0; i < fileView.Items.Count; i++) {
						if (!FilterMatches(fileView.Items[i])) {
							fileView.Items.RemoveAt(i);
							i--;
						}
					}
					fileView.EndUpdate();
				} else {

					m_Filter = upperFilter;
					m_MatchUnknownFileTypes = showUnknownFileTypes;

					fileView.BeginUpdate();
					fileView.Items.Clear();
					fileView.Items.AddRange(m_Files.Where(FilterMatches).ToArray());
					fileView.EndUpdate();
				}
			}
		}

		/// <summary>
		/// Returns whether the current filter text matches a list view item.
		/// </summary>
		/// <param name="item">The list item to check.</param>
		/// <returns>Whether this list item matches the filter text.</returns>
		private bool FilterMatches(ListViewItem item) {
			return (item.SubItems[0].Text.ToUpperInvariant().Contains(m_Filter)
							|| item.SubItems[1].Text.ToUpperInvariant().Contains(m_Filter))
					&& (m_MatchUnknownFileTypes
							|| !IsSystemOrUnknownFile(item));
		}

		private bool IsSystemOrUnknownFile(ListViewItem item) {
			try {
				string ext = Path.GetExtension(item.SubItems[0].Text);
				return !m_ExtensionMap.ContainsKey(ext)
						|| m_ExtensionMap[ext].UnrecognisedExtension
						|| m_SystemFileExtensions.Contains(ext.ToUpper());
			} catch (ArgumentException) {
				return true;
			}
		}

		private void UpdateTimer_Tick(object sender, EventArgs e) {
			IList<INodeMetadata> deletedFiles = m_ScanState.GetDeletedFiles();
			int fileCount = deletedFiles.Count;
			if (fileCount > m_Files.Count) {
				var items = MakeListItems(deletedFiles.GetRange(m_Files.Count, fileCount - m_Files.Count));
				m_Files.AddRange(items);
				fileView.BeginUpdate();
				fileView.Items.AddRange(items.Where(FilterMatches).ToArray());
				fileView.EndUpdate();
			}
		}

		private void fileView_MouseClick(object sender, MouseEventArgs e) {
			if (e.Button == MouseButtons.Right) {
				if (fileView.SelectedItems.Count == 1) {
					INodeMetadata metadata = fileView.SelectedItems[0].Tag as INodeMetadata;
					if (metadata != null) {
						ContextMenu menu = new ContextMenu();
						MenuItem recoverFile = new MenuItem("Recover File...", new EventHandler(delegate(object o, EventArgs ea) {
							PromptUserToSaveFile(metadata);
						}));
						recoverFile.Enabled = !m_Scanning;
						menu.MenuItems.Add(recoverFile);
						menu.Show(fileView, e.Location);
					}
				} else if (fileView.SelectedItems.Count > 1) {
					// We need slightly different behaviour to save multiple files.
					ContextMenu menu = new ContextMenu();
					MenuItem recoverFiles = new MenuItem("Recover Files...", new EventHandler(delegate(object o, EventArgs ea) {
						PromptUserToSaveFiles(fileView.SelectedItems);
					}));
					recoverFiles.Enabled = !m_Scanning;
					menu.MenuItems.Add(recoverFiles);
					menu.Show(fileView, e.Location);
				}
			}
		}

		private void PromptUserToSaveFile(INodeMetadata metadata) {
			if (metadata != null) {
				SaveFileDialog saveFileDialog = new SaveFileDialog();
				saveFileDialog.OverwritePrompt = true;
				saveFileDialog.InitialDirectory = Environment.ExpandEnvironmentVariables("%SystemDrive");
				saveFileDialog.FileName = metadata.Name;
				saveFileDialog.Filter = "Any Files|*.*";
				saveFileDialog.Title = "Select a Location";

				if (saveFileDialog.ShowDialog() == DialogResult.OK) {
					// Check that the drive isn't the same as the drive being copied from.
					if (saveFileDialog.FileName[0] != m_ScanState.DiskName[0]
						|| MessageBox.Show("WARNING: You are about to save this file to the same disk you are " +
						"trying to recover from. This may cause recovery to fail, and overwrite your data " +
						"permanently! Are you sure you wish to continue?", "Warning!",
						MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {

						FileSystemNode node = metadata.GetFileSystemNode();
						SaveSingleFile(node, saveFileDialog.FileName);
					}
				}
			}
		}

		/// <summary>
		/// Recovers a single file to the specified filepath.
		/// </summary>
		/// <param name="node">The file to recover.</param>
		/// <param name="filePath">The path to save the file to.</param>
		private void SaveSingleFile(FileSystemNode node, string filePath) {
			m_MostRecentlySavedFile = filePath;
			if (!m_ProgressPopup.Visible) {
				m_ProgressPopup.Show(this);
			}
			m_FileSavingQueue.Push(filePath, node);
		}

		private void PromptUserToSaveFiles(IEnumerable items) {
			FolderBrowserDialog folderDialog = new FolderBrowserDialog();

			if (folderDialog.ShowDialog() == DialogResult.OK) {
				// Check that the drive isn't the same as the drive being copied from.
				if (folderDialog.SelectedPath[0] != m_ScanState.DiskName[0]
					|| MessageBox.Show("WARNING: You are about to save this file to the same disk you are " +
					"trying to recover from. This may cause recovery to fail, and overwrite your data " +
					"permanently! Are you sure you wish to continue?", "Warning!",
					MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {

					List<FileSystemNode> nodes = new List<FileSystemNode>();
					foreach (ListViewItem item in items) {
						INodeMetadata metadata = item.Tag as INodeMetadata;
						if (metadata != null) {
							nodes.Add(metadata.GetFileSystemNode());
						}
					}
					SaveMultipleFiles(nodes, folderDialog.SelectedPath);
				}
			}
		}

		/// <summary>
		/// Recovers multiple files into the specified folder.
		/// </summary>
		/// <param name="nodes">The files to recover.</param>
		/// <param name="folderPath">The folder in which to save the recovered files.</param>
		private void SaveMultipleFiles(IEnumerable<FileSystemNode> nodes, string folderPath) {
			foreach (FileSystemNode node in nodes) {
				string file = node.Name;
				string fileName = Path.Combine(folderPath, file);
				SaveSingleFile(node, fileName);
			}
		}

		private void m_FileSavingQueue_Finished() {
			if (!string.IsNullOrEmpty(m_MostRecentlySavedFile)) {
				Process.Start("explorer.exe", "/select, \"" + m_MostRecentlySavedFile + '"');
			}
		}

		private void fileView_ColumnClick(object sender, ColumnClickEventArgs e) {
			// Determine if clicked column is already the column that is being sorted.
			if (e.Column == lvwColumnSorter.SortColumn) {
				// Reverse the current sort direction for this column.
				if (lvwColumnSorter.Order == SortOrder.Ascending) {
					lvwColumnSorter.Order = SortOrder.Descending;
				} else {
					lvwColumnSorter.Order = SortOrder.Ascending;
				}
			} else {
				// Set the column number that is to be sorted; default to ascending.
				lvwColumnSorter.SortColumn = e.Column;
				lvwColumnSorter.Order = SortOrder.Ascending;
			}

			// Perform the sort with these new sort options.
			this.fileView.Sort();
		}

		private void tbFilter_Enter(object sender, EventArgs e) {
			if (tbFilter.Text.Length == 0 || tbFilter.Text == EMPTY_FILTER_TEXT) {
				tbFilter.Text = "";
				tbFilter.ForeColor = Color.Black;
				tbFilter.Font = new Font(tbFilter.Font, FontStyle.Regular);
			}
		}

		private void tbFilter_Leave(object sender, EventArgs e) {
			UpdateFilterTextBox();
		}

		private void Filter() {
			if (tbFilter.Text.Length > 0 && tbFilter.Text != EMPTY_FILTER_TEXT) {
				FilterBy(tbFilter.Text, cbShowUnknownFiles.Checked);
			} else {
				FilterBy("", cbShowUnknownFiles.Checked);
			}
		}

		private void tbFilter_TextChanged(object sender, EventArgs e) {
			Filter();
		}

		private void cbShowUnknownFiles_CheckedChanged(object sender, EventArgs e) {
			Filter();
		}

		private void bRestoreFiles_Click(object sender, EventArgs e) {
			if (fileView.CheckedItems.Count == 1) {
				PromptUserToSaveFile(fileView.CheckedItems[0].Tag as INodeMetadata);
			} else if (fileView.CheckedItems.Count > 1) {
				PromptUserToSaveFiles(fileView.CheckedItems);
			}
		}

		/// <summary>
		/// Sets the restore button to be enabled if there are list items checked.
		/// </summary>
		private void UpdateRestoreButton(int change) {
			if (!m_Scanning) {
				bRestoreFiles.Enabled = fileView.CheckedItems.Count + change > 0;
			}
		}

		private void fileView_ItemCheck(object sender, ItemCheckEventArgs e) {
			UpdateRestoreButton(e.NewValue == CheckState.Checked ? 1 : -1);
		}
	}
}
