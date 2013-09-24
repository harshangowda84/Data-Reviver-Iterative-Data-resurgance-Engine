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
using System.Text;
using KFA.DataStream;
using KFA.Disks;
using System.Collections.Generic;

namespace FileSystems.FileSystem.NTFS {
	public class FileNTFS : File, IDescribable {

		private MFTRecord m_record;
		private NTFSFileStream m_stream;

		public FileNTFS(MFTRecord record, string path) {
			m_record = record;
			if (m_record.GetAttribute(AttributeType.Data) != null) {
				m_stream = new NTFSFileStream(m_record.PartitionStream, m_record, AttributeType.Data);
			}
			Name = record.FileName;
			Path = path + Name;
			FileSystem = record.FileSystem;
			Deleted = m_record.Deleted;
		}

		public FileNTFS(MFTRecord record, MFTAttribute attr, string path) {
			m_record = record;
			m_stream = new NTFSFileStream(m_record.PartitionStream, m_record, attr);
			Name = record.FileName + ":" + attr.Name;
			Path = path + Name;
			FileSystem = record.FileSystem;
			Deleted = m_record.Deleted;
		}

		/// <summary>
		/// Gets a list of the on-disk runs of this NTFSFile. Returns null if resident.
		/// </summary>
		public IEnumerable<NTFSDataRun> GetRuns() {
			return m_stream.GetRuns();
		}

		public override long Identifier {
			get { return (long)m_record.MFTRecordNumber; }
		}

		public override byte GetByte(ulong offset) {
			return m_stream.GetByte(offset);
		}

		public override byte[] GetBytes(ulong offset, ulong length) {
			return m_stream.GetBytes(offset, length);
		}

		public override ulong StreamLength {
			get { return m_stream == null ? 0 : m_stream.StreamLength; }
		}

		public override String StreamName {
			get { return "NTFS File - " + m_record.FileName; }
		}

		public override IDataStream ParentStream {
			get { return m_record.PartitionStream; }
		}

		public override ulong DeviceOffset {
			get { return m_stream.DeviceOffset; }
		}

		public override void Open() {
		}

		public override void Close() {
		}

		public DateTime CreationTime {
			get { return m_record.CreationTime; }
		}

		public DateTime LastAccessed {
			get { return m_record.LastAccessTime; }
		}

		public override DateTime LastModified {
			get { return m_record.LastDataChangeTime; }
		}

		public DateTime LastModifiedMFT {
			get { return m_record.LastMFTChangeTime; }
		}

		public string VolumeLabel {
			get { return m_record.VolumeLabel ?? ""; }
		}

		#region IDescribable Members

		public string TextDescription {
			get {
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat("{0}: {1}\r\n", "Name", Name);
				sb.AppendFormat("{0}: {1}\r\n", "Size", Util.ByteFormat(StreamLength));
				sb.AppendFormat("{0}: {1}\r\n", "Deleted", Deleted);
				sb.AppendFormat("{0}: {1}\r\n", "Created", CreationTime);
				sb.AppendFormat("{0}: {1}\r\n", "Last Modified", LastModified);
				sb.AppendFormat("{0}: {1}\r\n", "MFT Record Last Modified", LastModifiedMFT);
				sb.AppendFormat("{0}: {1}\r\n", "Last Accessed", LastAccessed);
				return sb.ToString();
			}
		}

		#endregion
	}
}
