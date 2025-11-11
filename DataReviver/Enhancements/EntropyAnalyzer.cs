using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DataReviver.Enhancements
{
    /// <summary>
    /// Analyzes file content using entropy, magic bytes, and content characteristics
    /// to determine actual file type (more reliable than extension-based detection)
    /// </summary>
    public class EntropyAnalyzer
    {
        private readonly Dictionary<string, MagicBytePattern> _magicBytes;
        
        public EntropyAnalyzer()
        {
            _magicBytes = InitializeMagicBytes();
        }
        
        /// <summary>
        /// Analyze file and return detailed type information
        /// </summary>
        public FileAnalysisResult AnalyzeFile(byte[] data, string declaredExtension = null)
        {
            if (data == null || data.Length == 0)
                return new FileAnalysisResult { FileType = "Empty", Confidence = 0.0 };
            
            var result = new FileAnalysisResult();
            
            // Calculate entropy
            result.Entropy = CalculateEntropy(data);
            result.EntropyCategory = CategorizeEntropy(result.Entropy);
            
            // Detect by magic bytes
            result.DetectedType = DetectByMagicBytes(data);
            
            // Content analysis
            result.IsText = IsLikelyText(data);
            result.IsBinary = !result.IsText;
            result.IsCompressed = result.Entropy > 7.0;
            result.IsEncrypted = result.Entropy > 7.5;
            
            // Calculate confidence
            result.Confidence = CalculateConfidence(result, declaredExtension);
            
            // Final file type determination
            if (!string.IsNullOrEmpty(result.DetectedType))
                result.FileType = result.DetectedType;
            else if (result.IsText)
                result.FileType = "Text/Plain";
            else if (result.IsCompressed)
                result.FileType = "Compressed Data";
            else if (result.IsEncrypted)
                result.FileType = "Encrypted Data";
            else
                result.FileType = "Binary Data";
            
            return result;
        }
        
        /// <summary>
        /// Calculate Shannon entropy (0-8 bits)
        /// </summary>
        public double CalculateEntropy(byte[] data)
        {
            if (data == null || data.Length == 0) return 0.0;
            
            var frequency = new int[256];
            foreach (byte b in data)
                frequency[b]++;
            
            double entropy = 0.0;
            foreach (int count in frequency)
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
        /// Categorize entropy level
        /// </summary>
        private string CategorizeEntropy(double entropy)
        {
            if (entropy < 1.0) return "Very Low (Repetitive)";
            if (entropy < 3.0) return "Low (Structured Text)";
            if (entropy < 5.0) return "Medium (Mixed Content)";
            if (entropy < 7.0) return "High (Binary/Rich Content)";
            if (entropy < 7.5) return "Very High (Compressed)";
            return "Maximum (Encrypted/Random)";
        }
        
        /// <summary>
        /// Check if data appears to be text
        /// </summary>
        private bool IsLikelyText(byte[] data)
        {
            if (data.Length == 0) return false;
            
            int textChars = 0;
            int controlChars = 0;
            int nullChars = 0;
            
            int sampleSize = Math.Min(data.Length, 8192); // Sample first 8KB
            
            for (int i = 0; i < sampleSize; i++)
            {
                byte b = data[i];
                
                // Count nulls (common in binary)
                if (b == 0) nullChars++;
                
                // Count text characters (ASCII printable + common whitespace)
                if ((b >= 32 && b <= 126) || b == 9 || b == 10 || b == 13)
                    textChars++;
                
                // Count control characters
                if (b < 32 && b != 9 && b != 10 && b != 13)
                    controlChars++;
            }
            
            double textRatio = (double)textChars / sampleSize;
            double nullRatio = (double)nullChars / sampleSize;
            
            // Text files should have >90% printable characters and <5% nulls
            return textRatio > 0.90 && nullRatio < 0.05;
        }
        
        /// <summary>
        /// Detect file type by magic bytes (file signature)
        /// </summary>
        private string DetectByMagicBytes(byte[] data)
        {
            if (data.Length < 16) return null;
            
            foreach (var pattern in _magicBytes.Values)
            {
                if (MatchesMagicBytes(data, pattern))
                    return pattern.FileType;
            }
            
            return null;
        }
        
        /// <summary>
        /// Check if data matches magic byte pattern
        /// </summary>
        private bool MatchesMagicBytes(byte[] data, MagicBytePattern pattern)
        {
            if (pattern.Offset + pattern.Signature.Length > data.Length)
                return false;
            
            for (int i = 0; i < pattern.Signature.Length; i++)
            {
                // 0xFF = wildcard (any byte)
                if (pattern.Signature[i] != 0xFF && 
                    data[pattern.Offset + i] != pattern.Signature[i])
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Calculate overall confidence score
        /// </summary>
        private double CalculateConfidence(FileAnalysisResult result, string declaredExtension)
        {
            double confidence = 0.5; // Base confidence
            
            // Magic byte match is very reliable
            if (!string.IsNullOrEmpty(result.DetectedType))
                confidence = 0.9;
            
            // Extension matches detection
            if (!string.IsNullOrEmpty(declaredExtension) && 
                !string.IsNullOrEmpty(result.DetectedType))
            {
                if (result.DetectedType.IndexOf(declaredExtension.TrimStart('.'), 
                    StringComparison.OrdinalIgnoreCase) >= 0)
                    confidence = 0.95;
            }
            
            // Text detection is fairly reliable
            if (result.IsText && result.Entropy < 5.0)
                confidence = Math.Max(confidence, 0.8);
            
            // Compression/encryption detection
            if (result.IsCompressed || result.IsEncrypted)
                confidence = Math.Max(confidence, 0.75);
            
            return confidence;
        }
        
        /// <summary>
        /// Analyze specific byte patterns
        /// </summary>
        public Dictionary<string, double> AnalyzeBytePatterns(byte[] data)
        {
            var patterns = new Dictionary<string, double>();
            
            if (data.Length == 0) return patterns;
            
            // Null byte ratio
            int nulls = data.Count(b => b == 0);
            patterns["NullRatio"] = (double)nulls / data.Length;
            
            // Printable ASCII ratio
            int printable = data.Count(b => b >= 32 && b <= 126);
            patterns["PrintableRatio"] = (double)printable / data.Length;
            
            // High-bit set ratio (non-ASCII)
            int highBit = data.Count(b => b >= 128);
            patterns["HighBitRatio"] = (double)highBit / data.Length;
            
            // Whitespace ratio
            int whitespace = data.Count(b => b == 32 || b == 9 || b == 10 || b == 13);
            patterns["WhitespaceRatio"] = (double)whitespace / data.Length;
            
            // Repetition score (simple run-length analysis)
            patterns["RepetitionScore"] = CalculateRepetitionScore(data);
            
            return patterns;
        }
        
        /// <summary>
        /// Calculate how repetitive the data is (0-1, higher = more repetitive)
        /// </summary>
        private double CalculateRepetitionScore(byte[] data)
        {
            if (data.Length < 2) return 0.0;
            
            int runs = 0;
            int totalRunLength = 0;
            
            byte lastByte = data[0];
            int currentRunLength = 1;
            
            for (int i = 1; i < Math.Min(data.Length, 4096); i++)
            {
                if (data[i] == lastByte)
                {
                    currentRunLength++;
                }
                else
                {
                    if (currentRunLength > 2)
                    {
                        runs++;
                        totalRunLength += currentRunLength;
                    }
                    lastByte = data[i];
                    currentRunLength = 1;
                }
            }
            
            return runs > 0 ? (double)totalRunLength / Math.Min(data.Length, 4096) : 0.0;
        }
        
        /// <summary>
        /// Initialize magic byte patterns for common file types
        /// </summary>
        private Dictionary<string, MagicBytePattern> InitializeMagicBytes()
        {
            var patterns = new Dictionary<string, MagicBytePattern>();
            
            // Documents
            patterns["PDF"] = new MagicBytePattern
            {
                FileType = "PDF Document",
                Signature = new byte[] { 0x25, 0x50, 0x44, 0x46 }, // %PDF
                Offset = 0
            };
            
            patterns["DOCX"] = new MagicBytePattern
            {
                FileType = "Word Document (DOCX)",
                Signature = new byte[] { 0x50, 0x4B, 0x03, 0x04 },
                Offset = 0,
                SecondaryCheck = (data) => CheckZipContent(data, "word/")
            };
            
            patterns["XLSX"] = new MagicBytePattern
            {
                FileType = "Excel Spreadsheet (XLSX)",
                Signature = new byte[] { 0x50, 0x4B, 0x03, 0x04 },
                Offset = 0,
                SecondaryCheck = (data) => CheckZipContent(data, "xl/")
            };
            
            // Images
            patterns["JPEG"] = new MagicBytePattern
            {
                FileType = "JPEG Image",
                Signature = new byte[] { 0xFF, 0xD8, 0xFF },
                Offset = 0
            };
            
            patterns["PNG"] = new MagicBytePattern
            {
                FileType = "PNG Image",
                Signature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
                Offset = 0
            };
            
            patterns["GIF"] = new MagicBytePattern
            {
                FileType = "GIF Image",
                Signature = new byte[] { 0x47, 0x49, 0x46, 0x38 }, // GIF8
                Offset = 0
            };
            
            patterns["BMP"] = new MagicBytePattern
            {
                FileType = "BMP Image",
                Signature = new byte[] { 0x42, 0x4D }, // BM
                Offset = 0
            };
            
            // Archives
            patterns["ZIP"] = new MagicBytePattern
            {
                FileType = "ZIP Archive",
                Signature = new byte[] { 0x50, 0x4B, 0x03, 0x04 },
                Offset = 0
            };
            
            patterns["RAR"] = new MagicBytePattern
            {
                FileType = "RAR Archive",
                Signature = new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07 },
                Offset = 0
            };
            
            patterns["7Z"] = new MagicBytePattern
            {
                FileType = "7-Zip Archive",
                Signature = new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C },
                Offset = 0
            };
            
            // Executables
            patterns["EXE"] = new MagicBytePattern
            {
                FileType = "Windows Executable",
                Signature = new byte[] { 0x4D, 0x5A }, // MZ
                Offset = 0
            };
            
            // Media
            patterns["MP3"] = new MagicBytePattern
            {
                FileType = "MP3 Audio",
                Signature = new byte[] { 0xFF, 0xFB },
                Offset = 0
            };
            
            patterns["MP4"] = new MagicBytePattern
            {
                FileType = "MP4 Video",
                Signature = new byte[] { 0x66, 0x74, 0x79, 0x70 }, // ftyp
                Offset = 4
            };
            
            return patterns;
        }
        
        /// <summary>
        /// Check if ZIP file contains specific content (for DOCX/XLSX differentiation)
        /// </summary>
        private bool CheckZipContent(byte[] data, string searchString)
        {
            if (data.Length < 100) return false;
            
            // Simple string search in first few KB
            string text = Encoding.ASCII.GetString(data, 0, Math.Min(data.Length, 4096));
            return text.Contains(searchString);
        }
    }
    
    #region Supporting Classes
    
    public class MagicBytePattern
    {
        public string FileType { get; set; }
        public byte[] Signature { get; set; }
        public int Offset { get; set; }
        public Func<byte[], bool> SecondaryCheck { get; set; }
    }
    
    public class FileAnalysisResult
    {
        public string FileType { get; set; }
        public string DetectedType { get; set; }
        public double Entropy { get; set; }
        public string EntropyCategory { get; set; }
        public bool IsText { get; set; }
        public bool IsBinary { get; set; }
        public bool IsCompressed { get; set; }
        public bool IsEncrypted { get; set; }
        public double Confidence { get; set; }
        
        public override string ToString()
        {
            return $"{FileType} (Entropy: {Entropy:F2}, Confidence: {Confidence:P0})";
        }
    }
    
    #endregion
}
