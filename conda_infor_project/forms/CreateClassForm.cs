using conda_infor_project.models;
using conda_infor_project.repository;

namespace conda_infor_project
{
    public class CreateClassForm : Form
    {
        private readonly string _accessToken;
        private readonly ClassRepository _classRepository;
        private readonly TextBox _classNameBox;
        private readonly TextBox _passwordBox;
        private readonly TextBox _studentsBox;
        private readonly Button _createButton;
        private readonly DataGridView _resultGrid;
        private readonly Label _statusLabel;

        public bool WasClassCreated { get; private set; }

        public CreateClassForm(string accessToken)
        {
            _accessToken = accessToken;
            _classRepository = new ClassRepository();

            Text = "Создание класса";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(860, 640);
            Size = new Size(960, 720);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(24),
                BackColor = Color.FromArgb(245, 247, 250)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

            root.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Создать класс",
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 34, 51),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            _classNameBox = CreateTextBox();
            root.Controls.Add(CreateLabeledControl("Название класса", _classNameBox), 0, 1);

            _passwordBox = CreateTextBox();
            _passwordBox.UseSystemPasswordChar = false;
            root.Controls.Add(CreateLabeledControl("Общий пароль учеников", _passwordBox), 0, 2);

            _studentsBox = CreateTextBox();
            _studentsBox.Multiline = true;
            _studentsBox.ScrollBars = ScrollBars.Vertical;
            root.Controls.Add(CreateLabeledControl("Ученики, каждый с новой строки", _studentsBox), 0, 3);

            _resultGrid = CreateResultGrid();
            root.Controls.Add(_resultGrid, 0, 4);

            var footer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));

            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Введите данные класса.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(91, 104, 124),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var closeButton = new Button
            {
                Dock = DockStyle.Fill,
                Text = "Закрыть",
                Font = new Font("Segoe UI", 10F)
            };
            StyleSecondaryButton(closeButton);
            closeButton.Click += (_, _) => Close();

            _createButton = new Button
            {
                Dock = DockStyle.Fill,
                Text = "Создать",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            StylePrimaryButton(_createButton);
            _createButton.Click += async (_, _) => await CreateClassAsync();

            footer.Controls.Add(_statusLabel, 0, 0);
            footer.Controls.Add(closeButton, 1, 0);
            footer.Controls.Add(_createButton, 2, 0);
            root.Controls.Add(footer, 0, 5);

            Controls.Add(root);
        }

        private static TextBox CreateTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F),
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private static Control CreateLabeledControl(string labelText, Control control)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = labelText,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 34, 51),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            panel.Controls.Add(control, 0, 1);

            return panel;
        }

        private static DataGridView CreateResultGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(229, 235, 241)
            };

            grid.Columns.Add("fullName", "ФИО");
            grid.Columns.Add("email", "Логин");
            grid.Columns.Add("password", "Пароль");
            grid.Columns.Add("status", "Статус");

            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 8, 8, 8);
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(237, 244, 250);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(22, 34, 51);
            grid.EnableHeadersVisualStyles = false;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
            grid.DefaultCellStyle.Padding = new Padding(10, 8, 10, 8);
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 254);
            grid.RowTemplate.Height = 46;

            grid.Columns["fullName"]!.FillWeight = 150;
            grid.Columns["email"]!.FillWeight = 180;
            grid.Columns["password"]!.FillWeight = 110;
            grid.Columns["status"]!.FillWeight = 170;

            return grid;
        }

        private async Task CreateClassAsync()
        {
            string className = _classNameBox.Text.Trim();
            string password = _passwordBox.Text.Trim();
            List<string> students = _studentsBox.Lines
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (string.IsNullOrWhiteSpace(className))
            {
                MessageBox.Show("Введите название класса.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (password.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать минимум 6 символов.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (students.Count == 0)
            {
                MessageBox.Show("Введите хотя бы одного ученика.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _createButton.Enabled = false;
                _createButton.Text = "Создание...";
                _statusLabel.Text = "Создаем класс и аккаунты учеников...";
                _resultGrid.Rows.Clear();

                CreateClassResponse response = await _classRepository.CreateClassAsync(new CreateClassRequest
                {
                    ClassName = className,
                    StudentPassword = password,
                    Students = students
                }, _accessToken);

                WasClassCreated = true;
                foreach (StudentCredential student in response.CreatedStudents)
                {
                    _resultGrid.Rows.Add(student.FullName, student.Email, student.Password, "Создан");
                }

                foreach (FailedStudent student in response.FailedStudents)
                {
                    _resultGrid.Rows.Add(student.FullName, "-", "-", $"Ошибка: {student.Reason}");
                }

                _statusLabel.Text = $"Класс создан. Аккаунтов: {response.CreatedStudents.Count}, ошибок: {response.FailedStudents.Count}.";
                MessageBox.Show("Класс создан. Логины и пароль показаны в таблице.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Не удалось создать класс.";
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _createButton.Enabled = true;
                _createButton.Text = "Создать";
            }
        }

        private static void StylePrimaryButton(Button button)
        {
            button.BackColor = Color.FromArgb(31, 97, 141);
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Margin = new Padding(8, 14, 0, 14);
            button.Cursor = Cursors.Hand;
        }

        private static void StyleSecondaryButton(Button button)
        {
            button.BackColor = Color.White;
            button.ForeColor = Color.FromArgb(31, 97, 141);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(31, 97, 141);
            button.FlatAppearance.BorderSize = 1;
            button.Margin = new Padding(8, 14, 8, 14);
            button.Cursor = Cursors.Hand;
        }
    }
}
