using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace DataReviver
{
    public class DigitalForensicsTools
    {
        public static class FileAnalysis
        {
            public static Dictionary<string, object> AnalyzeFile(string filePath)
            {
                var analysis = new Dictionary<string, object>();
                var fileInfo = new FileInfo(filePath);

                analysis["FileName"] = fileInfo.Name;
                analysis["FileSize"] = fileInfo.Length;
                analysis["CreationTime"] = fileInfo.CreationTime;
                analysis["LastWriteTime"] = fileInfo.LastWriteTime;
                analysis["LastAccessTime"] = fileInfo.LastAccessTime;
                analysis["IsReadOnly"] = fileInfo.IsReadOnly;
                analysis["IsHidden"] = (fileInfo.Attributes & FileAttributes.Hidden) != 0;
                analysis["Extension"] = fileInfo.Extension.ToLower();

                // File signature analysis
                analysis["FileSignature"] = GetFileSignature(filePath);
                analysis["TrueFileType"] = DetectTrueFileType(filePath);
                
                // Hash calculations
                analysis["MD5"] = CalculateMD5(filePath);
                analysis["SHA1"] = CalculateSHA1(filePath);
                analysis["SHA256"] = CalculateSHA256(filePath);

                // Entropy analysis
                analysis["Entropy"] = CalculateEntropy(filePath);
                analysis["IsPossiblyEncrypted"] = (double)analysis["Entropy"] > 7.5;

                return analysis;
            }

            private static string GetFileSignature(string filePath)
            {
                try
                {
                    using (var fs = File.OpenRead(filePath))
                    {
                        var buffer = new byte[16];
                        fs.Read(buffer, 0, 16);
                        return BitConverter.ToString(buffer).Replace("-", " ");
                    }
                }
                catch
                {
                    return "Unable to read signature";
                }
            }

            private static string DetectTrueFileType(string filePath)
            {
                var signature = GetFileSignature(filePath).Replace(" ", "");
                
                var signatures = new Dictionary<string, string>
                {
                    {"FFD8FF", "JPEG Image"},
                    {"89504E47", "PNG Image"},
                    {"474946", "GIF Image"},
                    {"504B0304", "ZIP Archive"},
                    {"504B0506", "ZIP Archive (Empty)"},
                    {"504B0708", "ZIP Archive (Spanned)"},
                    {"52617221", "RAR Archive"},
                    {"377ABCAF271C", "7-Zip Archive"},
                    {"D0CF11E0A1B11AE1", "Microsoft Office Document"},
                    {"25504446", "PDF Document"},
                    {"4D5A", "Windows Executable"},
                    {"7F454C46", "Linux Executable (ELF)"},
                    {"CAFEBABE", "Java Class File"},
                    {"FFFE", "Unicode Text (UTF-16 LE BOM)"},
                    {"FEFF", "Unicode Text (UTF-16 BE BOM)"},
                    {"EFBBBF", "Unicode Text (UTF-8 BOM)"}
                };

                foreach (var sig in signatures)
                {
                    if (signature.StartsWith(sig.Key))
                        return sig.Value;
                }

                return "Unknown/Custom Format";
            }

            private static string CalculateMD5(string filePath)
            {
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }

            private static string CalculateSHA1(string filePath)
            {
                using (var sha1 = SHA1.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = sha1.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }

            private static string CalculateSHA256(string filePath)
            {
                using (var sha256 = SHA256.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }

            private static double CalculateEntropy(string filePath)
            {
                var frequency = new int[256];
                var totalBytes = 0;

                using (var fs = File.OpenRead(filePath))
                {
                    int b;
                    while ((b = fs.ReadByte()) != -1)
                    {
                        frequency[b]++;
                        totalBytes++;
                        
                        // Limit analysis to first 64KB for performance
                        if (totalBytes > 65536) break;
                    }
                }

                if (totalBytes == 0) return 0;

                double entropy = 0;
                for (int i = 0; i < 256; i++)
                {
                    if (frequency[i] > 0)
                    {
                        double probability = (double)frequency[i] / totalBytes;
                        entropy -= probability * Math.Log(probability, 2);
                    }
                }

                return entropy;
            }
        }

        public static class NetworkAnalysis
        {
            public static Dictionary<string, object> AnalyzeNetworkArtifacts(string systemPath)
            {
                var artifacts = new Dictionary<string, object>();
                
                // Browser history analysis
                artifacts["BrowserHistory"] = ExtractBrowserHistory(systemPath);
                
                // Network connections
                artifacts["NetworkConnections"] = GetNetworkConnections();
                
                // DNS cache
                artifacts["DNSCache"] = GetDNSCache();
                
                return artifacts;
            }

            private static List<string> ExtractBrowserHistory(string systemPath)
            {
                var history = new List<string>();
                
                // Chrome history locations
                var chromeHistoryPaths = new[]
                {
                    Path.Combine(systemPath, @"Users\*\AppData\Local\Google\Chrome\User Data\Default\History"),
                    Path.Combine(systemPath, @"Users\*\AppData\Local\Google\Chrome\User Data\Profile *\History")
                };

                // Firefox history locations  
                var firefoxHistoryPaths = new[]
                {
                    Path.Combine(systemPath, @"Users\*\AppData\Roaming\Mozilla\Firefox\Profiles\*\places.sqlite")
                };

                history.Add("Browser history analysis would extract URLs, timestamps, and visit counts");
                history.Add("Supported browsers: Chrome, Firefox, Edge, Internet Explorer");
                
                return history;
            }

            private static List<string> GetNetworkConnections()
            {
                return new List<string>
                {
                    "Active network connections analysis",
                    "Historical connection logs",
                    "Port usage analysis"
                };
            }

            private static List<string> GetDNSCache()
            {
                return new List<string>
                {
                    "DNS resolution history",
                    "Cached domain lookups",
                    "Network activity timeline"
                };
            }
        }

        public static class RegistryAnalysis
        {
            public static Dictionary<string, object> AnalyzeWindowsRegistry(string systemPath)
            {
                var analysis = new Dictionary<string, object>();
                
                analysis["RecentDocuments"] = GetRecentDocuments(systemPath);
                analysis["USBDevices"] = GetUSBDeviceHistory(systemPath);
                analysis["StartupPrograms"] = GetStartupPrograms(systemPath);
                analysis["UninstallEntries"] = GetUninstallEntries(systemPath);
                analysis["UserActivity"] = GetUserActivityArtifacts(systemPath);
                
                return analysis;
            }

            private static List<string> GetRecentDocuments(string systemPath)
            {
                return new List<string>
                {
                    "Recently opened documents from registry",
                    "Office document history",
                    "Recent file access patterns"
                };
            }

            private static List<string> GetUSBDeviceHistory(string systemPath)
            {
                return new List<string>
                {
                    "USB device connection history",
                    "Device serial numbers and vendor info",
                    "First and last connection times"
                };
            }

            private static List<string> GetStartupPrograms(string systemPath)
            {
                return new List<string>
                {
                    "Programs configured to start with Windows",
                    "Startup locations and persistence mechanisms",
                    "Potential malware persistence analysis"
                };
            }

            private static List<string> GetUninstallEntries(string systemPath)
            {
                return new List<string>
                {
                    "Installed software inventory",
                    "Installation and uninstall timestamps",
                    "Software version and publisher information"
                };
            }

            private static List<string> GetUserActivityArtifacts(string systemPath)
            {
                return new List<string>
                {
                    "User login/logout activity",
                    "Application usage patterns",
                    "System shutdown and startup events"
                };
            }
        }

        public static class TimelineGeneration
        {
            public static List<TimelineEvent> GenerateSystemTimeline(string systemPath)
            {
                var events = new List<TimelineEvent>();
                
                // File system timeline
                events.AddRange(GenerateFileSystemTimeline(systemPath));
                
                // Registry timeline
                events.AddRange(GenerateRegistryTimeline(systemPath));
                
                // Event log timeline
                events.AddRange(GenerateEventLogTimeline(systemPath));
                
                return events.OrderBy(e => e.Timestamp).ToList();
            }

            private static List<TimelineEvent> GenerateFileSystemTimeline(string systemPath)
            {
                var events = new List<TimelineEvent>();
                
                try
                {
                    // Analyze key directories for timeline events
                    var keyDirectories = new[]
                    {
                        Path.Combine(systemPath, "Users"),
                        Path.Combine(systemPath, "Windows", "System32"),
                        Path.Combine(systemPath, "Program Files"),
                        Path.Combine(systemPath, "ProgramData")
                    };

                    foreach (var dir in keyDirectories)
                    {
                        if (Directory.Exists(dir))
                        {
                            // Sample timeline generation
                            events.Add(new TimelineEvent
                            {
                                Timestamp = Directory.GetCreationTime(dir),
                                EventType = "Directory Created",
                                Description = $"Directory created: {dir}"
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    events.Add(new TimelineEvent
                    {
                        Timestamp = DateTime.Now,
                        EventType = "Analysis Error",
                        Description = $"Error analyzing file system: {ex.Message}"
                    });
                }
                
                return events;
            }

            private static List<TimelineEvent> GenerateRegistryTimeline(string systemPath)
            {
                return new List<TimelineEvent>
                {
                    new TimelineEvent
                    {
                        Timestamp = DateTime.Now,
                        EventType = "Registry Analysis",
                        Description = "Registry timeline analysis - examining key modifications"
                    }
                };
            }

            private static List<TimelineEvent> GenerateEventLogTimeline(string systemPath)
            {
                return new List<TimelineEvent>
                {
                    new TimelineEvent
                    {
                        Timestamp = DateTime.Now,
                        EventType = "Event Log Analysis",
                        Description = "Windows Event Log timeline analysis"
                    }
                };
            }
        }
    }
}
