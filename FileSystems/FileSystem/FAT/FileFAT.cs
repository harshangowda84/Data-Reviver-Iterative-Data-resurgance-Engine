using System;
using System.Collections.Generic;
using System.Diagnostics;
using KFA.DataStream;
using KFA.Disks;
using FileSystems.FileSystem;

namespace KFA.FileSystem.FAT {
    public class FileFAT : File, IDescribable {
        public FileAttributesFAT Attributes { get; private set; }
        public long FirstCluster { get; private set; }
        public new FileSystemFAT FileSystem {
            get {
                return (FileSystemFAT)base.FileSystem;
            }
            private set {
                base.FileSystem = value;
            }
        }
        public FileFAT(FileSystemFAT fileSystem, FolderFAT.DirectoryEntry entry, string path) {
            FileSystem = fileSystem;
            Name = entry.FileName;
            Path = path + Name;
            Length = entry.Length;
            Attributes = new FileAttributesFAT(entry);
            FirstCluster = entry.ClusterNum;
            Deleted = Attributes.Deleted;
        }
        public FileFAT(FileSystemFAT fileSystem, long firstCluster) {
            FileSystem = fileSystem;
            FirstCluster = firstCluster;
            Name = Util.GetRandomString(8);
            Path = "?/" + Name;
            long currentCluster = FirstCluster;
            Length = 0;
            while (currentCluster >= 0) {
                currentCluster = FileSystem.GetNextCluster(currentCluster);
                Length += FileSystem.BytesPerCluster;
            }
            Attributes = new FileAttributesFAT();
            Deleted = true;
        }

        private Dictionary<long, byte[]> m_ClusterCache = new Dictionary<long, byte[]>();

        public override byte GetByte(ulong _offset) {
            long offset = (long)_offset;
            long desiredCluster = FirstCluster + offset / FileSystem.BytesPerCluster;
            long modOffset = offset % FileSystem.BytesPerCluster;
            lock (m_ClusterCache) {
                if (!m_ClusterCache.ContainsKey(desiredCluster)) {
                    long currentCluster = FirstCluster;
                    while (offset >= FileSystem.BytesPerCluster) {
                        currentCluster = FileSystem.GetNextCluster(currentCluster);
                        if (currentCluster < 0) {
                            m_ClusterCache[desiredCluster] = null;
                        }
                        offset -= FileSystem.BytesPerCluster;
                    }
                    Debug.Assert(offset == modOffset);
                    byte[] data = new byte[FileSystem.BytesPerCluster];
                    for (int i = 0; i < FileSystem.BytesPerCluster; i++) {
                        data[i] = FileSystem.Store.GetByte((ulong)(FileSystem.GetDiskOffsetOfFATCluster(currentCluster) + i));
                    }
                    m_ClusterCache[desiredCluster] = data;
                }
                if (m_ClusterCache[desiredCluster] == null) {
                    return 0; // deleted file
                } else {
                    return m_ClusterCache[desiredCluster][modOffset];
                }
            }
        }

        public override byte[] GetBytes(ulong offset, ulong length) {
            byte[] res = new byte[length];
            for (uint i = 0; i < length; i++) {
                res[i] = GetByte(offset + i);
            }
            return res;
        }

        public override ulong DeviceOffset {
            get { return (ulong)FileSystem.GetDiskOffsetOfFATCluster(FirstCluster); }
        }

        public override ulong StreamLength {
            get { return (ulong)Length; }
        }

        public override String StreamName {
            get { return "FAT file " + Name; }
        }

        public override IDataStream Parent {
            get { return this.FileSystem.Store; }
        }

        public override void Open() {
            FileSystem.Store.Open();
        }

        public override void Close() {
            FileSystem.Store.Close();
        }

        public string TextDescription {
            get { return Attributes.TextDescription; }
        }
    }
}
