using KFA.DataStream;

namespace KFA.FileSystem.NTFS {
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

        public override byte[] GetBytes(ulong offset, ulong length) {
            byte[] res = base.GetBytes(offset, length);
            ulong current = offset - (offset % m_SectorSize) + m_SectorSize - 2;
            while (current < offset + length) {
                if (current >= offset) {
                    res[current - offset] = (byte)(m_Array[current / m_SectorSize] & 0xFF);
                }
                // We don't need to check that current + 1 >= offset here, because if it were
                // less than offset, current would've been rounded to the next sector up.
                if (current + 1 < offset + length) {
                    res[current + 1 - offset] = (byte)((m_Array[current / m_SectorSize] >> 8) & 0xFF);
                }

                current += m_SectorSize;
            }
            return res;
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
