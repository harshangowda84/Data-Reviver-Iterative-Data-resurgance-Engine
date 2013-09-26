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

using KFS.DataStream;
using System;
using System.Collections.Generic;
using System.IO;

namespace KFS.FileSystems {
	/// <summary>
	/// Allows a folder on the host system to be treated as an IFolder.
	/// </summary>
	public class FolderMounted : Folder {
		private string m_Path;
		private IDataStream m_Parent;
		private DirectoryInfo m_Info;

		public FolderMounted(string filePath, IDataStream parent) {
			m_Path = filePath;
			m_Parent = parent;
			m_Info = new DirectoryInfo(m_Path);
			Name = m_Info.Name;
		}

		public override DateTime LastModified {
			get { return m_Info.LastWriteTime; }
		}

		public override long Identifier {
			get { return 0; /* no-op */ }
		}

		public override IEnumerable<IFileSystemNode> GetChildren() {
			foreach (FileSystemInfo entry in m_Info.GetFileSystemInfos()) {
				if ((entry.Attributes & FileAttributes.Directory) != 0) {
					yield return new FolderMounted(entry.FullName, this);
				} else {
					yield return new FileFromHostSystem(entry.FullName, this);
				}
			}
		}

		public override byte GetByte(ulong offset) {
			return 0;
		}

		public override byte[] GetBytes(ulong offset, ulong length) {
			return new byte[length];
		}

		public override ulong StreamLength {
			get { return 0; }
		}

		public override ulong DeviceOffset {
			get { return 0; }
		}

		public override string StreamName {
			get { return "Temporary Folder " + Name; }
		}

		public override IDataStream ParentStream {
			get { return m_Parent; }
		}

		public override void Open() { }

		public override void Close() { }
	}
}
