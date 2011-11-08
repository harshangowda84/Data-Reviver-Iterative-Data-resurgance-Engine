using System;
using System.Collections.Generic;
using KFA.DataStream;
using KFA.Exceptions;
using FileSystems.FileSystem;

namespace FileSystems.FileSystem.NTFS {

    #region Attribute Structs

    public class AttributeRecord {
        public MFTRecord.AttributeType type;
        public UInt32 Length;
        public bool NonResident;
        public bool Compressed;
        public byte NameLength;
        public UInt16 NameOffset;
        public String Name;
        public UInt16 Flags;
        public UInt16 Instance;
        public UInt16 Id;

        /* Used for resident */
        public UInt32 ValueLength;
        public UInt16 ValueOffset;
        public Byte ResidentFlags;
        public IDataStream value;

        /* Used for non resident */
        public Int64 lowVCN, highVCN;
        public UInt32 MappingPairsOffset;
        public Byte CompressionUnit;
        public UInt64 AllocatedSize, DataSize, InitialisedSize;
        public UInt64 CompressedSize;

        public List<Run> Runs;

    };

    #endregion

    public class Run : IDataStream {
        private ulong m_vcn, m_lcn, m_length, m_bytesPerCluster, m_lengthInBytes;
        private MFTRecord m_record;
        public ulong VCN { get { return m_vcn; } }
        public ulong LCN { get { return m_lcn; } }
        public ulong Length { get { return m_length; } }
        public Run(ulong vcn, ulong lcn, ulong length, MFTRecord record) {
            m_vcn = vcn;
            m_lcn = lcn;
            m_length = length;
            m_record = record;
            m_bytesPerCluster = (ulong)(m_record.BytesPerSector * m_record.SectorsPerCluster);
            m_lengthInBytes = m_length * m_bytesPerCluster;
        }

        public bool Contains(ulong vcn) {
            return vcn >= VCN && vcn < VCN + Length;
        }

        #region IDataStream Members

        public virtual byte GetByte(ulong offset) {
            if (offset < m_lengthInBytes) {
                return m_record.PartitionStream.GetByte(LCN * m_bytesPerCluster + offset);
            } else {
                throw new Exception("Offset does not exist in this run!");
            }
        }

        public virtual byte[] GetBytes(ulong offset, ulong length) {
            if (offset + length - 1 < m_lengthInBytes) {
                return m_record.PartitionStream.GetBytes(LCN * m_bytesPerCluster + offset, length);
            } else {
                throw new Exception("Offset does not exist in this run!");
            }
        }

        public ulong StreamLength {
            get { return m_lengthInBytes; }
        }

        public string StreamName {
            get { return "Non-resident Attribute Run"; }
        }

        public IDataStream Parent {
            get { return m_record.PartitionStream; }
        }

        public ulong DeviceOffset {
            get { return Parent.DeviceOffset + LCN * m_bytesPerCluster; }
        }

        public void Open() {
            m_record.PartitionStream.Open();
        }

        public void Close() {
            m_record.PartitionStream.Close();
        }

        #endregion

        public override string ToString() {
            return string.Format("Run: VCN {0}, Length {1}, LCN {2}", VCN, Length, LCN);
        }
    };

    public class SparseRun : Run {
        public SparseRun(ulong vcn, ulong length, MFTRecord record) :
            base(vcn, 0, length, record) { }

        public override byte GetByte(ulong offset) {
            return 0;
        }

        public override string ToString() {
            return "Sparse " + base.ToString();
        }
    }

    public class MFTRecord : INodeMetadata {

        #region Attribute Enums

        public enum FilenameType {
            Posix = 1,
            Win32 = 2,
            Dos = 4,
            DosAndWin32 = 8
        };

        public enum RecordFlags {
            InUse = 1,
            Directory = 2,
            Is4 = 4, //?
            ViewIndex = 8,
            SpaceFiller = 16
        };

