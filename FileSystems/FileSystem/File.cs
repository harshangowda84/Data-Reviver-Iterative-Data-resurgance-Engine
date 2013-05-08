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
#if !KFS_LEAN_AND_MEAN
using Ionic.Zip;
#endif
using System.IO;
using KFA.DataStream;

namespace FileSystems.FileSystem {
    public abstract class File : FileSystemNode {
#if !KFS_LEAN_AND_MEAN
        private bool m_IsZip = false;
        private bool m_Known = false;
#endif

        public bool IsZip {
            get {
#if KFS_LEAN_AND_MEAN
                return false;
#else
                if (!m_Known) {
                    //m_Known = ZipFile.IsZipFile(new ForensicsAppStream(this), false);
                    m_IsZip = Name.Trim().ToLower().EndsWith("zip");
                    m_Known = true;
                }
                return m_IsZip;
#endif
            }
        }

        public override IEnumerable<FileSystemNode> GetChildren() {
#if KFS_LEAN_AND_MEAN
            return new List<FileSystemNode>();
#else
            if (IsZip) {
                ZipFile f = ZipFile.Read(new ForensicsAppStream(this));
                string tempDir = Util.CreateTemporaryDirectory();
                // TODO: Add progress bar here
                f.ExtractAll(tempDir, ExtractExistingFileAction.InvokeExtractProgressEvent);
                FolderMounted folder = new FolderMounted(tempDir, this);
                return folder.GetChildren();
            } else {
                return new List<FileSystemNode>();
            }
#endif
        }

        public override string ToString() {
            return Name;
        }

        public override FileSystemNode.NodeType Type {
            get { return NodeType.File; }
        }
    }
}
