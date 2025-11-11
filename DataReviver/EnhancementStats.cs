using System;

namespace DataReviver
{
    /// <summary>
    /// Tracks statistics for enhancement features during scanning
    /// </summary>
    public class EnhancementStats
    {
        // Validation stats
        public int TotalFilesScanned { get; set; }
        public int FilesPassedValidation { get; set; }
        public int FilesFailedValidation { get; set; }
        public int FilesWithWarnings { get; set; }
        
        // Recovery chance breakdown
        public int HighRecoveryChance { get; set; }
        public int MediumRecoveryChance { get; set; }
        public int LowRecoveryChance { get; set; }
        
        // Entropy analysis stats
        public int FileTypeMismatches { get; set; }
        public int EncryptedFiles { get; set; }
        public int CompressedFiles { get; set; }
        
        public DateTime ScanStartTime { get; set; }
        public DateTime ScanEndTime { get; set; }
        
        public double ValidationPassRate => TotalFilesScanned > 0 
            ? (double)FilesPassedValidation / TotalFilesScanned * 100 
            : 0;
        
        public string GetSummary()
        {
            return $@"ENHANCEMENT STATISTICS:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📊 Validation Results:
   Total Scanned: {TotalFilesScanned:N0}
   ✓ Passed: {FilesPassedValidation:N0} ({ValidationPassRate:F1}%)
   ✗ Failed: {FilesFailedValidation:N0}
   ⚠ Warnings: {FilesWithWarnings:N0}

📈 Recovery Chance:
   🟢 High:   {HighRecoveryChance:N0} files
   🟡 Medium: {MediumRecoveryChance:N0} files
   🔴 Low:    {LowRecoveryChance:N0} files

🔍 Content Analysis:
   Type Mismatches: {FileTypeMismatches:N0}
   Encrypted: {EncryptedFiles:N0}
   Compressed: {CompressedFiles:N0}

⏱ Scan Time: {(ScanEndTime - ScanStartTime).TotalSeconds:F1}s

💡 TIP: Files marked as 'Low' recovery chance may be 
   partially overwritten. Try recovering them soon!";
        }
        
        public string GetShortSummary()
        {
            return $"✓ {FilesPassedValidation}/{TotalFilesScanned} valid | " +
                   $"🟢 {HighRecoveryChance} high | " +
                   $"🟡 {MediumRecoveryChance} med | " +
                   $"🔴 {LowRecoveryChance} low";
        }
    }
}
