using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KFA.FileSystem {
    public abstract class Folder : FileSystemNode {
        public override string ToString() {
            return Name;
        }
    }
}
