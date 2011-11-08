using System;
using System.Collections.Generic;
using System.Text;
using KFA.DataStream;
using KFA.Disks;

namespace FileSystems.FileSystem.NTFS {
    public class FolderNTFS : Folder, IDescribable {
        private class IndexBuffer {
            List<IndexEntry> entries = null;
            ulong clusterStart;
            UInt16 entriesStart;
            UInt16 entriesEnd;
            FolderNTFS m_Folder;
            IDataStream m_Stream;
            public IndexBuffer(IDataStream stream, ulong vcn, FolderNTFS folder) {
                m_Folder = folder;
                clusterStart = vcn * (ulong)(m_Folder.m_record.SectorsPerCluster * m_Folder.m_record.BytesPerSector);
                String magic = Util.GetASCIIString(stream, clusterStart + 0x0, 4);

                if (!magic.Equals("INDX")) {
                    throw new Exception("Magic INDX value not present");
                }

                entriesStart = (ushort)(Util.GetUInt16(stream, clusterStart + 0x18) + 0x18);
                entriesEnd = (ushort)(Util.GetUInt16(stream, clusterStart + 0x1c) + entriesStart);

                ushort updateSequenceOffset = Util.GetUInt16(stream, clusterStart + 0x04);
                ushort updateSequenceLength = Util.GetUInt16(stream, clusterStart + 0x06);

                ushort updateSequenceNumber = Util.GetUInt16(stream, clusterStart + updateSequenceOffset);
                ushort[] updateSequenceArray = new ushort[updateSequenceLength - 1];
                ushort read = 1;
                while (read < updateSequenceLength) {
                    updateSequenceArray[read - 1] = Util.GetUInt16(stream, clusterStart + updateSequenceOffset + (ushort)(read * 2));
                    read++;
                }

                m_Stream = new FixupStream(stream, clusterStart, entriesEnd, updateSequenceNumber, updateSequenceArray, (ulong)folder.BytesPerSector);

                if (entriesEnd == entriesStart) {
                    throw new Exception("Entry size was 0");
                }
            }

            private void LoadEntries() {
                entries = new List<IndexEntry>();
                HashSet<ulong> recordNumbers = new HashSet<ulong>(); // to check for dupes
                ulong offset = entriesStart;
                IndexEntry entry;
                do {
                    entry = new IndexEntry(m_Stream, offset, m_Folder);
                    if (!recordNumbers.Contains(entry.RecordNum)) {
                        // check for dupes
                        entries.Add(entry);
                        if (!entry.DummyEntry) {
                            recordNumbers.Add(entry.RecordNum);
                        }
                    }
                    offset += entry.EntryLength;
                } while (!entry.LastEntry && offset < entriesEnd);
            }

            public List<IndexEntry> GetEntries() {
                if (entries == null) {
                    LoadEntries();
                }
                return entries;
            }
        }
        private class IndexEntry {
            private UInt64 indexedFile;
            private UInt16 indexEntryLength;
            private UInt16 filenameOffset;
            private Byte filenameLength;
            private UInt16 flags;
            private UInt64 recordNum;

            private IndexBuffer child = null;
            private FileSystemNode node = null;

            private FolderNTFS m_Folder;
            private ulong m_Offset;
            private IDataStream m_Stream;

            public IndexEntry(IDataStream stream, ulong offset, FolderNTFS folder) {
                m_Folder = folder;
                m_Stream = stream;
                m_Offset = offset;
                ulong mask = 0x0000ffffffffffff; // This is a hack

                indexedFile = Util.GetUInt64(stream, offset);
                indexEntryLength = Util.GetUInt16(stream, offset + 8);
                filenameOffset = Util.GetUInt16(stream, offset + 10);
                flags = stream.GetByte(offset + 12);
                if (indexEntryLength > 0x50) {
                    filenameLength = stream.GetByte(offset + 0x50);
                    Name = Util.GetUnicodeString(stream, offset + 0x52, (ulong)filenameLength * 2);
                    DummyEntry = false;
                } else {
                    // no filename, dummy entry
                    DummyEntry = true;
                }

                recordNum = (indexedFile & mask);
            }

