using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KFA.DataStream {
    public class SubStream : IDataStream {

        private IDataStream m_stream;
        private ulong m_start, m_length;

        public SubStream(IDataStream stream, ulong start, ulong length) {
            m_stream = stream;
            m_start = start;
            m_length = length;
        }

        public virtual byte GetByte(ulong offset) {
            return m_stream.GetByte(m_start + offset);
        }

        public virtual byte[] GetBytes(ulong offset, ulong length) {
            return m_stream.GetBytes(m_start + offset, length);
        }

        public ulong StreamLength {
            get {
                return m_length;
            }
        }

        public ulong DeviceOffset {
            get {
                return m_stream.DeviceOffset + m_start;
            }
        }

        public virtual String StreamName {
            get { return "Substream of " + m_stream.StreamName; }
        }

        public virtual IDataStream Parent {
            get { return m_stream; }
        }

        public void Open() {
            m_stream.Open();
        }

        public void Close() {
            m_stream.Close();
        }

        public override string ToString() {
            return StreamName;
        }
    }
}
