using System;
using System.Collections.Generic;
using System.Linq;
using KFS.FileSystems;
using KFS.FileSystems.NTFS;

namespace DataReviver.Enhancements
{
    /// <summary>
    /// Deep validation of file metadata to reduce false positives in recovery results.
    /// Performs multi-layer checks on deleted files before presenting them to users.
    /// </summary>
    public class DeepMetadataValidator
    {
        private readonly IFileSystem _fileSystem;
        
        public DeepMetadataValidator(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        
        /// <summary>
        /// Perform comprehensive validation on deleted file metadata
        /// </summary>
        public ValidationResult ValidateDeletedFile(INodeMetadata metadata)
        {
            var result = new ValidationResult
            {
                IsValid = true,
                Confidence = 1.0,
                Metadata = metadata
            };
            
            // Layer 1: Basic checks
            if (!BasicValidation(metadata, result))
                return result;
            
            // Layer 2: Timestamp validation
            if (!ValidateTimestamps(metadata, result))
                result.Confidence *= 0.8;
            
            // Layer 3: Size validation
            if (!ValidateSize(metadata, result))
                result.Confidence *= 0.7;
            
            // Layer 4: Cluster validation
            if (!ValidateClusters(metadata, result))
                result.Confidence *= 0.6;
            
            // Layer 5: Path validation
            if (!ValidatePath(metadata, result))
                result.Confidence *= 0.9;
            
            // Layer 6: Extension validation
            if (!ValidateExtension(metadata, result))
                result.Confidence *= 0.95;
            
            // Final decision: Invalid if confidence drops below threshold
            if (result.Confidence < 0.3)
            {
                result.IsValid = false;
                result.Issues.Add("Overall confidence too low");
            }
            
            return result;
        }
        
        /// <summary>
        /// Basic validation checks
        /// </summary>
        private bool BasicValidation(INodeMetadata metadata, ValidationResult result)
        {
            // Must be marked as deleted
            if (!metadata.Deleted)
            {
                result.IsValid = false;
                result.Issues.Add("File not marked as deleted");
                return false;
            }
            
            // Must have a name
            if (string.IsNullOrWhiteSpace(metadata.Name))
            {
                result.IsValid = false;
                result.Issues.Add("File has no name");
                return false;
            }
            
            // Skip system files
            if (IsSystemFile(metadata.Name))
            {
                result.IsValid = false;
                result.Issues.Add("System file (excluded by default)");
                return false;
            }
            
            // Must be a file (not directory)
            try
            {
                var node = metadata.GetFileSystemNode();
                if (node.Type != FSNodeType.File)
                {
                    result.IsValid = false;
                    result.Issues.Add("Not a file (is directory or other)");
                    return false;
                }
                
                // Must have non-zero size
                if (node.Size == 0)
                {
                    result.IsValid = false;
                    result.Issues.Add("File size is zero");
                    return false;
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Issues.Add($"Cannot read file node: {ex.Message}");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Validate file timestamps
        /// </summary>
        private bool ValidateTimestamps(INodeMetadata metadata, ValidationResult result)
        {
            try
            {
                var now = DateTime.Now;
                var minDate = new DateTime(1980, 1, 1); // FAT earliest date
                var maxDate = now.AddYears(1); // Allow 1 year in future for clock skew
                
                // Check created time
                if (metadata.CreatedTime < minDate || metadata.CreatedTime > maxDate)
                {
                    result.Warnings.Add($"Created time out of range: {metadata.CreatedTime}");
                    return false;
                }
                
                // Check modified time
                if (metadata.ModifiedTime < minDate || metadata.ModifiedTime > maxDate)
                {
                    result.Warnings.Add($"Modified time out of range: {metadata.ModifiedTime}");
                    return false;
                }
                
                // Modified should be >= created
                if (metadata.ModifiedTime < metadata.CreatedTime)
                {
                    result.Warnings.Add("Modified time before created time");
                    return false;
                }
                
                return true;
            }
            catch
            {
                result.Warnings.Add("Cannot validate timestamps");
                return false;
            }
        }
        
        /// <summary>
        /// Validate file size
        /// </summary>
        private bool ValidateSize(INodeMetadata metadata, ValidationResult result)
        {
            try
            {
                var node = metadata.GetFileSystemNode();
                long size = node.Size;
                
                // Sanity check: files larger than 10GB are suspicious for most cases
                if (size > 10L * 1024 * 1024 * 1024)
                {
                    result.Warnings.Add($"File size very large: {size:N0} bytes");
                    return false;
                }
                
                // Check if size is reasonable for file type
                string ext = GetExtension(metadata.Name);
                if (!string.IsNullOrEmpty(ext))
                {
                    var expectedRange = GetExpectedSizeRange(ext);
                    if (size < expectedRange.Min || size > expectedRange.Max)
                    {
                        result.Warnings.Add($"Size {size:N0} unusual for {ext} files");
                        return false;
                    }
                }
                
                return true;
            }
            catch
            {
                result.Warnings.Add("Cannot validate size");
                return false;
            }
        }
        
        /// <summary>
        /// Validate cluster allocation
        /// </summary>
        private bool ValidateClusters(INodeMetadata metadata, ValidationResult result)
        {
            try
            {
                var record = metadata as MFTRecord;
                if (record == null) return true; // Only for NTFS
                
                // Check if data runs exist
                if (record.Runs == null || !record.Runs.Any())
                {
                    result.Warnings.Add("No data runs (file may be resident or damaged)");
                    return false;
                }
                
                // Check cluster status for each run
                int overwrittenClusters = 0;
                int totalClusters = 0;
                
                foreach (var run in record.Runs)
                {
                    if (run.IsDataRun())
                    {
                        ulong lcn = run.LCN;
                        ulong length = run.Length;
                        
                        for (ulong i = 0; i < length && i < 100; i++) // Sample first 100 clusters
                        {
                            totalClusters++;
                            var status = _fileSystem.GetSectorStatus(lcn + i);
                            
                            if (status != SectorStatus.Free)
                            {
                                overwrittenClusters++;
                            }
                        }
                    }
                }
                
                // Calculate overwrite percentage
                if (totalClusters > 0)
                {
                    double overwriteRatio = (double)overwrittenClusters / totalClusters;
                    
                    if (overwriteRatio > 0.5)
                    {
                        result.Warnings.Add($"File likely overwritten ({overwriteRatio:P0} of clusters reused)");
                        result.RecoveryChance = "Low";
                        return false;
                    }
                    else if (overwriteRatio > 0.1)
                    {
                        result.Warnings.Add($"File partially overwritten ({overwriteRatio:P0} of clusters reused)");
                        result.RecoveryChance = "Medium";
                    }
                    else
                    {
                        result.RecoveryChance = "High";
                    }
                }
                
                return true;
            }
            catch
            {
                result.Warnings.Add("Cannot validate clusters");
                return true; // Don't fail validation for this
            }
        }
        
        /// <summary>
        /// Validate file path
        /// </summary>
        private bool ValidatePath(INodeMetadata metadata, ValidationResult result)
        {
            try
            {
                string path = metadata.Path;
                
                // Check for invalid characters
                char[] invalidChars = System.IO.Path.GetInvalidPathChars();
                if (path != null && path.Any(c => invalidChars.Contains(c)))
                {
                    result.Warnings.Add("Path contains invalid characters");
                    return false;
                }
                
                // Check path length (Windows MAX_PATH)
                if (path != null && path.Length > 260)
                {
                    result.Warnings.Add("Path exceeds MAX_PATH limit");
                    return false;
                }
                
                return true;
            }
            catch
            {
                return true; // Don't fail on path issues
            }
        }
        
        /// <summary>
        /// Validate file extension
        /// </summary>
        private bool ValidateExtension(INodeMetadata metadata, ValidationResult result)
        {
            string ext = GetExtension(metadata.Name);
            
            if (string.IsNullOrEmpty(ext))
            {
                result.Warnings.Add("File has no extension");
                return true; // Not critical
            }
            
            // Check for double extensions (suspicious)
            int dotCount = metadata.Name.Count(c => c == '.');
            if (dotCount > 1)
            {
                result.Warnings.Add("Multiple extensions in filename");
            }
            
            // Check for executable in suspicious locations
            if (IsExecutableExtension(ext))
            {
                if (metadata.Path != null && 
                    (metadata.Path.Contains("\\Temp\\") || metadata.Path.Contains("\\Downloads\\")))
                {
                    result.Warnings.Add("Executable in temp/downloads (potential malware)");
                }
            }
            
            return true;
        }
        
        #region Helper Methods
        
        private bool IsSystemFile(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return false;
            
            string lower = filename.ToLower();
            
            // Windows system file patterns
            return lower.EndsWith(".manifest") ||
                   lower.EndsWith(".cat") ||
                   lower.EndsWith(".mum") ||
                   lower.StartsWith("$") ||
                   lower.StartsWith("~") ||
                   lower == "thumbs.db" ||
                   lower == "desktop.ini";
        }
        
        private string GetExtension(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return null;
            
            int lastDot = filename.LastIndexOf('.');
            if (lastDot < 0 || lastDot == filename.Length - 1)
                return null;
            
            return filename.Substring(lastDot).ToLower();
        }
        
        private bool IsExecutableExtension(string ext)
        {
            if (string.IsNullOrEmpty(ext)) return false;
            
            string[] exeExts = { ".exe", ".dll", ".com", ".bat", ".cmd", ".scr", ".vbs", ".ps1" };
            return exeExts.Contains(ext.ToLower());
        }
        
        private (long Min, long Max) GetExpectedSizeRange(string extension)
        {
            // Define reasonable size ranges for common file types
            switch (extension.ToLower())
            {
                case ".txt":
                case ".log":
                    return (1, 10 * 1024 * 1024); // 1B - 10MB
                
                case ".doc":
                case ".docx":
                    return (1024, 50 * 1024 * 1024); // 1KB - 50MB
                
                case ".xls":
                case ".xlsx":
                    return (1024, 100 * 1024 * 1024); // 1KB - 100MB
                
                case ".pdf":
                    return (1024, 100 * 1024 * 1024); // 1KB - 100MB
                
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                    return (1024, 50 * 1024 * 1024); // 1KB - 50MB
                
                case ".mp3":
                    return (10 * 1024, 50 * 1024 * 1024); // 10KB - 50MB
                
                case ".mp4":
                case ".avi":
                case ".mkv":
                    return (100 * 1024, 10L * 1024 * 1024 * 1024); // 100KB - 10GB
                
                case ".zip":
                case ".rar":
                case ".7z":
                    return (1024, 5L * 1024 * 1024 * 1024); // 1KB - 5GB
                
                case ".exe":
                case ".dll":
                    return (1024, 500 * 1024 * 1024); // 1KB - 500MB
                
                default:
                    return (0, long.MaxValue); // No restriction for unknown types
            }
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public double Confidence { get; set; }
        public INodeMetadata Metadata { get; set; }
        public List<string> Issues { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public string RecoveryChance { get; set; } = "Unknown";
        
        public string Summary
        {
            get
            {
                if (!IsValid)
                    return $"Invalid: {string.Join("; ", Issues)}";
                
                if (Warnings.Any())
                    return $"Valid (Confidence: {Confidence:P0}, Warnings: {Warnings.Count})";
                
                return $"Valid (Confidence: {Confidence:P0})";
            }
        }
    }
    
    #endregion
}
