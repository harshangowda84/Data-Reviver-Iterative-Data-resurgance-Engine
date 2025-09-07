using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DataReviver
{
    public class SelectFileForm : Form
    {
        private ListBox fileListBox;
        private Label headerLabel;
        private Button selectButton;
        private Button cancelButton;
    private string _caseFolderPath;
    private string _recoveredFolderPath;
    private string _currentFolderPath;
    public string SelectedFilePath { get; private set; }

        public SelectFileForm(string caseFolderPath)
        {
            _caseFolderPath = caseFolderPath;
            _recoveredFolderPath = Path.Combine(caseFolderPath, "recovered");
            if (Directory.Exists(_recoveredFolderPath))
                _currentFolderPath = _recoveredFolderPath;
            else
                _currentFolderPath = caseFolderPath;
            InitializeComponent();
            LoadFiles();
        }

    private void InitializeComponent()
        {
            this.Text = "Select File in Current Case";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 251);

            headerLabel = new Label
            {
                Text = "Select a file from the current case folder:",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 255),
                Location = new Point(20, 20),
                Size = new Size(540, 32),
                BackColor = Color.Transparent
            };

            fileListBox = new ListBox
            {
                Location = new Point(20, 60),
                Size = new Size(540, 220),
                Font = new Font("Segoe UI", 11F),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(0, 122, 255),
                BorderStyle = BorderStyle.FixedSingle
            };
            fileListBox.SelectedIndexChanged += (s, e) =>
            {
                if (fileListBox.SelectedIndex < 0) { selectButton.Enabled = false; return; }
                var selected = fileListBox.SelectedItem.ToString();
                // Enable select only if it's a file
                var fullPath = Path.Combine(_currentFolderPath, selected);
                selectButton.Enabled = File.Exists(fullPath);
            };
            fileListBox.DoubleClick += (s, e) =>
            {
                if (fileListBox.SelectedIndex < 0) return;
                var selected = fileListBox.SelectedItem.ToString();
                if (selected == "..")
                {
                    GoUpDirectory();
                }
                else
                {
                    var fullPath = Path.Combine(_currentFolderPath, selected);
                    if (Directory.Exists(fullPath))
                    {
                        _currentFolderPath = fullPath;
                        LoadFiles();
                    }
                    else if (File.Exists(fullPath))
                    {
                        SelectedFilePath = fullPath;
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
            };

            selectButton = new Button
            {
                Text = "Select",
                Location = new Point(320, 300),
                Size = new Size(100, 36),
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            selectButton.FlatAppearance.BorderSize = 0;
            selectButton.Click += (s, e) => {
                if (fileListBox.SelectedIndex >= 0)
                {
                    var selected = fileListBox.SelectedItem.ToString();
                    var fullPath = Path.Combine(_currentFolderPath, selected);
                    if (File.Exists(fullPath))
                    {
                        SelectedFilePath = fullPath;
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(440, 300),
                Size = new Size(100, 36),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            cancelButton.FlatAppearance.BorderSize = 0;
            cancelButton.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.Add(headerLabel);
            this.Controls.Add(fileListBox);
            this.Controls.Add(selectButton);
            this.Controls.Add(cancelButton);
        }

        private void LoadFiles()
        {
            fileListBox.Items.Clear();
            // Only allow navigation within the 'recovered' folder if it exists
            if (!Directory.Exists(_recoveredFolderPath))
            {
                fileListBox.Items.Add("No 'recovered' folder found in this case.");
                fileListBox.Enabled = false;
                selectButton.Enabled = false;
                return;
            }
            fileListBox.Enabled = true;
            selectButton.Enabled = false;
            if (!string.IsNullOrEmpty(_currentFolderPath) && Directory.Exists(_currentFolderPath))
            {
                // Show .. if not at root of 'recovered'
                if (!string.Equals(_currentFolderPath.TrimEnd(Path.DirectorySeparatorChar), _recoveredFolderPath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                {
                    fileListBox.Items.Add("..");
                }
                // Add folders first
                var dirs = Directory.GetDirectories(_currentFolderPath);
                foreach (var dir in dirs)
                {
                    fileListBox.Items.Add(Path.GetFileName(dir));
                }
                // Then files
                var files = Directory.GetFiles(_currentFolderPath);
                foreach (var file in files)
                {
                    fileListBox.Items.Add(Path.GetFileName(file));
                }
            }
        }

        private void GoUpDirectory()
        {
            if (string.Equals(_currentFolderPath.TrimEnd(Path.DirectorySeparatorChar), _recoveredFolderPath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                return;
            var parent = Directory.GetParent(_currentFolderPath);
            if (parent != null && parent.FullName.Length >= _recoveredFolderPath.Length)
            {
                _currentFolderPath = parent.FullName;
                LoadFiles();
            }
        }
    }
}
