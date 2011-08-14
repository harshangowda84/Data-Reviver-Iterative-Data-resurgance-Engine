using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KFA.Disks;
using KFA.FileSystem.FAT;
using KFA.FileSystem.NTFS;
using FileSystems.FileSystem;

namespace KFA.FileSystem {
    public abstract class FileSystem {
        public delegate bool NodeVisitCallback(INodeMetadata node, ulong current, ulong total);

        public static FileSystem TryLoad(IFileSystemStore store) {
            if (store == null || store.StreamLength == 0) return null;
            switch (store.StorageType) {
                case StorageType.PhysicalDiskPartition: {
                        PhysicalDiskPartitionAttributes attributes = (PhysicalDiskPartitionAttributes)store.Attributes;
                        if (attributes.PartitionType == PartitionType.NTFS) {
                            return new FileSystemNTFS(store);
                        } else if (attributes.PartitionType == PartitionType.FAT16
                            || attributes.PartitionType == PartitionType.FAT32) {
                            return new FileSystemFAT(store, attributes.PartitionType);
                        } else if (attributes.PartitionType == PartitionType.FAT32WithInt13Support) {
                            return new FileSystemFAT(store, PartitionType.FAT32);
                        } else {
                            return null;
                        }
                    }
                case StorageType.LogicalVolume: {
                        LogicalDiskAttributes attributes = (LogicalDiskAttributes)store.Attributes;
                        if (attributes.FileSystem == "NTFS") {
                            return new FileSystemNTFS(store);
                        } else if (attributes.FileSystem == "FAT16") {
                            return new FileSystemFAT(store, PartitionType.FAT16);
                        } else if (attributes.FileSystem == "FAT32") {
                            return new FileSystemFAT(store, PartitionType.FAT32);
                        } else {
                            return null;
                        }
                    }
                default:
                    return null;
            }
        }

        public static bool HasFileSystem(IFileSystemStore store) {
            if (store == null || store.StreamLength == 0) return false;
            switch (store.StorageType) {
                case StorageType.PhysicalDiskPartition: {
                        PhysicalDiskPartitionAttributes attributes = (PhysicalDiskPartitionAttributes)store.Attributes;
                        if (attributes.PartitionType == PartitionType.NTFS) {
                            return true;
                        } else if (attributes.PartitionType == PartitionType.FAT16
                            || attributes.PartitionType == PartitionType.FAT32) {
                            return true;
                        } else if (attributes.PartitionType == PartitionType.FAT32WithInt13Support) {
                            return true;
                        } else {
                            return false;
                        }
                    }
                case StorageType.LogicalVolume: {
                        LogicalDiskAttributes attributes = (LogicalDiskAttributes)store.Attributes;
                        if (attributes.FileSystem == "NTFS") {
                            return true;
                        } else if (attributes.FileSystem == "FAT16") {
                            return true;
                        } else if (attributes.FileSystem == "FAT32") {
                            return true;
                        } else {
                            return false;
                        }
                    }
                default:
                    return false;
            }
        }

        public abstract FileSystemNode GetRoot();

        public IFileSystemStore Store { get; protected set; }

        public IEnumerable<FileSystemNode> GetFile(string path) {
            if (GetRoot() != null) {
                path = path.Replace('\\', '/');
                foreach (FileSystemNode node in GetRoot().GetChildrenAtPath(path)) {
                    yield return node;
                }
            }
        }

        public virtual SectorStatus GetSectorStatus(ulong sectorNum) {
            return SectorStatus.Unknown;
        }

        public abstract void VisitFiles(NodeVisitCallback callback, bool sectorSearch);
    }
}
