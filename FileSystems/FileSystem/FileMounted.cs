// Copyright (C) 2011  Joey Scarr, Josh Oosterman
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

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
        private FileInfo m_Info;
        private string m_Path;

        public FileMounted(string filePath, IDataStream parent) {
            m_Path = filePath;
            m_Parent = parent;
            Open();
            m_Info = new FileInfo(filePath);
            Name = m_Info.Name;
        }

        public override DateTime LastModified {
            get { return m_Info.LastWriteTime; }
        }

        public override long Identifier {
            get { return 0; /* no-op */ }
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
