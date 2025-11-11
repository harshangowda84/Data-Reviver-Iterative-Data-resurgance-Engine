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
            
            return $"Enhancements: ON ({activeCount} active - Deep Validation, Entropy Analysis" + 
                   (EnableFileCarving ? ", File Carving" : "") + ")";
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

✗ File Carving: Signature-based recovery (SLOW - disabled by default)
  - Scans raw sectors for file signatures
  - Recovers files even when MFT is corrupted
  - 15+ file type signatures supported

These features are experimental and may affect scan speed.
Toggle them on/off to compare results.";
        }
    }
}
