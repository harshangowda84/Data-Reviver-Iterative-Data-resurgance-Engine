using KFA.DataStream;

namespace KFA.Disks {

    public enum SectorStatus {
        Unknown,
        NTFSUsed,
        NTFSFree,
        NTFSBad,
        FATUsed,
        FATFree,
        FATBad,
        FATReserved,
        FATFAT,
        MasterBootRecord,
        SlackSpace,
        UnknownFilesystem
    };

    public interface IHasSectors : IDataStream {
        ulong GetSectorSize();
        SectorStatus GetSectorStatus(ulong sectorNum);
    }
}
