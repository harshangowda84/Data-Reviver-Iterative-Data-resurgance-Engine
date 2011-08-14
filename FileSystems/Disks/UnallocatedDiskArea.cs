using System.Text;

namespace KFA.Disks {
    public class UnallocatedDiskArea : PhysicalDiskSection {
        public UnallocatedDiskArea(PhysicalDisk disk, ulong offset, ulong len) {
            PhysicalDisk = disk;
            Offset = offset;
            Length = len;
        }

        public override string ToString() {
            return StreamName;
        }

        public override string StreamName {
            get {
                return "Unallocated Disk Space";
            }
        }

        #region IDescribable Members

        public override string TextDescription {
            get {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Unallocated disk space");
                sb.AppendFormat("{0}: {1}\r\n", "Offset", Offset);
                sb.AppendFormat("{0}: {1}\r\n", "Length", Length);
                return sb.ToString();
            }
        }

        public override Attributes GetAttributes() {
            return new UnallocatedDiskAreaAttributes();
        }

        #endregion

        public override ulong GetSectorSize() {
            return PhysicalDisk.Attributes.BytesPerSector;
        }

        public override SectorStatus GetSectorStatus(ulong sectorNum) {
            return SectorStatus.SlackSpace;
        }
    }
}