        public enum AttributeType {
            Unused = 0,
            StandardInformation = 0x10,
            AttributeList = 0x20,
            FileName = 0x30,
            ObjectId = 0x40,
            SecurityDescriptor = 0x50,
            VolumeName = 0x60,
            VolumeInformation = 0x70,
            Data = 0x80,
            IndexRoot = 0x90,
            IndexAllocation = 0xa0,
            Bitmap = 0xb0,
            ReparseFont = 0xc0,
            EAInformation = 0xd0,
            EA = 0xe0,
            PropertySet = 0xf0,
            LoggedUtilityStream = 0x100,
            FirstUserDefinedAttribute = 0x1000,
            End = 0xffffff
        };

        #endregion

        #region Fields

        public byte[] record_Magic;
        public UInt16 record_Ofs, record_Count;
        public UInt64 LogSequenceNumber;
        public UInt16 SequenceNumber, record_NumHardLinks;
        public UInt16 AttributeOffset;
        public UInt16 Flags;
        public UInt32 BytesInUse;
        public UInt32 BytesAllocated;
        public UInt64 BaseMFTRecord;
        public UInt16 NextAttrInstance;
        public UInt16 Reserved;
        public UInt32 MFTRecordNumber;

        public UInt64 ParentDirectory;
        public DateTime fileCreationTime, fileLastDataChangeTime, fileLastMFTChangeTime, fileLastAccessTime;
        public UInt64 AllocatedSize, ActualSize;
        public Int32 _Attributes;
        public Int32 _Attributes2;
        public Byte FileNameLength;
        public UInt16 FileNameType;
        public String FileName;
        public string VolumeLabel;

        public long BytesPerSector;
        public long SectorsPerCluster;
        public ulong RecordNum, StartOffset;
        public FileSystemNTFS FileSystem;
        public IDataStream PartitionStream;
        public List<AttributeRecord> Attributes;

        #endregion

        private FileSystemNode m_Node = null;
        private IDataStream m_Stream = null;
        private bool m_DataLoaded = false;

        public static MFTRecord Create(ulong recordNum, FileSystemNTFS fileSystem) {
            return Create(recordNum, fileSystem, true);
        }

        public static MFTRecord Create(ulong recordNum, FileSystemNTFS fileSystem, bool loadData) {
            ulong startOffset = recordNum * (ulong)fileSystem.SectorsPerMFTRecord * (ulong)fileSystem.BytesPerSector;

            IDataStream stream;

            //Special case for MFT - can't read itself
            if (recordNum == 0) {
                stream = new SubStream(fileSystem.Store, fileSystem.MFTSector * (ulong)fileSystem.BytesPerSector, (ulong)(fileSystem.SectorsPerMFTRecord * fileSystem.BytesPerSector));
            } else {
                stream = new SubStream(fileSystem.MFT, startOffset, (ulong)(fileSystem.SectorsPerMFTRecord * fileSystem.BytesPerSector));
            }

            string Magic = Util.GetASCIIString(stream, 0, 4);
            if (!Magic.Equals("FILE")) {
                return null;
            }

            return new MFTRecord(recordNum, fileSystem, stream, loadData);
        }

        private MFTRecord(ulong recordNum, FileSystemNTFS fileSystem, IDataStream stream, bool loadData) {
            this.RecordNum = recordNum;
            this.FileSystem = fileSystem;
            this.BytesPerSector = fileSystem.BytesPerSector;
            this.SectorsPerCluster = fileSystem.SectorsPerCluster;
            this.PartitionStream = fileSystem.Store;

            m_Stream = stream;

            Flags = Util.GetUInt16(m_Stream, 22);

            if (loadData) {
                LoadData();
            }
        }

        private void LoadData() {
            if (m_DataLoaded) return;
            m_DataLoaded = true;

            ushort updateSequenceOffset = Util.GetUInt16(m_Stream, 0x04);
            ushort updateSequenceLength = Util.GetUInt16(m_Stream, 0x06);

            ushort updateSequenceNumber = Util.GetUInt16(m_Stream, updateSequenceOffset);
            ushort[] updateSequenceArray = new ushort[updateSequenceLength - 1];
            ushort read = 1;
            while (read < updateSequenceLength) {
                updateSequenceArray[read - 1] = Util.GetUInt16(m_Stream, (ushort)(updateSequenceOffset + read * 2));
                read++;
            }

            FixupStream fixedStream = new FixupStream(m_Stream, 0, m_Stream.StreamLength, updateSequenceNumber, updateSequenceArray, (ulong)BytesPerSector);

            LoadHeader(fixedStream);
            LoadAttributes(fixedStream, AttributeOffset);

            if (Attributes.Count == 0) {
                //throw new InvalidFILERecordException(FileSystem, fixedStream.DeviceOffset, "MFT record had no attributes.");
            }
        }

