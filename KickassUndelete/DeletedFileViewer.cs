using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FileSystems.FileSystem;
using KFA.FileSystem;
using System.Threading;
using KFA.DataStream;
using System.IO;

namespace KickassUndelete {
    public partial class DeletedFileViewer : UserControl {
        private ScanState m_ScanState = null;
        private int m_NumFilesShown = 0;

        public DeletedFileViewer(ScanState state) {
            InitializeComponent();

            m_ScanState = state;
            state.ProgressUpdated += new Action(state_ProgressUpdated);
            state.ScanStarted += new Action(state_ScanStarted);
            state.ScanFinished += new Action(state_ScanFinished);
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
        }

        public void SetScanButtonFinished() {
            bScan.Enabled = false;
            bScan.Text = "Finished Scanning!";
        }

        private void bScan_Click(object sender, EventArgs e) {
            m_ScanState.StartScan();
        }

        private ListViewItem[] MakeListItems(List<INodeMetadata> metadatas) {
            ListViewItem[] items = new ListViewItem[metadatas.Count];
            for (int i = 0; i < metadatas.Count; i++) {
                items[i] = MakeListItem(metadatas[i]);
            }
            return items;
        }

        private ListViewItem MakeListItem(INodeMetadata metadata) {
            ListViewItem lvi = new ListViewItem(new string[] { metadata.Name, Util.ByteFormat(metadata.GetFileSystemNode().Size) });
            lvi.Tag = metadata;
            return lvi;
        }

        private void SetProgress(double progress) {
            progressBar.Value = (int)(progress * progressBar.Maximum);
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
                            using (BinaryWriter bw = new BinaryWriter(new FileStream(saveFileDialog.FileName, FileMode.Create))) {
                                bw.Write(Util.GetBytes(node));
                            }
                        }
                    })));
                    menu.Show(fileView, e.Location);
                }
            }
        }
    }
}
