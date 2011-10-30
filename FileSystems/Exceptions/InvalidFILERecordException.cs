using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileSystems.FileSystem.NTFS;

namespace KFA.Exceptions {
    public class InvalidFILERecordException : NTFSException {
        public InvalidFILERecordException(FileSystemNTFS fileSystem, ulong errorOffset, string expected, string found)
            : base(fileSystem, errorOffset, string.Format("Error parsing file record at {0}. Expected {1}, found {2}", errorOffset, expected, found)) {

        }
        public InvalidFILERecordException(FileSystemNTFS fileSystem, ulong errorOffset, string error)
            : base(fileSystem, errorOffset, string.Format("Error parsing file record at {0}. {1}", errorOffset, error)) {

        }
    }
}
