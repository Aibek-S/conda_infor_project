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
            string role = "student";

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

            if (password.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать минимум 6 символов.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                registerEnterButton.Enabled = false;
                registerEnterButton.Text = "Регистрация...";

                User user = await _authService.RegisterAsync(email, password, fullName, role);

                if (user != null)
                {
                    MessageBox.Show($"Аккаунт создан: {user.FullName}\n\nТеперь можно войти с этим email и паролем.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);

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
                    errorMessage = "Этот email уже зарегистрирован. Попробуйте войти или используйте другой email.";
                }
                else if (errorMessage.Contains("invalid", StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = "Неверный email или пароль. Проверьте данные и попробуйте снова.";
                }

                MessageBox.Show($"Не удалось зарегистрироваться: {errorMessage}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                registerEnterButton.Enabled = true;
                registerEnterButton.Text = "Зарегистрироваться";
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
