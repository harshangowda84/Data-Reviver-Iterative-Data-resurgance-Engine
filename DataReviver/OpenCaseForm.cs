using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DataReviver;

namespace DataReviver
{
    public class OpenCaseForm : Form
    {
    private List<ForensicCase> cases;
    private ListBox caseListBox;
    private TextBox searchBox;
    private Label detailsLabel;
    private Button backButton;
    private Panel cardPanel;
    private Button openCaseButton;
    private Button deleteCaseButton;

        public ForensicCase SelectedCase { get; private set; }

        public OpenCaseForm(List<ForensicCase> cases)
        {
            this.cases = cases ?? new List<ForensicCase>();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Open Existing Case";
            this.Size = new Size(700, 430);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 251);

            var headerLabel = new Label
            {
                Text = "Open Existing Case",
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 255),
                Location = new Point((this.Width - 400) / 2, 20), // Center horizontally
                Size = new Size(400, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            backButton = new Button
            {
                Text = "← Back",
                Location = new Point(20, 28), // Next to headerLabel
                Size = new Size(90, 32),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                TabStop = false
            };
            backButton.FlatAppearance.BorderSize = 0;
            backButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(180, 40, 50);
            backButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 40, 50);
            backButton.Cursor = Cursors.Hand;
            backButton.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            cardPanel = new Panel
            {
                Location = new Point(40, 90),
                Size = new Size(620, 290),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            cardPanel.Paint += (s, e) => {
                var g = e.Graphics;
                var rect = cardPanel.ClientRectangle;
                rect.Inflate(-2, -2);
                using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                {
                    g.FillRectangle(shadowBrush, rect);
                }
            };

            searchBox = new TextBox
            {
                Location = new Point(30, 30),
                Size = new Size(260, 32),
                Font = new Font("Segoe UI", 12F),
                ForeColor = Color.Gray,
                Text = "Search by name or ID"
            };
            searchBox.GotFocus += (s, e) => {
                if (searchBox.Text == "Search by name or ID") {
                    searchBox.Text = "";
                    searchBox.ForeColor = Color.Black;
                }
                // Reset details when focusing search
                detailsLabel.Text = "Select a case to view details.";
                SelectedCase = null;
                openCaseButton.Visible = false;
                deleteCaseButton.Visible = false;
            };
            searchBox.LostFocus += (s, e) => {
                if (string.IsNullOrWhiteSpace(searchBox.Text)) {
                    searchBox.Text = "Search by name or ID";
                    searchBox.ForeColor = Color.Gray;
                }
            };
            searchBox.TextChanged += (s, e) => {
                FilterCases();
                // Reset details if no case is selected
                if (caseListBox.SelectedIndex < 0) {
                    detailsLabel.Text = "Select a case to view details.";
                    SelectedCase = null;
                    openCaseButton.Visible = false;
                    deleteCaseButton.Visible = false;
                }
            };

            caseListBox = new ListBox
            {
                Location = new Point(30, 70),
                Size = new Size(260, 180),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(0, 122, 255),
                BorderStyle = BorderStyle.None
            };
            caseListBox.SelectedIndexChanged += (s, e) => ShowCaseDetails();

            detailsLabel = new Label
            {
                Text = "Select a case to view details.",
                Location = new Point(320, 30),
                Size = new Size(270, 110),
                Font = new Font("Segoe UI", 12F),
                ForeColor = Color.FromArgb(60, 60, 60),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                TextAlign = ContentAlignment.TopLeft
            };

            openCaseButton = new Button
            {
                Text = "Open This Case",
                Location = new Point(340, 150),
                Size = new Size(180, 40),
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Visible = false,
                TabStop = false
            };
            openCaseButton.FlatAppearance.BorderSize = 0;
            openCaseButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 90, 200);
            openCaseButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 90, 200);
            openCaseButton.Cursor = Cursors.Hand;
            openCaseButton.Click += (s, e) => {
                if (SelectedCase != null)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            };

            deleteCaseButton = new Button
            {
                Text = "Delete This Case",
                Location = new Point(340, 200),
                Size = new Size(180, 40),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Visible = false,
                TabStop = false
            };
            deleteCaseButton.FlatAppearance.BorderSize = 0;
            deleteCaseButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(180, 40, 50);
            deleteCaseButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 40, 50);
            deleteCaseButton.Cursor = Cursors.Hand;
            deleteCaseButton.Click += (s, e) => {
                if (SelectedCase != null)
                {
                    using (var confirmForm = new DeleteCaseConfirmForm(SelectedCase.CaseName, SelectedCase.CaseId))
                    {
                        if (confirmForm.ShowDialog(this) == DialogResult.OK && confirmForm.Confirmed)
                        {
                            using (var pwdForm = new PasswordPromptForm())
                            {
                                if (pwdForm.ShowDialog(this) == DialogResult.OK)
                                {
                                    string correctPassword = "forensic123";
                                    if (pwdForm.EnteredPassword == correctPassword)
                                    {
                                        cases.Remove(SelectedCase);
                                        FilterCases();
                                        detailsLabel.Text = "Select a case to view details.";
                                        openCaseButton.Visible = false;
                                        deleteCaseButton.Visible = false;
                                        SelectedCase = null;
                                        new SuccessDialogForm("Case deleted successfully.").ShowDialog(this);
                                    }
                                    else
                                    {
                                        MessageBox.Show("Incorrect password.", "Authentication Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                            }
                        }
                    }
                }
            };

            cardPanel.Controls.Add(searchBox);
            cardPanel.Controls.Add(caseListBox);
            cardPanel.Controls.Add(detailsLabel);
            cardPanel.Controls.Add(openCaseButton);
            cardPanel.Controls.Add(deleteCaseButton);

            this.Controls.Add(headerLabel);
            this.Controls.Add(backButton);
            this.Controls.Add(cardPanel);
            backButton.BringToFront();
            // Ensure cardPanel is positioned below the backButton and headerLabel
            cardPanel.Location = new Point(40, 70); // Move cardPanel down to avoid overlap

            LoadCases();
        }

        private void LoadCases()
        {
            caseListBox.Items.Clear();
            foreach (var c in cases)
            {
                caseListBox.Items.Add($"Case_{c.CaseId} (ID: {c.CaseId})");
            }
        }

        private void FilterCases()
        {
            if (cases == null || searchBox == null || caseListBox == null)
                return;
            var query = searchBox.Text.Trim().ToLower();
            caseListBox.Items.Clear();
            foreach (var c in cases)
            {
                if (query == "search by name or id" ||
                    (c.CaseName != null && c.CaseName.ToLower().Contains(query)) ||
                    (c.CaseId != null && c.CaseId.ToLower().Contains(query)))
                {
                    caseListBox.Items.Add($"Case_{c.CaseId} (ID: {c.CaseId})");
                }
            }
        }

        private void ShowCaseDetails()
        {
            if (caseListBox.SelectedIndex >= 0)
            {
                var c = cases[caseListBox.SelectedIndex];
                detailsLabel.Text =
                    $"Case Name: {c.CaseName}\n" +
                    $"Case ID: {c.CaseId}\n" +
                    $"Investigator: {c.InvestigatorName}\n" +
                    $"Created: {c.CreatedDate}\n" +
                    $"Status: {c.Status}\n" +
                    $"Description: {c.Description}";
                SelectedCase = c;
                openCaseButton.Visible = true;
                deleteCaseButton.Visible = true;
            }
            else
            {
                detailsLabel.Text = "Select a case to view details.";
                SelectedCase = null;
                openCaseButton.Visible = false;
                deleteCaseButton.Visible = false;
            }
        }
    }
}
