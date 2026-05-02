using conda_infor_project.models;
using conda_infor_project.repository;

namespace conda_infor_project
{
    public class ClassesForm : Form
    {
        private readonly User _currentUser;
        private readonly string _accessToken;
        private readonly ClassRepository _classRepository;
        private readonly FlowLayoutPanel _classesPanel;
        private readonly Label _statusLabel;

        public ClassesForm(User currentUser, string accessToken)
        {
            _currentUser = currentUser;
            _accessToken = accessToken;
            _classRepository = new ClassRepository();

            Text = Ru("Мои классы");
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 560);
            Size = new Size(1050, 640);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(24),
                BackColor = Color.FromArgb(245, 247, 250)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

            root.Controls.Add(CreateHeader(), 0, 0);

            _classesPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                BackColor = Color.White,
                Padding = new Padding(16)
            };
            root.Controls.Add(_classesPanel, 0, 1);

            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = Ru("Загрузка классов..."),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(91, 104, 124),
                Font = new Font("Segoe UI", 10F)
            };
            root.Controls.Add(_statusLabel, 0, 2);

            Controls.Add(root);
            Shown += async (_, _) => await LoadClassesAsync();
        }

        private Control CreateHeader()
        {
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 42));

            var title = new Label
            {
                Dock = DockStyle.Fill,
                Text = Ru("Мои классы"),
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 34, 51),
                TextAlign = ContentAlignment.BottomLeft
            };

            var subtitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = Ru($"Учитель: {_currentUser.FullName} ({_currentUser.Email})"),
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = Color.FromArgb(91, 104, 124),
                TextAlign = ContentAlignment.TopLeft
            };

            var addButton = new Button
            {
                Dock = DockStyle.Fill,
                Text = Ru("+ Создать класс"),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(31, 97, 141),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            addButton.FlatAppearance.BorderSize = 0;
            addButton.Click += async (_, _) => await OpenCreateClassFormAsync();

            header.Controls.Add(title, 0, 0);
            header.Controls.Add(subtitle, 0, 1);
            header.Controls.Add(addButton, 1, 0);
            header.SetRowSpan(addButton, 2);

            return header;
        }

        private async Task LoadClassesAsync()
        {
            try
            {
                _statusLabel.Text = Ru("Загрузка классов...");
                _classesPanel.Controls.Clear();

                List<SchoolClass> classes = await _classRepository.GetTeacherClassesAsync(_currentUser.Id, _accessToken);
                if (classes.Count == 0)
                {
                    ShowEmptyState();
                    _statusLabel.Text = Ru("Классов пока нет.");
                    return;
                }

                foreach (SchoolClass schoolClass in classes)
                {
                    _classesPanel.Controls.Add(CreateClassCard(schoolClass));
                }

                _statusLabel.Text = Ru($"Классов: {classes.Count}");
            }
            catch (Exception ex)
            {
                _statusLabel.Text = Ru("Ошибка загрузки классов.");
                MessageBox.Show(ex.Message, Ru("Ошибка"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Control CreateClassCard(SchoolClass schoolClass)
        {
            var button = new Button
            {
                Width = 260,
                Height = 120,
                Margin = new Padding(8),
                Text = Ru($"{schoolClass.Name}\nСоздан: {schoolClass.CreatedAt:dd.MM.yyyy}"),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(237, 244, 250),
                ForeColor = Color.FromArgb(22, 34, 51),
                FlatStyle = FlatStyle.Flat
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(197, 215, 232);
            button.FlatAppearance.BorderSize = 1;
            button.Click += (_, _) => OpenTeacherDashboard(schoolClass);

            return button;
        }

        private void ShowEmptyState()
        {
            var panel = new TableLayoutPanel
            {
                Width = Math.Max(760, _classesPanel.Width - 48),
                Height = Math.Max(360, _classesPanel.Height - 48),
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.White
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 55));

            var addButton = new Button
            {
                Anchor = AnchorStyles.None,
                Width = 340,
                Height = 72,
                Text = Ru("+ Добавить первый класс"),
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                BackColor = Color.FromArgb(31, 97, 141),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            addButton.FlatAppearance.BorderSize = 0;
            addButton.Click += async (_, _) => await OpenCreateClassFormAsync();

            panel.Controls.Add(new Label(), 0, 0);
            panel.Controls.Add(addButton, 0, 1);
            panel.Controls.Add(new Label(), 0, 2);
            _classesPanel.Controls.Add(panel);
        }

        private async Task OpenCreateClassFormAsync()
        {
            using var form = new CreateClassForm(_accessToken);
            form.ShowDialog(this);

            if (form.WasClassCreated)
            {
                await LoadClassesAsync();
            }
        }

        private void OpenTeacherDashboard(SchoolClass schoolClass)
        {
            var dashboard = new TeacherDashboard(_currentUser, _accessToken, schoolClass);
            dashboard.FormClosed += (_, _) => Show();
            dashboard.Show();
            Hide();
        }

        private static string Ru(string value) => value;
    }
}