        public static DateTime fromNTFS(ulong time) {
            try {
                return (new DateTime(1601, 1, 1)).AddMilliseconds((double)time / 10000.0);
            } catch (Exception) {
                return new DateTime(1601, 1, 1);
            }
        }

        public bool Deleted {
            get {
                return (Flags & (ushort)RecordFlags.InUse) == 0;
            }
        }

        public DateTime LastModified {
            get {
                return fileLastDataChangeTime;
            }
        }

        public FileSystemNode GetFileSystemNode() {
            return GetFileSystemNode(null);
        }

        public FileSystemNode GetFileSystemNode(String path) {
            LoadData();
            if (m_Node == null) {
                if ((Flags & (int)MFTRecord.RecordFlags.Directory) > 0) {
                    m_Node = new FolderNTFS(this, path);
                } else if (HiddenDataStreamFileNTFS.GetHiddenDataStreams(this).Count > 0) {
                    m_Node = new HiddenDataStreamFileNTFS(this, path);
                } else {
                    m_Node = new FileNTFS(this, path);
                }
            }
            return m_Node;
        }

        public AttributeRecord GetAttribute(String name) {
            LoadData();
            try {
                AttributeType flag = (AttributeType)Enum.Parse(typeof(AttributeType), name);
                foreach (AttributeRecord attr in Attributes) {
                    if (attr.type == flag) {
                        return attr;
                    }
                }
            } catch { }
            return null;
        }

        private void LoadHeader(IDataStream stream) {
            record_Magic = stream.GetBytes(0, 4);
            record_Ofs = Util.GetUInt16(stream, 4);
            record_Count = Util.GetUInt16(stream, 6);
            LogSequenceNumber = Util.GetUInt64(stream, 8);
            SequenceNumber = Util.GetUInt16(stream, 16);
            record_NumHardLinks = Util.GetUInt16(stream, 18);
            AttributeOffset = Util.GetUInt16(stream, 20);
            Flags = Util.GetUInt16(stream, 22);
            BytesInUse = Util.GetUInt32(stream, 24);
            BytesAllocated = Util.GetUInt32(stream, 28);
            BaseMFTRecord = Util.GetUInt64(stream, 32);
            NextAttrInstance = Util.GetUInt16(stream, 40);
            Reserved = Util.GetUInt16(stream, 42);
            MFTRecordNumber = Util.GetUInt32(stream, 44);
        }

        private void LoadAttributes(IDataStream stream, ulong startOffset) {
            Attributes = new List<AttributeRecord>();
            while (true) {
                //Align to 8 byte boundary
                if (startOffset % 8 != 0) {
                    startOffset = (startOffset / 8 + 1) * 8;
                }

                //0xFF... marks end of attributes;
                if (Util.GetUInt32(stream, startOffset) == 0xFFFFFFFF) {
                    break;
                }

                AttributeRecord attr = new AttributeRecord();
                attr.type = (AttributeType)Util.GetUInt32(stream, startOffset + 0);
                attr.Length = Util.GetUInt16(stream, startOffset + 4);
                attr.NonResident = stream.GetByte(startOffset + 8) > 0;
                attr.NameLength = stream.GetByte(startOffset + 9);
                attr.NameOffset = Util.GetUInt16(stream, startOffset + 10);
                attr.Compressed = stream.GetByte(startOffset + 0xC) > 0;
                attr.Id = Util.GetUInt16(stream, startOffset + 0xE);
                if (attr.NameLength > 0) {
                    attr.Name = Util.GetUnicodeString(stream, startOffset + attr.NameOffset, (ulong)(attr.NameLength * 2));
                }
                attr.Flags = Util.GetUInt16(stream, startOffset + 12);
                attr.Instance = Util.GetUInt16(stream, startOffset + 14);
                bool success = true;
                if (!attr.NonResident) {
                    LoadResidentAttribute(stream, startOffset, attr);
                    if ((AttributeType)attr.type == AttributeType.StandardInformation) {
                        LoadStandardAttributes(stream, startOffset + attr.ValueOffset);
                    } else if ((AttributeType)attr.type == AttributeType.FileName) {
                        LoadNameAttributes(stream, startOffset + attr.ValueOffset);
                    } else if ((AttributeType)attr.type == AttributeType.AttributeList) {
                        LoadExternalAttributeList(attr.value, attr);
                    } else if ((AttributeType)attr.type == AttributeType.VolumeName) {
                        LoadVolumeNameAttributes(stream, startOffset + attr.ValueOffset, (ulong)attr.ValueLength);
                    }
                } else {
                    success = LoadNonResidentAttribute(stream, startOffset, attr);
                }
                if (success) {
                    Attributes.Add(attr);
                }

                startOffset += attr.Length;
            }
        }

