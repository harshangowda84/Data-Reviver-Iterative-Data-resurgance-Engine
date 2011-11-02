using System;
using System.Text;
using KFA.Disks;
using FileSystems.FileSystem;
using System.Collections.Generic;

namespace FileSystems.FileSystem.FAT {
    /// <summary>
    /// A FAT filesystem. Only deals with FAT16 for now.
    /// </summary>
    public class FileSystemFAT : FileSystem {
        private const int BPB_SIZE = 128;
        #region BPB fields
        string BS_OEMName;
        ushort BPB_BytsPerSec;
        byte BPB_SecPerClus;
        ushort BPB_RsvdSecCnt;
        byte BPB_NumFATs;
        ushort BPB_RootEntCnt;
        ushort BPB_TotSec16;
        byte BPB_Media;
        ushort BPB_FATSz16;
        ushort BPB_SecPerTrk;
        ushort BPB_NumHeads;
        uint BPB_HiddSec;
        uint BPB_TotSec32;

        uint BPB_FATSz32;
        ushort BPB_ExtFlags;
        ushort BPB_FSVer;
        uint BPB_RootClus;
        ushort BPB_FSInfo;
        ushort BPB_BkBootSec;

        private void LoadBPB() {
            byte[] bpb = new byte[BPB_SIZE];
            for (int i = 0; i < BPB_SIZE; i++) {
                bpb[i] = Store.GetByte((ulong) i);
            }
            BS_OEMName = ASCIIEncoding.ASCII.GetString(bpb, 3, 8);
            BPB_BytsPerSec = BitConverter.ToUInt16(bpb, 11);
            BPB_SecPerClus = bpb[13];
            BPB_RsvdSecCnt = BitConverter.ToUInt16(bpb, 14);
            BPB_NumFATs = bpb[16];
            BPB_RootEntCnt = BitConverter.ToUInt16(bpb, 17);
            BPB_TotSec16 = BitConverter.ToUInt16(bpb, 19);
            BPB_Media = bpb[21];
            BPB_FATSz16 = BitConverter.ToUInt16(bpb, 22);
            BPB_SecPerTrk = BitConverter.ToUInt16(bpb, 24);
            BPB_NumHeads = BitConverter.ToUInt16(bpb, 26);
            BPB_HiddSec = BitConverter.ToUInt32(bpb, 28);
            BPB_TotSec32 = BitConverter.ToUInt32(bpb, 32);
            if (Type == PartitionType.FAT32) {
                BPB_FATSz32 = BitConverter.ToUInt32(bpb, 36);
                BPB_ExtFlags = BitConverter.ToUInt16(bpb, 40);
                BPB_FSVer = BitConverter.ToUInt16(bpb, 42);
                BPB_RootClus = BitConverter.ToUInt32(bpb, 44);
                BPB_FSInfo = BitConverter.ToUInt16(bpb, 48);
                BPB_BkBootSec = BitConverter.ToUInt16(bpb, 50);
            }
        }
        #endregion

        long m_FATLocation; // in bytes
        long m_RootDirLocation; // in bytes
        long m_DataLocation; // in bytes

        FileSystemNode m_Root = null;
        public FileSystemFAT(IFileSystemStore store, PartitionType type) {
            Store = store;
            Type = type;
            LoadBPB();
            m_FATLocation = BPB_RsvdSecCnt * BPB_BytsPerSec;
            long RootDirSectors = ((BPB_RootEntCnt * 32) + (BPB_BytsPerSec - 1)) / BPB_BytsPerSec;
            long afterFAT = m_FATLocation + BPB_NumFATs * FATSize * BPB_BytsPerSec;
            m_DataLocation = afterFAT + RootDirSectors * BPB_BytsPerSec;
            if (Type == PartitionType.FAT32) {
                m_RootDirLocation = GetDiskOffsetOfFATCluster(BPB_RootClus);
            } else {
                m_RootDirLocation = afterFAT;
            }
            m_Root = new FolderFAT(this, m_RootDirLocation, 2);
        }

        private uint GetFATEntry(long N) {
            int EntrySize;
            if (Type == PartitionType.FAT16) {
                EntrySize = 2;
            } else {
                //if (Type == PartitionType.FAT32) {
                EntrySize = 4;
            }
            long FATOffset = N * EntrySize;

            if (N < 0 || FATOffset > FATSize * SectorsPerCluster) return 0;

            long FATEntryLoc = m_FATLocation + FATOffset;
            byte[] data = new byte[4];
            for (int i = 0; i < EntrySize; i++) {
                data[i] = Store.GetByte((ulong)(FATEntryLoc + i));
            }
            return BitConverter.ToUInt32(data, 0);
        }