            public bool LastEntry {
                get { return (flags & 2) == 2; }
            }

            public ushort EntryLength {
                get { return indexEntryLength; }
            }

            public ulong RecordNum {
                get { return recordNum; }
            }

            public bool DummyEntry {
                get;
                private set;
            }

            public string Name {
                get;
                private set;
            }

            public FileSystemNode FileSystemNode {
                get {
                    if (node == null && !DummyEntry) {
                        //Not last entry
                        if ((flags & 2) == 0) {
                            //Could read index stream here (i.e. file name etc - but we can just grab the info
                            //from the actual mft record at the cost of efficiency.
                        }

                        if (recordNum != m_Folder.m_record.RecordNum) {
                            MFTRecord record = MFTRecord.Create(recordNum, m_Folder.m_record.FileSystem);
                            node = record.GetFileSystemNode(m_Folder.Path);
                        }
                    }
                    return node;
                }
            }

            public IndexBuffer Child {
                get {
                    if (child == null && m_Folder.m_indexAllocation != null && (flags & 1) > 0) {
                        //This isn't a leaf - points to more index entries
                        UInt64 vcn = Util.GetUInt32(m_Stream, m_Offset + (ulong)(indexEntryLength - 8));
                        child = new IndexBuffer(m_Folder.m_indexAllocation, vcn, m_Folder);
                    }
                    return child;
                }
            }

            public IEnumerable<FileSystemNode> GetNodes() {
                List<FileSystemNode> res = new List<FileSystemNode>();
                if (Child != null) {
                    foreach (IndexEntry entry in Child.GetEntries()) {
                        res.AddRange(entry.GetNodes());
                    }
                }
                if (FileSystemNode != null) {
                    res.Add(FileSystemNode);
                }
                return res;
            }

            public FileSystemNode FindNode(string name) {
                if (Child != null) {
                    name = name.ToUpperInvariant();
                    foreach (IndexEntry entry in Child.GetEntries()) {
                        if (entry.LastEntry) {
                            return entry.FindNode(name);
                        } else if (!string.IsNullOrEmpty(entry.Name)) {
                            string currentName = entry.Name.ToUpper();
                            if (name == currentName) {
                                return entry.FileSystemNode;
                            } else if (name.CompareTo(currentName) < 0) {
                                return entry.FindNode(name);
                            }
                        }
                    }
                }
                return null;
            }
            public override string ToString() {
                return FileSystemNode == null ? "null" : FileSystemNode.Name;
            }
        }

        private MFTRecord m_record;
        private NTFSFileStream m_indexRoot, m_indexAllocation;
        private List<IndexEntry> m_rootEntries = null;

        public FolderNTFS(MFTRecord record, string path) {
            m_record = record;
            m_indexRoot = new NTFSFileStream(m_record.PartitionStream, m_record, "IndexRoot");

            AttributeRecord attr = m_record.GetAttribute("IndexAllocation");
            if (attr != null) {
                m_indexAllocation = new NTFSFileStream(m_record.PartitionStream, m_record, "IndexAllocation");
            }
            Name = record.FileName;
            if (path == null) {
                Path = ComputePath();
            } else if (path == "") { // root
                Root = true;
                Path = "\\";
                foreach (FileSystemNode node in GetChildren("$Volume")) {
                    FileNTFS file = node as FileNTFS;
                    if (file != null && file.VolumeLabel != "") {
                        Name = file.VolumeLabel;
                        break;
                    }
                }
            } else {
                Path = path + Name + "/";
            }
            FileSystem = record.FileSystem;
            Deleted = m_record.Deleted;
        }

        private string ComputePath() {
            
            return "";
            //throw new NotImplementedException();
        }

        public long BytesPerSector {
            get { return m_record.BytesPerSector; }
        }

        public override long Identifier {
            get { return (long)m_record.MFTRecordNumber; }
        }

