using conda_infor_project.models;
using conda_infor_project.services;

namespace conda_infor_project
{
    public partial class register : Form
    {
        private readonly AuthService _authService;

        public register()
        {
            InitializeComponent();
            _authService = new AuthService();
        }

        private async void registerEnterButton_Click(object sender, EventArgs e)
        {
            string email = loginBox.Text?.Trim() ?? string.Empty;
            string password = passwordBox.Text ?? string.Empty;

            string fullName = email.Split('@')[0];
            string role = "user";

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

            if (password.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                registerEnterButton.Enabled = false;
                registerEnterButton.Text = "Registering...";

                User user = await _authService.RegisterAsync(email, password, fullName, role);

                if (user != null)
                {
                    MessageBox.Show($"Registration successful! Welcome {user.FullName}\n\nYou can now login with your email and password.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    login loginForm = new login();
                    loginForm.Show();
                    Hide();
                }
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                if (errorMessage.Contains("already registered", StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = "This email is already registered. Try logging in or use another email.";
                }
                else if (errorMessage.Contains("invalid", StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = "Invalid email or password. Check the data and try again.";
                }

                MessageBox.Show($"Registration failed: {errorMessage}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                registerEnterButton.Enabled = true;
                registerEnterButton.Text = "Register";
            }
        }

        private void loginLinkLabel_Click(object sender, EventArgs e)
        {
            login loginForm = new login();
            loginForm.Show();
            Hide();
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
