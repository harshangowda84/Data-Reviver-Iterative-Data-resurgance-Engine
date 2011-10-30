using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using KFA.DataStream;
using KFA.Disks;
using FileSystems.FileSystem;
using FileSystems.FileSystem.NTFS;
using File = FileSystems.FileSystem.File;

namespace KFA.GUI.Explorers {
    public partial class FileExplorer : UserControl, IExplorer {
        private IDataStream m_CurrentStream = null;

        public FileExplorer() {
            InitializeComponent();
            treeFiles.ImageList = imageList1;
            treeFiles.SelectedImageKey = null;
        }

        private void AppendChildren(TreeNode node, IEnumerable<FileSystemNode> children) {
            node.Nodes.Clear();
            foreach (FileSystemNode child in children) {
                TreeNode treeNode = new TreeNode(child.ToString());
                treeNode.Tag = child;
                if (child.Deleted) {
                    treeNode.ImageKey = "Deleted";
                    treeNode.ForeColor = Color.Red;
                } else if (child is File || child is HiddenDataStreamFileNTFS) {
                    treeNode.ImageKey = "File";
                } else {
                    treeNode.ImageKey = "Directory";
                }

                if (child is Folder || (child is File && ((File)child).IsZip)) {
                    treeNode.Nodes.Add(new TreeNode("dummy"));
                    child.Loaded = false;
                }
                treeNode.SelectedImageKey = treeNode.ImageKey;
                node.Nodes.Add(treeNode);
            }
        }

        private void treeFiles_BeforeExpand(object sender, TreeViewCancelEventArgs e) {
            FileSystemNode fsNode = e.Node.Tag as FileSystemNode;
            if (fsNode != null && !fsNode.Loaded) {
                AppendChildren(e.Node, fsNode.GetChildren());
                fsNode.Loaded = true;
            }
        }

        private void treeFiles_AfterSelect(object sender, TreeViewEventArgs e) {
            IDescribable fsNode = e.Node.Tag as IDescribable;
            if (fsNode != null) {
                tbDescription.Text = fsNode.TextDescription;
            }
            IDataStream stream = e.Node.Tag as IDataStream;
            if (stream != null) {
                OnStreamSelected(stream);
            }
        }

        #region IExplorer Members

        public bool CanView(IDataStream stream) {
            return stream is IFileSystemStore;
        }

        public void View(IDataStream stream) {
            if (stream != m_CurrentStream) {
                m_CurrentStream = stream;
                treeFiles.Nodes.Clear();
                if (stream is IFileSystemStore) {
                    FileSystem fs = (stream as IFileSystemStore).FS;
                    FileSystemNode fsRoot;
                    if (fs != null) {
                        fsRoot = fs.GetRoot();
                        TreeNode root = new TreeNode(fsRoot.ToString());
                        root.Tag = fsRoot;
                        root.SelectedImageKey = root.ImageKey = "Directory";
                        treeFiles.Nodes.Add(root);
                        root.Nodes.Add(new TreeNode("dummy"));
                        ((Folder)fsRoot).Loaded = false;
                    } else {
                        treeFiles.Nodes.Add("Unknown file system");
                    }
                }
            }
        }

        #endregion

        private void treeFiles_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e) {
            if (e.Button == MouseButtons.Right) {
                ShowContextMenu(e);
            }
        }

        private void ShowContextMenu(TreeNodeMouseClickEventArgs e) {
            FileSystemNode fsnode = e.Node.Tag as FileSystemNode;
            if (fsnode != null) {
                ContextMenu menu = new ContextMenu();
                if (fsnode is Folder) {
                    Folder f = fsnode as Folder;
                    menu.MenuItems.Add(new MenuItem("Refresh", new EventHandler(delegate(object o, EventArgs ea) {
                        fsnode.ReloadChildren();
                        AppendChildren(e.Node, fsnode.GetChildren());
                    })));
                    menu.MenuItems.Add(new MenuItem("Save Folder...", new EventHandler(delegate(object o, EventArgs ea) {
                        SaveFileDialog saveFileDialog = new SaveFileDialog();
                        saveFileDialog.Filter = "Any Files|*.*";
                        saveFileDialog.Title = "Select a Location";
                        saveFileDialog.FileName = f.Name;
                        saveFileDialog.OverwritePrompt = true;

                        if (saveFileDialog.ShowDialog() == DialogResult.OK) {
                            SaveFolder(f, saveFileDialog.FileName);                        }
                    })));
                }
                if (fsnode is File) {
                    File f = fsnode as File;
                    menu.MenuItems.Add(new MenuItem("Save File...", new EventHandler(delegate(object o, EventArgs ea) {
                        SaveFileDialog saveFileDialog = new SaveFileDialog();
                        saveFileDialog.Filter = "Any Files|*.*";
                        saveFileDialog.Title = "Select a Location";
                        saveFileDialog.FileName = f.Name;
                        saveFileDialog.OverwritePrompt = true;

                        if (saveFileDialog.ShowDialog() == DialogResult.OK) {
                            SaveFile(f, saveFileDialog.FileName);
                        }
                    })));
                }
                menu.Show(e.Node.TreeView, e.Location);
            }
        }

        private void SaveFolder(Folder f, string p) {
            Directory.CreateDirectory(p);
            foreach (FileSystemNode node in f.GetChildren()) {
                if (node is File || node is HiddenDataStreamFileNTFS) {
                    SaveFile(node, Path.Combine(p, node.Name));
                } else if (node is Folder) {
                    SaveFolder(node as Folder, Path.Combine(p, node.Name));
                }
            }
        }

        private void SaveFile(FileSystemNode file, string filepath) {
            if (!System.IO.File.Exists(filepath)) {
                using (ForensicsAppStream fas = new ForensicsAppStream(file)) {
                    using (Stream output = new FileStream(filepath, FileMode.Create)) {
                        byte[] buffer = new byte[32 * 1024];
                        int read;

                        while ((read = fas.Read(buffer, 0, buffer.Length)) > 0) {
                            output.Write(buffer, 0, read);
                        }
                    }
                }
            }
        }

        public event StreamSelectedEventHandler StreamSelected;

        private void OnStreamSelected(IDataStream stream) {
            if (StreamSelected != null) {
                StreamSelected(this, stream);
            }
        }
    }

    public delegate void StreamSelectedEventHandler(object sender, IDataStream stream);
}
