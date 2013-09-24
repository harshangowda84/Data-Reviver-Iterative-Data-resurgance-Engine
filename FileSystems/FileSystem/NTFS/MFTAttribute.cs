using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KFA.DataStream;
using KFA.Exceptions;

namespace FileSystems.FileSystem.NTFS {
	public enum AttributeType : uint {
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
		End = 0xFFFFFFFF
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

		private MFTRecord m_Record;

		public bool Valid { get; private set; }

		public static MFTAttribute Load(byte[] data, int startOffset, MFTRecord record) {
			return new MFTAttribute(data, startOffset, record);
		}

		public MFTAttribute() {
			Valid = true;
		}

		protected MFTAttribute(byte[] data, int startOffset, MFTRecord record)
			: this() {
			m_Record = record;

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

			if (NonResident) {
				LoadNonResidentHeader(data, startOffset);
			} else {
				LoadResidentHeader(data, startOffset);
			}
		}

		private void LoadResidentHeader(byte[] data, int startOffset) {
			ValueLength = BitConverter.ToUInt32(data, startOffset + 16);
			ValueOffset = BitConverter.ToUInt16(data, startOffset + 20);
			ResidentFlags = data[startOffset + 22];
			value = new ArrayBackedStream(data, (uint)(startOffset + ValueOffset), ValueLength);
		}

		private void LoadNonResidentHeader(byte[] data, int startOffset) {
			lowVCN = BitConverter.ToInt32(data, startOffset + 16);
			highVCN = BitConverter.ToInt64(data, startOffset + 24);

			MappingPairsOffset = BitConverter.ToUInt16(data, startOffset + 32);
			CompressionUnit = data[startOffset + 34];
			AllocatedSize = BitConverter.ToUInt64(data, startOffset + 40);
			DataSize = BitConverter.ToUInt64(data, startOffset + 48);
			InitialisedSize = BitConverter.ToUInt64(data, startOffset + 56);
			ValueLength = (uint)DataSize;
			if (CompressionUnit > 0) {
				CompressedSize = BitConverter.ToUInt64(data, startOffset + 64);
				Valid = false;
				return;
			}

			Runs = new List<NTFSDataRun>();
			ulong cur_vcn = (ulong)lowVCN;
			ulong lcn = 0;
			ulong offset = (ulong)startOffset + MappingPairsOffset;
			ulong endOffset = (ulong)startOffset + Length;

			while (offset < endOffset && cur_vcn <= (ulong)highVCN && data[offset] > 0) {
				ulong length;

				byte F = (Byte)((data[offset] >> 4) & 0xf);
				byte L = (Byte)(data[offset] & 0xf);

				if (L == 0 || L > 8) {
					// The length is mandatory and must be at most 8 bytes.
					// The data is therefore corrupt, so ignore this whole attribute
					Valid = false;
					return;
				} else {
					// Read in the length
					length = Util.GetArbitraryUInt(data, (int)offset + 1, L);
					if (F > 0 && length + cur_vcn > (ulong)highVCN + 1) {
						// The run goes too far, so log an error and skip.
						Console.Error.WriteLine("Error: A data run went longer than the high VCN in MFT record {0}!", m_Record.MFTRecordNumber);
						Valid = false;
						return;
					}
				}

				if (F == 0) {
					// This is a sparse run
					Runs.Add(new SparseRun(cur_vcn, (ulong)length, m_Record));
				} else {
					//if (vcn + run.length > attr.highVCN) break; // data is corrupt

					try {
						lcn = (ulong)((long)lcn + Util.GetArbitraryInt(data, (int)offset + 1 + L, F));
					} catch (Exception e) {
						Console.Error.WriteLine(e);
						Valid = false;
						return;
					}

					NTFSDataRun run = new NTFSDataRun(cur_vcn, lcn, (ulong)length, m_Record);
					Runs.Add(run);
				}
				cur_vcn += (ulong)length;

				offset += (ulong)(F + L + 1);
			}
		}
	}
}
