using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ionic.Zip;
using System.IO;
using KFA.DataStream;

namespace KFA.FileSystem {
    public abstract class File : FileSystemNode {
        public long Length { get; protected set; }

        private bool m_IsZip = false;
        private bool m_Known = false;
        public bool IsZip {
            get {
                if (!m_Known) {
                    //m_Known = ZipFile.IsZipFile(new ForensicsAppStream(this), false);
                    m_IsZip = Name.Trim().ToLower().EndsWith("zip");
                    m_Known = true;
                }
                return m_IsZip;
            }
        }

        public override IEnumerable<FileSystemNode> GetChildren() {
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
        }

        public override string ToString() {
            return Name;
        }
    }
}
