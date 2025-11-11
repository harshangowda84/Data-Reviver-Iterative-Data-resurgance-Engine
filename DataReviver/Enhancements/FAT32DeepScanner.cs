using System;
using System.Collections.Generic;
using System.Text;
using KFS.FileSystems;
using KFS.FileSystems.FAT;
using KFS.DataStream;

namespace DataReviver.Enhancements
{
    /// <summary>
    /// Deep sector scanner for FAT32 to find deleted files that are no longer in directory entries
    /// Addresses the limitation where FAT32 tree walk only finds files still in folder structure
    /// </summary>
    public class FAT32DeepScanner
    {
        private readonly IFileSystem _fileSystem;
        private readonly IFileSystemStore _store;
        private const int DIR_ENTRY_SIZE = 32;
        
        public event EventHandler<ProgressEventArgs> ProgressUpdated;
        public event EventHandler<DeletedFileFoundEventArgs> DeletedFileFound;
        
        public FAT32DeepScanner(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _store = fileSystem.Store;
        }
        
        /// <summary>
        /// Scan ALL sectors for deleted directory entries, not just the folder hierarchy
        /// This finds files even when:
        /// - Parent folder was deleted
        /// - Directory entry was partially overwritten
        /// - FAT chain was broken
        /// </summary>
        public List<DeletedFATFile> DeepScanAllSectors()
        {
            var results = new List<DeletedFATFile>();
            var fatSystem = _fileSystem as FileSystemFAT;
            
            if (fatSystem == null)
            {
                Console.WriteLine("ERROR: Not a FAT file system");
                return results;
            }
            
            long bytesPerSector = fatSystem.BytesPerSector;
            long totalSectors = fatSystem.TotalSectors;
            long sectorsProcessed = 0;
            
            Console.WriteLine($"Starting deep FAT32 scan: {totalSectors:N0} sectors to scan");
            
            // Scan every sector looking for directory entry patterns
            for (long sector = 0; sector < totalSectors; sector++)
            {
                try
                {
                    byte[] sectorData = _store.GetBytes(sector * bytesPerSector, (int)bytesPerSector);
                    
                    // Check every 32-byte block in this sector (directory entries are 32 bytes)
                    for (int offset = 0; offset <= sectorData.Length - DIR_ENTRY_SIZE; offset += DIR_ENTRY_SIZE)
                    {
                        var entry = TryParseDeletedEntry(sectorData, offset, sector, offset);
                        if (entry != null && entry.IsValid)
                        {
                            results.Add(entry);
                            DeletedFileFound?.Invoke(this, new DeletedFileFoundEventArgs(entry));
                        }
                    }
                    
                    sectorsProcessed++;
                    
                    // Report progress every 1000 sectors
                    if (sectorsProcessed % 1000 == 0)
                    {
                        double progress = (double)sectorsProcessed / totalSectors;
                        ProgressUpdated?.Invoke(this, new ProgressEventArgs(progress, results.Count));
                        Console.WriteLine($"Deep scan progress: {progress:P1} - Found {results.Count} deleted files");
                    }
                }
                catch (Exception ex)
                {
                    // Continue scanning even if one sector fails
                    Console.WriteLine($"Error reading sector {sector}: {ex.Message}");
                }
            }
            
            Console.WriteLine($"Deep scan complete! Found {results.Count} deleted files");
            return results;
        }
        
