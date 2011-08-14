using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KFA.DataStream;
using FileSystems.FileSystem;

namespace KFA.FileSystem {
    public abstract class FileSystemNode : IDataStream, INodeMetadata {
        public string Name { get; protected set; }
        public ulong Size {
            get { return StreamLength; }
        }
        public string Path { get; protected set; }
        public bool Deleted { get; protected set; }
        public FileSystem FileSystem { get; protected set; }
        public abstract IEnumerable<FileSystemNode> GetChildren();
        public bool Loaded { get; set; }

        public virtual void ReloadChildren() { }

        public virtual IEnumerable<FileSystemNode> GetChildren(string name) {
            name = name.Trim('/', '\\');
            if (name == "*") {
                return GetChildren();
            } else {
                List<FileSystemNode> res = new List<FileSystemNode>();
                foreach (FileSystemNode node in GetChildren()) {
                    if (Matches(name, node.Name)) {
                        res.Add(node);
                    }
                }
                return res;
            }
        }

        public IEnumerable<FileSystemNode> GetChildrenAtPath(string path) {
            if (path.StartsWith("/")) path = path.Substring(1);
            string nextFile, remainingPath;
            int nextSlash = path.IndexOf('/');
            if (nextSlash > -1) {
                nextFile = path.Substring(0, nextSlash);
                remainingPath = path.Substring(nextSlash + 1);
            } else {
                nextFile = path;
                remainingPath = "";
            }
            List<FileSystemNode> res = new List<FileSystemNode>();
            if (remainingPath.Replace("/", "") == "") {
                foreach (FileSystemNode node in GetChildren(nextFile)) {
                    res.Add(node);
                }
            } else {
                foreach (FileSystemNode node in GetChildren(nextFile)) {
                    if (node is Folder) {
                        res.AddRange(node.GetChildrenAtPath(remainingPath));
                    }
                }
            }
            return res;
        }

        private bool Matches(string expression, string s) {
            if (s == null) return false;
            expression = expression.ToLower();
            s = s.ToLower();
            // This could use Regexes in future
            return expression == "*" || expression == s;
        }

        #region IDataStream Members

        public abstract byte GetByte(ulong offset);

        public abstract byte[] GetBytes(ulong offset, ulong length);

        public abstract ulong StreamLength { get; }

        public abstract ulong DeviceOffset { get; }

        public abstract String StreamName { get; }

        public abstract IDataStream Parent { get; }

        public abstract void Open();

        public abstract void Close();

        #endregion

        public override string ToString() {
            return Name;
        }

        #region INodeMetadata Members


        public FileSystemNode GetFileSystemNode() {
            return this;
        }

        #endregion
    }
}
