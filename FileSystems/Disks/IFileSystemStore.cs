using KFA.DataStream;
using FileSystems.FileSystem;

namespace KFA.Disks {
    public enum StorageType {
        PhysicalDiskRange = 0,
        PhysicalDisk = 1,
        PhysicalDiskPartition = 2,
        LogicalVolume = 3
    }
    public interface IFileSystemStore : IDataStream {
        StorageType StorageType { get; }
        Attributes Attributes { get; }
        FileSystem FS { get; }
    }
}
