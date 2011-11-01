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

namespace KickassUndelete {
    public partial class DeletedFileViewer : UserControl {
        private const string EMPTY_FILTER_TEXT = "Enter filter text here...";


        private ScanState m_ScanState = null;
        private int m_NumFilesShown = 0;
        private string m_Filter = "";
        private bool m_Scanning = false;
        private bool m_Saving = false;

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
            m_Scanning = true;
            try {
                this.Invoke(new Action(() => {
                    SetScanButtonScanning();
                    UpdateTimer.Start();
                }));
            } catch (InvalidOperationException) { }
        }

        void state_ScanFinished() {
            try {
                this.Invoke(new Action(() => {
                    SetScanButtonFinished();
                    UpdateTimer.Stop();
                    UpdateTimer_Tick(null, null);
                }));
                m_Scanning = false;
            } catch (InvalidOperationException) { }
        }

        void state_ProgressUpdated() {
            try {
                this.BeginInvoke(new Action(() => {
                    SetProgress(m_ScanState.Progress);
                }));
            } catch (InvalidOperationException) { }
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
            bRestoreFiles.Show();
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
            if (e.Button == MouseButtons.Right) {
                if (fileView.SelectedItems.Count == 1) {
                    INodeMetadata metadata = fileView.SelectedItems[0].Tag as INodeMetadata;
                    if (metadata != null) {
                        ContextMenu menu = new ContextMenu();
                        MenuItem recoverFile = new MenuItem("Recover File...", new EventHandler(delegate(object o, EventArgs ea) {
                            SaveFileDialog saveFileDialog = new SaveFileDialog();
                            saveFileDialog.OverwritePrompt = true;
                            saveFileDialog.FileName = metadata.Name;
                            saveFileDialog.Filter = "Any Files|*.*";
                            saveFileDialog.Title = "Select a Location";

                            if (saveFileDialog.ShowDialog() == DialogResult.OK) {
                                FileSystemNode node = metadata.GetFileSystemNode();
                                SaveFile(node, saveFileDialog.FileName);
                            }
                        }));
                        recoverFile.Enabled = !m_Scanning && !m_Saving;
                        menu.MenuItems.Add(recoverFile);
                        menu.Show(fileView, e.Location);
                    }
                } else if (fileView.SelectedItems.Count > 1) {
                    ContextMenu menu = new ContextMenu();
                    MenuItem recoverFiles = new MenuItem("Recover Files...", new EventHandler(delegate(object o, EventArgs ea) {
                        FolderBrowserDialog folderDialog = new FolderBrowserDialog();

                        if (folderDialog.ShowDialog() == DialogResult.OK) {
                            List<FileSystemNode> nodes = new List<FileSystemNode>();
                            foreach (ListViewItem item in fileView.SelectedItems) {
                                INodeMetadata metadata = item.Tag as INodeMetadata;
                                if (metadata != null) {
                                    nodes.Add(metadata.GetFileSystemNode());
                                }
                            }
                            SaveFiles(nodes, folderDialog.SelectedPath);
                        }
                    }));
                    recoverFiles.Enabled = !m_Scanning && !m_Saving;
                    menu.MenuItems.Add(recoverFiles);
                    menu.Show(fileView, e.Location);
                }
            }
        }

        private void SaveFile(FileSystemNode node, string fileName) {
            SaveProgressDialog progressBar = new SaveProgressDialog();
            progressBar.Show(this);
            string file = Path.GetFileName(fileName);
            Thread t = new Thread(delegate() {
                using (BinaryWriter bw = new BinaryWriter(new FileStream(fileName, FileMode.Create))) {
                    ulong BLOCK_SIZE = 1024 * 1024; // 1MB
                    ulong offset = 0;
                    while (offset < node.StreamLength) {
                        if (offset + BLOCK_SIZE < node.StreamLength) {
                            bw.Write(node.GetBytes(offset, BLOCK_SIZE));
                        } else {
                            bw.Write(node.GetBytes(offset, node.StreamLength - offset));
                        }
                        this.BeginInvoke(new Action<double>(delegate(double progress) {
                            progressBar.SetProgress(file, progress);
                        }), (double)offset / (double)node.StreamLength);
                        offset += BLOCK_SIZE;
                    }
                    this.BeginInvoke(new Action(delegate() {
                        progressBar.Close();
                    }));
                }
            });

            t.Start();
        }

        private void SaveFiles(IEnumerable<FileSystemNode> nodes, string folderPath) {
            SaveProgressDialog progressBar = new SaveProgressDialog();
            progressBar.Show(this);

            Thread t = new Thread(delegate() {
                foreach (FileSystemNode node in nodes) {
                    string file = node.Name;
                    string fileName = Path.Combine(folderPath, file);
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
                            this.BeginInvoke(new Action<double>(delegate(double progress) {
                                progressBar.SetProgress(file, progress);
                            }), Math.Min(1, (double)offset / (double)node.StreamLength));
                        }
                    }
                }
                this.BeginInvoke(new Action(delegate() {
                    progressBar.Close();
                    UpdateRestoreButton();
                }));
            });

            t.Start();
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

        private void bRestoreFiles_Click(object sender, EventArgs e) {
            if (fileView.CheckedItems.Count > 1) {
                FolderBrowserDialog folderDialog = new FolderBrowserDialog();

                if (folderDialog.ShowDialog() == DialogResult.OK) {
                    List<FileSystemNode> nodes = new List<FileSystemNode>();
                    foreach (ListViewItem item in fileView.CheckedItems) {
                        INodeMetadata metadata = item.Tag as INodeMetadata;
                        if (metadata != null) {
                            nodes.Add(metadata.GetFileSystemNode());
                        }
                    }
                    SaveFiles(nodes, folderDialog.SelectedPath);
                }
            }
        }

        private void UpdateRestoreButton() {
            if (!m_Scanning && !m_Saving) {
                bRestoreFiles.Enabled = fileView.CheckedItems.Count > 0;
            }
        }

        private void fileView_ItemCheck(object sender, ItemCheckEventArgs e) {
            UpdateRestoreButton();
        }
    }
}
