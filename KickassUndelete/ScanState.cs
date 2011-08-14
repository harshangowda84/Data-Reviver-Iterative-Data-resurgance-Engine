using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileSystems.FileSystem;
using System.Threading;
using System.Windows.Forms;
using KFA.FileSystem;

namespace KickassUndelete {
    public class ScanState {
        public List<INodeMetadata> DeletedFiles = new List<INodeMetadata>();
        public DeletedFileViewer Viewer;
        public double Progress = 0.0;
        public Thread Thread = null;
        public bool m_ScanCancelled = false;
        public FileSystem m_FileSystem = null;

        public ScanState(FileSystem fileSystem) {
            m_FileSystem = fileSystem;
            Viewer = new DeletedFileViewer(this);
        }

        public void StartScan() {
            m_ScanCancelled = false;
            Thread = new Thread(Run);
            Thread.Start();
        }

        public void CancelScan() {
            m_ScanCancelled = true;
        }

        public void Run() {
            ScanStarted();
            Progress = 0;
            ProgressUpdated();

            m_FileSystem.VisitFiles(new FileSystem.NodeVisitCallback(delegate(INodeMetadata node, ulong current, ulong total) {
                if (node.Deleted && node.Name != null
                    && !node.Name.EndsWith(".manifest") 
                    && !node.Name.EndsWith(".cat")
                    && !node.Name.EndsWith(".mum")
                    && node.GetFileSystemNode().Size > 0 ) {
                    DeletedFiles.Add(node);
                }

                if (current % 100 == 0) {
                    Progress = (double)current / (double)total;
                    ProgressUpdated();
                }
                return !m_ScanCancelled;
            }), true);

            if (!m_ScanCancelled) {
                Progress = 1;
                ProgressUpdated();
                ScanFinished();
            }
        }

        public event Action ProgressUpdated;
        public event Action ScanStarted;
        public event Action ScanFinished;
    }
}
