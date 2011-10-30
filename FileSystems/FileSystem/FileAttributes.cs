using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KFA.DataStream;
using KFA.Disks;
using FileSystems.FileSystem.FAT;

namespace FileSystems.FileSystem {
    public class FileAttributesFAT : IDescribable {
        public string Name { get; private set; }
        public ulong Size { get; private set; }
        public bool Deleted { get; private set; }
        public bool Hidden { get; private set; }
        public bool System { get; private set; }
        public bool Archive { get; private set; }
        public bool ReadOnly { get; private set; }
        public DateTime Created { get; private set; }
        public DateTime LastModified { get; private set; }
        public DateTime LastAccessed { get; private set; }

        public FileAttributesFAT(FolderFAT.DirectoryEntry entry){
            Name = entry.FileName;
            Size = (ulong)entry.Length;
            Deleted = entry.Free;
            Hidden = (entry.Attributes & FATDirectoryAttributes.ATTR_HIDDEN) != 0;
            System = (entry.Attributes & FATDirectoryAttributes.ATTR_SYSTEM) != 0;
            Archive = (entry.Attributes & FATDirectoryAttributes.ATTR_ARCHIVE) != 0;
            ReadOnly = (entry.Attributes & FATDirectoryAttributes.ATTR_READ_ONLY) != 0;
            Created = entry.CreationTime;
            LastModified = entry.LastWrite;
            LastAccessed = entry.LastAccess;
        }

        public FileAttributesFAT() {
            Name = "Root Directory";
        }

        public string TextDescription {
            get {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}: {1}\r\n", "Name", Name);
                sb.AppendFormat("{0}: {1}\r\n", "Size", Util.ByteFormat(Size));
                sb.AppendFormat("{0}: {1}\r\n", "Deleted", Deleted);
                sb.AppendFormat("{0}: {1}\r\n", "Hidden", Hidden);
                sb.AppendFormat("{0}: {1}\r\n", "System", System);
                sb.AppendFormat("{0}: {1}\r\n", "Archive", Archive);
                sb.AppendFormat("{0}: {1}\r\n", "Read Only", ReadOnly);
                sb.AppendFormat("{0}: {1}\r\n", "Created", Created);
                sb.AppendFormat("{0}: {1}\r\n", "Last Modified", LastModified);
                sb.AppendFormat("{0}: {1}\r\n", "Last Accessed", LastAccessed.ToShortDateString());
                return sb.ToString();
            }
        }
    }
}
