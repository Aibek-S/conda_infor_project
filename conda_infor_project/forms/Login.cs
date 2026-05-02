using conda_infor_project.models;
using conda_infor_project.services;

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
                MessageBox.Show("Введите email и пароль.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!IsValidEmail(email))
            {
                MessageBox.Show("Введите корректный email, например user@example.com.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                loginEnterButton.Enabled = false;
                loginEnterButton.Text = "Вход...";

                User user = await _authService.LoginAsync(email, password);

                if (string.IsNullOrWhiteSpace(_authService.CurrentAccessToken))
                {
                    MessageBox.Show("Вход выполнен, но токен сессии не найден. Попробуйте войти еще раз.", "Ошибка сессии", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Form nextForm = string.Equals(user.Role, "teacher", StringComparison.OrdinalIgnoreCase)
                    ? new ClassesForm(user, _authService.CurrentAccessToken)
                    : new StudentAgentForm(user, _authService.CurrentAccessToken);

                nextForm.FormClosed += (_, _) =>
                {
                    if (!Visible)
                    {
                        Close();
                    }
                };
                nextForm.Show();
                Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось войти: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                loginEnterButton.Enabled = true;
                loginEnterButton.Text = "Войти";
            }
        }

        private void registerLinkLabel_Click(object sender, EventArgs e)
        {
            register registerForm = new register();
            registerForm.Show();
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
