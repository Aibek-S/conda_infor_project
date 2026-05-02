using conda_infor_project.services;
using conda_infor_project.models;

namespace conda_infor_project
{
    public partial class login : Form
    {
        private readonly AuthService _authService;

        public login()
        {
            InitializeComponent();
            _authService = new AuthService();
        }

        private async void loginEnterButton_Click(object sender, EventArgs e)
        {
            string email = loginBox.Text?.Trim() ?? string.Empty;
            string password = passwordBox.Text ?? string.Empty;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter email and password", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!IsValidEmail(email))
            {
                MessageBox.Show("Please enter a valid email address (e.g., user@example.com)", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                loginEnterButton.Enabled = false;
                loginEnterButton.Text = "Logging in...";

                User user = await _authService.LoginAsync(email, password);

                if (user != null)
                {
                    MessageBox.Show($"Login successful! Welcome {user.FullName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    if (!string.Equals(user.Role, "teacher", StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show("This account is not a teacher account. Teacher dashboard is available only for role: teacher.", "Access denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    TeacherDashboard dashboard = new TeacherDashboard(user);
                    dashboard.FormClosed += (_, _) => Close();
                    dashboard.Show();
                    Hide();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Login failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                loginEnterButton.Enabled = true;
                loginEnterButton.Text = "Login";
            }
        }

        private void registerLinkLabel_Click(object sender, EventArgs e)
        {
            register registerForm = new register();
            registerForm.Show();
            this.Hide();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}