        /// <summary>
        /// Try to parse a deleted directory entry from raw bytes
        /// </summary>
        private DeletedFATFile TryParseDeletedEntry(byte[] data, int offset, long sector, int posInSector)
        {
            try
            {
                byte firstByte = data[offset];
                
                // Check if this is a deleted entry (0xE5) or free (0x00, 0x05)
                bool isDeleted = (firstByte == 0xE5);
                bool isFree = (firstByte == 0x00 || firstByte == 0x05);
                
                if (!isDeleted && !isFree)
                    return null; // Not a deleted/free entry
                
                // Get attributes byte
                byte attributes = data[offset + 11];
                
                // Check if this looks like a valid directory entry
                // Valid attributes should only use bits 0-5 (0x3F mask)
                if ((attributes & 0xC0) != 0)
                    return null; // Invalid attributes
                
                // Skip volume labels and long name entries
                bool isVolumeLabel = (attributes & 0x08) != 0;
                bool isLongName = (attributes & 0x0F) == 0x0F;
                
                if (isVolumeLabel || isLongName)
                    return null;
                
                // Parse filename (8.3 format)
                byte[] nameBytes = new byte[8];
                byte[] extBytes = new byte[3];
                Array.Copy(data, offset + 0, nameBytes, 0, 8);
                Array.Copy(data, offset + 8, extBytes, 0, 3);
                
                string name = Encoding.ASCII.GetString(nameBytes).Trim();
                string ext = Encoding.ASCII.GetString(extBytes).Trim();
                
                // Restore first character (was replaced with 0xE5 on deletion)
                if (isDeleted && name.Length > 0)
                {
                    name = "_" + name.Substring(1); // Use underscore as placeholder
                }
                
                // Check if name contains only printable characters
                if (!IsPrintableName(name))
                    return null;
                
                string filename = ext.Length > 0 ? $"{name}.{ext}" : name;
                
                // Parse file size
                uint fileSize = BitConverter.ToUInt32(data, offset + 28);
                
                // Parse first cluster
                ushort firstClusterLo = BitConverter.ToUInt16(data, offset + 26);
                ushort firstClusterHi = BitConverter.ToUInt16(data, offset + 20);
                uint firstCluster = (uint)((firstClusterHi << 16) | firstClusterLo);
                
                // Parse timestamps
                ushort modTime = BitConverter.ToUInt16(data, offset + 22);
                ushort modDate = BitConverter.ToUInt16(data, offset + 24);
                DateTime? modifiedTime = ParseFATDateTime(modDate, modTime);
                
                // Validate entry
                bool isDirectory = (attributes & 0x10) != 0;
                
                // Files must have size > 0 (unless it's a directory)
                if (!isDirectory && fileSize == 0)
                    return null;
                
                // Skip "." and ".." entries
                if (filename == "." || filename == "..")
                    return null;
                
                // Validate cluster number (should be reasonable)
                var fatSystem = _fileSystem as FileSystemFAT;
                if (fatSystem != null)
                {
                    long totalClusters = fatSystem.TotalSectors / fatSystem.SectorsPerCluster;
                    if (firstCluster > totalClusters)
                        return null; // Invalid cluster
                }
                
                return new DeletedFATFile
                {
                    FileName = filename,
                    FileSize = fileSize,
                    FirstCluster = firstCluster,
                    ModifiedTime = modifiedTime,
                    IsDirectory = isDirectory,
                    Attributes = attributes,
                    Sector = sector,
                    OffsetInSector = posInSector,
                    IsValid = true,
                    WasDeleted = isDeleted
                };
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Check if a name contains only printable ASCII characters
        /// </summary>
        private bool IsPrintableName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            
            foreach (char c in name)
            {
                if (c < 0x20 || c > 0x7E)
                    return false;
            }
            return true;
        }
        
        /// <summary>
        /// Parse FAT date/time format
        /// </summary>
        private DateTime? ParseFATDateTime(ushort date, ushort time)
        {
            try
            {
                int year = 1980 + ((date >> 9) & 0x7F);
                int month = (date >> 5) & 0x0F;
                int day = date & 0x1F;
                
                int hour = (time >> 11) & 0x1F;
                int minute = (time >> 5) & 0x3F;
                int second = (time & 0x1F) * 2;
                
                if (year < 1980 || year > 2100)
                    return null;
                if (month < 1 || month > 12)
                    return null;
                if (day < 1 || day > 31)
                    return null;
                
                return new DateTime(year, month, day, hour, minute, second);
            }
            catch
            {
                return null;
            }
        }
    }
    
    #region Supporting Classes
    
    public class DeletedFATFile
    {
        public string FileName { get; set; }
        public uint FileSize { get; set; }
        public uint FirstCluster { get; set; }
        public DateTime? ModifiedTime { get; set; }
        public bool IsDirectory { get; set; }
        public byte Attributes { get; set; }
        public long Sector { get; set; }
        public int OffsetInSector { get; set; }
        public bool IsValid { get; set; }
        public bool WasDeleted { get; set; }
        
        public string SizeFormatted
        {
            get
            {
                if (FileSize < 1024)
                    return $"{FileSize} B";
                else if (FileSize < 1024 * 1024)
                    return $"{FileSize / 1024.0:F1} KB";
                else if (FileSize < 1024 * 1024 * 1024)
                    return $"{FileSize / (1024.0 * 1024):F1} MB";
                else
                    return $"{FileSize / (1024.0 * 1024 * 1024):F2} GB";
            }
        }
        
        public override string ToString()
        {
            return $"{FileName} ({SizeFormatted}) - Cluster {FirstCluster} - {ModifiedTime:yyyy-MM-dd HH:mm:ss}";
        }
    }
    
    public class ProgressEventArgs : EventArgs
    {
        public double Progress { get; set; }
        public int FilesFound { get; set; }
        
        public ProgressEventArgs(double progress, int filesFound)
        {
            Progress = progress;
            FilesFound = filesFound;
        }
    }
    
    public class DeletedFileFoundEventArgs : EventArgs
    {
        public DeletedFATFile File { get; set; }
        
        public DeletedFileFoundEventArgs(DeletedFATFile file)
        {
            File = file;
        }
    }
    
    #endregion
}
