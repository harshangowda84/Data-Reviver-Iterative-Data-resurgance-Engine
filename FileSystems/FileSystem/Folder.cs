using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileSystems.FileSystem {
    public abstract class Folder : FileSystemNode {
        public override string ToString() {
            return Name;
        }
    }
}
