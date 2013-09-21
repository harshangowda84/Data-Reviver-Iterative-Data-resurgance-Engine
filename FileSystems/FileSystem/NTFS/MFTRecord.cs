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
using System.Collections.Generic;
using KFA.DataStream;
using KFA.Exceptions;
using FileSystems.FileSystem;
using System.Text;

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

		public virtual bool HasRealClusters {
			get { return true; }
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

		public IDataStream ParentStream {
			get { return m_record.PartitionStream; }
		}

		public ulong DeviceOffset {
			get { return ParentStream.DeviceOffset + LCN * m_bytesPerCluster; }
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

		public override bool HasRealClusters {
			get { return false; }
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

		public UInt64 ParentDirectory = 0;
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
		private byte[] m_Data;
		private IDataStream m_Stream;
		private bool m_DataLoaded = false;
		private string m_Path = "";

		public static MFTRecord Create(ulong recordNum, FileSystemNTFS fileSystem, bool loadAllData = true, bool loadOnlyParentName = false, string path = "") {
			ulong startOffset = recordNum * (ulong)fileSystem.SectorsPerMFTRecord * (ulong)fileSystem.BytesPerSector;

			IDataStream stream;

			//Special case for MFT - can't read itself
			if (recordNum == 0) {
				stream = new SubStream(fileSystem.Store, fileSystem.MFTSector * (ulong)fileSystem.BytesPerSector, (ulong)(fileSystem.SectorsPerMFTRecord * fileSystem.BytesPerSector));
			} else {
				stream = new SubStream(fileSystem.MFT, startOffset, (ulong)(fileSystem.SectorsPerMFTRecord * fileSystem.BytesPerSector));
			}

			// Read the whole record into memory
			byte[] data = stream.GetBytes(0, stream.StreamLength);

			string Magic = Encoding.ASCII.GetString(data, 0, 4);
			if (!Magic.Equals("FILE")) {
				return null;
			}

			return new MFTRecord(recordNum, fileSystem, data, stream, loadAllData, loadOnlyParentName, path);
		}

		private MFTRecord(ulong recordNum, FileSystemNTFS fileSystem,
				byte[] data, IDataStream stream, bool loadAllData, bool loadOnlyParentName,
				string path) {
			this.RecordNum = recordNum;
			this.FileSystem = fileSystem;
			this.BytesPerSector = fileSystem.BytesPerSector;
			this.SectorsPerCluster = fileSystem.SectorsPerCluster;
			this.PartitionStream = fileSystem.Store;

			m_Data = data;
			m_Stream = stream;
			m_Path = path;

			Flags = BitConverter.ToUInt16(m_Data, 22);

			if (loadAllData || loadOnlyParentName) {
				LoadData(loadOnlyParentName);
			}
		}

		private void LoadData(bool loadOnlyParentName = false) {
			if (m_DataLoaded) {
				return;
			}
			if (!loadOnlyParentName) {
				m_DataLoaded = true;
			}

			ushort updateSequenceOffset = BitConverter.ToUInt16(m_Data, 4);
			ushort updateSequenceLength = BitConverter.ToUInt16(m_Data, 6);

			ushort updateSequenceNumber = BitConverter.ToUInt16(m_Data, updateSequenceOffset);
			ushort[] updateSequenceArray = new ushort[updateSequenceLength - 1];
			ushort read = 1;
			while (read < updateSequenceLength) {
				updateSequenceArray[read - 1] = BitConverter.ToUInt16(m_Data, (ushort)(updateSequenceOffset + read * 2));
				read++;
			}

			// Apply fixups to both the stream and the in-memory array
			m_Stream = new FixupStream(m_Stream, 0, m_Stream.StreamLength, updateSequenceNumber, updateSequenceArray, (ulong)BytesPerSector);
			try {
				FixupStream.FixArray(m_Data, updateSequenceNumber, updateSequenceArray, (int)BytesPerSector);
			} catch (NTFSFixupException e) {
				Console.Error.WriteLine(e);
				// This record is invalid, so don't read any more.
				return;
			}

			LoadHeader();
			LoadAttributes(AttributeOffset, loadOnlyParentName);

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
			if (m_Node == null) {
				return GetFileSystemNode(m_Path);
			}
			return m_Node;
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

		private void LoadHeader() {
			// record_Magic = data[0..4]
			record_Ofs = BitConverter.ToUInt16(m_Data, 4);
			record_Count = BitConverter.ToUInt16(m_Data, 6);
			LogSequenceNumber = BitConverter.ToUInt64(m_Data, 8);
			SequenceNumber = BitConverter.ToUInt16(m_Data, 16);
			record_NumHardLinks = BitConverter.ToUInt16(m_Data, 18);
			AttributeOffset = BitConverter.ToUInt16(m_Data, 20);
			Flags = BitConverter.ToUInt16(m_Data, 22);
			BytesInUse = BitConverter.ToUInt32(m_Data, 24);
			BytesAllocated = BitConverter.ToUInt32(m_Data, 28);
			BaseMFTRecord = BitConverter.ToUInt64(m_Data, 32);
			NextAttrInstance = BitConverter.ToUInt16(m_Data, 40);
			Reserved = BitConverter.ToUInt16(m_Data, 42);
			MFTRecordNumber = BitConverter.ToUInt32(m_Data, 44);
		}

		private void LoadAttributes(int startOffset, bool loadOnlyNameAttr) {
			Attributes = new List<AttributeRecord>();
			while (true) {
				//Align to 8 byte boundary
				if (startOffset % 8 != 0) {
					startOffset = (startOffset / 8 + 1) * 8;
				}

				//0xFF... marks end of attributes;
				if (BitConverter.ToUInt32(m_Data, startOffset) == 0xFFFFFFFF) {
					break;
				}

				AttributeRecord attr = new AttributeRecord();
				attr.type = (AttributeType)BitConverter.ToUInt32(m_Data, startOffset + 0);
				attr.Length = BitConverter.ToUInt16(m_Data, startOffset + 4);
				if (loadOnlyNameAttr && (AttributeType)attr.type != AttributeType.FileName) {
					startOffset += (int)attr.Length;
					continue;
				}
				attr.NonResident = m_Data[startOffset + 8] > 0;
				attr.NameLength = m_Data[startOffset + 9];
				attr.NameOffset = BitConverter.ToUInt16(m_Data, startOffset + 10);
				attr.Compressed = m_Data[startOffset + 0xC] > 0;
				attr.Id = BitConverter.ToUInt16(m_Data, startOffset + 0xE);
				if (attr.NameLength > 0) {
					attr.Name = Encoding.Unicode.GetString(m_Data, startOffset + attr.NameOffset, attr.NameLength * 2);
				}
				attr.Flags = BitConverter.ToUInt16(m_Data, startOffset + 12);
				attr.Instance = BitConverter.ToUInt16(m_Data, startOffset + 14);
				bool success = true;
				if (!attr.NonResident) {
					LoadResidentAttribute(startOffset, attr);
					if ((AttributeType)attr.type == AttributeType.StandardInformation) {
						LoadStandardAttributes(startOffset + attr.ValueOffset);
					} else if ((AttributeType)attr.type == AttributeType.FileName) {
						LoadNameAttributes(startOffset + attr.ValueOffset);
					} else if ((AttributeType)attr.type == AttributeType.AttributeList) {
						LoadExternalAttributeList(startOffset + attr.ValueOffset, attr);
					} else if ((AttributeType)attr.type == AttributeType.VolumeName) {
						LoadVolumeNameAttributes(startOffset + attr.ValueOffset, (int)attr.ValueLength);
					}
				} else {
					success = LoadNonResidentAttribute(startOffset, attr);
				}
				if (success) {
					Attributes.Add(attr);
				}

				startOffset += (int)attr.Length;
			}
		}

		private void LoadResidentAttribute(int startOffset, AttributeRecord attr) {
			attr.ValueLength = BitConverter.ToUInt32(m_Data, startOffset + 16);
			attr.ValueOffset = BitConverter.ToUInt16(m_Data, startOffset + 20);
			attr.ResidentFlags = m_Data[startOffset + 22];
			attr.value = new SubStream(m_Stream, (ulong)(startOffset + attr.ValueOffset), attr.ValueLength);
		}

		private bool LoadNonResidentAttribute(int startOffset, AttributeRecord attr) {
			attr.lowVCN = BitConverter.ToInt32(m_Data, startOffset + 16);
			attr.highVCN = BitConverter.ToInt64(m_Data, startOffset + 24);

			attr.MappingPairsOffset = BitConverter.ToUInt16(m_Data, startOffset + 32);
			attr.CompressionUnit = m_Data[startOffset + 34];
			attr.AllocatedSize = BitConverter.ToUInt64(m_Data, startOffset + 40);
			attr.DataSize = BitConverter.ToUInt64(m_Data, startOffset + 48);
			attr.InitialisedSize = BitConverter.ToUInt64(m_Data, startOffset + 56);
			attr.ValueLength = (uint)attr.DataSize;
			if (attr.CompressionUnit > 0) {
				attr.CompressedSize = BitConverter.ToUInt64(m_Data, startOffset + 64);
				return false;
			}

			attr.Runs = new List<Run>();
			ulong cur_vcn = (ulong)attr.lowVCN;
			ulong lcn = 0;
			ulong offset = (ulong)startOffset + attr.MappingPairsOffset;
			ulong endOffset = (ulong)startOffset + attr.Length;

			while (offset < endOffset && cur_vcn <= (ulong)attr.highVCN && m_Data[offset] > 0) {
				ulong length;

				/*byte F = (Byte)((m_Data[offset] >> 4) & 0xf);
				byte L = (Byte)(m_Data[offset] & 0xf);*/
				byte F = (Byte)((m_Stream.GetByte(offset) >> 4) & 0xf);
				byte L = (Byte)(m_Stream.GetByte(offset) & 0xf);

				if (L == 0 || L > 8) {
					// The length is mandatory and must be at most 8 bytes.
					// The data is therefore corrupt, so ignore this whole attribute
					return false;
				} else {
					// Read in the length
					length = Util.GetArbitraryUInt(m_Data, (int)offset + 1, L);
					if (F > 0 && length + cur_vcn > (ulong)attr.highVCN + 1) {
						// The run goes too far, so throw an exception
						ulong recordOffset = MFTRecordNumber * (ulong)FileSystem.SectorsPerMFTRecord * (ulong)FileSystem.BytesPerSector;
						throw new InvalidFILERecordException(FileSystem, recordOffset + offset + 1, "Error: A data run went longer than the high VCN!");
					}
				}

				if (F == 0) {
					// This is a sparse run
					attr.Runs.Add(new SparseRun(cur_vcn, (ulong)length, this));
				} else {
					//if (vcn + run.length > attr.highVCN) break; // data is corrupt

					try {
						lcn = (ulong)((long)lcn + Util.GetArbitraryInt(m_Data, (int)offset + 1 + L, F));
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

		private void LoadStandardAttributes(int startOffset) {
			fileCreationTime = fromNTFS(BitConverter.ToUInt64(m_Data, startOffset));
			fileLastDataChangeTime = fromNTFS(BitConverter.ToUInt64(m_Data, startOffset + 8));
			fileLastMFTChangeTime = fromNTFS(BitConverter.ToUInt64(m_Data, startOffset + 16));
			fileLastAccessTime = fromNTFS(BitConverter.ToUInt64(m_Data, startOffset + 24));
			_Attributes = BitConverter.ToInt32(m_Data, startOffset + 32);
		}

		private void LoadNameAttributes(int startOffset) {
			// Read in the bytes, then parse them.
			ParentDirectory = BitConverter.ToUInt64(m_Data, startOffset) & 0xFFFFFF;
			fileCreationTime = fromNTFS(BitConverter.ToUInt64(m_Data, startOffset + 8));
			fileLastDataChangeTime = fromNTFS(BitConverter.ToUInt64(m_Data, startOffset + 16));
			fileLastMFTChangeTime = fromNTFS(BitConverter.ToUInt64(m_Data, startOffset + 24));
			fileLastAccessTime = fromNTFS(BitConverter.ToUInt64(m_Data, startOffset + 32));
			AllocatedSize = BitConverter.ToUInt64(m_Data, startOffset + 40);
			ActualSize = BitConverter.ToUInt64(m_Data, startOffset + 48);
			_Attributes = BitConverter.ToInt32(m_Data, startOffset + 56);
			_Attributes2 = BitConverter.ToInt32(m_Data, startOffset + 60);
			FileNameLength = m_Data[startOffset + 64];
			FileNameType = m_Data[startOffset + 65];
			if (FileName == null || FileName.Contains("~")) {
				FileName = Encoding.Unicode.GetString(m_Data, startOffset + 66, FileNameLength * 2);
			}
		}

		private void LoadVolumeNameAttributes(int startOffset, int length) {
			VolumeLabel = Encoding.Unicode.GetString(m_Data, startOffset, length);
		}

		private void LoadExternalAttributeList(int startOffset, AttributeRecord attrList) {
			int offset = 0;
			while (true) {
				//Align to 8 byte boundary
				if (offset % 8 != 0) {
					offset = (offset / 8 + 1) * 8;
				}

				//0xFF... marks end of attributes;
				if (offset == attrList.ValueLength || BitConverter.ToUInt32(m_Data, offset+startOffset) == 0xFFFFFFFF) {
					break;
				}

				AttributeRecord attr = new AttributeRecord();
				attr.type = (AttributeType)BitConverter.ToUInt32(m_Data, offset + startOffset + 0x0);
				attr.Length = BitConverter.ToUInt16(m_Data, offset + startOffset + 0x4);
				attr.NameLength = m_Data[offset + startOffset + 0x6];
				attr.Id = BitConverter.ToUInt16(m_Data, offset + startOffset + 0x18);

				ulong vcn = BitConverter.ToUInt64(m_Data, offset + startOffset + 0x8);
				ulong fileRef = (BitConverter.ToUInt64(m_Data, offset + startOffset + 0x10) & 0x00000000FFFFFFFF);
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
					attr.Name = Encoding.Unicode.GetString(m_Data, 0x1A, (attr.NameLength * 2));
				}

				ulong startByte = vcn * (ulong)FileSystem.BytesPerCluster;
				attr.value = new SubStream(attrList.value, startByte, startByte + attr.Length);
				offset += 0x1A + (attr.NameLength * 2);
			}
		}

		private void MergeRunLists(ref List<Run> list1, List<Run> list2) {
			list1.AddRange(list2);
			// TODO: Verify that the runlists don't overlap
		}

		#region INodeMetadata Members

		public string Name {
			get {
				if (string.IsNullOrEmpty(FileName)) {
					LoadData(true);
				}
				return FileName;
			}
		}

		public ulong Size {
			get {
				LoadData();
				return ActualSize;
			}
		}

		public FileRecoveryStatus GetChanceOfRecovery() {
			return GetFileSystemNode().GetChanceOfRecovery();
		}

		#endregion
	}
}
