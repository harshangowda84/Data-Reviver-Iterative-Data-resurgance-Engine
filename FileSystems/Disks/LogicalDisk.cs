using System;
using System.Management;
using System.Runtime.InteropServices;
using KFA.DataStream;
using FileSystems.FileSystem;

namespace KFA.Disks {
    public class LogicalDisk : Disk, IFileSystemStore, IDescribable {
        public LogicalDiskAttributes Attributes { get; private set; }

        private ulong m_Size;
        private FileSystem m_fileSystem;
        public LogicalDisk(ManagementObject mo) {
            Attributes = new LogicalDiskAttributes(mo);
            Handle = Win32.CreateFile(@"\\.\" + Attributes.DeviceID, EFileAccess.GenericRead, EFileShare.Read | EFileShare.Write | EFileShare.Delete, IntPtr.Zero, ECreationDisposition.OpenExisting, EFileAttributes.None, IntPtr.Zero);

            if (Handle.IsInvalid) {
                throw new Exception("Failed to get a handle to the logical volume. " + Marshal.GetLastWin32Error());
            }
            m_Size = Util.GetDiskSize(Handle);
            m_fileSystem = FileSystem.TryLoad(this as IFileSystemStore);
        }

        #region IDescribable Members

        public string TextDescription {
            get { return Attributes.TextDescription; }
        }

        #endregion

        #region IDataStream Members

        public override ulong StreamLength {
            get { return m_Size; }
        }

        public override string StreamName {
            get { return "Logical Volume " + Attributes.VolumeName; }
        }

        #endregion

        #region IFileSystemStore Members

        public StorageType StorageType {
            get { return StorageType.LogicalVolume; }
        }

        Attributes IFileSystemStore.Attributes {
            get { return Attributes; }
        }

        public FileSystem FS {
            get { return m_fileSystem; }
        }

        #endregion

        public override string ToString() {
            string volume;
            if (string.IsNullOrEmpty(Attributes.VolumeName.Trim())) {
                volume = Attributes.DriveType.ToString();
            } else {
                volume = Attributes.VolumeName;
            }
            if (string.IsNullOrEmpty(Attributes.FileSystem)) {
                return string.Format("{0} {1}", Attributes.DeviceID, volume);
            } else {
                return string.Format("{0} {1} ({2})", Attributes.DeviceID, volume, Attributes.FileSystem);
            }
        }
    }
}
