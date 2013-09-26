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

namespace KFS.DataStream {
	public class ForensicsAppStream : System.IO.Stream {
		IDataStream m_Stream = null;
		ulong m_Position = 0;
		public ForensicsAppStream(IDataStream stream) {
			m_Stream = stream;
			m_Stream.Open();
		}

		public override bool CanRead { get { return true; } }
		public override bool CanSeek { get { return true; } }
		public override bool CanWrite { get { return false; } }

		public override void Flush() { }

		public override long Length {
			get { return (long)m_Stream.StreamLength; }
		}

		public override long Position {
			get { return (long)m_Position; }
			set { m_Position = (ulong)Math.Max(0, value); }
		}

		public override int Read(byte[] buffer, int offset, int count) {
			ulong read = Math.Min((ulong)count, m_Stream.StreamLength - m_Position);
			m_Stream.GetBytes(m_Position, read);
			m_Position += read;
			return (int)read;
		}

		public override long Seek(long offset, System.IO.SeekOrigin origin) {
			switch (origin) {
				case System.IO.SeekOrigin.Begin:
					m_Position = (ulong)offset;
					break;
				case System.IO.SeekOrigin.Current:
					m_Position = (ulong)((long)m_Position + offset);
					break;
				case System.IO.SeekOrigin.End:
					m_Position = (ulong)((long)m_Stream.StreamLength + offset);
					break;
			}
			return Position;
		}

		public override void SetLength(long value) { }

		public override void Write(byte[] buffer, int offset, int count) { }

		public override void Close() {
			m_Stream.Close();
		}
	}
}
