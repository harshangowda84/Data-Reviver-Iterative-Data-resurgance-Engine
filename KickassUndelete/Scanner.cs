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
using System.Linq;
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
			// Dictionary storing a tree that allows us to rebuild deleted file paths.
			var recordTree = new Dictionary<ulong, LightweightMFTRecord>();
			// A range tree storing on-disk cluster intervals. Allows us to tell whether files are overwritten.
			var runIndex = new RangeTree<ulong, RangeItem>(new RangeItemComparer());

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
					var lightweightRecord = new LightweightMFTRecord(record);
					recordTree[record.RecordNum] = lightweightRecord;

					foreach (IRun run in record.Runs) {
						runIndex.Add(new RangeItem(run, lightweightRecord));
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
					node.Path = GetPathForRecord(recordTree, record.ParentDirectory) + "\\" + node.Path;
					if (record.ChanceOfRecovery == FileRecoveryStatus.MaybeOverwritten) {
						record.ChanceOfRecovery = FileRecoveryStatus.Recoverable;
						// Query all the runs for this node.
						foreach (IRun run in record.Runs) {
							List<RangeItem> overlapping = runIndex.Query(new Range<ulong>(run.LCN, run.LCN + run.LengthInClusters - 1));

							if (overlapping.Count(x => x.Record.RecordNumber != record.RecordNum) > 0) {
								record.ChanceOfRecovery = FileRecoveryStatus.PartiallyOverwritten;
								break;
							}
						}
					}
				}
			}

			runIndex.Clear();
			recordTree.Clear();
			GC.Collect();

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

		private string GetPathForRecord(Dictionary<ulong, LightweightMFTRecord> recordTree,
										ulong recordNum) {
			if (recordNum == 0 || !recordTree.ContainsKey(recordNum)
					|| recordTree[recordNum].ParentRecord == recordNum) {
				// This is the root record
				return "";
			} else if (!recordTree[recordNum].IsDirectory) {
				// This isn't a directory, so the path must have been broken.
				return "\\?";
			} else {
				var record = recordTree[recordNum];
				return (GetPathForRecord(recordTree, record.ParentRecord)) +
					"\\" + record.FileName;
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
