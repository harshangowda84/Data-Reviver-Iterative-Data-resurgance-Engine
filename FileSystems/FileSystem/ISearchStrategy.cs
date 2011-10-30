using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileSystems.FileSystem {
    public interface ISearchStrategy {
        string Name { get; set; }
        void Search(FileSystem.NodeVisitCallback callback);
        void Search(FileSystem.NodeVisitCallback callback, string path);
    }
}
