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

namespace KFS.FileSystems.NTFS {
	/// <summary>
	/// An NTFS file that contains alternate (named) data streams.
	/// </summary>
	public class HiddenDataStreamFileNTFS : Folder {
		private List<IFileSystemNode> children;
		private MFTRecord m_record;

		public HiddenDataStreamFileNTFS(MFTRecord record, string path) {
			m_record = record;
			Name = record.FileName + "(Hidden Streams)";
			Path = path + Name + "/";
			children = new List<IFileSystemNode>();
			children.Add(new FileNTFS(m_record, Path));
			foreach (MFTAttribute attr in m_record.NamedDataAttributes) {
				children.Add(new FileNTFS(m_record, attr, Path));
			}
		}

		public override DateTime LastModified {
			get { return m_record.LastDataChangeTime; }
		}

		public override long Identifier {
			// TODO: This needs rethinking, since it'll have the same identifier as its base stream.
			// Not a problem until Identifier starts actually being used in NTFS searches.
			get { return (long)m_record.MFTRecordNumber; }
		}

		public IList<MFTAttribute> GetHiddenDataStreams() {
			return m_record.NamedDataAttributes;
		}


		public override IEnumerable<IFileSystemNode> GetChildren() {
			return children;
		}

		public override IEnumerable<IFileSystemNode> GetChildren(string path) {
			return new List<IFileSystemNode>();
		}

		public override byte GetByte(ulong offset) {
			return 0;
		}

		public override byte[] GetBytes(ulong offset, ulong length) {
			return new byte[length];
		}

		public override ulong DeviceOffset {
			get { return 0; }
		}

		public override ulong StreamLength {
			get { return 0; }
		}

		public override String StreamName {
			get { return "NTFS file w/ hidden streams"; }
		}

		public override IDataStream ParentStream {
			get { return m_record.PartitionStream; }
		}

		public override void Open() {
		}

		public override void Close() {
		}
	}
}
