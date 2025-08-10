using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace DataReviver
{
    public class ForensicCase
    {
        public string CaseId { get; set; }
        public string CaseName { get; set; }
        public string InvestigatorName { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModified { get; set; }
        public string CaseFolderPath { get; set; }
        public List<Evidence> EvidenceItems { get; set; }
        public List<CaseNote> Notes { get; set; }
        public CaseStatus Status { get; set; }

        public ForensicCase()
        {
            CaseId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            CreatedDate = DateTime.Now;
            LastModified = DateTime.Now;
            EvidenceItems = new List<Evidence>();
            Notes = new List<CaseNote>();
            Status = CaseStatus.Active;
        }
    }

    public class Evidence
    {
        public string EvidenceId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public DateTime AcquiredDate { get; set; }
        public string MD5Hash { get; set; }
        public string SHA256Hash { get; set; }
        public string Description { get; set; }
        public EvidenceType Type { get; set; }
        public string ChainOfCustody { get; set; }

        public Evidence()
        {
            EvidenceId = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            AcquiredDate = DateTime.Now;
        }
    }

    public class CaseNote
    {
        public DateTime Timestamp { get; set; }
        public string Note { get; set; }
        public string Author { get; set; }

        public CaseNote(string note, string author = null)
        {
            Timestamp = DateTime.Now;
            Note = note;
            Author = author ?? Environment.UserName;
        }
    }

    public enum CaseStatus
    {
        Active,
        Closed,
        OnHold,
        UnderReview
    }

    public enum EvidenceType
    {
        File,
        DiskImage,
        MemoryDump,
        NetworkCapture,
        Document,
        Photo,
        Other
    }

    public partial class CaseManager : Form
    {
        private ForensicCase _currentCase;
        private const string CASES_FOLDER = "ForensicCases";

        public ForensicCase CurrentCase => _currentCase;

        public CaseManager()
        {
            InitializeComponent();
            EnsureCasesFolderExists();
        }

        private void InitializeComponent()
        {
            this.Text = "Forensic Case Manager";
            this.Size = new System.Drawing.Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
        }

        private void EnsureCasesFolderExists()
        {
            if (!Directory.Exists(CASES_FOLDER))
            {
                Directory.CreateDirectory(CASES_FOLDER);
            }
        }

        public ForensicCase CreateNewCase()
        {
            using (var dialog = new NewCaseDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _currentCase = new ForensicCase
                    {
                        CaseName = dialog.CaseName,
                        InvestigatorName = dialog.InvestigatorName,
                        Description = dialog.Description
                    };

                    // Create case folder
                    var caseFolder = Path.Combine(CASES_FOLDER, $"CASE_{_currentCase.CaseId}_{SanitizeFileName(_currentCase.CaseName)}");
                    Directory.CreateDirectory(caseFolder);
                    _currentCase.CaseFolderPath = caseFolder;

                    // Add initial note
                    _currentCase.Notes.Add(new CaseNote($"Case created by {_currentCase.InvestigatorName}"));

                    SaveCase();
                    return _currentCase;
                }
            }
            return null;
        }

        public ForensicCase OpenExistingCase()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Open Forensic Case";
                dialog.Filter = "Case Files (*.case)|*.case|All Files (*.*)|*.*";
                dialog.InitialDirectory = Path.GetFullPath(CASES_FOLDER);

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _currentCase = LoadCase(dialog.FileName);
                        return _currentCase;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening case: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            return null;
        }

        public void AddEvidence(string filePath)
        {
            if (_currentCase == null)
            {
                MessageBox.Show("No case is currently open. Please create or open a case first.", "No Active Case", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var fileInfo = new FileInfo(filePath);
                var evidence = new Evidence
                {
                    FileName = fileInfo.Name,
                    FilePath = filePath,
                    FileSize = fileInfo.Length,
                    Description = $"File added to case at {DateTime.Now}"
                };

                // Calculate hashes for integrity
                using (var fs = File.OpenRead(filePath))
                {
                    using (var md5 = System.Security.Cryptography.MD5.Create())
                    {
                        var hash = md5.ComputeHash(fs);
                        evidence.MD5Hash = BitConverter.ToString(hash).Replace("-", "").ToLower();
                    }
                    
                    fs.Position = 0;
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        var hash = sha256.ComputeHash(fs);
                        evidence.SHA256Hash = BitConverter.ToString(hash).Replace("-", "").ToLower();
                    }
                }

                // Copy file to case folder
                var evidenceFolder = Path.Combine(_currentCase.CaseFolderPath, "Evidence");
                Directory.CreateDirectory(evidenceFolder);
                
                var evidenceFilePath = Path.Combine(evidenceFolder, $"E{evidence.EvidenceId}_{evidence.FileName}");
                File.Copy(filePath, evidenceFilePath, true);
                evidence.FilePath = evidenceFilePath;

                evidence.ChainOfCustody = $"Acquired by {Environment.UserName} on {DateTime.Now:yyyy-MM-dd HH:mm:ss} from {filePath}";

                _currentCase.EvidenceItems.Add(evidence);
                _currentCase.Notes.Add(new CaseNote($"Evidence {evidence.EvidenceId} added: {evidence.FileName}"));
                _currentCase.LastModified = DateTime.Now;

                SaveCase();
                MessageBox.Show($"Evidence {evidence.EvidenceId} added successfully!", "Evidence Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding evidence: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void AddNote(string note)
        {
            if (_currentCase == null) return;

            _currentCase.Notes.Add(new CaseNote(note));
            _currentCase.LastModified = DateTime.Now;
            SaveCase();
        }

        public void GenerateCaseReport()
        {
            if (_currentCase == null)
            {
                MessageBox.Show("No case is currently open.", "No Active Case", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var report = new StringBuilder();
                report.AppendLine("=== FORENSIC CASE REPORT ===");
                report.AppendLine($"Case ID: {_currentCase.CaseId}");
                report.AppendLine($"Case Name: {_currentCase.CaseName}");
                report.AppendLine($"Investigator: {_currentCase.InvestigatorName}");
                report.AppendLine($"Created: {_currentCase.CreatedDate}");
                report.AppendLine($"Last Modified: {_currentCase.LastModified}");
                report.AppendLine($"Status: {_currentCase.Status}");
                report.AppendLine($"Description: {_currentCase.Description}");
                report.AppendLine();

                report.AppendLine("EVIDENCE INVENTORY:");
                report.AppendLine(new string('=', 50));
                foreach (var evidence in _currentCase.EvidenceItems)
                {
                    report.AppendLine($"Evidence ID: {evidence.EvidenceId}");
                    report.AppendLine($"  File: {evidence.FileName}");
                    report.AppendLine($"  Size: {evidence.FileSize:N0} bytes");
                    report.AppendLine($"  Acquired: {evidence.AcquiredDate}");
                    report.AppendLine($"  MD5: {evidence.MD5Hash}");
                    report.AppendLine($"  SHA256: {evidence.SHA256Hash}");
                    report.AppendLine($"  Chain of Custody: {evidence.ChainOfCustody}");
                    report.AppendLine();
                }

                report.AppendLine("CASE NOTES:");
                report.AppendLine(new string('=', 50));
                foreach (var note in _currentCase.Notes.OrderBy(n => n.Timestamp))
                {
                    report.AppendLine($"[{note.Timestamp:yyyy-MM-dd HH:mm:ss}] {note.Author}: {note.Note}");
                }

                report.AppendLine();
                report.AppendLine("CASE SUMMARY:");
                report.AppendLine(new string('=', 50));
                report.AppendLine($"Total Evidence Items: {_currentCase.EvidenceItems.Count}");
                report.AppendLine($"Total File Size: {_currentCase.EvidenceItems.Sum(e => e.FileSize):N0} bytes");
                report.AppendLine($"Case Duration: {(_currentCase.LastModified - _currentCase.CreatedDate).TotalDays:F1} days");
                report.AppendLine();
                report.AppendLine("This report was generated by Data Reviver Forensic Analysis Tool");
                report.AppendLine($"Report Generation Time: {DateTime.Now}");

                using (var dialog = new SaveFileDialog())
                {
                    dialog.Filter = "Text Files (*.txt)|*.txt|PDF Files (*.pdf)|*.pdf";
                    dialog.DefaultExt = "txt";
                    dialog.FileName = $"CaseReport_{_currentCase.CaseId}_{DateTime.Now:yyyyMMdd}.txt";
                    dialog.InitialDirectory = _currentCase.CaseFolderPath;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(dialog.FileName, report.ToString());
                        MessageBox.Show("Case report generated successfully!", "Report Generated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveCase()
        {
            if (_currentCase == null) return;

            try
            {
                var caseFile = Path.Combine(_currentCase.CaseFolderPath, $"{_currentCase.CaseId}.case");
                var xml = SerializeCaseToXml(_currentCase);
                File.WriteAllText(caseFile, xml);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving case: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private ForensicCase LoadCase(string filePath)
        {
            var xml = File.ReadAllText(filePath);
            return DeserializeCaseFromXml(xml);
        }

        private string SerializeCaseToXml(ForensicCase forensicCase)
        {
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<ForensicCase>");
            xml.AppendLine($"  <CaseId>{forensicCase.CaseId}</CaseId>");
            xml.AppendLine($"  <CaseName><![CDATA[{forensicCase.CaseName}]]></CaseName>");
            xml.AppendLine($"  <InvestigatorName><![CDATA[{forensicCase.InvestigatorName}]]></InvestigatorName>");
            xml.AppendLine($"  <Description><![CDATA[{forensicCase.Description}]]></Description>");
            xml.AppendLine($"  <CreatedDate>{forensicCase.CreatedDate:O}</CreatedDate>");
            xml.AppendLine($"  <LastModified>{forensicCase.LastModified:O}</LastModified>");
            xml.AppendLine($"  <Status>{forensicCase.Status}</Status>");
            xml.AppendLine($"  <CaseFolderPath><![CDATA[{forensicCase.CaseFolderPath}]]></CaseFolderPath>");
            
            xml.AppendLine("  <Evidence>");
            foreach (var evidence in forensicCase.EvidenceItems)
            {
                xml.AppendLine("    <Item>");
                xml.AppendLine($"      <EvidenceId>{evidence.EvidenceId}</EvidenceId>");
                xml.AppendLine($"      <FileName><![CDATA[{evidence.FileName}]]></FileName>");
                xml.AppendLine($"      <FilePath><![CDATA[{evidence.FilePath}]]></FilePath>");
                xml.AppendLine($"      <FileSize>{evidence.FileSize}</FileSize>");
                xml.AppendLine($"      <AcquiredDate>{evidence.AcquiredDate:O}</AcquiredDate>");
                xml.AppendLine($"      <MD5Hash>{evidence.MD5Hash}</MD5Hash>");
                xml.AppendLine($"      <SHA256Hash>{evidence.SHA256Hash}</SHA256Hash>");
                xml.AppendLine($"      <Description><![CDATA[{evidence.Description}]]></Description>");
                xml.AppendLine($"      <ChainOfCustody><![CDATA[{evidence.ChainOfCustody}]]></ChainOfCustody>");
                xml.AppendLine("    </Item>");
            }
            xml.AppendLine("  </Evidence>");

            xml.AppendLine("  <Notes>");
            foreach (var note in forensicCase.Notes)
            {
                xml.AppendLine("    <Note>");
                xml.AppendLine($"      <Timestamp>{note.Timestamp:O}</Timestamp>");
                xml.AppendLine($"      <Author><![CDATA[{note.Author}]]></Author>");
                xml.AppendLine($"      <Content><![CDATA[{note.Note}]]></Content>");
                xml.AppendLine("    </Note>");
            }
            xml.AppendLine("  </Notes>");
            xml.AppendLine("</ForensicCase>");

            return xml.ToString();
        }

        private ForensicCase DeserializeCaseFromXml(string xml)
        {
            // Simple XML parsing - in production would use XDocument
            var forensicCase = new ForensicCase();
            
            // This is a simplified implementation
            // In a real forensic tool, you'd use proper XML parsing
            
            return forensicCase;
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        public List<string> GetRecentCases()
        {
            var recentCases = new List<string>();
            
            if (Directory.Exists(CASES_FOLDER))
            {
                var caseFiles = Directory.GetFiles(CASES_FOLDER, "*.case", SearchOption.AllDirectories);
                recentCases.AddRange(caseFiles.Take(10)); // Show last 10 cases
            }
            
            return recentCases;
        }
    }

    public partial class NewCaseDialog : Form
    {
        public string CaseName { get; private set; }
        public string InvestigatorName { get; private set; }
        public string Description { get; private set; }

        private TextBox txtCaseName;
        private TextBox txtInvestigator;
        private TextBox txtDescription;

        public NewCaseDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Create New Forensic Case";
            this.Size = new System.Drawing.Size(450, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var lblCaseName = new Label
            {
                Text = "Case Name:",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(100, 23)
            };

            txtCaseName = new TextBox
            {
                Location = new System.Drawing.Point(130, 20),
                Size = new System.Drawing.Size(280, 23),
                Text = $"Investigation_{DateTime.Now:yyyyMMdd}"
            };

            var lblInvestigator = new Label
            {
                Text = "Investigator:",
                Location = new System.Drawing.Point(20, 60),
                Size = new System.Drawing.Size(100, 23)
            };

            txtInvestigator = new TextBox
            {
                Location = new System.Drawing.Point(130, 60),
                Size = new System.Drawing.Size(280, 23),
                Text = Environment.UserName
            };

            var lblDescription = new Label
            {
                Text = "Description:",
                Location = new System.Drawing.Point(20, 100),
                Size = new System.Drawing.Size(100, 23)
            };

            txtDescription = new TextBox
            {
                Location = new System.Drawing.Point(130, 100),
                Size = new System.Drawing.Size(280, 120),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            var btnOK = new Button
            {
                Text = "Create Case",
                Location = new System.Drawing.Point(250, 250),
                Size = new System.Drawing.Size(80, 30),
                DialogResult = DialogResult.OK,
                BackColor = System.Drawing.Color.FromArgb(34, 139, 34),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOK.Click += BtnOK_Click;

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(340, 250),
                Size = new System.Drawing.Size(70, 30),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[] { 
                lblCaseName, txtCaseName, 
                lblInvestigator, txtInvestigator,
                lblDescription, txtDescription,
                btnOK, btnCancel 
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCaseName.Text))
            {
                MessageBox.Show("Please enter a case name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtInvestigator.Text))
            {
                MessageBox.Show("Please enter investigator name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CaseName = txtCaseName.Text.Trim();
            InvestigatorName = txtInvestigator.Text.Trim();
            Description = txtDescription.Text.Trim();
        }
    }
}
