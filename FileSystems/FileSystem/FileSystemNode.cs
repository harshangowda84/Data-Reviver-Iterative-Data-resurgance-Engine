// Copyright (C) 2011  Joey Scarr, Josh Oosterman
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KFA.DataStream;
using FileSystems.FileSystem;

namespace FileSystems.FileSystem {
    public abstract class FileSystemNode : IDataStream, INodeMetadata {
        public enum NodeType {
            File,
            Folder
        }

        public abstract long Identifier { get; }
        public abstract NodeType Type { get; }
        public string Name { get; protected set; }
        public ulong Size {
            get { return StreamLength; }
        }
        public string Path { get; set; }
        public bool Deleted { get; protected set; }
        public abstract DateTime LastModified { get; }
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

        private FileRecoveryStatus m_RecoveryStatus = FileRecoveryStatus.Unknown;
        public FileRecoveryStatus GetChanceOfRecovery() {
            if (m_RecoveryStatus == FileRecoveryStatus.Unknown) {
                return FileSystem.GetChanceOfRecovery(this);
            }
            return m_RecoveryStatus;
        }

        #region IDataStream Members

        public abstract byte GetByte(ulong offset);

        public abstract byte[] GetBytes(ulong offset, ulong length);

        public abstract ulong StreamLength { get; }

        public abstract ulong DeviceOffset { get; }

        public abstract String StreamName { get; }

        public abstract IDataStream ParentStream { get; }

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
