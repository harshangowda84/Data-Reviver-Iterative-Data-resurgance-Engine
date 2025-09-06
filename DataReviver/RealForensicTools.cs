using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace DataReviver
{
    public partial class RealForensicTools : Form
    {
    private TabControl _tabControl;
    private ImageList _tabIcons;
    private string _selectedFilePath;

        public RealForensicTools()
        {
            InitializeComponent();
            SetupTabIcons();
            SetupTabs();
        }

        private void InitializeComponent()
        {
            this.Text = "Data Reviver - Working Forensic Tools";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(240, 248, 255);
        }

        private void SetupTabs()
        {
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                ImageList = _tabIcons
            };

            // File Information Tab
            var infoTab = new TabPage("File Information") { ImageIndex = 0 };
            SetupFileInfoTab(infoTab);
            _tabControl.TabPages.Add(infoTab);

            // Hash Calculator Tab
            var hashTab = new TabPage("Hash Calculator") { ImageIndex = 1 };
            SetupHashTab(hashTab);
            _tabControl.TabPages.Add(hashTab);

            // File Content Viewer Tab
            var contentTab = new TabPage("Content Viewer") { ImageIndex = 2 };
            SetupContentTab(contentTab);
            _tabControl.TabPages.Add(contentTab);

            // File Type Detector Tab
            var typeTab = new TabPage("File Type Detector") { ImageIndex = 3 };
            SetupFileTypeTab(typeTab);
            _tabControl.TabPages.Add(typeTab);

            this.Controls.Add(_tabControl);

        }

        private void SetupTabIcons()
        {
            _tabIcons = new ImageList();
            _tabIcons.ImageSize = new Size(20, 20);
            _tabIcons.Images.Add(CreateTabIcon("📄")); // File Info
            _tabIcons.Images.Add(CreateTabIcon("#")); // Hash
            _tabIcons.Images.Add(CreateTabIcon("👁️")); // Viewer
            _tabIcons.Images.Add(CreateTabIcon("🔍")); // Type Detector
        }

        // Helper to create a bitmap icon from emoji
        private Bitmap CreateTabIcon(string emoji)
        {
            Bitmap bmp = new Bitmap(20, 20);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                using (Font font = new Font("Segoe UI Emoji", 13, FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    g.DrawString(emoji, font, Brushes.Black, -2, 0);
                }
            }
            return bmp;
        }

        private void SetupFileInfoTab(TabPage tab)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            // File selection
            var selectButton = new Button
            {
                Text = "Select File",
                Size = new Size(120, 30),
                Location = new Point(10, 10),
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            
            var pathLabel = new Label
            {
                Text = "No file selected",
                Location = new Point(140, 15),
                Size = new Size(600, 20),
                ForeColor = Color.Gray
            };
            
            // Results display
            var resultsText = new RichTextBox
            {
                Location = new Point(10, 50),
                Size = new Size(960, 600),
                Font = new Font("Consolas", 10),
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            selectButton.Click += (s, e) => SelectFile(pathLabel, resultsText, AnalyzeFileInfo);
            
            panel.Controls.AddRange(new Control[] { selectButton, pathLabel, resultsText });
            tab.Controls.Add(panel);
        }

        private void SetupHashTab(TabPage tab)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            var selectButton = new Button
            {
                Text = "Select File",
                Size = new Size(120, 30),
                Location = new Point(10, 10),
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            
            var pathLabel = new Label
            {
                Text = "No file selected",
                Location = new Point(140, 15),
                Size = new Size(600, 20),
                ForeColor = Color.Gray
            };
            
            var resultsText = new RichTextBox
            {
                Location = new Point(10, 50),
                Size = new Size(960, 600),
                Font = new Font("Consolas", 10),
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            selectButton.Click += (s, e) => SelectFile(pathLabel, resultsText, CalculateRealHashes);
            
            panel.Controls.AddRange(new Control[] { selectButton, pathLabel, resultsText });
            tab.Controls.Add(panel);
        }

        private void SetupContentTab(TabPage tab)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            var selectButton = new Button
            {
                Text = "Select File",
                Size = new Size(120, 30),
                Location = new Point(10, 10),
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            
            var pathLabel = new Label
            {
                Text = "No file selected",
                Location = new Point(140, 15),
                Size = new Size(600, 20),
                ForeColor = Color.Gray
            };
            
            var resultsText = new RichTextBox
            {
                Location = new Point(10, 50),
                Size = new Size(960, 600),
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            selectButton.Click += (s, e) => SelectFile(pathLabel, resultsText, ViewFileContent);
            
            panel.Controls.AddRange(new Control[] { selectButton, pathLabel, resultsText });
            tab.Controls.Add(panel);
        }

        private void SetupFileTypeTab(TabPage tab)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            var selectButton = new Button
            {
                Text = "Select File",
                Size = new Size(120, 30),
                Location = new Point(10, 10),
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            
            var pathLabel = new Label
            {
                Text = "No file selected",
                Location = new Point(140, 15),
                Size = new Size(600, 20),
                ForeColor = Color.Gray
            };
            
            var resultsText = new RichTextBox
            {
                Location = new Point(10, 50),
                Size = new Size(960, 600),
                Font = new Font("Consolas", 10),
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            selectButton.Click += (s, e) => SelectFile(pathLabel, resultsText, DetectFileType);
            
            panel.Controls.AddRange(new Control[] { selectButton, pathLabel, resultsText });
            tab.Controls.Add(panel);
        }

        private void SelectFile(Label pathLabel, RichTextBox resultsText, Action<string, RichTextBox> analysisAction)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select File for Analysis";
                dialog.Filter = "All Files (*.*)|*.*";
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    pathLabel.Text = dialog.FileName;
                    pathLabel.ForeColor = Color.Black;
                    _selectedFilePath = dialog.FileName;
                    analysisAction(dialog.FileName, resultsText);
                }
            }
        }

        private void AnalyzeFileInfo(string filePath, RichTextBox resultsText)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var report = new StringBuilder();
                
                report.AppendLine("=== FILE INFORMATION ANALYSIS ===");
                report.AppendLine($"Analysis Time: {DateTime.Now}");
                report.AppendLine();
                
                report.AppendLine("BASIC INFORMATION:");
                report.AppendLine($"File Name: {fileInfo.Name}");
                report.AppendLine($"Full Path: {fileInfo.FullName}");
                report.AppendLine($"Directory: {fileInfo.DirectoryName}");
                report.AppendLine($"Extension: {fileInfo.Extension}");
                report.AppendLine($"Size: {fileInfo.Length:N0} bytes ({GetHumanReadableSize(fileInfo.Length)})");
                report.AppendLine();
                
                report.AppendLine("TIMESTAMPS:");
                report.AppendLine($"Created: {fileInfo.CreationTime}");
                report.AppendLine($"Modified: {fileInfo.LastWriteTime}");
                report.AppendLine($"Accessed: {fileInfo.LastAccessTime}");
                report.AppendLine();
                
                report.AppendLine("ATTRIBUTES:");
                report.AppendLine($"Read Only: {fileInfo.IsReadOnly}");
                report.AppendLine($"Hidden: {(fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden}");
                report.AppendLine($"System: {(fileInfo.Attributes & FileAttributes.System) == FileAttributes.System}");
                report.AppendLine($"Archive: {(fileInfo.Attributes & FileAttributes.Archive) == FileAttributes.Archive}");
                report.AppendLine();
                
                if (fileInfo.Length > 0)
                {
                    report.AppendLine("FILE STRUCTURE ANALYSIS:");
                    using (var fs = File.OpenRead(filePath))
                    {
                        var buffer = new byte[Math.Min(64, fileInfo.Length)];
                        fs.Read(buffer, 0, buffer.Length);
                        
                        report.AppendLine("First 64 bytes (hex):");
                        for (int i = 0; i < buffer.Length; i += 16)
                        {
                            var hex = new StringBuilder();
                            var ascii = new StringBuilder();
                            for (int j = 0; j < 16 && i + j < buffer.Length; j++)
                            {
                                hex.Append($"{buffer[i + j]:X2} ");
                                char c = (char)buffer[i + j];
                                ascii.Append(char.IsControl(c) ? '.' : c);
                            }
                            report.AppendLine($"{i:X8}: {hex.ToString().PadRight(48)} {ascii}");
                        }
                    }
                }
                
                resultsText.Text = report.ToString();
            }
            catch (Exception ex)
            {
                resultsText.Text = $"Error analyzing file: {ex.Message}";
            }
        }

        private void CalculateRealHashes(string filePath, RichTextBox resultsText)
        {
            try
            {
                resultsText.Text = "Calculating hashes... Please wait...";
                resultsText.Refresh();
                
                var report = new StringBuilder();
                report.AppendLine("=== HASH CALCULATION REPORT ===");
                report.AppendLine($"File: {Path.GetFileName(filePath)}");
                report.AppendLine($"Path: {filePath}");
                report.AppendLine($"Size: {new FileInfo(filePath).Length:N0} bytes");
                report.AppendLine($"Calculation Time: {DateTime.Now}");
                report.AppendLine();
                
                using (var fs = File.OpenRead(filePath))
                {
                    // MD5
                    fs.Position = 0;
                    using (var md5 = MD5.Create())
                    {
                        var hash = md5.ComputeHash(fs);
                        report.AppendLine($"MD5:    {BitConverter.ToString(hash).Replace("-", "").ToLower()}");
                    }
                    
                    // SHA1
                    fs.Position = 0;
                    using (var sha1 = SHA1.Create())
                    {
                        var hash = sha1.ComputeHash(fs);
                        report.AppendLine($"SHA1:   {BitConverter.ToString(hash).Replace("-", "").ToLower()}");
                    }
                    
                    // SHA256
                    fs.Position = 0;
                    using (var sha256 = SHA256.Create())
                    {
                        var hash = sha256.ComputeHash(fs);
                        report.AppendLine($"SHA256: {BitConverter.ToString(hash).Replace("-", "").ToLower()}");
                    }
                }
                
                report.AppendLine();
                report.AppendLine("SECURITY ANALYSIS:");
                report.AppendLine("✅ File integrity can be verified with these hashes");
                report.AppendLine("✅ Hashes can be compared against malware databases");
                report.AppendLine("✅ SHA256 is recommended for forensic verification");
                
                resultsText.Text = report.ToString();
            }
            catch (Exception ex)
            {
                resultsText.Text = $"Error calculating hashes: {ex.Message}";
            }
        }

        private void ViewFileContent(string filePath, RichTextBox resultsText)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var report = new StringBuilder();
                
                report.AppendLine("=== FILE CONTENT ANALYSIS ===");
                report.AppendLine($"File: {fileInfo.Name}");
                report.AppendLine($"Size: {fileInfo.Length:N0} bytes");
                report.AppendLine();
                
                if (fileInfo.Length == 0)
                {
                    report.AppendLine("File is empty.");
                    resultsText.Text = report.ToString();
                    return;
                }
                
                // Read content based on file type
                var extension = fileInfo.Extension.ToLower();
                
                if (IsTextFile(extension))
                {
                    report.AppendLine("TEXT CONTENT:");
                    report.AppendLine(new string('=', 50));
                    try
                    {
                        var content = File.ReadAllText(filePath);
                        if (content.Length > 10000)
                        {
                            report.AppendLine("(Showing first 10,000 characters)");
                            report.AppendLine();
                            content = content.Substring(0, 10000) + "...";
                        }
                        report.AppendLine(content);
                    }
                    catch
                    {
                        report.AppendLine("Unable to read as text. Showing binary view:");
                        ShowBinaryContent(filePath, report);
                    }
                }
                else
                {
                    report.AppendLine("BINARY CONTENT (Hex View):");
                    report.AppendLine(new string('=', 50));
                    ShowBinaryContent(filePath, report);
                }
                
                resultsText.Text = report.ToString();
            }
            catch (Exception ex)
            {
                resultsText.Text = $"Error viewing file content: {ex.Message}";
            }
        }

        private void DetectFileType(string filePath, RichTextBox resultsText)
        {
            try
            {
                var report = new StringBuilder();
                report.AppendLine("=== FILE TYPE DETECTION ===");
                report.AppendLine($"File: {Path.GetFileName(filePath)}");
                report.AppendLine($"Analysis Time: {DateTime.Now}");
                report.AppendLine();
                
                var fileInfo = new FileInfo(filePath);
                
                // Extension-based detection
                report.AppendLine("EXTENSION ANALYSIS:");
                report.AppendLine($"File Extension: {fileInfo.Extension}");
                report.AppendLine($"Suggested Type: {GetTypeFromExtension(fileInfo.Extension)}");
                report.AppendLine();
                
                // Magic number detection
                report.AppendLine("MAGIC NUMBER ANALYSIS:");
                if (fileInfo.Length >= 4)
                {
                    using (var fs = File.OpenRead(filePath))
                    {
                        var buffer = new byte[16];
                        fs.Read(buffer, 0, Math.Min(16, (int)fileInfo.Length));
                        
                        var hex = BitConverter.ToString(buffer, 0, Math.Min(8, buffer.Length)).Replace("-", " ");
                        report.AppendLine($"First 8 bytes: {hex}");
                        
                        var detectedType = DetectTypeByMagicNumber(buffer);
                        report.AppendLine($"Detected Type: {detectedType}");
                        
                        if (detectedType != "Unknown" && !detectedType.Contains(fileInfo.Extension.TrimStart('.')))
                        {
                            report.AppendLine("⚠️  WARNING: File extension doesn't match detected type!");
                            report.AppendLine("   This could indicate file tampering or incorrect extension.");
                        }
                    }
                }
                else
                {
                    report.AppendLine("File too small for magic number analysis");
                }
                
                report.AppendLine();
                report.AppendLine("CONTENT CHARACTERISTICS:");
                AnalyzeContentCharacteristics(filePath, report);
                
                resultsText.Text = report.ToString();
            }
            catch (Exception ex)
            {
                resultsText.Text = $"Error detecting file type: {ex.Message}";
            }
        }

        private void AnalyzeContentCharacteristics(string filePath, StringBuilder report)
        {
            try
            {
                using (var fs = File.OpenRead(filePath))
                {
                    var buffer = new byte[Math.Min(1024, (int)fs.Length)];
                    fs.Read(buffer, 0, buffer.Length);
                    
                    var printableCount = 0;
                    var nullCount = 0;
                    var highBitCount = 0;
                    
                    foreach (var b in buffer)
                    {
                        if (b == 0) nullCount++;
                        else if (b >= 32 && b <= 126) printableCount++;
                        else if (b >= 128) highBitCount++;
                    }
                    
                    var printableRatio = (double)printableCount / buffer.Length;
                    var nullRatio = (double)nullCount / buffer.Length;
                    
                    report.AppendLine($"Printable characters: {printableRatio:P1}");
                    report.AppendLine($"Null bytes: {nullRatio:P1}");
                    report.AppendLine($"High bit set: {(double)highBitCount / buffer.Length:P1}");
                    
                    if (printableRatio > 0.8)
                        report.AppendLine("Assessment: Likely text file");
                    else if (nullRatio > 0.1)
                        report.AppendLine("Assessment: Likely binary file");
                    else
                        report.AppendLine("Assessment: Mixed content");
                }
            }
            catch (Exception ex)
            {
                report.AppendLine($"Error analyzing content: {ex.Message}");
            }
        }

        private string DetectTypeByMagicNumber(byte[] buffer)
        {
            if (buffer.Length < 4) return "Unknown";
            
            // Common file signatures
            if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
                return "JPEG Image";
            if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
                return "PNG Image";
            if (buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46)
                return "GIF Image";
            if (buffer[0] == 0x42 && buffer[1] == 0x4D)
                return "BMP Image";
            if (buffer[0] == 0x50 && buffer[1] == 0x4B)
                return "ZIP Archive (or Office document)";
            if (buffer[0] == 0x4D && buffer[1] == 0x5A)
                return "Windows Executable (PE)";
            if (buffer[0] == 0x7F && buffer[1] == 0x45 && buffer[2] == 0x4C && buffer[3] == 0x46)
                return "Linux Executable (ELF)";
            if (buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46)
                return "PDF Document";
            
            return "Unknown";
        }

        private string GetTypeFromExtension(string extension)
        {
            switch (extension.ToLower())
            {
                case ".txt": case ".log": case ".ini": return "Text File";
                case ".jpg": case ".jpeg": case ".png": case ".gif": case ".bmp": return "Image File";
                case ".exe": case ".dll": case ".msi": return "Executable File";
                case ".pdf": return "PDF Document";
                case ".doc": case ".docx": return "Word Document";
                case ".xls": case ".xlsx": return "Excel Spreadsheet";
                case ".zip": case ".rar": case ".7z": return "Archive File";
                case ".mp3": case ".wav": case ".flac": return "Audio File";
                case ".mp4": case ".avi": case ".mkv": return "Video File";
                default: return "Unknown File Type";
            }
        }

        private bool IsTextFile(string extension)
        {
            var textExtensions = new[] { ".txt", ".log", ".ini", ".cfg", ".xml", ".html", ".css", ".js", ".cs", ".cpp", ".h", ".py", ".java" };
            return textExtensions.Contains(extension.ToLower());
        }

        private void ShowBinaryContent(string filePath, StringBuilder report)
        {
            using (var fs = File.OpenRead(filePath))
            {
                var buffer = new byte[512]; // Show first 512 bytes
                var bytesRead = fs.Read(buffer, 0, buffer.Length);
                
                for (int i = 0; i < bytesRead; i += 16)
                {
                    var hex = new StringBuilder();
                    var ascii = new StringBuilder();
                    
                    for (int j = 0; j < 16 && i + j < bytesRead; j++)
                    {
                        hex.Append($"{buffer[i + j]:X2} ");
                        char c = (char)buffer[i + j];
                        ascii.Append(char.IsControl(c) ? '.' : c);
                    }
                    
                    report.AppendLine($"{i:X8}: {hex.ToString().PadRight(48)} {ascii}");
                }
                
                if (fs.Length > 512)
                {
                    report.AppendLine($"... ({fs.Length - 512} more bytes)");
                }
            }
        }

        private string GetHumanReadableSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
