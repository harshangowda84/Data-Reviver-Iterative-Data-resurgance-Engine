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
using System.IO;

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
		private byte[] m_Bitmap;

		Dictionary<ulong, ulong> m_ParentLinks = new Dictionary<ulong, ulong>();
		Dictionary<ulong, string> m_RecordNames = new Dictionary<ulong, string>();
		Dictionary<ulong, string> m_RecordPaths = new Dictionary<ulong, string>();

		public FileSystemNTFS(IFileSystemStore store) {
			Store = store;

			LoadBPB();

			m_mftSector = (BPB_MFTStartCluster64 * BPB_SecPerClus);
			m_MFT = new FileNTFS(MFTRecord.Create(0, this), "");
			m_Root = new FolderNTFS(MFTRecord.Create(5, this), "", true);

			m_bitmapFile = new FileNTFS(MFTRecord.Create(6, this), "");
			m_Bitmap = m_bitmapFile.GetBytes(0, m_bitmapFile.StreamLength);
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
			if (lcn / 8 >= (ulong)m_Bitmap.Length) {
				Console.Error.WriteLine(string.Format("ERROR: Tried to read off the end of " +
					"the $Bitmap file. $Bitmap length = {0}, lcn = {1}, lcn / 8 = {2}",
					m_Bitmap.Length, lcn, lcn / 8));
				return SectorStatus.Unknown;
			}
			Byte b = m_Bitmap[lcn / 8];
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
				IEnumerable<NTFSDataRun> runs = file.GetRuns();
				if (runs == null) {
					// The data stream is resident, so recovery is trivial.
					return FileRecoveryStatus.Recoverable;
				} else {
					ulong totalClusters = 0;
					ulong usedClusters = 0;
					// Check the status of each cluster in the runs.
					foreach (NTFSDataRun run in runs) {
						if (run.HasRealClusters) {
							totalClusters += run.Length;
							for (ulong i = run.LCN; i < run.Length; i++) {
								if (GetClusterStatus(run.LCN + i) == SectorStatus.NTFSUsed
										|| GetClusterStatus(run.LCN + i) == SectorStatus.NTFSBad) {
									usedClusters++;
								}
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

		public long BytesPerMFTRecord {
			get { return BytesPerSector * SectorsPerMFTRecord; }
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
				MFTRecord record = MFTRecord.Create(i, this, MftLoadDepth.NameAndParentOnly);

				if (record != null) {
					if (!callback(record, i, numFiles)) {
						return;
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
