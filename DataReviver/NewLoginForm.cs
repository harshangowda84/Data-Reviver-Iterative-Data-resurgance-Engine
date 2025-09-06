using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataReviver
{
    public partial class NewLoginForm : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Button btnExit;
        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblUsername;
        private Label lblPassword;
        private Panel headerPanel;
        private Panel loginPanel;
        private PictureBox logoBox;
        private CheckBox chkShowPassword; // Declare chkShowPassword as a class-level field

        public bool LoginSuccessful { get; private set; } = false;
        public UserSession CurrentUser { get; private set; } = null;

        public NewLoginForm()
        {
            InitializeComponent();
            SetupLoginForm();
            if (MainForm.DRIcon != null)
                this.Icon = MainForm.DRIcon;
        }

        private void SetupLoginForm()
        {
            // Form Properties
            this.Text = "Data Reviver - Digital Evidence Recovery System";
            this.Size = new Size(820, 640);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.BackColor = Color.FromArgb(245, 247, 251);

            // Main TableLayoutPanel for layout
            var mainLayout = new TableLayoutPanel();
            mainLayout.RowCount = 3;
            mainLayout.ColumnCount = 1;
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.BackColor = Color.FromArgb(245, 247, 251);
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Content
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Footer

            // Header
            var headerLabel = new Label();
            headerLabel.Text = "Empowering Digital Investigations. Trusted. Secure. Proven.";
            headerLabel.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            headerLabel.ForeColor = Color.FromArgb(0, 122, 255);
            headerLabel.Dock = DockStyle.Fill;
            headerLabel.TextAlign = ContentAlignment.MiddleCenter;
            mainLayout.Controls.Add(headerLabel, 0, 0);

            // Content TableLayoutPanel
            var contentLayout = new TableLayoutPanel();
            contentLayout.RowCount = 1;
            contentLayout.ColumnCount = 2;
            contentLayout.Dock = DockStyle.Fill;
            contentLayout.BackColor = Color.Transparent;
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // Branding Panel
            var brandingPanel = new Panel();
            brandingPanel.Dock = DockStyle.Fill;
            brandingPanel.BackColor = Color.FromArgb(230, 240, 255);
            brandingPanel.Padding = new Padding(0);
            brandingPanel.Margin = new Padding(40, 20, 20, 20);
            var brandingIcon = new Label();
            brandingIcon.Text = "🔒";
            brandingIcon.Font = new Font("Segoe UI", 70F, FontStyle.Bold);
            brandingIcon.ForeColor = Color.FromArgb(0, 122, 255);
            brandingIcon.Dock = DockStyle.Top;
            brandingIcon.TextAlign = ContentAlignment.MiddleCenter;
            brandingIcon.Height = 120;
            var brandingTitle = new Label();
            brandingTitle.Text = "DATA REVIVER";
            brandingTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            brandingTitle.ForeColor = Color.FromArgb(0, 122, 255);
            brandingTitle.Dock = DockStyle.Top;
            brandingTitle.TextAlign = ContentAlignment.MiddleCenter;
            brandingTitle.Height = 50;
            var brandingSubtitle = new Label();
            brandingSubtitle.Text = "Iterative Data Resurgence Engine";
            brandingSubtitle.Font = new Font("Segoe UI", 13F, FontStyle.Regular);
            brandingSubtitle.ForeColor = Color.FromArgb(0, 122, 255);
            brandingSubtitle.Dock = DockStyle.Top;
            brandingSubtitle.TextAlign = ContentAlignment.MiddleCenter;
            brandingSubtitle.Height = 40;
            brandingPanel.Controls.Add(brandingSubtitle);
            brandingPanel.Controls.Add(brandingTitle);
            brandingPanel.Controls.Add(brandingIcon);
            contentLayout.Controls.Add(brandingPanel, 0, 0);

            // Login Panel
            loginPanel = new Panel();
            loginPanel.Dock = DockStyle.Fill;
            loginPanel.BackColor = Color.White;
            loginPanel.Padding = new Padding(0);
            loginPanel.Margin = new Padding(20, 20, 40, 20);
            var loginLayout = new TableLayoutPanel();
            loginLayout.RowCount = 8;
            loginLayout.ColumnCount = 2;
            loginLayout.Dock = DockStyle.Fill;
            loginLayout.BackColor = Color.Transparent;
            loginLayout.Padding = new Padding(30, 30, 30, 30);
            loginLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            loginLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            // Add extra space above icon
            loginLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Spacer
            loginLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); // Icon
            loginLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Username
            loginLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Password
            loginLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Show password
            loginLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Remember me
            loginLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Buttons
            loginLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Filler
            // Autofill button
            btnAutofill = new Button();
            btnAutofill.Text = "Autofill";
            btnAutofill.Font = new Font("Segoe UI", 11F, FontStyle.Regular);
            btnAutofill.Dock = DockStyle.Left;
            btnAutofill.AutoSize = true;
            btnAutofill.BackColor = Color.FromArgb(230, 230, 230);
            btnAutofill.ForeColor = Color.Black;
            btnAutofill.FlatStyle = FlatStyle.Flat;
            btnAutofill.FlatAppearance.BorderSize = 0;
            btnAutofill.Click += (s, e) => {
                txtUsername.Text = "admin";
                txtPassword.Text = "forensic123";
                BtnLogin_Click(btnAutofill, EventArgs.Empty);
            };
            loginLayout.Controls.Add(new Label(), 0, 5); // Empty cell for alignment
            loginLayout.Controls.Add(btnAutofill, 1, 5);

            // Spacer row
            loginLayout.Controls.Add(new Label(), 0, 0);
            loginLayout.SetColumnSpan(loginLayout.GetControlFromPosition(0,0), 2);

            var loginIcon = new Label();
            loginIcon.Text = "🔒";
            loginIcon.Font = new Font("Segoe UI", 40F, FontStyle.Bold);
            loginIcon.ForeColor = Color.FromArgb(0, 122, 255);
            loginIcon.Dock = DockStyle.Fill;
            loginIcon.TextAlign = ContentAlignment.MiddleCenter;
            loginLayout.Controls.Add(loginIcon, 0, 1);
            loginLayout.SetColumnSpan(loginIcon, 2);

            lblUsername = new Label();
            lblUsername.Text = "Username:";
            lblUsername.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblUsername.ForeColor = Color.Black;
            lblUsername.Dock = DockStyle.Fill;
            lblUsername.TextAlign = ContentAlignment.MiddleRight;
            txtUsername = new TextBox();
            txtUsername.Font = new Font("Segoe UI", 13F);
            txtUsername.Dock = DockStyle.Fill;
            txtUsername.BorderStyle = BorderStyle.FixedSingle;
            // Move focus to password when Enter is pressed in username textbox
            txtUsername.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    txtPassword.Focus();
                    e.SuppressKeyPress = true;
                }
            };
            loginLayout.Controls.Add(lblUsername, 0, 2);
            loginLayout.Controls.Add(txtUsername, 1, 2);

            lblPassword = new Label();
            lblPassword.Text = "Password:";
            lblPassword.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblPassword.ForeColor = Color.Black;
            lblPassword.Dock = DockStyle.Fill;
            lblPassword.TextAlign = ContentAlignment.MiddleRight;
            txtPassword = new TextBox();
            txtPassword.Font = new Font("Segoe UI", 13F);
            txtPassword.Dock = DockStyle.Fill;
            txtPassword.BorderStyle = BorderStyle.FixedSingle;
            txtPassword.PasswordChar = '●';
            txtPassword.UseSystemPasswordChar = true;
            // Pressing Enter in password box triggers login
            txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    BtnLogin_Click(btnLogin, EventArgs.Empty);
                    e.SuppressKeyPress = true;
                }
            };
            // Attach KeyDown event to txtUsername after txtPassword is created
            txtUsername.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    txtPassword.Focus();
                    e.SuppressKeyPress = true;
                }
            };
            loginLayout.Controls.Add(lblPassword, 0, 3);
            loginLayout.Controls.Add(txtPassword, 1, 3);

            chkShowPassword = new CheckBox();
            chkShowPassword.Text = "Show password";
            chkShowPassword.Font = new Font("Segoe UI", 11F, FontStyle.Regular);
            chkShowPassword.Dock = DockStyle.Left;
            chkShowPassword.AutoSize = true;
            chkShowPassword.CheckedChanged += (s, e) =>
            {
                if (chkShowPassword.Checked)
                {
                    txtPassword.UseSystemPasswordChar = false;
                    txtPassword.PasswordChar = '\0';
                }
                else
                {
                    txtPassword.UseSystemPasswordChar = true;
                    txtPassword.PasswordChar = '●';
                }
            };
            loginLayout.Controls.Add(new Label(), 0, 4); // Empty cell for alignment
            loginLayout.Controls.Add(chkShowPassword, 1, 4);

            btnLogin = new Button();
            btnLogin.Text = "LOGIN";
            btnLogin.Height = 40;
            btnLogin.Dock = DockStyle.Fill;
            btnLogin.BackColor = Color.FromArgb(0, 122, 255);
            btnLogin.ForeColor = Color.White;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            btnLogin.Cursor = Cursors.Hand;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 90, 200);
            btnLogin.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 70, 150);
            btnLogin.Click += BtnLogin_Click;
            btnExit = new Button();
            btnExit.Text = "EXIT";
            btnExit.Height = 40;
            btnExit.Dock = DockStyle.Fill;
            btnExit.BackColor = Color.FromArgb(220, 53, 69);
            btnExit.ForeColor = Color.White;
            btnExit.FlatStyle = FlatStyle.Flat;
            btnExit.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            btnExit.Cursor = Cursors.Hand;
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 40, 50);
            btnExit.FlatAppearance.MouseDownBackColor = Color.FromArgb(150, 30, 40);
            btnExit.Click += BtnExit_Click;
            loginLayout.Controls.Add(btnLogin, 0, 6);
            loginLayout.Controls.Add(btnExit, 1, 6);
            loginPanel.Controls.Add(loginLayout);
            contentLayout.Controls.Add(loginPanel, 1, 0);
            mainLayout.Controls.Add(contentLayout, 0, 1);
            // Footer
            var footerLabel = new Label();
            footerLabel.Text = "© 2025 Data Reviver. All rights reserved.";
            footerLabel.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            footerLabel.ForeColor = Color.FromArgb(100, 140, 180);
            footerLabel.Dock = DockStyle.Fill;
            footerLabel.TextAlign = ContentAlignment.MiddleCenter;
            mainLayout.Controls.Add(footerLabel, 0, 2);
            this.Controls.Clear();
            this.Controls.Add(mainLayout);
            // Always focus on username after loading settings
            this.Shown += (s, e) => txtUsername.Focus();
            // Remove AcceptButton so Enter in username only moves focus to password
        }

        private void LoginForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                BtnLogin_Click(sender, e);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                BtnExit_Click(sender, e);
            }
        }

    private Button btnAutofill;

    private void BtnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;
            // Save login details for autofill
            try {
                Properties.Settings.Default["SavedUsername"] = username;
                Properties.Settings.Default["SavedPassword"] = password;
                Properties.Settings.Default.Save();
            } catch {}

            // Simple authentication (you can enhance this)
            var userInfo = ValidateCredentials(username, password);
            if (userInfo != null)
            {
                LoginSuccessful = true;
                CurrentUser = userInfo;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid credentials!\n\nValid login:\n• admin / forensic123 (Administrator)",
                              "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Clear();
                txtUsername.Focus();
            }
        }

        private UserSession ValidateCredentials(string username, string password)
        {
            // Only allow admin login
            var validCredentials = new[]
            {
                new { Username = "admin", Password = "forensic123", Role = UserRole.Admin, FullName = "System Administrator" }
            };

            foreach (var cred in validCredentials)
            {
                if (string.Equals(cred.Username, username, StringComparison.OrdinalIgnoreCase) &&
                    cred.Password == password)
                {
                    return new UserSession(cred.Username, cred.Role, cred.FullName);
                }
            }
            return null;
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(450, 350);
            this.Name = "NewLoginForm";
            this.Text = "Data Reviver - Login";
            this.ResumeLayout(false);
        }
    }
}
