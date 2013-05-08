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
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using KFA.DataStream;
using FileSystems.FileSystem;

namespace KFA.Disks {
	public class LinLogicalDisk : LinDisk, IFileSystemStore, IDescribable {
		public LinLogicalDiskAttributes Attributes { get; private set; }

		private string m_DevName;
		private ulong m_Size;
		private FileSystem m_fileSystem;

		public LinLogicalDisk(string dev) {
			m_DevName = dev;
			Handle = System.IO.File.Open(dev, FileMode.Open, FileAccess.Read);
			if (Handle == null)
				throw new Exception("Linux Bug!");
			m_Size = (ulong)Handle.Length;
			Attributes = new LinLogicalDiskAttributes();
			Attributes.FileSystem = Util.DetectFSType(this);
			m_fileSystem = FileSystem.TryLoad(this as IFileSystemStore);
		}

		public string TextDescription {
			get { return "LinLogicalDisk::TextDescription not implemented."; }
		}

		public override ulong StreamLength {
			get { return m_Size; }
		}

		public override string StreamName {
			get { return "LinLogicalDisk::StreamName not implemented."; }
		}

		public StorageType StorageType {
			get { return StorageType.LogicalVolume; }
		}

		Attributes IFileSystemStore.Attributes { 
			get { return Attributes; }
		}

		public FileSystem FS {
			get { return m_fileSystem; }
		}

		public override string ToString() {
			return m_DevName;
		}
	}
}

