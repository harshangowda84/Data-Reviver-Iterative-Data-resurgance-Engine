using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KFA.DataStream {
    public class ForensicsAppStream : System.IO.Stream {
        IDataStream m_Stream = null;
        ulong m_Position = 0;
        public ForensicsAppStream(IDataStream stream) {
            m_Stream = stream;
            m_Stream.Open();
        }

        public override bool CanRead { get { return true; }  }
        public override bool CanSeek {  get { return true; }  }
        public override bool CanWrite {  get { return false; } }

        public override void Flush() { }

        public override long Length {
            get { return (long)m_Stream.StreamLength; }
        }

        public override long Position {
            get { return (long)m_Position; }
            set {   m_Position = (ulong)Math.Max(0, value); }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            UInt32 i;
            for (i = 0; i < count && m_Position + (ulong)i < m_Stream.StreamLength; i++) {
                buffer[offset + i] = m_Stream.GetByte(m_Position + (ulong)i);
            }
            m_Position += i;
            return (int)i;
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin) {
            switch (origin) {
                case System.IO.SeekOrigin.Begin:
                    m_Position = (ulong)offset;
                    break;
                case System.IO.SeekOrigin.Current:
                    m_Position = (ulong)((long)m_Position + offset);
                    break;
                case System.IO.SeekOrigin.End:
                    m_Position = (ulong)((long)m_Stream.StreamLength + offset);
                    break;
            }
            return Position;
        }

        public override void SetLength(long value) { }

        public override void Write(byte[] buffer, int offset, int count) { }

        public override void Close() {
            m_Stream.Close();
        }
    }
}