        private void LoadResidentAttribute(IDataStream stream, ulong startOffset, AttributeRecord attr) {
            attr.ValueLength = Util.GetUInt32(stream, startOffset + 16);
            attr.ValueOffset = Util.GetUInt16(stream, startOffset + 20);
            attr.ResidentFlags = stream.GetByte(startOffset + 22);
            attr.value = new SubStream(stream, startOffset + attr.ValueOffset, attr.ValueLength);
        }

        private bool LoadNonResidentAttribute(IDataStream stream, ulong startOffset, AttributeRecord attr) {
            attr.lowVCN = Util.GetInt32(stream, startOffset + 16);
            attr.highVCN = Util.GetInt64(stream, startOffset + 24);

            attr.MappingPairsOffset = Util.GetUInt16(stream, startOffset + 32);
            attr.CompressionUnit = stream.GetByte(startOffset + 34);
            attr.AllocatedSize = Util.GetUInt64(stream, startOffset + 40);
            attr.DataSize = Util.GetUInt64(stream, startOffset + 48);
            attr.InitialisedSize = Util.GetUInt64(stream, startOffset + 56);
            attr.ValueLength = (uint)attr.DataSize;
            if (attr.CompressionUnit > 0) {
                attr.CompressedSize = Util.GetUInt64(stream, startOffset + 64);
                return false;
            }

            attr.Runs = new List<Run>();
            ulong cur_vcn = (ulong)attr.lowVCN;
            ulong lcn = 0;
            ulong offset = startOffset + attr.MappingPairsOffset;
            ulong endOffset = startOffset + attr.Length;

            while (offset < endOffset && cur_vcn <= (ulong)attr.highVCN && stream.GetByte(offset) > 0) {
                ulong length;

                byte F = (Byte)((stream.GetByte(offset) >> 4) & 0xf);
                byte L = (Byte)(stream.GetByte(offset) & 0xf);

                if (L == 0 || L > 8) {
                    // The length is mandatory and must be at most 8 bytes.
                    // The data is therefore corrupt, so ignore this whole attribute
                    return false;
                } else {
                    // Read in the length
                    length = Util.GetArbitraryUInt(stream, offset + 1, L);
                    if (F > 0 && length > 100000000000000) { // 100 TB limit for now, this is kind of a hack
                        // The data is corrupt, so ignore this whole attribute
                        return false;
                    }
                }

                if (F == 0) {
                    // This is a sparse run
                    attr.Runs.Add(new SparseRun(cur_vcn, (ulong)length, this));
                } else {
                    //if (vcn + run.length > attr.highVCN) break; // data is corrupt

                    try {
                        lcn = (ulong)((long)lcn + Util.GetArbitraryInt(stream, offset + 1 + L, F));
                    } catch {
                        return false;
                    }

                    Run run = new Run(cur_vcn, lcn, (ulong)length, this);
                    attr.Runs.Add(run);
                }
                cur_vcn += (ulong)length;

                offset += (ulong)(F + L + 1);
            }
            return true;
        }

        private void LoadStandardAttributes(IDataStream stream, ulong startOffset) {
            fileCreationTime = fromNTFS(Util.GetUInt64(stream, startOffset));
            fileLastDataChangeTime = fromNTFS(Util.GetUInt64(stream, startOffset + 8));
            fileLastMFTChangeTime = fromNTFS(Util.GetUInt64(stream, startOffset + 16));
            fileLastAccessTime = fromNTFS(Util.GetUInt64(stream, startOffset + 24));
            _Attributes = Util.GetInt32(stream, startOffset + 32);
        }

