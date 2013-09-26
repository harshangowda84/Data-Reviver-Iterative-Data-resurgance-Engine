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

namespace KFS.DataStream {
	/// <summary>
	/// A data stream that allows access to a subset of another stream.
	/// </summary>
	public class SubStream : IDataStream {
		private IDataStream m_stream;
		private ulong m_start, m_length;

		public SubStream(IDataStream stream, ulong start, ulong length) {
			m_stream = stream;
			m_start = start;
			m_length = length;
		}

		public virtual byte GetByte(ulong offset) {
			return m_stream.GetByte(m_start + offset);
		}

		public virtual byte[] GetBytes(ulong offset, ulong length) {
			return m_stream.GetBytes(m_start + offset, length);
		}

		public ulong StreamLength {
			get {
				return m_length;
			}
		}

		public ulong DeviceOffset {
			get {
				return m_stream.DeviceOffset + m_start;
			}
		}

		public virtual String StreamName {
			get { return "Substream of " + m_stream.StreamName; }
		}

		public virtual IDataStream ParentStream {
			get { return m_stream; }
		}

		public void Open() {
			m_stream.Open();
		}

		public void Close() {
			m_stream.Close();
		}

		public override string ToString() {
			return StreamName;
		}
	}
}
