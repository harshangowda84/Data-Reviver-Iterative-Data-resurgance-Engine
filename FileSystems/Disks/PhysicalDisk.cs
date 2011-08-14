using System;
using System.Collections.Generic;
using System.Management;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using KFA.DataStream;

namespace KFA.Disks {
    public class PhysicalDisk : Disk, IImageable, IDescribable {
        public PhysicalDiskAttributes Attributes { get; private set; }

        private ulong m_Size;
        public PhysicalDisk(ManagementObject mo) {
            Attributes = new PhysicalDiskAttributes(mo);

            Handle = Win32.CreateFile(Attributes.DeviceID, EFileAccess.GenericRead, EFileShare.Read | EFileShare.Write | EFileShare.Delete, IntPtr.Zero, ECreationDisposition.OpenExisting, EFileAttributes.None, IntPtr.Zero);

            if (Handle.IsInvalid) {
                throw new Exception("Failed to get a handle to the physical disk. " + Marshal.GetLastWin32Error());
            }
            m_Size = Util.GetDiskSize(Handle);

            GetDiskSections();
        }

        private List<PhysicalDiskSection> m_Sections;
        [XmlIgnore]
        public List<PhysicalDiskSection> Sections {
            get { return new List<PhysicalDiskSection>(m_Sections); }
        }

        private MasterBootRecord m_MasterBootRecord;
        private void GetDiskSections() {
            m_Sections = new List<PhysicalDiskSection>();
            try {
                m_MasterBootRecord = new MasterBootRecord(this);
                m_Sections.Add(m_MasterBootRecord);
                ulong offset = MasterBootRecord.MBR_SIZE;
                foreach (MasterBootRecord.PartitionEntry pEntry in m_MasterBootRecord.PartitionEntries) {
                    if (pEntry.PartitionOffset > offset) {
                        m_Sections.Add(new UnallocatedDiskArea(this, offset, pEntry.PartitionOffset - offset));
                    }
                    if (offset > pEntry.PartitionOffset) {
                        throw new Exception("Something went wrong!");
                    }

                    m_Sections.Add(new PhysicalDiskPartition(this, pEntry));

                    offset = pEntry.PartitionOffset + pEntry.PartitionLength;
                }
                if (StreamLength > offset) {
                    m_Sections.Add(new UnallocatedDiskArea(this, offset, StreamLength - offset));
                }
            } catch (Exception) { }
        }

        public override string ToString() {
            return StreamName;
        }

        #region IDataStream Members

        public override ulong StreamLength {
            get { return m_Size; }
        }

        public override String StreamName {
            get { return Attributes.Caption; }
        }

        #endregion

        #region IDescribable Members

        public string TextDescription {
            get {
                return Attributes.TextDescription;
            }
        }

        #endregion

        #region IImageable Members

        public Attributes GetAttributes() {
            return Attributes;
        }

        public ulong GetSectorSize() {
            return Attributes.BytesPerSector;
        }

        public SectorStatus GetSectorStatus(ulong sectorNum) {
            foreach (PhysicalDiskSection section in m_Sections) {
                if (section.Offset / Attributes.BytesPerSector <= sectorNum
                    && (section.Offset + section.Length) / Attributes.BytesPerSector > sectorNum) {
                    return section.GetSectorStatus(sectorNum - section.Offset / Attributes.BytesPerSector);
                }
            }
            return SectorStatus.Unknown;
        }

        #endregion
    }
}
