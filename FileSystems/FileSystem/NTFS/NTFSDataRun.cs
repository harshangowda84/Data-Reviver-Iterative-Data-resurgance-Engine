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

namespace KFS.FileSystems.NTFS {
	public class NTFSDataRun : IRun {
		private ulong m_vcn, m_lcn, m_bytesPerCluster, m_lengthInBytes;
		private MFTRecord m_record;
		public ulong VCN { get { return m_vcn; } }
		public ulong LCN { get { return m_lcn; } }
		public ulong LengthInClusters { get; private set; }
		public NTFSDataRun(ulong vcn, ulong lcn, ulong lengthInClusters, MFTRecord record) {
			m_vcn = vcn;
			m_lcn = lcn;
			LengthInClusters = lengthInClusters;
			m_record = record;
			m_bytesPerCluster = (ulong)(m_record.BytesPerSector * m_record.SectorsPerCluster);
			m_lengthInBytes = LengthInClusters * m_bytesPerCluster;
		}

		public bool Contains(ulong vcn) {
			return vcn >= VCN && vcn < VCN + LengthInClusters;
		}

		public virtual bool HasRealClusters {
			get { return true; }
		}

		#region IDataStream Members

		public virtual byte GetByte(ulong offset) {
			if (offset < m_lengthInBytes) {
				return m_record.PartitionStream.GetByte(LCN * m_bytesPerCluster + offset);
			} else {
				throw new Exception("Offset does not exist in this run!");
			}
		}

		public virtual byte[] GetBytes(ulong offset, ulong length) {
			if (offset + length - 1 < m_lengthInBytes) {
				return m_record.PartitionStream.GetBytes(LCN * m_bytesPerCluster + offset, length);
			} else {
				throw new Exception("Offset does not exist in this run!");
			}
		}

		public ulong StreamLength {
			get { return m_lengthInBytes; }
		}

		public string StreamName {
			get { return "Non-resident Attribute Run"; }
		}

		public IDataStream ParentStream {
			get { return m_record.PartitionStream; }
		}

		public ulong DeviceOffset {
			get { return ParentStream.DeviceOffset + LCN * m_bytesPerCluster; }
		}

		public void Open() {
			m_record.PartitionStream.Open();
		}

		public void Close() {
			m_record.PartitionStream.Close();
		}

		#endregion

		public override string ToString() {
			return string.Format("Run: VCN {0}, Length {1}, LCN {2}", VCN, LengthInClusters, LCN);
		}
	}
}
