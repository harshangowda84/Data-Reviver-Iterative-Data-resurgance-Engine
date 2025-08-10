using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataReviver
{
    public partial class LoginForm : Form
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

        public bool LoginSuccessful { get; private set; } = false;
        public UserSession CurrentUser { get; private set; } = null;

        public LoginForm()
        {
            InitializeComponent();
            SetupLoginForm();
        }

        private void SetupLoginForm()
        {
            // Form Properties
            this.Text = "Data Reviver - Forensic Login";
            this.Size = new Size(450, 350);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(240, 242, 245);
            
            // Header Panel
            headerPanel = new Panel
            {
                Size = new Size(450, 80),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(0, 122, 255)
            };

            logoBox = new PictureBox
            {
                Size = new Size(40, 40),
                Location = new Point(20, 20),
                BackColor = Color.Transparent
            };

            lblTitle = new Label
            {
                Text = "🔐 FORENSIC ACCESS",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(80, 15),
                AutoSize = true
            };

            lblSubtitle = new Label
            {
                Text = "Digital Evidence Recovery System",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 220, 220),
                Location = new Point(80, 45),
                AutoSize = true
            };

            headerPanel.Controls.AddRange(new Control[] { logoBox, lblTitle, lblSubtitle });

            // Login Panel
            loginPanel = new Panel
            {
                Size = new Size(350, 200),
                Location = new Point(50, 100),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Username
            lblUsername = new Label
            {
                Text = "Username:",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Location = new Point(30, 30),
                Size = new Size(80, 20)
            };

            txtUsername = new TextBox
            {
                Font = new Font("Segoe UI", 10F),
                Location = new Point(30, 55),
                Size = new Size(290, 25),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Password
            lblPassword = new Label
            {
                Text = "Password:",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Location = new Point(30, 90),
                Size = new Size(80, 20)
            };

            txtPassword = new TextBox
            {
                Font = new Font("Segoe UI", 10F),
                Location = new Point(30, 115),
                Size = new Size(290, 25),
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = '●',
                UseSystemPasswordChar = true
            };

            // Buttons
            btnLogin = new Button
            {
                Text = "LOGIN",
                Size = new Size(100, 35),
                Location = new Point(140, 155),
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            btnExit = new Button
            {
                Text = "EXIT",
                Size = new Size(80, 35),
                Location = new Point(250, 155),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.Click += BtnExit_Click;

            loginPanel.Controls.AddRange(new Control[] { 
                lblUsername, txtUsername, lblPassword, txtPassword, btnLogin, btnExit 
            });

            // Add controls to form
            this.Controls.AddRange(new Control[] { headerPanel, loginPanel });

            // Set default credentials info
            var infoLabel = new Label
            {
                Text = "Default: admin / forensic123 (Administrator)",
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.Gray,
                Location = new Point(50, 310),
                AutoSize = true
            };
            this.Controls.Add(infoLabel);

            // Focus on username
            txtUsername.Focus();

            // Enable Enter key for login
            this.AcceptButton = btnLogin;
            this.KeyPreview = true;
            this.KeyDown += LoginForm_KeyDown;
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

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

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
                MessageBox.Show("Invalid credentials!\n\nValid logins:\n• admin / forensic123 (Administrator)\n• investigator / evidence456 (Lead Investigator)\n• analyst / data789 (Forensic Analyst)\n• harshan / mca2024 (Your Account)", 
                              "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Clear();
                txtUsername.Focus();
            }
        }

        private UserSession ValidateCredentials(string username, string password)
        {
            // You can enhance this with database authentication, AD integration, etc.
            var validCredentials = new[]
            {
                new { Username = "admin", Password = "forensic123", Role = UserRole.Admin, FullName = "System Administrator" },
                new { Username = "investigator", Password = "evidence456", Role = UserRole.Investigator, FullName = "Lead Digital Investigator" },
                new { Username = "analyst", Password = "data789", Role = UserRole.Analyst, FullName = "Forensic Data Analyst" },
                new { Username = "harshan", Password = "mca2024", Role = UserRole.Admin, FullName = "Harshan Gowda B B" }  // Your personal login
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
            this.Name = "LoginForm";
            this.Text = "Data Reviver - Login";
            this.ResumeLayout(false);
        }
    }
}
