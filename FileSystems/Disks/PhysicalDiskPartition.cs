using System;
using System.Text;
using System.Management;
using FileSystems.FileSystem;

namespace KFA.Disks {
    public class PhysicalDiskPartition : PhysicalDiskSection, IFileSystemStore {
        public PhysicalDiskPartitionAttributes Attributes { get; private set; }
        private FileSystem m_fileSystem;

        public PhysicalDiskPartition(PhysicalDisk disk, MasterBootRecord.PartitionEntry pEntry) {
            PhysicalDisk = disk;
            Offset = pEntry.PartitionOffset;
            Length = pEntry.PartitionLength;

            ManagementScope ms = new ManagementScope();
            ObjectQuery oq = new ObjectQuery(
                string.Format("SELECT * FROM Win32_DiskPartition WHERE DiskIndex = {0} AND Index = {1}",
                disk.Attributes.Index, pEntry.Index));
            ManagementObjectSearcher mos = new ManagementObjectSearcher(ms, oq);
            ManagementObjectCollection moc = mos.Get();
            if (moc.Count != 1) {
                throw new Exception("Unable to get partition data from WMI");
            }
            foreach (ManagementObject mo in moc) {
                Attributes = new PhysicalDiskPartitionAttributes(mo, disk);
                break;
            }
            Attributes.PartitionType = pEntry.PartitionType;

            m_fileSystem = FileSystem.TryLoad(this as IFileSystemStore);
        }

        public override string ToString() {
            return StreamName;
        }

        public override String StreamName {
            get { return Attributes.PartitionType.ToString() + " Partition"; }
        }

        #region IDescribable Members

        public override string TextDescription {
            get {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}: {1}\r\n", "Offset", Offset);
                sb.AppendFormat("{0}: {1}\r\n", "Length", Length);
                sb.Append(Attributes.TextDescription);
                return sb.ToString();
            }
        }

        #endregion

        public override Attributes GetAttributes() {
            return Attributes;
        }

        public override ulong GetSectorSize() {
            return PhysicalDisk.Attributes.BytesPerSector;
        }

        public override SectorStatus GetSectorStatus(ulong sectorNum) {
            if (m_fileSystem != null) {
                return m_fileSystem.GetSectorStatus(sectorNum);
            } else {
                return SectorStatus.UnknownFilesystem;
            }
        }

        #region IFileSystemStore Members

        public StorageType StorageType {
            get { return StorageType.PhysicalDiskPartition; }
        }

        Attributes IFileSystemStore.Attributes {
            get { return Attributes; }
        }

        public FileSystem FS {
            get { return m_fileSystem; }
        }

        #endregion
    }
}
