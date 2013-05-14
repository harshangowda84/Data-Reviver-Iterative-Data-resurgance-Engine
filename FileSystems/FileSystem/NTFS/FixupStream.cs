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

using KFA.DataStream;
using KFA.Exceptions;

namespace FileSystems.FileSystem.NTFS {
	public class FixupStream : SubStream {
		// Static method that just works on an array.
		public static void FixArray(byte[] data, ushort updateSequenceNumber, ushort[] updateSequenceArray, int sectorSize) {
			int current = sectorSize - 2;
			while (current < data.Length) {
				// Verify the fixup.
				ushort check = (ushort)(data[current] + (data[current + 1] << 8));
				// TODO: I have no idea why check is incorrect so often (mostly 0). I'm assuming there's something
				// we don't know about the fixup spec.
				if (check == updateSequenceNumber) {
					data[current] = (byte)(updateSequenceArray[current / sectorSize] & 0xFF);
					data[current + 1] = (byte)((updateSequenceArray[current / sectorSize] >> 8) & 0xFF);
				}/* else {
					throw new NTFSFixupException((ulong)current, updateSequenceNumber, check);
				}*/
				current += sectorSize;
			}
		}








		ushort m_Number;
		ushort[] m_Array;
		ulong m_SectorSize;
		public FixupStream(IDataStream stream, ulong start, ulong length,
						ushort updateSequenceNumber, ushort[] updateSequenceArray, ulong sectorSize)
			: base(stream, start, length) {
			m_Number = updateSequenceNumber;
			m_Array = updateSequenceArray;
			m_SectorSize = sectorSize;
		}

		public override byte GetByte(ulong offset) {
			if (offset % m_SectorSize == m_SectorSize - 2) {
				return (byte)(m_Array[offset / m_SectorSize] & 0xFF);
			} else if (offset % m_SectorSize == m_SectorSize - 1) {
				return (byte)((m_Array[offset / m_SectorSize] >> 8) & 0xFF);
			} else {
				return base.GetByte(offset);
			}
		}

		public override byte[] GetBytes(ulong offset, ulong length) {
			byte[] res = base.GetBytes(offset, length);
			ulong current = offset - (offset % m_SectorSize) + m_SectorSize - 2;
			while (current < offset + length) {
				if (current >= offset) {
					res[current - offset] = (byte)(m_Array[current / m_SectorSize] & 0xFF);
				}
				// We don't need to check that current + 1 >= offset here, because if it were
				// less than offset, current would've been rounded to the next sector up.
				if (current + 1 < offset + length) {
					res[current + 1 - offset] = (byte)((m_Array[current / m_SectorSize] >> 8) & 0xFF);
				}

				current += m_SectorSize;
			}
			return res;
		}

		public override string StreamName {
			get {
				return "Fixup " + base.StreamName;
			}
		}

		public override IDataStream ParentStream {
			get {
				return base.ParentStream.ParentStream;
			}
		}
	}
}
