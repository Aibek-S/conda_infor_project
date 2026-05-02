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
        private readonly DataGridView _activityGrid;
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

            _activityGrid = CreateActivityGrid();
            root.Controls.Add(_activityGrid, 0, 1);

            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Загрузка активности...",
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(91, 104, 124),
                Font = new Font("Segoe UI", 10F)
            };
            root.Controls.Add(_statusLabel, 0, 2);

            Controls.Add(root);
            Shown += async (_, _) => await LoadActivityAsync();
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
            refreshButton.Click += async (_, _) => await LoadActivityAsync();

            header.Controls.Add(title, 0, 0);
            header.Controls.Add(subtitle, 0, 1);
            header.Controls.Add(backButton, 1, 0);
            header.SetRowSpan(backButton, 2);
            header.Controls.Add(refreshButton, 2, 0);
            header.SetRowSpan(refreshButton, 2);

            return header;
        }

        private static DataGridView CreateActivityGrid()
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
            grid.Columns.Add("status", "Статус");
            grid.Columns.Add("activeWindow", "Активное окно");
            grid.Columns.Add("processes", "Процессы");
            grid.Columns.Add("lastSeen", "Последний сигнал");

            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
            grid.DefaultCellStyle.Padding = new Padding(6);
            grid.RowTemplate.Height = 36;

            return grid;
        }

        private async Task LoadActivityAsync()
        {
            try
            {
                _statusLabel.Text = "Загрузка активности...";
                _activityGrid.Rows.Clear();

                List<LiveActivityRow> rows = await _classRepository.GetClassLiveActivityAsync(_schoolClass.Id, _accessToken);
                foreach (LiveActivityRow row in rows)
                {
                    string processText = row.ProcessList.Count == 0
                        ? "-"
                        : string.Join(", ", row.ProcessList.Take(8));
                    if (row.ProcessList.Count > 8)
                    {
                        processText += $" +{row.ProcessList.Count - 8}";
                    }

                    _activityGrid.Rows.Add(
                        row.FullName,
                        row.Email,
                        row.Status == "online" ? "Онлайн" : "Офлайн",
                        string.IsNullOrWhiteSpace(row.ActiveWindow) ? "-" : row.ActiveWindow,
                        processText,
                        row.LastSeen?.ToLocalTime().ToString("HH:mm:ss") ?? "-"
                    );
                }

                if (rows.Count == 0)
                {
                    _activityGrid.Rows.Add("В классе пока нет учеников", "-", "-", "-", "-", "-");
                    _statusLabel.Text = "Ученики не найдены.";
                    return;
                }

                int onlineCount = rows.Count(row => row.Status == "online");
                _statusLabel.Text = $"Учеников: {rows.Count}. Онлайн: {onlineCount}. Обновлено: {DateTime.Now:HH:mm:ss}.";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Ошибка загрузки активности.";
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
