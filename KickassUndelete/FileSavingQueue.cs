// Copyright (C) 2013  Joey Scarr
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

using GuiComponents;
using KFS.FileSystems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace KickassUndelete {
	public class FileSavingQueue : IProgressable {
		private bool m_Saving = false;
		private Thread m_ProcessingThread;
		private Queue<KeyValuePair<string, IFileSystemNode>> m_Queue = new Queue<KeyValuePair<string, IFileSystemNode>>();

		public FileSavingQueue() { }

		public void Push(string filepath, IFileSystemNode fileNode) {
			lock (m_Queue) {
				m_Queue.Enqueue(new KeyValuePair<string, IFileSystemNode>(filepath, fileNode));
				if (!m_Saving) {
					m_Saving = true;
					m_ProcessingThread = new Thread(delegate() {
						int remaining = 0;
						lock (m_Queue) {
							remaining = m_Queue.Count;
						}
						while (remaining > 0) {
							KeyValuePair<string, IFileSystemNode> nextFile;
							lock (m_Queue) {
								nextFile = m_Queue.Dequeue();
							}
							var filePath = nextFile.Key;
							var node = nextFile.Value;
							WriteFileToDisk(filePath, node);
							lock (m_Queue) {
								remaining = m_Queue.Count;
								if (remaining == 0) {
									m_Saving = false;
									OnFinished();
									return;
								}
							}
						}
					});
					m_ProcessingThread.Start();
				}
			}
		}

		private void WriteFileToDisk(string filePath, IFileSystemNode node) {
			using (BinaryWriter bw = new BinaryWriter(new FileStream(filePath, FileMode.Create))) {
				ulong BLOCK_SIZE = 1024 * 1024; // 1MB
				ulong offset = 0;
				while (offset < node.StreamLength) {
					if (offset + BLOCK_SIZE < node.StreamLength) {
						bw.Write(node.GetBytes(offset, BLOCK_SIZE));
					} else {
						bw.Write(node.GetBytes(offset, node.StreamLength - offset));
					}
					offset += BLOCK_SIZE;

					// Notify the progress listeners that bytes have been saved to disk.
					string filename = Path.GetFileName(filePath);
					double progress = Math.Min(1, (double)offset / (double)node.StreamLength);
					OnProgress(string.Concat("Recovering ", filename, "..."), progress);
				}
			}
		}

		private void OnProgress(string status, double progress) {
			if (Progress != null) {
				Progress(status, progress);
			}
		}
		public event ProgressEvent Progress;

		private void OnFinished() {
			if (Finished != null) {
				Finished();
			}
		}
		public event Action Finished;
	}
}
