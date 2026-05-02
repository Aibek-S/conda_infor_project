using conda_infor_project.models;

namespace conda_infor_project
{
    public class TeacherDashboard : Form
    {
        private readonly User _currentUser;
        private readonly DataGridView _activityGrid;
        private readonly Label _statusLabel;

        public TeacherDashboard(User currentUser)
        {
            _currentUser = currentUser;

            Text = "Teacher Dashboard";
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
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

            root.Controls.Add(CreateHeader(), 0, 0);

            _activityGrid = CreateActivityGrid();
            root.Controls.Add(_activityGrid, 0, 1);

            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "No live activity data loaded yet.",
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(91, 104, 124),
                Font = new Font("Segoe UI", 10F)
            };
            root.Controls.Add(_statusLabel, 0, 2);

            Controls.Add(root);
            LoadSampleRows();
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
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 45));

            var title = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Teacher Dashboard",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 34, 51),
                TextAlign = ContentAlignment.BottomLeft
            };

            var subtitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = $"Signed in as {_currentUser.FullName} ({_currentUser.Email})",
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = Color.FromArgb(91, 104, 124),
                TextAlign = ContentAlignment.TopLeft
            };

            var refreshButton = new Button
            {
                Dock = DockStyle.Fill,
                Text = "Refresh",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(31, 97, 141),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            refreshButton.FlatAppearance.BorderSize = 0;
            refreshButton.Click += (_, _) => RefreshDashboard();

            header.Controls.Add(title, 0, 0);
            header.Controls.Add(subtitle, 0, 1);
            header.Controls.Add(refreshButton, 1, 0);
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

            grid.Columns.Add("student", "Student");
            grid.Columns.Add("activeWindow", "Active window");
            grid.Columns.Add("processes", "Processes");
            grid.Columns.Add("status", "Status");
            grid.Columns.Add("updatedAt", "Updated");

            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
            grid.DefaultCellStyle.Padding = new Padding(6);
            grid.RowTemplate.Height = 36;

            return grid;
        }

        private void LoadSampleRows()
        {
            _activityGrid.Rows.Clear();
            _activityGrid.Rows.Add("No students online", "-", "-", "Waiting", DateTime.Now.ToString("HH:mm:ss"));
        }

        private void RefreshDashboard()
        {
            _statusLabel.Text = $"Last refresh: {DateTime.Now:HH:mm:ss}. Live Supabase activity loading is not connected yet.";
        }
    }
}
