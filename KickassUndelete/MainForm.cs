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
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using KFA.Disks;
using System.Threading;
using FileSystems.FileSystem;
using KFA.DataStream;
using FileSystems;

namespace KickassUndelete {
    /// <summary>
    /// The main form of Kickass Undelete.
    /// </summary>
    public partial class MainForm : Form {
        FileSystem m_FileSystem;
        Dictionary<FileSystem, ScanState> m_ScanStates = new Dictionary<FileSystem, ScanState>();
        Dictionary<FileSystem, DeletedFileViewer> m_DeletedViewers = new Dictionary<FileSystem, DeletedFileViewer>();

        /// <summary>
        /// Constructs the main form.
        /// </summary>
        public MainForm() {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e) {
            LoadLogicalDisks();
        }

        private void LoadLogicalDisks() {
            foreach (LogicalDisk disk in DiskLoader.LoadLogicalVolumes()) {
                TreeNode node = new TreeNode(disk.ToString());
                node.Tag = disk;
                node.ImageKey = "HDD";
                if (disk.FS == null) {
                    node.ForeColor = Color.Gray;
                }
                diskTree.Nodes.Add(node);
            }
        }

        private void diskTree_AfterSelect(object sender, TreeViewEventArgs e) {
            SetFileSystem((LogicalDisk)e.Node.Tag);
        }

        private void SetFileSystem(LogicalDisk logicalDisk) {
            if (logicalDisk.FS != null) {
                if (!m_ScanStates.ContainsKey(logicalDisk.FS)) {
                    m_ScanStates[logicalDisk.FS] = new ScanState(logicalDisk.FS);
                    m_DeletedViewers[logicalDisk.FS] = new DeletedFileViewer(m_ScanStates[logicalDisk.FS]);
                    AddDeletedFileViewer(m_DeletedViewers[logicalDisk.FS]);
                }
                if (m_FileSystem != null && m_ScanStates.ContainsKey(m_FileSystem)) {
                    m_DeletedViewers[m_FileSystem].Hide();
                }
                m_FileSystem = logicalDisk.FS;
                m_DeletedViewers[logicalDisk.FS].Show();
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
            foreach (ScanState state in m_ScanStates.Values) {
                state.CancelScan();
            }
        }

        private void diskTree_BeforeSelect(object sender, TreeViewCancelEventArgs e) {
            if (((IFileSystemStore)e.Node.Tag).FS == null) {
                e.Cancel = true;
            }
        }
    }
}
