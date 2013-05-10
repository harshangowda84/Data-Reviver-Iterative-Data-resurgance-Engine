// Copyright (C) 2011  Joey Scarr, Josh Oosterman
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
using KFA.Disks;
using System.Collections.Generic;
using System.Collections;

namespace FileSystems.FileSystem.NTFS {
	public class FileSystemNTFS : FileSystem {
		private const int BPB_SIZE = 84;
		ushort BPB_BytsPerSec;
		byte BPB_SecPerClus;
		ushort BPB_SecPerTrk;
		ushort BPB_NumHeads;
		uint BPB_HiddSec;
		ulong BPB_TotSec64;
		ulong BPB_MFTStartCluster64;
		ulong BPB_MFTMirrorStartCluster64;
		ushort BPB_SectorsPerMFTRecord;
		ulong BPB_SerialNumber;

		private void LoadBPB() {
			byte[] bpb = Store.GetBytes((ulong)0x0B, (ulong)BPB_SIZE);

			BPB_BytsPerSec = BitConverter.ToUInt16(bpb, 0);
			BPB_SecPerClus = bpb[2];
			BPB_SecPerTrk = bpb[13];
			BPB_NumHeads = bpb[15];
			BPB_HiddSec = BitConverter.ToUInt32(bpb, 17);
			BPB_TotSec64 = BitConverter.ToUInt64(bpb, 29);
			BPB_MFTStartCluster64 = BitConverter.ToUInt64(bpb, 37);
			BPB_MFTMirrorStartCluster64 = BitConverter.ToUInt64(bpb, 45);

			byte b = bpb[53];
			if (b > 0x80) {
				BPB_SectorsPerMFTRecord = (ushort)(Math.Pow(2, Math.Abs(256 - b)) / BPB_BytsPerSec);
			} else {
				BPB_SectorsPerMFTRecord = (ushort)(BPB_SecPerClus * b);
			}
			BPB_SerialNumber = BitConverter.ToUInt64(bpb, 57);
		}

		private FileSystemNode m_Root = null;
		private FileNTFS m_MFT = null;
		private UInt64 m_mftSector;
		private FileNTFS m_bitmapFile;

		public FileSystemNTFS(IFileSystemStore store) {
			Store = store;

			LoadBPB();

			m_mftSector = (BPB_MFTStartCluster64 * BPB_SecPerClus);
			m_MFT = new FileNTFS(MFTRecord.Create(0, this), "");
			m_Root = new FolderNTFS(MFTRecord.Create(5, this), "");

			m_bitmapFile = new FileNTFS(MFTRecord.Create(6, this), "");
		}

		public override FileSystemNode GetRoot() {
			return m_Root;
		}

		public override string FileSystemType {
			get {
				return "NTFS";
			}
		}

		public override SectorStatus GetSectorStatus(ulong sectorNum) {
			ulong lcn = sectorNum / (ulong)(BPB_SecPerClus);
			return GetClusterStatus(lcn);
		}

		private SectorStatus GetClusterStatus(ulong lcn) {
			Byte b = m_bitmapFile.GetByte(lcn / 8);
			Byte mask = (byte)(0x1 << (int)(lcn % 8));
			if ((b & mask) > 0) {
				return SectorStatus.NTFSUsed;
			} else {
				return SectorStatus.NTFSFree;
			}
		}

		public override FileRecoveryStatus GetChanceOfRecovery(FileSystemNode node) {
			FileNTFS file = node as FileNTFS;
			if (file == null) {
				return FileRecoveryStatus.Unknown;
			} else {
				IEnumerable<Run> runs = file.GetRuns();
				if (runs == null) {
					// The data stream is resident, so recovery is trivial.
					return FileRecoveryStatus.Recoverable;
				} else {
					ulong totalClusters = 0;
					ulong usedClusters = 0;
					// Check the status of each cluster in the runs.
					foreach (Run run in runs) {
						totalClusters += run.Length;
						for (ulong i = run.LCN; i < run.Length; i++) {
							if (GetClusterStatus(run.LCN + i) == SectorStatus.NTFSUsed
									|| GetClusterStatus(run.LCN + i) == SectorStatus.NTFSBad) {
								usedClusters++;
							}
						}
					}
					if (usedClusters == 0) {
						return FileRecoveryStatus.ProbablyRecoverable;
					} else if (usedClusters < totalClusters) {
						return FileRecoveryStatus.PartiallyRecoverable;
					} else {
						return FileRecoveryStatus.Overwritten;
					}
				}
			}
		}

		public FileNTFS MFT {
			get { return m_MFT; }
		}

		public ulong MFTSector {
			get { return m_mftSector; }
		}

		public long BytesPerSector {
			get { return BPB_BytsPerSec; }
		}

		public long SectorsPerMFTRecord {
			get { return BPB_SectorsPerMFTRecord; }
		}

		public long SectorsPerCluster {
			get { return BPB_SecPerClus; }
		}

		public long BytesPerCluster {
			get { return BytesPerSector * SectorsPerCluster; }
		}

		public void SearchByTree(FileSystem.NodeVisitCallback callback, string searchPath) {
			FileSystemNode searchRoot = this.m_Root;
			if (!string.IsNullOrEmpty(searchPath)) {
				searchRoot = this.GetFirstFile(searchPath) ?? searchRoot;
			}
			Visit(callback, searchRoot);
		}

		public void SearchByMFT(FileSystem.NodeVisitCallback callback, string searchPath) {
			MftScan(callback);
		}

		public override List<ISearchStrategy> GetSearchStrategies() {
			List<ISearchStrategy> res = new List<ISearchStrategy>();

			// Add the MFT search strategy (default)
			res.Add(new SearchStrategy("MFT scan", SearchByMFT));

			// Add the tree search strategy
			res.Add(new SearchStrategy("Folder hierarchy scan", SearchByTree));

			return res;
		}

		private void MftScan(FileSystem.NodeVisitCallback callback) {
			ulong numFiles = m_MFT.StreamLength / (ulong)(SectorsPerMFTRecord * BytesPerSector);
			for (ulong i = 0; i < numFiles; i++) {
				MFTRecord record = MFTRecord.Create(i, this, false);
				if (record != null) {
					if (!callback(record, i, numFiles)) {
						break;
					}
				}
			}
		}

		private void Visit(FileSystem.NodeVisitCallback callback, FileSystemNode node) {
			if (!callback(node, 0, 1)) {
				return;
			}
			if (node is Folder && !(node is HiddenDataStreamFileNTFS)) {  //No zip support yet
				foreach (FileSystemNode child in node.GetChildren()) {
					Visit(callback, child);
				}
			}
		}
	}
}
