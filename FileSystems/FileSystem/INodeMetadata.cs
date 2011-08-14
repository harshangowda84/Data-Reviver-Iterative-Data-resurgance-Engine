using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KFA.FileSystem;

namespace FileSystems.FileSystem {
    public interface INodeMetadata {
        string Name { get; }
        ulong Size { get; }
        bool Deleted { get; }
        FileSystemNode GetFileSystemNode();
    }
}
