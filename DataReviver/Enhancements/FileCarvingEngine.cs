using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KFS.FileSystems;
using KFS.DataStream;

namespace DataReviver.Enhancements
{
    /// <summary>
    /// File carving engine - recovers files by scanning raw disk sectors for file signatures.
    /// Works even when MFT is corrupted or filesystem metadata is lost.
    /// </summary>
    public class FileCarvingEngine
    {
        private readonly IFileSystem _fileSystem;
        private readonly Dictionary<string, FileSignature> _signatures;
        
        public event EventHandler<CarvingProgressEventArgs> ProgressUpdated;
        public event EventHandler<FileCarvedEventArgs> FileCarved;
        
        public FileCarvingEngine(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _signatures = InitializeSignatures();
        }
        
        /// <summary>
        /// Start file carving from unallocated sectors
        /// </summary>
        public List<CarvedFile> CarveUnallocatedSectors(long startSector = 0, long endSector = -1)
        {
            var results = new List<CarvedFile>();
            
            try
            {
                long totalSectors = (long)(_fileSystem.Store.StreamLength / 512);
                if (endSector == -1) endSector = totalSectors;
                
                byte[] buffer = new byte[65536]; // 64KB buffer
                long currentSector = startSector;
                
                while (currentSector < endSector)
                {
                    // Read a chunk
                    ulong offset = (ulong)(currentSector * 512);
                    ulong length = (ulong)Math.Min(buffer.Length, (endSector - currentSector) * 512);
                    
                    if (offset + length > _fileSystem.Store.StreamLength)
                        break;
                    
                    byte[] data = _fileSystem.Store.GetBytes(offset, length);
                    
                    // Check if sector is free (skip allocated sectors)
                    SectorStatus status = _fileSystem.GetSectorStatus((ulong)currentSector);
                    if (status == SectorStatus.Free || status == SectorStatus.Unknown)
                    {
                        // Scan for file signatures
                        foreach (var sig in _signatures.Values)
                        {
                            int foundAt = FindSignature(data, sig.Header);
                            if (foundAt >= 0)
                            {
                                var carvedFile = ExtractFile(data, foundAt, sig, currentSector, offset + (ulong)foundAt);
                                if (carvedFile != null)
                                {
                                    results.Add(carvedFile);
                                    OnFileCarved(new FileCarvedEventArgs(carvedFile));
                                }
                            }
                        }
                    }
                    
                    currentSector += buffer.Length / 512;
                    
                    // Report progress
                    if (currentSector % 1000 == 0)
                    {
                        double progress = (double)(currentSector - startSector) / (endSector - startSector);
                        OnProgressUpdated(new CarvingProgressEventArgs(progress, results.Count));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"File carving error: {ex.Message}");
            }
            
            return results;
        }
        
        /// <summary>
        /// Extract file from buffer starting at found signature
        /// </summary>
        private CarvedFile ExtractFile(byte[] buffer, int startIndex, FileSignature sig, long sectorNum, ulong diskOffset)
        {
            try
            {
                // Find footer if defined
                int endIndex = buffer.Length;
                if (sig.Footer != null && sig.Footer.Length > 0)
                {
                    int footerPos = FindSignature(buffer, sig.Footer, startIndex + sig.Header.Length);
                    if (footerPos > 0)
                        endIndex = footerPos + sig.Footer.Length;
                }
                else if (sig.MaxSize > 0)
                {
                    endIndex = Math.Min(startIndex + sig.MaxSize, buffer.Length);
                }
                
                int fileSize = endIndex - startIndex;
                if (fileSize < sig.MinSize || fileSize > sig.MaxSize)
                    return null;
                
                byte[] fileData = new byte[fileSize];
                Array.Copy(buffer, startIndex, fileData, 0, fileSize);
                
                return new CarvedFile
                {
                    Data = fileData,
                    FileType = sig.FileType,
                    Extension = sig.Extension,
                    Size = fileSize,
                    DiskOffset = diskOffset,
                    SectorNumber = sectorNum,
                    RecoveryMethod = "File Carving",
                    Confidence = CalculateConfidence(fileData, sig)
                };
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Find signature pattern in byte array
        /// </summary>
        private int FindSignature(byte[] data, byte[] signature, int startIndex = 0)
        {
            if (signature == null || signature.Length == 0) return -1;
            
            for (int i = startIndex; i <= data.Length - signature.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < signature.Length; j++)
                {
                    // 0xFF in signature means "any byte" (wildcard)
                    if (signature[j] != 0xFF && data[i + j] != signature[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }
        
        /// <summary>
        /// Calculate confidence score for carved file (0.0 to 1.0)
        /// </summary>
        private double CalculateConfidence(byte[] data, FileSignature sig)
        {
            double confidence = 0.5; // Base confidence for signature match
            
            // Check if footer matches (if defined)
            if (sig.Footer != null && sig.Footer.Length > 0)
            {
                bool footerMatch = true;
                int footerStart = data.Length - sig.Footer.Length;
                if (footerStart >= 0)
                {
                    for (int i = 0; i < sig.Footer.Length; i++)
                    {
                        if (sig.Footer[i] != 0xFF && data[footerStart + i] != sig.Footer[i])
                        {
                            footerMatch = false;
                            break;
                        }
                    }
                    if (footerMatch) confidence += 0.3;
                }
            }
            
            // Check size reasonableness
            if (data.Length >= sig.MinSize && data.Length <= sig.MaxSize)
                confidence += 0.1;
            
            // Entropy check (compressed/encrypted files have high entropy)
            double entropy = CalculateEntropy(data);
            if (sig.ExpectedEntropy > 0)
            {
                double entropyDiff = Math.Abs(entropy - sig.ExpectedEntropy);
                if (entropyDiff < 1.0) confidence += 0.1;
            }
            
            return Math.Min(confidence, 1.0);
        }
        
        /// <summary>
        /// Calculate Shannon entropy of data
        /// </summary>
        private double CalculateEntropy(byte[] data)
        {
            if (data.Length == 0) return 0;
            
            var freq = new int[256];
            foreach (byte b in data)
                freq[b]++;
            
            double entropy = 0.0;
            foreach (int count in freq)
            {
                if (count > 0)
                {
                    double probability = (double)count / data.Length;
                    entropy -= probability * Math.Log(probability, 2);
                }
            }
            
            return entropy;
        }
        
        /// <summary>
        /// Initialize file signatures database
        /// </summary>
        private Dictionary<string, FileSignature> InitializeSignatures()
        {
            var sigs = new Dictionary<string, FileSignature>();
            
            // Document formats
            sigs["PDF"] = new FileSignature
            {
                FileType = "PDF Document",
                Extension = ".pdf",
                Header = new byte[] { 0x25, 0x50, 0x44, 0x46 }, // %PDF
                Footer = new byte[] { 0x25, 0x25, 0x45, 0x4F, 0x46 }, // %%EOF
                MinSize = 1024,
                MaxSize = 100 * 1024 * 1024,
                ExpectedEntropy = 5.5
            };
            
            sigs["DOCX"] = new FileSignature
            {
                FileType = "Word Document",
                Extension = ".docx",
                Header = new byte[] { 0x50, 0x4B, 0x03, 0x04 }, // ZIP signature (DOCX is ZIP)
                MinSize = 4096,
                MaxSize = 50 * 1024 * 1024,
                ExpectedEntropy = 7.0 // Compressed
            };
            
            sigs["XLSX"] = new FileSignature
            {
                FileType = "Excel Spreadsheet",
                Extension = ".xlsx",
                Header = new byte[] { 0x50, 0x4B, 0x03, 0x04 },
                MinSize = 4096,
                MaxSize = 50 * 1024 * 1024,
                ExpectedEntropy = 7.0
            };
            
            // Image formats
            sigs["JPEG"] = new FileSignature
            {
                FileType = "JPEG Image",
                Extension = ".jpg",
                Header = new byte[] { 0xFF, 0xD8, 0xFF },
                Footer = new byte[] { 0xFF, 0xD9 },
                MinSize = 1024,
                MaxSize = 50 * 1024 * 1024,
                ExpectedEntropy = 6.5
            };
            
            sigs["PNG"] = new FileSignature
            {
                FileType = "PNG Image",
                Extension = ".png",
                Header = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
                Footer = new byte[] { 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 },
                MinSize = 1024,
                MaxSize = 50 * 1024 * 1024,
                ExpectedEntropy = 7.0
            };
            
            sigs["GIF"] = new FileSignature
            {
                FileType = "GIF Image",
                Extension = ".gif",
                Header = new byte[] { 0x47, 0x49, 0x46, 0x38 }, // GIF8
                Footer = new byte[] { 0x00, 0x3B },
                MinSize = 1024,
                MaxSize = 20 * 1024 * 1024,
                ExpectedEntropy = 5.0
            };
            
            // Archive formats
            sigs["ZIP"] = new FileSignature
            {
                FileType = "ZIP Archive",
                Extension = ".zip",
                Header = new byte[] { 0x50, 0x4B, 0x03, 0x04 },
                MinSize = 512,
                MaxSize = 2000 * 1024 * 1024,
                ExpectedEntropy = 7.5
            };
            
            sigs["RAR"] = new FileSignature
            {
                FileType = "RAR Archive",
                Extension = ".rar",
                Header = new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07 },
                MinSize = 512,
                MaxSize = 2000 * 1024 * 1024,
                ExpectedEntropy = 7.5
            };
            
            // Video formats
            sigs["MP4"] = new FileSignature
            {
                FileType = "MP4 Video",
                Extension = ".mp4",
                Header = new byte[] { 0x00, 0x00, 0x00, 0xFF, 0x66, 0x74, 0x79, 0x70 },
                MinSize = 10240,
                MaxSize = 5000 * 1024 * 1024,
                ExpectedEntropy = 7.0
            };
            
            sigs["AVI"] = new FileSignature
            {
                FileType = "AVI Video",
                Extension = ".avi",
                Header = new byte[] { 0x52, 0x49, 0x46, 0x46, 0xFF, 0xFF, 0xFF, 0xFF, 0x41, 0x56, 0x49, 0x20 },
                MinSize = 10240,
                MaxSize = 5000 * 1024 * 1024,
                ExpectedEntropy = 6.5
            };
            
            // Audio formats
            sigs["MP3"] = new FileSignature
            {
                FileType = "MP3 Audio",
                Extension = ".mp3",
                Header = new byte[] { 0xFF, 0xFB },
                MinSize = 4096,
                MaxSize = 100 * 1024 * 1024,
                ExpectedEntropy = 7.0
            };
            
            // Executable formats
            sigs["EXE"] = new FileSignature
            {
                FileType = "Windows Executable",
                Extension = ".exe",
                Header = new byte[] { 0x4D, 0x5A }, // MZ
                MinSize = 1024,
                MaxSize = 500 * 1024 * 1024,
                ExpectedEntropy = 6.0
            };
            
            sigs["DLL"] = new FileSignature
            {
                FileType = "Dynamic Link Library",
                Extension = ".dll",
                Header = new byte[] { 0x4D, 0x5A },
                MinSize = 1024,
                MaxSize = 100 * 1024 * 1024,
                ExpectedEntropy = 6.0
            };
            
            return sigs;
        }
        
        protected virtual void OnProgressUpdated(CarvingProgressEventArgs e)
        {
            ProgressUpdated?.Invoke(this, e);
        }
        
        protected virtual void OnFileCarved(FileCarvedEventArgs e)
        {
            FileCarved?.Invoke(this, e);
        }
    }
    
    #region Supporting Classes
    
    public class FileSignature
    {
        public string FileType { get; set; }
        public string Extension { get; set; }
        public byte[] Header { get; set; }
        public byte[] Footer { get; set; }
        public int MinSize { get; set; }
        public int MaxSize { get; set; }
        public double ExpectedEntropy { get; set; }
    }
    
    public class CarvedFile
    {
        public byte[] Data { get; set; }
        public string FileType { get; set; }
        public string Extension { get; set; }
        public int Size { get; set; }
        public ulong DiskOffset { get; set; }
        public long SectorNumber { get; set; }
        public string RecoveryMethod { get; set; }
        public double Confidence { get; set; }
        
        public string SuggestedFileName
        {
            get
            {
                return $"carved_{DiskOffset:X8}_{DateTime.Now:yyyyMMdd_HHmmss}{Extension}";
            }
        }
    }
    
    public class CarvingProgressEventArgs : EventArgs
    {
        public double Progress { get; }
        public int FilesFound { get; }
        
        public CarvingProgressEventArgs(double progress, int filesFound)
        {
            Progress = progress;
            FilesFound = filesFound;
        }
    }
    
    public class FileCarvedEventArgs : EventArgs
    {
        public CarvedFile File { get; }
        
        public FileCarvedEventArgs(CarvedFile file)
        {
            File = file;
        }
    }
    
    #endregion
}
