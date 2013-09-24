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
using System.Linq;
using System.Text;

namespace KFA.DataStream {
	public class ArrayBackedStream : IDataStream {
		private uint m_Offset;
		private uint m_Length;
		private byte[] m_Data;

		public ArrayBackedStream(byte[] data, uint offset, uint length) {
			m_Data = data;
			m_Offset = offset;
			m_Length = length;
		}

		public byte GetByte(ulong offset) {
			return m_Data[(int)(m_Offset + offset)];
		}

		public byte[] GetBytes(ulong offset, ulong length) {
			byte[] result = new byte[length];
			Array.Copy(m_Data, (int)(m_Offset + offset), result, 0, (int)length);
			return result;
		}

		public ulong DeviceOffset {
			get { return m_Offset; }
		}

		public ulong StreamLength {
			get { return m_Length; }
		}

		public string StreamName {
			get { return "Array-backed stream"; }
		}

		public IDataStream ParentStream {
			get { return null; }
		}

		public void Open() { }

		public void Close() {
			// Remove reference to the array.
			m_Data = null;
		}
	}
}
