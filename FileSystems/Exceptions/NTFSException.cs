using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KFA.FileSystem.NTFS;

namespace KFA.Exceptions {
    public class NTFSException : FileSystemException {
        private FileSystemNTFS _FileSystem;
        private ulong _ErrorOffset;
        public NTFSException(FileSystemNTFS fileSystem, ulong errorOffset, string errorMessage) : base(errorMessage) {
            _FileSystem = fileSystem;
            _ErrorOffset = errorOffset;
        }

        public override System.Collections.IDictionary Data {
            get {
                return new Dictionary<string, object>() { { "Filesystem", _FileSystem }, { "Offset", _ErrorOffset } };
            }
        }
    }
}
