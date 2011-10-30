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

namespace KickassUndelete {
    public partial class DeletedFileViewer : UserControl {
        private const string EMPTY_FILTER_TEXT = "Enter filter text here...";


        private ScanState m_ScanState = null;
        private int m_NumFilesShown = 0;
        private string m_Filter = "";

        private ListViewColumnSorter lvwColumnSorter;

        public DeletedFileViewer(ScanState state) {
            InitializeComponent();

            lvwColumnSorter = new ListViewColumnSorter();
            fileView.ListViewItemSorter = lvwColumnSorter;

            m_ScanState = state;
            state.ProgressUpdated += new Action(state_ProgressUpdated);
            state.ScanStarted += new Action(state_ScanStarted);
            state.ScanFinished += new Action(state_ScanFinished);

            UpdateFilterTextBox();
        }

        private void UpdateFilterTextBox() {
            if (tbFilter.Text == "" || tbFilter.Text == EMPTY_FILTER_TEXT) {
                tbFilter.Text = EMPTY_FILTER_TEXT;
                tbFilter.ForeColor = Color.Gray;
                tbFilter.Font = new Font(tbFilter.Font, FontStyle.Italic);
            } else {
                tbFilter.ForeColor = Color.Black;
                tbFilter.Font = new Font(tbFilter.Font, FontStyle.Regular);
            }
        }

        void state_ScanStarted() {
            this.Invoke(new Action(() => {
                SetScanButtonScanning();
                UpdateTimer.Start();
            }));
        }

        void state_ScanFinished() {
            this.Invoke(new Action(() => {
                SetScanButtonFinished();
                UpdateTimer.Stop();
                UpdateTimer_Tick(null, null);
            }));
        }

        void state_ProgressUpdated() {
            this.Invoke(new Action(() => {
                SetProgress(m_ScanState.Progress);
            }));
        }

        public void EnableScanButton() {
            bScan.Enabled = true;
            bScan.Text = "Scan";
        }

        public void SetScanButtonScanning() {
            bScan.Enabled = false;
            bScan.Text = "Scanning...";
            progressBar.Show();
        }

        public void SetScanButtonFinished() {
            bScan.Enabled = false;
            bScan.Text = "Finished Scanning!";
            progressBar.Hide();
        }

        private void bScan_Click(object sender, EventArgs e) {
            m_ScanState.StartScan();
        }

        private ListViewItem[] MakeListItems(List<INodeMetadata> metadatas) {
            List<ListViewItem> items = new List<ListViewItem>(metadatas.Count);
            for (int i = 0; i < metadatas.Count; i++) {
                ListViewItem item = MakeListItem(metadatas[i]);
                if (item.Text.ToLower().Contains(m_Filter)) {
                    items.Add(item);
                }
            }
            return items.ToArray();
        }

        private ListViewItem MakeListItem(INodeMetadata metadata) {
            FileSystemNode node = metadata.GetFileSystemNode();
            ListViewItem lvi = new ListViewItem(new string[] {
                metadata.Name,
                Path.GetExtension(metadata.Name),
                Util.ByteFormat(node.Size)
            });
            lvi.Tag = metadata;
            return lvi;
        }

        private void SetProgress(double progress) {
            progressBar.Value = (int)(progress * progressBar.Maximum);
        }

        private void FilterBy(string filter) {
            if (m_Filter != filter.ToLower()) {
                m_Filter = filter.ToLower();

                fileView.Items.Clear();
                ListViewItem[] items = MakeListItems(m_ScanState.DeletedFiles);
                fileView.Items.AddRange(items);
                m_NumFilesShown = m_ScanState.DeletedFiles.Count;
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e) {
            int fileCount = m_ScanState.DeletedFiles.Count;
            if (fileCount > m_NumFilesShown) {
                ListViewItem[] items = MakeListItems(m_ScanState.DeletedFiles.GetRange(m_NumFilesShown, fileCount - m_NumFilesShown));
                fileView.Items.AddRange(items);
                m_NumFilesShown = fileCount;
            }
        }

        private void fileView_MouseClick(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Right && fileView.SelectedItems.Count == 1) {
                INodeMetadata metadata = fileView.SelectedItems[0].Tag as INodeMetadata;
                if (metadata != null) {
                    ContextMenu menu = new ContextMenu();
                    menu.MenuItems.Add(new MenuItem("Save File", new EventHandler(delegate(object o, EventArgs ea) {
                        SaveFileDialog saveFileDialog = new SaveFileDialog();
                        saveFileDialog.OverwritePrompt = true;
                        saveFileDialog.FileName = metadata.Name;
                        saveFileDialog.Filter = "Any Files|*.*";
                        saveFileDialog.Title = "Select a Location";

                        if (saveFileDialog.ShowDialog() == DialogResult.OK) {
                            FileSystemNode node = metadata.GetFileSystemNode();
                            SaveFile(node, saveFileDialog.FileName);
                        }
                    })));
                    menu.Show(fileView, e.Location);
                }
            }
        }

        private void SaveFile(FileSystemNode node, string fileName) {
            using (BinaryWriter bw = new BinaryWriter(new FileStream(fileName, FileMode.Create))) {
                ulong BLOCK_SIZE = 1024 * 1024; // 1MB
                ulong offset = 0;
                while (offset < node.StreamLength) {
                    if (offset + BLOCK_SIZE < node.StreamLength) {
                        bw.Write(node.GetBytes(offset, BLOCK_SIZE));
                    } else {
                        bw.Write(node.GetBytes(offset, node.StreamLength - offset));
                    }
                    offset += BLOCK_SIZE;
                }
            }
        }

        private void fileView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
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
            if (tbFilter.Text == "" || tbFilter.Text == EMPTY_FILTER_TEXT) {
                tbFilter.Text = "";
                tbFilter.ForeColor = Color.Black;
                tbFilter.Font = new Font(tbFilter.Font, FontStyle.Regular);
            }
        }

        private void tbFilter_Leave(object sender, EventArgs e) {
            UpdateFilterTextBox();
        }

        private void tbFilter_TextChanged(object sender, EventArgs e) {
            if (tbFilter.Text != "" && tbFilter.Text != EMPTY_FILTER_TEXT) {
                FilterBy(tbFilter.Text);
            } else {
                FilterBy("");
            }
        }
    }
}
