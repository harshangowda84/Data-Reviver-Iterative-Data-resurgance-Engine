// Copyright (C) 2013  Joey Scarr, Lukas Korsika
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

using KFS.FileSystems;
using KFS.FileSystems.NTFS;
using MB.Algodat;
using System;
using System.Collections.Generic;
using System.Threading;

namespace KickassUndelete {
	/// <summary>
	/// Encapsulates the state of a scan for deleted files.
	/// </summary>
	public class Scanner {
		private List<INodeMetadata> m_DeletedFiles = new List<INodeMetadata>();
		private double m_Progress;
		private DateTime m_StartTime;
		private Thread m_Thread;
		private bool m_ScanCancelled;
		private IFileSystem m_FileSystem;
		private string m_DiskName;

		/// <summary>
		/// Constructs a Scanner on the specified filesystem.
		/// </summary>
		/// <param name="fileSystem">The filesystem to scan.</param>
		public Scanner(string diskName, IFileSystem fileSystem) {
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
			// Dictionaries to figure out paths.
			// TODO: This could probably be one record[]
			Dictionary<ulong, ulong> parentLinks = new Dictionary<ulong, ulong>();
			Dictionary<ulong, string> recordNames = new Dictionary<ulong, string>();

			IRangeTree<ulong, RangeItem> runIndex = new RangeTree<ulong, RangeItem>(new RangeItemComparer());

			ulong numFiles;

			OnScanStarted();
			m_Progress = 0;
			OnProgressUpdated();

			// TODO: Replace me with a search strategy selected from a text box!
			ISearchStrategy strat = m_FileSystem.GetDefaultSearchStrategy();

			if (m_FileSystem is FileSystemNTFS) {
				var ntfsFS = m_FileSystem as FileSystemNTFS;
				numFiles = ntfsFS.MFT.StreamLength / (ulong)(ntfsFS.SectorsPerMFTRecord * ntfsFS.BytesPerSector);
			}

			Console.WriteLine("Beginning scan...");
			m_StartTime = DateTime.Now;

			strat.Search(new FileSystem.NodeVisitCallback(delegate(INodeMetadata metadata, ulong current, ulong total) {
				var record = metadata as MFTRecord;
				if (record != null) {
					parentLinks[record.RecordNum] = record.ParentDirectory;
					recordNames[record.RecordNum] = record.Name;

					foreach (IRun run in record.Runs) {
						runIndex.Add(new RangeItem(run, record));
					}
				}

				if (metadata != null && metadata.Deleted && metadata.Name != null
						&& !metadata.Name.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase)
						&& !metadata.Name.EndsWith(".cat", StringComparison.OrdinalIgnoreCase)
						&& !metadata.Name.EndsWith(".mum", StringComparison.OrdinalIgnoreCase)) {
					IFileSystemNode node = metadata.GetFileSystemNode();
					if (node.Type == FSNodeType.File && node.Size > 0) {
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

			if (m_FileSystem is FileSystemNTFS) {
				List<INodeMetadata> fileList;
				lock (m_DeletedFiles) {
					fileList = m_DeletedFiles;
				}
				foreach (var file in fileList) {
					var record = file as MFTRecord;
					var node = file.GetFileSystemNode();
					node.Path = GetPathForRecord(parentLinks, recordNames, record.RecordNum);
				}
			}

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

		private string GetPathForRecord(Dictionary<ulong, ulong> parentLinks,
										Dictionary<ulong, string> recordNames,
										ulong recordNum) {
			if (recordNum == 0 || !parentLinks.ContainsKey(recordNum) || parentLinks[recordNum] == recordNum) {
				return "";
			} else {
				if (!recordNames.ContainsKey(recordNum))
					throw new Exception("Record name not found: " + recordNum);
				return (GetPathForRecord(parentLinks, recordNames, parentLinks[recordNum])) +
					"\\" + recordNames[recordNum];
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
