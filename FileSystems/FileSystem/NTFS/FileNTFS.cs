using System;
using System.Text;
using KFA.DataStream;
using KFA.Disks;

namespace FileSystems.FileSystem.NTFS {
    public class FileNTFS : File, IDescribable {

        private MFTRecord m_record;
        private IDataStream m_stream;
        public DateTime m_CreationTime, m_LastModified, m_LastMFTModified, m_LastAccessTime;

        public FileNTFS(MFTRecord record, string path) {
            m_record = record;
            if (m_record.GetAttribute("Data") != null) {
                m_stream = new NTFSFileStream(m_record.PartitionStream, m_record, "Data");
            }
            Name = record.FileName;
            Path = path + Name;
            FileSystem = record.FileSystem;
            Deleted = m_record.Deleted;
        }

        public FileNTFS(MFTRecord record, AttributeRecord attr, string path) {
            m_record = record;
            m_stream = new NTFSFileStream(m_record.PartitionStream, m_record, attr);
            Name = record.FileName + ":" + attr.Name;
            Path = path + Name;
            FileSystem = record.FileSystem;
            Deleted = m_record.Deleted;
        }

        public override long Identifier {
            get { return (long)m_record.MFTRecordNumber; }
        }

        public override byte GetByte(ulong offset) {
            return m_stream.GetByte(offset);
        }

        public override byte[] GetBytes(ulong offset, ulong length) {
            return m_stream.GetBytes(offset, length);
        }

        public override ulong StreamLength {
            get { return m_stream == null ? 0 : m_stream.StreamLength; }
        }

        public override String StreamName {
            get { return "NTFS File - " + m_record.FileName; }
        }

        public override IDataStream Parent {
            get { return m_record.PartitionStream; }
        }

        public override ulong DeviceOffset {
            get { return m_stream.DeviceOffset; }
        }

        public override void Open() {
        }

        public override void Close() {
        }

        public DateTime CreationTime {
            get { return m_record.fileCreationTime; }
        }

        public DateTime LastAccessed {
            get { return m_record.fileLastAccessTime; }
        }

        public DateTime LastModified {
            get { return m_record.fileLataDataChangeTime; }
        }

        public DateTime LastModifiedMFT {
            get { return m_record.fileLastMFTChangeTime; }
        }

        public string VolumeLabel {
            get { return m_record.VolumeLabel ?? ""; }
        }

        #region IDescribable Members

        public string TextDescription {
            get {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}: {1}\r\n", "Name", Name);
                sb.AppendFormat("{0}: {1}\r\n", "Size", Util.ByteFormat(StreamLength));
                sb.AppendFormat("{0}: {1}\r\n", "Deleted", Deleted);
                sb.AppendFormat("{0}: {1}\r\n", "Created", CreationTime);
                sb.AppendFormat("{0}: {1}\r\n", "Last Modified", LastModified);
                sb.AppendFormat("{0}: {1}\r\n", "MFT Record Last Modified", LastModifiedMFT);
                sb.AppendFormat("{0}: {1}\r\n", "Last Accessed", LastAccessed);
                return sb.ToString();
            }
        }

        #endregion
    }
}
