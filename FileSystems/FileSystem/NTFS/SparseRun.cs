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

namespace KFS.FileSystems.NTFS {
	public class SparseRun : IRun {
		public SparseRun(ulong vcn, ulong lengthInClusters, MFTRecord record) {
			ulong clusterSize = (ulong)record.BytesPerSector * (ulong)record.SectorsPerCluster;
			StreamLength = lengthInClusters * clusterSize;
		}

		public byte GetByte(ulong offset) {
			return 0;
		}

		public byte[] GetBytes(ulong offset, ulong length) {
			return new byte[length];
		}

		public ulong DeviceOffset { get; private set; }

		public ulong StreamLength { get; private set; }

		public string StreamName { get; private set; }

		public IDataStream ParentStream { get; private set; }

		public void Open() {
		}

		public void Close() {
		}

		public bool HasRealClusters {
			get { return false; }
		}

		public override string ToString() {
			return "Sparse " + base.ToString();
		}
	}
}
