using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using KFA.DataStream;

namespace FileSystems.FileSystem {
    public class FileMounted : File {
        private FileStream m_Stream = null;
        private IDataStream m_Parent = null;
        private string m_Path;

        public FileMounted(string filePath, IDataStream parent) {
            m_Path = filePath;
            m_Parent = parent;
            Open();
            FileInfo i = new FileInfo(filePath);
            Name = i.Name;
        }

        public override byte GetByte(ulong offset) {
            if (m_Stream != null) {
                m_Stream.Seek((long) offset, SeekOrigin.Begin);
                return (byte)m_Stream.ReadByte();
            } else {
                throw new Exception("FileDataStream was closed");
            }
        }

        public override byte[] GetBytes(ulong offset, ulong length) {
            if (m_Stream != null) {
                m_Stream.Seek((long)offset, SeekOrigin.Begin);
                byte[] res = new byte[length];
                m_Stream.Read(res, (int)offset, (int)length);
                return res;
            } else {
                throw new Exception("FileDataStream was closed");
            }
        }

        public override ulong StreamLength {
            get {
                return (ulong)m_Stream.Length;
            }
        }

        public override ulong DeviceOffset {
            get { return 0; }
        }

        public override String StreamName {
            get { return "Temporary File " + Name; }
        }

        public override IDataStream Parent {
            get { return m_Parent; }
        }

        public override void Open() {
            if (m_Stream == null) {
                m_Stream = System.IO.File.OpenRead(m_Path);
            }
        }

        public override void Close() {
            if (m_Stream != null) {
                m_Stream.Close();
                m_Stream = null;
            }
        }
    }
}
