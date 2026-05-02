using conda_infor_project.models;
using conda_infor_project.repository;

namespace conda_infor_project
{
    public class TeacherDashboard : Form
    {
        private readonly User _currentUser;
        private readonly string _accessToken;
        private readonly SchoolClass _schoolClass;
        private readonly ClassRepository _classRepository;
        private readonly DataGridView _studentsGrid;
        private readonly Label _statusLabel;

        public TeacherDashboard(User currentUser, string accessToken, SchoolClass schoolClass)
        {
            _currentUser = currentUser;
            _accessToken = accessToken;
            _schoolClass = schoolClass;
            _classRepository = new ClassRepository();

            Text = $"Класс {_schoolClass.Name}";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(960, 600);
            Size = new Size(1100, 680);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(24),
                BackColor = Color.FromArgb(245, 247, 250)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

            root.Controls.Add(CreateHeader(), 0, 0);

            _studentsGrid = CreateStudentsGrid();
            root.Controls.Add(_studentsGrid, 0, 1);

            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Загрузка учеников...",
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(91, 104, 124),
                Font = new Font("Segoe UI", 10F)
            };
            root.Controls.Add(_statusLabel, 0, 2);

            Controls.Add(root);
            Shown += async (_, _) => await LoadStudentsAsync();
        }

        private Control CreateHeader()
        {
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 42));

            var title = new Label
            {
                Dock = DockStyle.Fill,
                Text = $"Класс {_schoolClass.Name}",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 34, 51),
                TextAlign = ContentAlignment.BottomLeft
            };

            var subtitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = $"Учитель: {_currentUser.FullName} ({_currentUser.Email})",
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = Color.FromArgb(91, 104, 124),
                TextAlign = ContentAlignment.TopLeft
            };

            var backButton = new Button
            {
                Dock = DockStyle.Fill,
                Text = "Назад",
                Font = new Font("Segoe UI", 10F),
                FlatStyle = FlatStyle.Flat
            };
            backButton.Click += (_, _) => Close();

            var refreshButton = new Button
            {
                Dock = DockStyle.Fill,
                Text = "Обновить",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(31, 97, 141),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            refreshButton.FlatAppearance.BorderSize = 0;
            refreshButton.Click += async (_, _) => await LoadStudentsAsync();

            header.Controls.Add(title, 0, 0);
            header.Controls.Add(subtitle, 0, 1);
            header.Controls.Add(backButton, 1, 0);
            header.SetRowSpan(backButton, 2);
            header.Controls.Add(refreshButton, 2, 0);
            header.SetRowSpan(refreshButton, 2);

            return header;
        }

        private static DataGridView CreateStudentsGrid()
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
                BorderStyle = BorderStyle.FixedSingle
            };

            grid.Columns.Add("fullName", "ФИО");
            grid.Columns.Add("email", "Логин");
            grid.Columns.Add("role", "Роль");
            grid.Columns.Add("status", "Статус");

            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
            grid.DefaultCellStyle.Padding = new Padding(6);
            grid.RowTemplate.Height = 36;

            return grid;
        }

        private async Task LoadStudentsAsync()
        {
            try
            {
                _statusLabel.Text = "Загрузка учеников...";
                _studentsGrid.Rows.Clear();

                List<User> students = await _classRepository.GetClassStudentsAsync(_schoolClass.Id, _accessToken);
                foreach (User student in students)
                {
                    _studentsGrid.Rows.Add(student.FullName, student.Email, student.Role, "Ожидает данные активности");
                }

                if (students.Count == 0)
                {
                    _studentsGrid.Rows.Add("В классе пока нет учеников", "-", "-", "-");
                    _statusLabel.Text = "Ученики не найдены.";
                    return;
                }

                _statusLabel.Text = $"Учеников: {students.Count}. Мониторинг активности подключим следующим шагом.";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Ошибка загрузки учеников.";
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
