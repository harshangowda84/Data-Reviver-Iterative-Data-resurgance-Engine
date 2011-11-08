using System;
using System.Collections.Generic;
using KFA.DataStream;

namespace FileSystems.FileSystem.NTFS {

    public class HiddenDataStreamFileNTFS : Folder {
        private List<FileSystemNode> children;
        private MFTRecord m_record;

        public HiddenDataStreamFileNTFS(MFTRecord record, string path) {
            m_record = record;
            Name = record.FileName + "(Hidden Streams)";
            Path = path + Name + "/";
            children = new List<FileSystemNode>();
            children.Add(new FileNTFS(m_record, Path));
            foreach (AttributeRecord attr in GetHiddenDataStreams(m_record)) {
                children.Add(new FileNTFS(m_record, attr, Path));
            }
        }

        public override DateTime LastModified {
            get { return m_record.fileLastDataChangeTime; }
        }

        public override long Identifier {
            // TODO: This needs rethinking, since it'll have the same identifier as its base stream.
            // Not a problem until Identifier starts actually being used in NTFS searches.
            get { return (long)m_record.MFTRecordNumber; }
        }

        public List<AttributeRecord> GetHiddenDataStreams() {
            return GetHiddenDataStreams(m_record);
        }

        public static List<AttributeRecord> GetHiddenDataStreams(MFTRecord record) {
            List<AttributeRecord> result = new List<AttributeRecord>();
            foreach (AttributeRecord attr in record.Attributes) {
                if (attr.type == MFTRecord.AttributeType.Data && attr.Name != null) {
                    result.Add(attr);
                }
            }
            return result;
        }


        public override IEnumerable<FileSystemNode> GetChildren() {
             return children;
        }

        public override IEnumerable<FileSystemNode> GetChildren(string path) {
            return new List<FileSystemNode>();
        }

        public override byte GetByte(ulong offset) {
            return 0;
        }

        public override byte[] GetBytes(ulong offset, ulong length) {
            return new byte[length];
        }

        public override ulong DeviceOffset {
            get { return 0; }
        }

        public override ulong StreamLength {
            get { return 0; }
        }

        public override String StreamName {
            get { return "NTFS file w/ hidden streams"; }
        }

        public override IDataStream Parent { 
            get { return m_record.PartitionStream; }
        }

        public override void Open() {
        }

        public override void Close() {
        }
    }
}
