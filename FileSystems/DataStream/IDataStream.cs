using System;

namespace KFA.DataStream {
    public interface IDataStream {
        byte GetByte(ulong offset);
        byte[] GetBytes(ulong offset, ulong length);
        ulong DeviceOffset { get; }
        ulong StreamLength { get; }
        String StreamName { get; }
        IDataStream Parent { get; }
        void Open();
        void Close();
    }
}