        public long GetNextCluster(long N) {
            uint FATContent = GetFATEntry(N);

            bool eof = false;
            bool bad = false;
            if (FATContent == 0) {
                eof = true;
            }
            if (Type == PartitionType.FAT12) {
                if (FATContent >= 0x0FF8)
                    eof = true;
                if (FATContent == 0x0FF7)
                    bad = true;
            } else if (Type == PartitionType.FAT16) {
                if (FATContent >= 0xFFF8)
                    eof = true;
                if (FATContent == 0xFFF7)
                    bad = true;
            } else if (Type == PartitionType.FAT32) {
                if (FATContent >= 0x0FFFFFF8)
                    eof = true;
                if (FATContent == 0x0FFFFFF7)
                    bad = true;
            }
            if (eof || bad) {
                return -1;
            } else {
                return FATContent;
            }
        }

        public long GetDiskOffsetOfFATCluster(long N) {
            return DataSectionOffset + (N - 2) * BytesPerCluster;
        }

        public long GetFATClusterFromOffset(long cluster) {
            return (cluster - DataSectionOffset) / BytesPerCluster + 2;
        }

        public PartitionType Type { get; private set; }
        public override FileSystemNode GetRoot() {
            return m_Root;
        }
        public long TotalSectors {
            get { return BPB_TotSec16 == 0 ? BPB_TotSec32 : BPB_TotSec16; }
        }
        public long BytesPerSector {
            get { return BPB_BytsPerSec; }
        }
        public long SectorsPerCluster {
            get { return BPB_SecPerClus; }
        }
        public long BytesPerCluster {
            get { return BytesPerSector * SectorsPerCluster; }
        }
        public long DataSectionOffset {
            get { return m_DataLocation; }
        }
        public long FATOffset {
            get { return m_FATLocation; }
        }
        public long FATSize {
            get {
                if (BPB_FATSz16 == 0) {
                    return BPB_FATSz32;
                } else {
                    return BPB_FATSz16;
                }
            }
        }
        public ushort RootEntryCount {
            get { return BPB_RootEntCnt; }
        }
        public uint RootCluster {
            get { return BPB_RootClus; }
        }

        public void SearchByTree(FileSystem.NodeVisitCallback callback, string searchPath) {
            FileSystemNode searchRoot = this.m_Root;
            if (!string.IsNullOrEmpty(searchPath)) {
                searchRoot = this.GetFirstFile(searchPath) ?? searchRoot;
            }
            Visit(callback, searchRoot, new HashSet<long>());
        }

        public void SearchByCluster(FileSystem.NodeVisitCallback callback, string searchPath) {
            SectorSearch(callback);
        }

        public override List<ISearchStrategy> GetSearchStrategies() {
            List<ISearchStrategy> res = new List<ISearchStrategy>();

            // Add the tree search strategy (default)
            res.Add(new SearchStrategy("Folder hierarchy scan", SearchByTree));

            // Add the cluster search strategy
            res.Add(new SearchStrategy("Cluster scan", SearchByCluster));

            return res;
        }

        private void SectorSearch(FileSystem.NodeVisitCallback callback) {
            long clusterNum = BPB_RootClus;
            ulong progress = 0;
            ulong total = (ulong)(TotalSectors * SectorsPerCluster - (m_RootDirLocation + BPB_RootClus * BytesPerCluster));
            while (m_RootDirLocation + clusterNum * BytesPerCluster < TotalSectors * SectorsPerCluster) {
                progress++;
                clusterNum++;
                if (!callback(new FileFAT(this, clusterNum), progress, total)) {
                    break;
                }
            }
        }

        private void Visit(FileSystem.NodeVisitCallback callback, FileSystemNode node, HashSet<long> visitedClusters) {
            if (!callback((INodeMetadata)node, 0, 1)) {
                return;
            }
            if (node is Folder) {  //No zip support yet
                foreach (FileSystemNode child in node.GetChildren()) {
                    if (!visitedClusters.Contains(child.Identifier)) {
                        visitedClusters.Add(child.Identifier);
                        Visit(callback, child, visitedClusters);
                    }
                }
            }
        }

        public override SectorStatus GetSectorStatus(ulong sectorNum) {
            if (sectorNum < BPB_RsvdSecCnt) {
                return SectorStatus.FATReserved;
            } else if ((long)sectorNum < BPB_RsvdSecCnt + BPB_NumFATs * FATSize) {
                return SectorStatus.FATFAT;
            } else {
                uint FATEntry = GetFATEntry(GetFATClusterFromOffset((long)sectorNum * BytesPerSector));
                if (FATEntry == 0x0) {
                    return SectorStatus.FATFree;
                } else if (Type == PartitionType.FAT12 && FATEntry == 0x0FF7
                    || Type == PartitionType.FAT16 && FATEntry == 0xFFF7
                    || Type == PartitionType.FAT32 && FATEntry == 0x0FFFFFF7) {
                    return SectorStatus.FATBad;
                } else {
                    return SectorStatus.FATUsed;
                }
            }
        }
    }
}
