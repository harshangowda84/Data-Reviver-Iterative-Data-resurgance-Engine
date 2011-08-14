using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using KFA.DataStream;

namespace KFA.FileSystem {
    public class FolderMounted : Folder {
        private string m_Path;
        private IDataStream m_Parent;
        private DirectoryInfo m_Info;

        public FolderMounted(string filePath, IDataStream parent) {
            m_Path = filePath;
            m_Parent = parent;
            m_Info = new DirectoryInfo(m_Path);
            Name = m_Info.Name;
        }

        public override IEnumerable<FileSystemNode> GetChildren() {
            foreach (FileSystemInfo entry in m_Info.GetFileSystemInfos()) {
                if ((entry.Attributes & FileAttributes.Directory) != 0) {
                    yield return new FolderMounted(entry.FullName, this);
                } else {
                    yield return new FileMounted(entry.FullName, this);
                }
            }
        }

        public override byte GetByte(ulong offset) {
            return 0;
        }

        public override byte[] GetBytes(ulong offset, ulong length) {
            return new byte[length];
        }

        public override ulong StreamLength {
            get { return 0; }
        }

        public override ulong DeviceOffset {
            get { return 0; }
        }

        public override string StreamName {
            get { return "Temporary Folder " + Name; }
        }

        public override IDataStream Parent {
            get { return m_Parent; }
        }

        public override void Open() { }

        public override void Close() { }
    }
}
