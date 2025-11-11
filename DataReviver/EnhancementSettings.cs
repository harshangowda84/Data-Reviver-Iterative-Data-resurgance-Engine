using System;

namespace DataReviver
{
    /// <summary>
    /// Settings for experimental enhancement features
    /// </summary>
    public static class EnhancementSettings
    {
        // Main toggle for all enhancements
        public static bool EnableEnhancements { get; set; } = true;
        
        // Individual enhancement toggles
        public static bool EnableDeepValidation { get; set; } = true;
        public static bool EnableEntropyAnalysis { get; set; } = true;
        public static bool EnableFileCarving { get; set; } = false; // Disabled by default (slower)
        public static bool EnableFAT32DeepScan { get; set; } = true; // NEW: Deep sector scan for FAT32
        
        // Thresholds
        public static double ValidationConfidenceThreshold { get; set; } = 0.3;
        
        /// <summary>
        /// Get a summary of active enhancements
        /// </summary>
        public static string GetActiveSummary()
        {
            if (!EnableEnhancements)
                return "Enhancements: OFF (Standard Mode)";
            
            int activeCount = 0;
            if (EnableDeepValidation) activeCount++;
            if (EnableEntropyAnalysis) activeCount++;
            if (EnableFileCarving) activeCount++;
            if (EnableFAT32DeepScan) activeCount++;
            
            return $"Enhancements: ON ({activeCount} active - Deep Validation, Entropy Analysis" + 
                   (EnableFileCarving ? ", File Carving" : "") + 
                   (EnableFAT32DeepScan ? ", FAT32 Deep Scan" : "") + ")";
        }
        
        /// <summary>
        /// Get a detailed description of what enhancements do
        /// </summary>
        public static string GetDescription()
        {
            return @"EXPERIMENTAL ENHANCEMENTS:

✓ Deep Validation: 6-layer validation to reduce false positives
  - Timestamp validation
  - Size validation  
  - Cluster overwrite detection
  - Path validation
  - Extension validation
  - Recovery chance estimation (High/Medium/Low)

✓ Entropy Analysis: Content-based file type detection
  - Shannon entropy calculation
  - Magic byte detection
  - Text vs binary classification
  - Compression/encryption detection

✓ FAT32 Deep Scan: Raw sector scanning for FAT32/FAT16 drives
  - Finds deleted files even when folder structure is lost
  - Scans ALL sectors for directory entry patterns (0xE5 marker)
  - Recovers files from deleted/formatted folders
  - Essential for large file recovery (movies, videos, etc.)

✗ File Carving: Signature-based recovery (SLOW - disabled by default)
  - Scans raw sectors for file signatures
  - Recovers files even when MFT is corrupted
  - 15+ file type signatures supported

These features are experimental and may affect scan speed.
Toggle them on/off to compare results.";
        }
    }
}
