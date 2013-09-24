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
	public enum MFTLoadDepth {
		Full,
		NameAndParentOnly,
		None
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
		public List<MFTAttribute> Attributes;

		#endregion

		private FileSystemNode m_Node = null;
		private byte[] m_Data;
		private IDataStream m_Stream;
		private bool m_DataLoaded = false;
		private string m_Path = "";

		public static MFTRecord Load(ulong recordNum, FileSystemNTFS fileSystem, MFTLoadDepth loadDepth = MFTLoadDepth.Full, string path = "") {
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
				Console.Error.WriteLine("Warning: MFT record number {0} at offset {1} was missing the 'FILE' header. Skipping.", recordNum, stream.DeviceOffset);
				return null;
			}

			return new MFTRecord(recordNum, fileSystem, data, stream, loadDepth, path);
		}

		private MFTRecord(ulong recordNum, FileSystemNTFS fileSystem, byte[] data,
				IDataStream stream, MFTLoadDepth loadDepth, string path) {
			this.RecordNum = recordNum;
			this.FileSystem = fileSystem;
			this.BytesPerSector = fileSystem.BytesPerSector;
			this.SectorsPerCluster = fileSystem.SectorsPerCluster;
			this.PartitionStream = fileSystem.Store;

			m_Data = data;
			m_Stream = stream;
			m_Path = path;

			Flags = BitConverter.ToUInt16(m_Data, 22);

			if (loadDepth != MFTLoadDepth.None) {
				LoadData(loadDepth);
			}
		}

		private void LoadData(MFTLoadDepth loadDepth = MFTLoadDepth.Full) {
			if (m_DataLoaded) {
				return;
			}
			if (loadDepth == MFTLoadDepth.Full) {
				// If we're loading everything on this pass, there's no need to load in the future.
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
			LoadAttributes(AttributeOffset, loadDepth);

			if (Attributes.Count == 0) {
				Console.Error.WriteLine("Warning: MFT record number {0} at offset {1} had no attributes.", RecordNum, m_Stream.DeviceOffset);
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

		public MFTAttribute GetAttribute(String name) {
			LoadData();
			try {
				AttributeType flag = (AttributeType)Enum.Parse(typeof(AttributeType), name);
				foreach (MFTAttribute attr in Attributes) {
					if (attr.Type == flag) {
						return attr;
					}
				}
			} catch (Exception e) {
				Console.Error.WriteLine(e);
			}
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

		private void LoadAttributes(int startOffset, MFTLoadDepth loadDepth) {
			Attributes = new List<MFTAttribute>();
			while (true) {
				//Align to 8 byte boundary
				if (startOffset % 8 != 0) {
					startOffset = (startOffset / 8 + 1) * 8;
				}

				// Read the attribute type and length and determine whether we care about this attribute.
				AttributeType type = (AttributeType)BitConverter.ToUInt32(m_Data, startOffset);
				if (type == AttributeType.End) {
					break;
				}
				int length = BitConverter.ToUInt16(m_Data, startOffset + 4);
				if (loadDepth == MFTLoadDepth.NameAndParentOnly && type != AttributeType.FileName) {
					startOffset += length;
					continue;
				}

				MFTAttribute attr = MFTAttribute.Load(m_Data, startOffset, this);
				if (!attr.NonResident) {
					if ((AttributeType)attr.Type == AttributeType.StandardInformation) {
						LoadStandardAttributes(startOffset + attr.ValueOffset);
					} else if ((AttributeType)attr.Type == AttributeType.FileName) {
						LoadNameAttributes(startOffset + attr.ValueOffset);
					} else if ((AttributeType)attr.Type == AttributeType.AttributeList) {
						LoadExternalAttributeList(startOffset + attr.ValueOffset, attr);
					} else if ((AttributeType)attr.Type == AttributeType.VolumeName) {
						LoadVolumeNameAttributes(startOffset + attr.ValueOffset, (int)attr.ValueLength);
					}
				}
				if (attr.Valid) {
					Attributes.Add(attr);
				}

				startOffset += (int)attr.Length;
			}
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

		private void LoadExternalAttributeList(int startOffset, MFTAttribute attrList) {
			int offset = 0;
			while (true) {
				//Align to 8 byte boundary
				if (offset % 8 != 0) {
					offset = (offset / 8 + 1) * 8;
				}

				//0xFF... marks end of attributes;
				if (offset == attrList.ValueLength || BitConverter.ToUInt32(m_Data, offset + startOffset) == 0xFFFFFFFF) {
					break;
				}

				MFTAttribute attr = new MFTAttribute();
				attr.Type = (AttributeType)BitConverter.ToUInt32(m_Data, offset + startOffset + 0x0);
				attr.Length = BitConverter.ToUInt16(m_Data, offset + startOffset + 0x4);
				attr.NameLength = m_Data[offset + startOffset + 0x6];
				attr.Id = BitConverter.ToUInt16(m_Data, offset + startOffset + 0x18);

				ulong vcn = BitConverter.ToUInt64(m_Data, offset + startOffset + 0x8);
				ulong fileRef = (BitConverter.ToUInt64(m_Data, offset + startOffset + 0x10) & 0x00000000FFFFFFFF);
				if (fileRef != this.MFTRecordNumber && fileRef != RecordNum) {
					MFTRecord mftRec = MFTRecord.Load(fileRef, this.FileSystem);
					foreach (MFTAttribute attr2 in mftRec.Attributes) {
						if (attr.Id == attr2.Id) {
							if (attr2.NonResident && attr2.Type == AttributeType.Data) {
								// Find the corresponding data attribute on this record and merge the runlists
								bool merged = false;
								foreach (MFTAttribute rec in Attributes) {
									if (rec.Type == AttributeType.Data && attr2.Name == rec.Name) {
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

		private void MergeRunLists(ref List<NTFSDataRun> list1, List<NTFSDataRun> list2) {
			list1.AddRange(list2);
			// TODO: Verify that the runlists don't overlap
		}

		#region INodeMetadata Members

		public string Name {
			get {
				if (string.IsNullOrEmpty(FileName)) {
					LoadData(MFTLoadDepth.NameAndParentOnly);
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