        private void LoadNameAttributes(IDataStream stream, ulong startOffset) {
            ParentDirectory = Util.GetUInt64(stream, (ulong)(startOffset));
            fileCreationTime = fromNTFS(Util.GetUInt64(stream, startOffset + 8));
            fileLastDataChangeTime = fromNTFS(Util.GetUInt64(stream, startOffset + 16));
            fileLastMFTChangeTime = fromNTFS(Util.GetUInt64(stream, startOffset + 24));
            fileLastAccessTime = fromNTFS(Util.GetUInt64(stream, startOffset + 32));
            AllocatedSize = Util.GetUInt64(stream, startOffset + 40);
            ActualSize = Util.GetUInt64(stream, startOffset + 48);
            _Attributes = Util.GetInt32(stream, startOffset + 56);
            _Attributes2 = Util.GetInt32(stream, startOffset + 58);
            FileNameLength = stream.GetByte(startOffset + 64);
            FileNameType = stream.GetByte(startOffset + 65);
            if (FileName == null || FileName.Contains("~")) {
                FileName = Util.GetUnicodeString(stream, startOffset + 66, (ulong)(FileNameLength * 2));
            }
        }

        private void LoadVolumeNameAttributes(IDataStream stream, ulong startOffset, ulong length) {
            VolumeLabel = Util.GetUnicodeString(stream, startOffset, length);
        }

        private void LoadExternalAttributeList(IDataStream stream, AttributeRecord attrList) {
            ulong offset = 0;
            while (true) {
                //Align to 8 byte boundary
                if (offset % 8 != 0) {
                    offset = (offset / 8 + 1) * 8;
                }

                //0xFF... marks end of attributes;
                if (offset == attrList.ValueLength || Util.GetUInt32(stream, offset) == 0xFFFFFFFF) {
                    break;
                }

                AttributeRecord attr = new AttributeRecord();
                attr.type = (AttributeType)Util.GetUInt32(stream, offset + 0x0);
                attr.Length = Util.GetUInt16(stream, offset + 0x4);
                attr.NameLength = stream.GetByte(offset + 0x6);
                attr.Id = Util.GetUInt16(stream, offset + 0x18);

                ulong vcn = Util.GetUInt64(stream, offset + 0x8);
                ulong fileRef = (Util.GetUInt64(stream, offset + 0x10) & 0x0000FFFFFFFFFFFF);
                if (fileRef != this.MFTRecordNumber && fileRef != RecordNum) {
                    MFTRecord mftRec = MFTRecord.Create(fileRef, this.FileSystem);
                    foreach (AttributeRecord attr2 in mftRec.Attributes) {
                        if (attr.Id == attr2.Id) {
                            if (attr2.NonResident && attr2.type == AttributeType.Data) {
                                // Find the corresponding data attribute on this record and merge the runlists
                                bool merged = false;
                                foreach (AttributeRecord rec in Attributes) {
                                    if (rec.type == AttributeType.Data && attr2.Name == rec.Name) {
                                        MergeRunLists(ref rec.Runs, attr2.Runs);
                                        merged = true;
                                        break;
                                    }
                                }
                                if (!merged) {
                                    this.Attributes.Add(attr2);
                                }
                            } else {
                                this.Attributes.Add(attr2);
                            }
                        }
                    }
                }
                if (attr.NameLength > 0) {
                    attr.Name = Util.GetUnicodeString(stream, StartOffset + 0x1A, (ulong)(attr.NameLength * 2));
                }

                ulong startByte = vcn * (ulong)FileSystem.BytesPerCluster;
                attr.value = new SubStream(attrList.value, startByte, startByte + attr.Length);
                offset += 0x1A + (ulong)(attr.NameLength * 2);
            }
        }

        private void MergeRunLists(ref List<Run> list1, List<Run> list2) {
            list1.AddRange(list2);
            // TODO: Verify that the runlists don't overlap
        }

        #region INodeMetadata Members

        public string Name {
            get {
                LoadData();
                return FileName;
            }
        }

        public ulong Size {
            get {
                LoadData();
                return ActualSize;
            }
        }

        #endregion
    }
}
