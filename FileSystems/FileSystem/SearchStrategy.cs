using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileSystems.FileSystem {
    public delegate void SearchFunction(FileSystem.NodeVisitCallback callback, string searchPath);

    public class SearchStrategy : ISearchStrategy {
        private SearchFunction m_Func;

        public string Name { get; set; }

        public void Search(FileSystem.NodeVisitCallback callback, string searchPath) {
            m_Func(callback, searchPath);
        }

        public void Search(FileSystem.NodeVisitCallback callback) {
            m_Func(callback, null);
        }

        public SearchStrategy(string name, SearchFunction func) {
            Name = name;
            m_Func = func;
        }
    }
}
