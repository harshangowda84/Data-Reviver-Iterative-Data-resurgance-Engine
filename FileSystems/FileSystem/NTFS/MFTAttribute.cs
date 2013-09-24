using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KFA.DataStream;

namespace FileSystems.FileSystem.NTFS {
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
	}

	public class MFTAttribute {
		public AttributeType Type;
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

		public List<NTFSDataRun> Runs;

		public static MFTAttribute Load(byte[] data, int startOffset) {
			return new MFTAttribute(data, startOffset);
		}

		public MFTAttribute() { }

		protected MFTAttribute(byte[] data, int startOffset) {
			// Read the attribute header.
			Type = (AttributeType)BitConverter.ToUInt32(data, startOffset + 0);
			Length = BitConverter.ToUInt16(data, startOffset + 4);
			NonResident = data[startOffset + 8] > 0;
			NameLength = data[startOffset + 9];
			NameOffset = BitConverter.ToUInt16(data, startOffset + 10);
			Compressed = data[startOffset + 0xC] > 0;
			Id = BitConverter.ToUInt16(data, startOffset + 0xE);
			if (NameLength > 0) {
				Name = Encoding.Unicode.GetString(data, startOffset + NameOffset, NameLength * 2);
			}
			Flags = BitConverter.ToUInt16(data, startOffset + 12);
			Instance = BitConverter.ToUInt16(data, startOffset + 14);
		}
	}
}
