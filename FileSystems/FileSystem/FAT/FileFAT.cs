using System;
using System.Collections.Generic;
using System.Diagnostics;
using KFA.DataStream;
using KFA.Disks;
using FileSystems.FileSystem;

namespace FileSystems.FileSystem.FAT {
    public class FileFAT : File, IDescribable {
        private long m_Length;

        public FileAttributesFAT Attributes { get; private set; }
        public override DateTime LastModified {
            get { return Attributes.LastModified; }
        }
        public long FirstCluster { get; private set; }
        public override long Identifier {
            get { return FirstCluster; }
        }
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
            m_Length = entry.Length;
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
            m_Length = 0;
            while (currentCluster >= 0) {
                currentCluster = FileSystem.GetNextCluster(currentCluster);
                m_Length += FileSystem.BytesPerCluster;
            }
            Attributes = new FileAttributesFAT();
            Deleted = true;
        }

        private Dictionary<long, byte[]> m_ClusterCache = new Dictionary<long, byte[]>();

        public override byte GetByte(ulong offset) {
            return GetBytes(offset, 1)[0];
        }

        public override byte[] GetBytes(ulong _offset, ulong _length) {
            long offset = (long)_offset;
            long length = (long)_length;
            long currentCluster = FirstCluster;

            lock (m_ClusterCache) {
                byte[] res = new byte[length];
                long resindex = 0;
                // Find the first cluster we want to read.
                while (offset >= FileSystem.BytesPerCluster && currentCluster >= 0) {
                    currentCluster = FileSystem.GetNextCluster(currentCluster);
                    offset -= FileSystem.BytesPerCluster;
                }
                // Cache and retrieve the data for each cluster until we get all we need.
                while (length > 0 && currentCluster >= 0) {
                    // Cache the current cluster.
                    if (!m_ClusterCache.ContainsKey(currentCluster)) {
                        m_ClusterCache[currentCluster] = FileSystem.Store.GetBytes(
                            (ulong)FileSystem.GetDiskOffsetOfFATCluster(currentCluster),
                            (ulong)FileSystem.BytesPerCluster);
                    }

                    // Read the cached data.
                    long read = Math.Min(length, FileSystem.BytesPerCluster-offset);
                    Array.Copy(m_ClusterCache[currentCluster], offset, res, resindex, read);
                    offset = 0;
                    length -= read;
                    currentCluster = FileSystem.GetNextCluster(currentCluster);
                }
                return res;
            }
        }

        public override ulong DeviceOffset {
            get { return (ulong)FileSystem.GetDiskOffsetOfFATCluster(FirstCluster); }
        }

        public override ulong StreamLength {
            get { return (ulong)m_Length; }
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