        private void loadChildrenIndexRoot() {
            NTFSFileStream stream = m_indexRoot;
            m_rootEntries = new List<IndexEntry>();

            //Index Root
            UInt32 attrTypes = Util.GetUInt32(stream, 0x0);
            UInt32 indexBufferSize = Util.GetUInt32(stream, 0x8);
            Byte clustersPerIndexBuffer = stream.GetByte(0xC);
            UInt32 size = Util.GetUInt32(stream, 0x14);
            UInt32 size2 = Util.GetUInt32(stream, 0x18);
            UInt32 flags = Util.GetUInt32(stream, 0x1C);

            ulong offset = 0x20;
            IndexEntry entry;
            do {
                entry = new IndexEntry(stream, offset, this);
                m_rootEntries.Add(entry);
                offset += entry.EntryLength;
            } while (!entry.LastEntry);
        }

        private IEnumerable<IndexEntry> RootEntries {
            get {
                if (m_rootEntries == null) {
                    loadChildrenIndexRoot();
                }
                return m_rootEntries;
            }
        }

        public override void ReloadChildren() {
            loadChildrenIndexRoot();
        }

        public override IEnumerable<FileSystemNode> GetChildren() {
            List<FileSystemNode> res = new List<FileSystemNode>();
            foreach (IndexEntry entry in RootEntries) {
                res.AddRange(entry.GetNodes());
            }
            return res;
        }

        public override IEnumerable<FileSystemNode> GetChildren(string name) {
            if (name == "*") {
                return GetChildren();
            } else {
                List<FileSystemNode> res = new List<FileSystemNode>();
                // Use the B+ tree to efficiently find the child
                name = name.ToUpperInvariant();
                foreach (IndexEntry entry in RootEntries) {
                    if (entry.LastEntry) {
                        FileSystemNode node = entry.FindNode(name);
                        if (node != null) {
                            res.Add(node);
                        }
                        break;
                    } else if (!string.IsNullOrEmpty(entry.Name)) {
                        string currentName = entry.Name.ToUpperInvariant();
                        if (name == currentName) {
                            res.Add(entry.FileSystemNode);
                            break;
                        } else if (name.CompareTo(currentName) < 0) {
                            FileSystemNode node = entry.FindNode(name);
                            if (node != null) {
                                res.Add(node);
                            }
                            break;
                        }
                    }
                }
                return res;
            }
        }

        public override byte GetByte(ulong offset) {
            return m_indexRoot.GetByte(offset);
        }

        public override byte[] GetBytes(ulong offset, ulong length) {
            return m_indexRoot.GetBytes(offset, length);
        }

        public override ulong DeviceOffset {
            get { return m_indexRoot.DeviceOffset; }
        }

        public override ulong StreamLength {
            get { return m_indexRoot.StreamLength; }
        }

        public override String StreamName {
            get { return "NTFS Directory " + m_record.FileName; }
        }

        public override IDataStream Parent {
            get { return m_record.PartitionStream; }
        }

        public override void Open() { }

        public override void Close() { }

        public DateTime CreationTime {
            get { return m_record.fileCreationTime; }
        }

        public DateTime LastAccessed {
            get { return m_record.fileLastAccessTime; }
        }

        public override DateTime LastModified {
            get { return m_record.fileLastDataChangeTime; }
        }

        public DateTime LastModifiedMFT {
            get { return m_record.fileLastMFTChangeTime; }
        }

        public bool Root {
            get;
            private set;
        }

        #region IDescribable Members

        public string TextDescription {
            get {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}: {1}\r\n", "Name", Name);
                sb.AppendFormat("{0}: {1}\r\n", "Size", Util.ByteFormat(StreamLength));
                sb.AppendFormat("{0}: {1}\r\n", "Deleted", Deleted);
                sb.AppendFormat("{0}: {1}\r\n", "Created", CreationTime);
                sb.AppendFormat("{0}: {1}\r\n", "Last Modified", LastModified);
                sb.AppendFormat("{0}: {1}\r\n", "MFT Record Last Modified", LastModifiedMFT);
                sb.AppendFormat("{0}: {1}\r\n", "Last Accessed", LastAccessed);
                return sb.ToString();
            }
        }

        #endregion
    }
}
