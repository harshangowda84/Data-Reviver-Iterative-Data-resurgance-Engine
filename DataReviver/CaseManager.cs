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
            // Load all case files from the cases folder
            var cases = new List<ForensicCase>();
            if (Directory.Exists(CASES_FOLDER))
            {
                foreach (var file in Directory.GetFiles(CASES_FOLDER, "*.case", SearchOption.AllDirectories))
                {
                    try
                    {
                        var loaded = LoadCase(file);
                        if (loaded != null) cases.Add(loaded);
                    }
                    catch { }
                }
            }
            using (var form = new OpenCaseForm(cases))
            {
                if (form.ShowDialog() == DialogResult.OK && form.SelectedCase != null)
                {
                    _currentCase = form.SelectedCase;
                    return _currentCase;
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
            this.Text = "Create New Case";
            this.Size = new System.Drawing.Size(520, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = System.Drawing.Color.FromArgb(245, 247, 251);

            var cardPanel = new Panel {
                Location = new System.Drawing.Point(30, 40),
                Size = new System.Drawing.Size(460, 380),
                BackColor = System.Drawing.Color.White,
                BorderStyle = BorderStyle.None
            };
            cardPanel.Paint += (s, e) => {
                var g = e.Graphics;
                var rect = cardPanel.ClientRectangle;
                rect.Inflate(-1, -1);
                using (var shadow = new System.Drawing.Drawing2D.GraphicsPath()) {
                    shadow.AddRectangle(rect);
                    using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(rect, System.Drawing.Color.FromArgb(30, 0, 0, 0), System.Drawing.Color.Transparent, 90F)) {
                        g.FillPath(brush, shadow);
                    }
                }
            };

            // Header icon and title
            var headerIcon = new Label {
                Text = "🗂️",
                Font = new System.Drawing.Font("Segoe UI Emoji", 36F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(0, 122, 255),
                Location = new System.Drawing.Point(0, 0),
                Size = new System.Drawing.Size(460, 60),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            var headerTitle = new Label {
                Text = "Create New Case",
                Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(0, 122, 255),
                Location = new System.Drawing.Point(0, 50),
                Size = new System.Drawing.Size(460, 50),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            // Investigator Name
            var iconInvestigator = new Label {
                Text = "👤",
                Font = new System.Drawing.Font("Segoe UI Emoji", 18F),
                Location = new System.Drawing.Point(20, 110),
                Size = new System.Drawing.Size(32, 32),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            var lblInvestigator = new Label {
                Text = "Investigator Name:",
                Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(60, 110),
                Size = new System.Drawing.Size(160, 32)
            };
            txtInvestigator = new TextBox {
                Location = new System.Drawing.Point(230, 110),
                Size = new System.Drawing.Size(210, 32),
                Font = new System.Drawing.Font("Segoe UI", 12F),
                BorderStyle = BorderStyle.FixedSingle,
                ForeColor = System.Drawing.Color.Gray,
                Text = "Enter investigator name"
            };
            txtInvestigator.GotFocus += (s, e) => {
                if (txtInvestigator.Text == "Enter investigator name") {
                    txtInvestigator.Text = "";
                    txtInvestigator.ForeColor = System.Drawing.Color.Black;
                }
            };
            txtInvestigator.LostFocus += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtInvestigator.Text)) {
                    txtInvestigator.Text = "Enter investigator name";
                    txtInvestigator.ForeColor = System.Drawing.Color.Gray;
                }
            };

            // Case ID
            var iconCaseId = new Label {
                Text = "🆔",
                Font = new System.Drawing.Font("Segoe UI Emoji", 18F),
                Location = new System.Drawing.Point(20, 155),
                Size = new System.Drawing.Size(32, 32),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            var lblCaseId = new Label {
                Text = "Case ID:",
                Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(60, 155),
                Size = new System.Drawing.Size(160, 32)
            };
            var txtCaseId = new TextBox {
                Location = new System.Drawing.Point(230, 155),
                Size = new System.Drawing.Size(210, 32),
                Font = new System.Drawing.Font("Segoe UI", 12F),
                Text = Guid.NewGuid().ToString("N").Substring(0, 8),
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Case Name
            var iconCaseName = new Label {
                Text = "📄",
                Font = new System.Drawing.Font("Segoe UI Emoji", 18F),
                Location = new System.Drawing.Point(20, 200),
                Size = new System.Drawing.Size(32, 32),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            var lblCaseName = new Label {
                Text = "Case Name:",
                Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(60, 200),
                Size = new System.Drawing.Size(160, 32)
            };
            txtCaseName = new TextBox {
                Location = new System.Drawing.Point(230, 200),
                Size = new System.Drawing.Size(210, 32),
                Font = new System.Drawing.Font("Segoe UI", 12F),
                Text = $"Case_{txtCaseId.Text}",
                BorderStyle = BorderStyle.FixedSingle,
                ForeColor = System.Drawing.Color.Gray
            };
            txtCaseName.GotFocus += (s, e) => {
                if (txtCaseName.Text == $"Case_{txtCaseId.Text}" || txtCaseName.Text == "Enter case name") {
                    txtCaseName.Text = "";
                    txtCaseName.ForeColor = System.Drawing.Color.Black;
                }
            };
            txtCaseName.LostFocus += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtCaseName.Text)) {
                    txtCaseName.Text = "Enter case name";
                    txtCaseName.ForeColor = System.Drawing.Color.Gray;
                }
            };

            // Case Description
            var iconDesc = new Label {
                Text = "📝",
                Font = new System.Drawing.Font("Segoe UI Emoji", 18F),
                Location = new System.Drawing.Point(20, 245),
                Size = new System.Drawing.Size(32, 32),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            var lblDescription = new Label {
                Text = "Case Description:",
                Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(60, 245),
                Size = new System.Drawing.Size(160, 32)
            };
            txtDescription = new TextBox {
                Location = new System.Drawing.Point(230, 245),
                Size = new System.Drawing.Size(210, 60),
                Font = new System.Drawing.Font("Segoe UI", 11F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                ForeColor = System.Drawing.Color.Gray,
                Text = "Enter case description..."
            };
            txtDescription.GotFocus += (s, e) => {
                if (txtDescription.Text == "Enter case description...") {
                    txtDescription.Text = "";
                    txtDescription.ForeColor = System.Drawing.Color.Black;
                }
            };
            txtDescription.LostFocus += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtDescription.Text)) {
                    txtDescription.Text = "Enter case description...";
                    txtDescription.ForeColor = System.Drawing.Color.Gray;
                }
            };

            // Buttons
            var btnOK = new Button {
                Text = "Create",
                Location = new System.Drawing.Point(110, 325),
                Size = new System.Drawing.Size(100, 38),
                DialogResult = DialogResult.OK,
                BackColor = System.Drawing.Color.FromArgb(0, 122, 255),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(0, 90, 200);
            btnOK.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(0, 90, 200);
            btnOK.Cursor = Cursors.Hand;
            btnOK.Click += BtnOK_Click;

            var btnCancel = new Button {
                Text = "Cancel",
                Location = new System.Drawing.Point(250, 325),
                Size = new System.Drawing.Size(100, 38),
                DialogResult = DialogResult.Cancel,
                BackColor = System.Drawing.Color.FromArgb(220, 53, 69),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(180, 40, 50);
            btnCancel.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(180, 40, 50);
            btnCancel.Cursor = Cursors.Hand;

            cardPanel.Controls.Add(headerIcon);
            cardPanel.Controls.Add(headerTitle);
            cardPanel.Controls.Add(iconInvestigator);
            cardPanel.Controls.Add(lblInvestigator);
            cardPanel.Controls.Add(txtInvestigator);
            cardPanel.Controls.Add(iconCaseId);
            cardPanel.Controls.Add(lblCaseId);
            cardPanel.Controls.Add(txtCaseId);
            cardPanel.Controls.Add(iconCaseName);
            cardPanel.Controls.Add(lblCaseName);
            cardPanel.Controls.Add(txtCaseName);
            cardPanel.Controls.Add(iconDesc);
            cardPanel.Controls.Add(lblDescription);
            cardPanel.Controls.Add(txtDescription);
            cardPanel.Controls.Add(btnOK);
            cardPanel.Controls.Add(btnCancel);

            this.Controls.Add(cardPanel);
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
