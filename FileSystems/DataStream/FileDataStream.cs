using System;
using System.IO;

namespace KFA.DataStream {
    class FileDataStream : IDataStream {

        private FileStream fs = null;
        private string path;

        public FileDataStream(String filePath) {
            path = filePath;
            Open();
        }

        public byte GetByte(ulong offset) {
            if (fs != null) {
                fs.Seek((long)offset, SeekOrigin.Begin);
                return (byte)fs.ReadByte();
            } else {
                throw new Exception("FileDataStream was closed");
            }
        }

        public byte[] GetBytes(ulong offset, ulong length) {
            if (fs != null) {
                fs.Seek((long)offset, SeekOrigin.Begin);
                byte[] res = new byte[length];
                fs.Read(res, (int)offset, (int)length);
                return res;
            } else {
                throw new Exception("FileDataStream was closed");
            }
        }

        public ulong StreamLength {
            get {
                return (ulong)fs.Length;
            }
        }

        public ulong DeviceOffset {
            get { return 0; }
        }

        public String StreamName {
            get { return "Local File"; }
        }

        public IDataStream Parent {
            get { return null; }
        }

        public void Open() {
            if (fs == null) {
                fs = File.OpenRead(path);
            }
        }

        public void Close() {
            if (fs != null) {
                fs.Close();
                fs = null;
            }
        }
    }
}
