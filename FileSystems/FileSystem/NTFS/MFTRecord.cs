// Copyright (C) 2013  Joey Scarr, Josh Oosterman, Lukas Korsika
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

	[Flags]
	public enum FilenameType {
		Posix = 0,
		Win32 = 1,
		Dos = 2,
	}

	[Flags]
	public enum RecordFlags {
		InUse = 1,
		Directory = 2,
		Is4 = 4, //?
		ViewIndex = 8,
		SpaceFiller = 16
	}

	[Flags]
	public enum FilePermissions {
		ReadOnly = 0x1,
		Hidden = 0x2,
		System = 0x4,
		Archive = 0x20,
		Device = 0x40,
		Normal = 0x80,
		Temporary = 0x100,
		SparseFile = 0x200,
		ReparsePoint = 0x400,
		Compressed = 0x800,
		Offline = 0x1000,
		NotContentIndexed = 0x2000,
		Encrypted = 0x4000
	}

	public class MFTRecord : INodeMetadata {

		#region Header Fields

		public UInt64 LogSequenceNumber;
		public UInt16 SequenceNumber;
		public UInt16 HardLinkCount;
		public UInt16 AttributeOffset;
		public RecordFlags Flags;
		public UInt32 BytesInUse;
		public UInt32 BytesAllocated;
		public UInt64 BaseMFTRecord;
		public UInt16 NextAttrInstance;
		public UInt32 MFTRecordNumber;

		#endregion

		#region Attribute Fields

		public UInt64 ParentDirectory = 0;
		public DateTime CreationTime;
		public DateTime LastDataChangeTime;
		public DateTime LastMFTChangeTime;
		public DateTime LastAccessTime;
		public UInt64 AllocatedSize;
		public UInt64 ActualSize;
		public FilePermissions FilePermissions;
		public Byte FileNameLength;
		public FilenameType FileNameType;
		public String FileName;
		public string VolumeLabel;

		#endregion

		public long BytesPerSector;
		public long SectorsPerCluster;
		public ulong RecordNum, StartOffset;
		public FileSystemNTFS FileSystem;
		public IDataStream PartitionStream;
		public List<MFTAttribute> Attributes;

		public bool Valid { get; private set; }
		private FileSystemNode m_Node = null;
		private byte[] m_Data;
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

			return new MFTRecord(recordNum, fileSystem, data, loadDepth, path);
		}

		private MFTRecord(ulong recordNum, FileSystemNTFS fileSystem, byte[] data,
				MFTLoadDepth loadDepth, string path) {
			this.RecordNum = recordNum;
			this.FileSystem = fileSystem;
			this.BytesPerSector = fileSystem.BytesPerSector;
			this.SectorsPerCluster = fileSystem.SectorsPerCluster;
			this.PartitionStream = fileSystem.Store;

			Valid = true;

			m_Data = data;
			m_Path = path;

			Flags = (RecordFlags)BitConverter.ToUInt16(m_Data, 22);

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

			string Magic = Encoding.ASCII.GetString(m_Data, 0, 4);
			if (!Magic.Equals("FILE")) {
				Console.Error.WriteLine("Warning: MFT record number {0} was missing the 'FILE' header. Skipping.", RecordNum);
				// This record is invalid, so don't read any more.
				Valid = false;
				return;
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

			// Apply fixups to the in-memory array
			try {
				FixupStream.FixArray(m_Data, updateSequenceNumber, updateSequenceArray, (int)BytesPerSector);
			} catch (NTFSFixupException e) {
				Console.Error.WriteLine(e);
				// This record is invalid, so don't read any more.
				Valid = false;
				return;
			}

			LoadHeader();
			LoadAttributes(AttributeOffset, loadDepth);

			if (Attributes.Count == 0) {
				Console.Error.WriteLine("Warning: MFT record number {0} had no attributes.", RecordNum);
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
				return (Flags & RecordFlags.InUse) == 0;
			}
		}

		public DateTime LastModified {
			get {
				return LastDataChangeTime;
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
				if ((Flags & RecordFlags.Directory) > 0) {
					m_Node = new FolderNTFS(this, path);
				} else if (HiddenDataStreamFileNTFS.GetHiddenDataStreams(this).Count > 0) {
					m_Node = new HiddenDataStreamFileNTFS(this, path);
				} else {
					m_Node = new FileNTFS(this, path);
				}
			}
			return m_Node;
		}

		public MFTAttribute GetAttribute(AttributeType type) {
			LoadData();
			try {
				foreach (MFTAttribute attr in Attributes) {
					if (attr.Type == type) {
						return attr;
					}
				}
			} catch (Exception e) {
				Console.Error.WriteLine(e);
			}
			return null;
		}

		private void LoadHeader() {
			LogSequenceNumber = BitConverter.ToUInt64(m_Data, 8);
			SequenceNumber = BitConverter.ToUInt16(m_Data, 16);
			HardLinkCount = BitConverter.ToUInt16(m_Data, 18);
			AttributeOffset = BitConverter.ToUInt16(m_Data, 20);
			BytesInUse = BitConverter.ToUInt32(m_Data, 24);
			BytesAllocated = BitConverter.ToUInt32(m_Data, 28);
			BaseMFTRecord = BitConverter.ToUInt64(m_Data, 32);
			NextAttrInstance = BitConverter.ToUInt16(m_Data, 40);
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

				MFTAttribute attribute = MFTAttribute.Load(m_Data, startOffset, this);
				if (!attribute.NonResident) {
					if ((AttributeType)attribute.Type == AttributeType.StandardInformation) {
						LoadStandardAttribute(startOffset + attribute.ValueOffset);
					} else if ((AttributeType)attribute.Type == AttributeType.FileName) {
						LoadNameAttribute(startOffset + attribute.ValueOffset);
					} else if ((AttributeType)attribute.Type == AttributeType.AttributeList) {
						LoadExternalAttributeList(startOffset + attribute.ValueOffset, attribute);
					} else if ((AttributeType)attribute.Type == AttributeType.VolumeLabel) {
						LoadVolumeLabelAttribute(startOffset + attribute.ValueOffset, (int)attribute.ValueLength);
					}
				}
				if (attribute.Valid) {
					Attributes.Add(attribute);
				}

				startOffset += (int)attribute.Length;
			}
		}

		private void LoadStandardAttribute(int startOffset) {
			CreationTime = fromNTFS(BitConverter.ToUInt64(m_Data, startOffset));
			LastDataChangeTime = fromNTFS(BitConverter.ToUInt64(m_Data, startOffset + 8));
			LastMFTChangeTime = fromNTFS(BitConverter.ToUInt64(m_Data, startOffset + 16));
			LastAccessTime = fromNTFS(BitConverter.ToUInt64(m_Data, startOffset + 24));
			FilePermissions = (FilePermissions)BitConverter.ToInt32(m_Data, startOffset + 32);
		}

		private void LoadNameAttribute(int startOffset) {
			// Read in the bytes, then parse them.
			ParentDirectory = BitConverter.ToUInt64(m_Data, startOffset) & 0xFFFFFF;
			AllocatedSize = BitConverter.ToUInt64(m_Data, startOffset + 40);
			ActualSize = BitConverter.ToUInt64(m_Data, startOffset + 48);
			FileNameLength = m_Data[startOffset + 64];
			FileNameType = (FilenameType)m_Data[startOffset + 65];
			if (FileName == null && FileNameType != FilenameType.Dos) { // Don't bother reading DOS (8.3) filenames
				FileName = Encoding.Unicode.GetString(m_Data, startOffset + 66, FileNameLength * 2);
			}
		}

		private void LoadVolumeLabelAttribute(int startOffset, int length) {
			VolumeLabel = Encoding.Unicode.GetString(m_Data, startOffset, length);
		}

		private void LoadExternalAttributeList(int startOffset, MFTAttribute attrList) {
			int offset = 0;
			while (true) {
				//Align to 8 byte boundary
				if (offset % 8 != 0) {
					offset = (offset / 8 + 1) * 8;
				}

				// Load the header for this external attribute reference.
				AttributeType type = (AttributeType)BitConverter.ToUInt32(m_Data, offset + startOffset + 0x0);
				// 0xFFFFFFFF marks end of attributes.
				if (offset == attrList.ValueLength || type == AttributeType.End) {
					break;
				}
				ushort length = BitConverter.ToUInt16(m_Data, offset + startOffset + 0x4);
				byte nameLength = m_Data[offset + startOffset + 0x6];
				ushort id = BitConverter.ToUInt16(m_Data, offset + startOffset + 0x18);
				ulong vcn = BitConverter.ToUInt64(m_Data, offset + startOffset + 0x8);
				ulong extensionRecordNumber = (BitConverter.ToUInt64(m_Data, offset + startOffset + 0x10) & 0x00000000FFFFFFFF);

				if (extensionRecordNumber != RecordNum && extensionRecordNumber != MFTRecordNumber) { // TODO: Are these ever different?
					// Load the MFT extension record, locate the attribute we want, and copy it over.
					MFTRecord extensionRecord = MFTRecord.Load(extensionRecordNumber, this.FileSystem);
					if (extensionRecord.Valid) {
						foreach (MFTAttribute externalAttribute in extensionRecord.Attributes) {
							if (id == externalAttribute.Id) {
								if (externalAttribute.NonResident && externalAttribute.Type == AttributeType.Data) {
									// Find the corresponding data attribute on this record and merge the runlists
									bool merged = false;
									foreach (MFTAttribute attribute in Attributes) {
										if (attribute.Type == AttributeType.Data && externalAttribute.Name == attribute.Name) {
											MergeRunLists(ref attribute.Runs, externalAttribute.Runs);
											merged = true;
											break;
										}
									}
									if (!merged) {
										this.Attributes.Add(externalAttribute);
									}
								} else {
									this.Attributes.Add(externalAttribute);
								}
							}
						}
					}
				}

				offset += 0x1A + (nameLength * 2);
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
