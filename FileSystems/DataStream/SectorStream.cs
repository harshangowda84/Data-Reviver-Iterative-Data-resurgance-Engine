using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KFA.DataStream {
    public class SectorStream : SubStream {

        private ulong m_sectorNum;

        public SectorStream(IDataStream stream, ulong start, ulong length, ulong sectorNum) :
            base(stream, start, length) {
            m_sectorNum = sectorNum;
        }

        public override String StreamName {
            get { return string.Concat("Sector ", m_sectorNum, " of ", Parent.StreamName); }
        }
    }
}
