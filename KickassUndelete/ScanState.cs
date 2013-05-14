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
		private DateTime m_StartTime;
		private Thread m_Thread;
		private bool m_ScanCancelled;
		private FileSystem m_FileSystem;
		private string m_DiskName;

		/// <summary>
		/// Constructs a ScanState on the specified filesystem.
		/// </summary>
		/// <param name="fileSystem">The filesystem to scan.</param>
		public ScanState(string diskName, FileSystem fileSystem) {
			m_FileSystem = fileSystem;
			m_DiskName = diskName;
		}

		/// <summary>
		/// Gets the deleted files found by the scan.
		/// </summary>
		public IList<INodeMetadata> GetDeletedFiles() {
			lock (m_DeletedFiles) {
				return new List<INodeMetadata>(m_DeletedFiles);
			}
		}

		public string DiskName {
			get { return m_DiskName; }
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

			Console.WriteLine("Beginning scan...");
			m_StartTime = DateTime.Now;

			strat.Search(new FileSystem.NodeVisitCallback(delegate(INodeMetadata metadata, ulong current, ulong total) {
				if (metadata != null && metadata.Deleted && metadata.Name != null
						&& !metadata.Name.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase)
						&& !metadata.Name.EndsWith(".cat", StringComparison.OrdinalIgnoreCase)
						&& !metadata.Name.EndsWith(".mum", StringComparison.OrdinalIgnoreCase)) {
					FileSystemNode node = metadata.GetFileSystemNode();
					if (node.Type == FileSystemNode.NodeType.File && node.Size > 0) {
						lock (m_DeletedFiles) {
							m_DeletedFiles.Add(metadata);
						}
					}
				}

				if (current % 100 == 0) {
					m_Progress = (double)current / (double)total;
					OnProgressUpdated();
				}
				return !m_ScanCancelled;
			}));

			TimeSpan timeTaken = DateTime.Now - m_StartTime;
			if (!m_ScanCancelled) {
				Console.WriteLine("Scan complete! Time taken: {0}", timeTaken);
				m_Progress = 1;
				OnProgressUpdated();
				OnScanFinished();
			} else {
				Console.WriteLine("Scan cancelled! Time taken: {0}", timeTaken);
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
