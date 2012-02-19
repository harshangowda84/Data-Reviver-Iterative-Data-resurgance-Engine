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
using System.Collections.Generic;
using KFA.DataStream;
using System.Collections.ObjectModel;

namespace FileSystems.FileSystem.NTFS {
    class NTFSFileStream : IDataStream {

        private IDataStream m_partitionStream, m_residentStream;
        private ulong m_length;
        private MFTRecord m_record;
        private List<Run> m_runs;
        private bool m_nonResident;

        public NTFSFileStream(IDataStream partition, MFTRecord record, AttributeRecord attr) {
            if (attr != null) {
                m_nonResident = attr.NonResident;
                if (m_nonResident) {
                    m_runs = attr.Runs;
                    m_length = attr.DataSize;
                } else {
                    m_residentStream = attr.value;
                    m_length = attr.value.StreamLength;
                }
            }
            m_record = record;
            m_partitionStream = partition;
        }

        public NTFSFileStream(IDataStream partition, MFTRecord record, String attrName) :
            this(partition, record, record.GetAttribute(attrName)) { }

        /// <summary>
        /// Gets a list of the on-disk runs of this NTFSFileStream. Returns null if resident.
        /// </summary>
        public IEnumerable<Run> GetRuns() {
            return m_nonResident ? new ReadOnlyCollection<Run>(m_runs) : null;
        }

        public byte GetByte(ulong offset) {
            if (offset >= m_length) {
                throw new Exception("Offset was off the end of the file!");
            }
            if (m_nonResident) {
                ulong bytesPerCluster = (ulong)(m_record.SectorsPerCluster * m_record.BytesPerSector);
                ulong clusterNum = offset / bytesPerCluster;
                foreach (Run run in m_runs) {
                    if (clusterNum >= run.VCN && clusterNum < run.VCN + run.Length) {
                        return run.GetByte(offset - run.VCN * bytesPerCluster);
                    }
                }
                //throw new Exception("No run contained the requested offset!");
                return 0;
            } else {
                return m_residentStream.GetByte(offset);
            }
        }

        public byte[] GetBytes(ulong offset, ulong length) {
            if (offset + length > m_length) {
                throw new Exception("Offset was off the end of the file!");
            }
            if (m_nonResident) {
                byte[] res = new byte[length];
                ulong bytesPerCluster = (ulong)(m_record.SectorsPerCluster * m_record.BytesPerSector);
                ulong firstCluster = offset / bytesPerCluster;
                ulong lastCluster = (offset + length - 1) / bytesPerCluster;
                foreach (Run run in m_runs) {
                    if (!(run.VCN > lastCluster || run.VCN + run.Length <= firstCluster)) {
                        ulong runStart, resStart, copyLength;

                        if (run.Contains(firstCluster)) {
                            resStart = 0;
                            runStart = offset - run.VCN * bytesPerCluster;
                        } else {
                            runStart = 0;
                            resStart = run.VCN * bytesPerCluster - offset;
                        }

                        copyLength = Math.Min((run.VCN + run.Length) * bytesPerCluster - runStart, length - resStart);

                        Array.Copy(run.GetBytes(runStart, copyLength), 0, res, (int)resStart, (int)copyLength);
                    }
                }
                return res;
            } else {
                return m_residentStream.GetBytes(offset, length);
            }
        }

        public ulong DeviceOffset {
            get { return 0; }
        }

        public ulong StreamLength {
            get {
                return m_length;
            }
        }

        public String StreamName {
            get { return "NTFS File " + m_record.FileName; }
        }

        public IDataStream Parent {
            get { return m_record.PartitionStream; }
        }

        public void Open() {
            if (m_nonResident) {
                m_partitionStream.Open();
            } else {
                m_residentStream.Open();
            }
        }

        public void Close() {
            if (m_nonResident) {
                m_partitionStream.Close();
            } else {
                m_residentStream.Close();
            }
        }
    }
}
