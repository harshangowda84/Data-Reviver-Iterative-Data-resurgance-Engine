using System;
using KFA.DataStream;

namespace KFA.Disks {
    public abstract class PhysicalDiskSection : IImageable, IDescribable {
        public PhysicalDisk PhysicalDisk { get; protected set; }
        public ulong Offset { get; protected set; }
        public ulong Length { get; protected set; }

        #region IDataStream Members

        public byte GetByte(ulong offset) {
            // TODO: This line is shit right?
            //if ((ulong)offset >= Offset + Length) {
            if ((ulong)offset >= Length) {
                throw new Exception("WHARRRRRGARBL");
            }
            return PhysicalDisk.GetByte(offset + Offset);
        }

        public byte[] GetBytes(ulong offset, ulong length) {
            if ((ulong)offset + length - 1 >= Length) {
                throw new Exception("WHARRRRRGARBL");
            }
            return PhysicalDisk.GetBytes(offset + Offset, length);
        }

        public ulong StreamLength {
            get { return Length; }
        }

        public virtual String StreamName {
            get { return "Physical Disk Section"; }
        }

        public virtual IDataStream Parent {
            get { return PhysicalDisk; }
        }

        public ulong DeviceOffset {
            get { return Parent.DeviceOffset + Offset; }
        }

        public void Open() { }

        public void Close() { }

        #endregion

        #region IImageable Members

        public abstract Attributes GetAttributes();

        public abstract ulong GetSectorSize();

        public abstract SectorStatus GetSectorStatus(ulong sectorNum);

        #endregion

        #region IDescribable Members

        public abstract string TextDescription { get; }

        #endregion
    }
}
