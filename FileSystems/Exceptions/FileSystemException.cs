using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KFA.Exceptions {
    public class FileSystemException : Exception {
        public FileSystemException(string errorMessage) : base(errorMessage) { }
    }
}
