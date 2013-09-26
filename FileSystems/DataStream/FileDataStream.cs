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
using System.IO;

namespace KFS.DataStream {
	/// <summary>
	/// A data stream wrapper for a file on the host system, such as a disk image.
	/// </summary>
	public class FileDataStream : IDataStream {
		private FileStream fs = null;
		private string path;

		public FileDataStream(String filePath, IDataStream parentStream) {
			path = filePath;
			ParentStream = parentStream;
			Open();
		}

		public byte GetByte(ulong offset) {
			if (fs != null) {
				fs.Seek((long)offset, SeekOrigin.Begin);
				return (byte)fs.ReadByte();
			} else {
				throw new Exception("FileDataStream was closed");
			}
		}

		public byte[] GetBytes(ulong offset, ulong length) {
			if (fs != null) {
				fs.Seek((long)offset, SeekOrigin.Begin);
				byte[] res = new byte[length];
				fs.Read(res, (int)offset, (int)length);
				return res;
			} else {
				throw new Exception("FileDataStream was closed");
			}
		}

		public long Identifier {
			get { return 0; /* no-op */ }
		}

		public ulong StreamLength {
			get {
				return (ulong)fs.Length;
			}
		}

		public ulong DeviceOffset {
			get { return 0; }
		}

		public virtual String StreamName {
			get { return "Local File"; }
		}

		public IDataStream ParentStream { get; private set; }

		public void Open() {
			if (fs == null) {
				fs = File.OpenRead(path);
			}
		}

		public void Close() {
			if (fs != null) {
				fs.Close();
				fs = null;
			}
		}
	}
}
