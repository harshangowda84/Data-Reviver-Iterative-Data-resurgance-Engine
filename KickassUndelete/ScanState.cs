using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileSystems.FileSystem;
using System.Threading;
using System.Windows.Forms;
using System.Collections.ObjectModel;

namespace KickassUndelete {
    /// <summary>
    /// Encapsulates the state of a scan for deleted files.
    /// </summary>
    public class ScanState {
        private List<INodeMetadata> m_DeletedFiles = new List<INodeMetadata>();
        private double m_Progress;
        private Thread m_Thread;
        private bool m_ScanCancelled;
        private FileSystem m_FileSystem;

        /// <summary>
        /// Constructs a ScanState on the specified filesystem.
        /// </summary>
        /// <param name="fileSystem">The filesystem to scan.</param>
        public ScanState(FileSystem fileSystem) {
            m_FileSystem = fileSystem;
        }

        /// <summary>
        /// Gets the deleted files found by the scan.
        /// </summary>
        public IList<INodeMetadata> GetDeletedFiles() {
			lock (m_DeletedFiles) { 
				return new List<INodeMetadata>(m_DeletedFiles); 
			}
        }

        /// <summary>
        /// Gets the current progress of the scan (between 0 and 1).
        /// </summary>
        public double Progress {
            get { return m_Progress; }
        }

        /// <summary>
        /// Starts a scan on the filesystem.
        /// </summary>
        public void StartScan() {
            m_ScanCancelled = false;
            m_Thread = new Thread(Run);
            m_Thread.Start();
        }

        /// <summary>
        /// Cancels the currently running scan.
        /// </summary>
        public void CancelScan() {
            m_ScanCancelled = true;
        }

        /// <summary>
        /// Runs a scan.
        /// </summary>
        private void Run() {
            OnScanStarted();
            m_Progress = 0;
            OnProgressUpdated();

            // TODO: Replace me with a search strategy selected from a text box!
            ISearchStrategy strat = m_FileSystem.GetDefaultSearchStrategy();
            strat.Search(new FileSystem.NodeVisitCallback(delegate(INodeMetadata node, ulong current, ulong total) {
                if (node.Deleted && node.Name != null
                    && !node.Name.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase)
                    && !node.Name.EndsWith(".cat", StringComparison.OrdinalIgnoreCase)
                    && !node.Name.EndsWith(".mum", StringComparison.OrdinalIgnoreCase)
                    && node.GetFileSystemNode().Size > 0 ) {
					lock (m_DeletedFiles) {
						m_DeletedFiles.Add(node);
					}
                }

                if (current % 100 == 0) {
                    m_Progress = (double)current / (double)total;
                    OnProgressUpdated();
                }
                return !m_ScanCancelled;
            }));

            if (!m_ScanCancelled) {
                m_Progress = 1;
                OnProgressUpdated();
                OnScanFinished();
            }
        }

        /// <summary>
        /// This event fires repeatedly as the scan progresses.
        /// </summary>
        public event EventHandler ProgressUpdated;
        private void OnProgressUpdated() {
            if (ProgressUpdated != null) {
                ProgressUpdated(this, null);
            }
        }

        /// <summary>
        /// This event fires when the scan is started.
        /// </summary>
        public event EventHandler ScanStarted;
        private void OnScanStarted() {
            if (ScanStarted != null) {
                ScanStarted(this, null);
            }
        }

        /// <summary>
        /// This event fires when the scan finishes.
        /// </summary>
        public event EventHandler ScanFinished;
        private void OnScanFinished() {
            if (ScanFinished != null) {
                ScanFinished(this, null);
            }
        }
    }
}
