using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ForensicsApp.DataStream {
    public class FixupStream : SubStream {
        ushort m_Number;
        ushort[] m_Array;
        ulong m_SectorSize;
        public FixupStream(IDataStream stream, ulong start, ulong length,
                ushort updateSequenceNumber, ushort[] updateSequenceArray, ulong sectorSize)
                : base(stream, start, length) {
            m_Number = updateSequenceNumber;
            m_Array = updateSequenceArray;
            m_SectorSize = sectorSize;
        }

        public override byte GetByte(ulong offset) {
            if (offset % m_SectorSize == m_SectorSize - 2) {
                return (byte)(m_Array[offset / m_SectorSize] & 0xFF);
            } else if (offset % m_SectorSize == m_SectorSize - 1) {
                return (byte)((m_Array[offset / m_SectorSize] >> 8) & 0xFF);
            } else {
                return base.GetByte(offset);
            }
        }

        public override string StreamName {
            get {
                return "Fixup " + base.StreamName;
            }
        }

        public override IDataStream Parent {
            get {
                return base.Parent.Parent;
            }
        }
    }
}
