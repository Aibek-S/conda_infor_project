using conda_infor_project.models;
using conda_infor_project.services;

namespace conda_infor_project
{
    public class StudentAgentForm : Form
    {
        private readonly User _currentUser;
        private readonly string _accessToken;
        private readonly ProcessMonitorService _monitorService;
        private readonly ActivityService _activityService;
        private readonly System.Windows.Forms.Timer _timer;
        private readonly Label _statusLabel;
        private readonly Label _activeWindowLabel;
        private readonly Label _processCountLabel;
        private readonly ListBox _logList;
        private readonly Button _startButton;
        private readonly Button _stopButton;
        private bool _isSending;

        public StudentAgentForm(User currentUser, string accessToken)
        {
            _currentUser = currentUser;
            _accessToken = accessToken;
            _monitorService = new ProcessMonitorService();
            _activityService = new ActivityService();

            Text = "Conda Student";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(760, 520);
            Size = new Size(860, 580);

            _timer = new System.Windows.Forms.Timer
            {
                Interval = 5000
            };
            _timer.Tick += async (_, _) => await CaptureAndSendAsync();

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(24),
                BackColor = Color.FromArgb(245, 247, 250)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

            root.Controls.Add(CreateHeader(), 0, 0);

            _activeWindowLabel = CreateInfoLabel("Активное окно: -");
            root.Controls.Add(_activeWindowLabel, 0, 1);

            _processCountLabel = CreateInfoLabel("Процессов найдено: -");
            root.Controls.Add(_processCountLabel, 0, 2);

            _logList = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10F),
                BackColor = Color.White
            };
            root.Controls.Add(_logList, 0, 3);

            var footer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));

            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Мониторинг остановлен.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(91, 104, 124),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _startButton = new Button
            {
                Dock = DockStyle.Fill,
                Text = "Старт",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(31, 97, 141),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _startButton.FlatAppearance.BorderSize = 0;
            _startButton.Click += async (_, _) => await StartMonitoringAsync();

            _stopButton = new Button
            {
                Dock = DockStyle.Fill,
                Text = "Стоп",
                Font = new Font("Segoe UI", 10F),
                Enabled = false,
                FlatStyle = FlatStyle.Flat
            };
            _stopButton.Click += (_, _) => StopMonitoring();

            footer.Controls.Add(_statusLabel, 0, 0);
            footer.Controls.Add(_startButton, 1, 0);
            footer.Controls.Add(_stopButton, 2, 0);
            root.Controls.Add(footer, 0, 4);

            Controls.Add(root);
            Shown += async (_, _) => await StartMonitoringAsync();
            FormClosing += (_, _) => _timer.Stop();
        }

        private Control CreateHeader()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));

            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Мониторинг ученика",
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 34, 51),
                TextAlign = ContentAlignment.BottomLeft
            }, 0, 0);

            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = $"Аккаунт: {_currentUser.FullName} ({_currentUser.Email})",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(91, 104, 124),
                TextAlign = ContentAlignment.TopLeft
            }, 0, 1);

            return panel;
        }

        private static Label CreateInfoLabel(string text)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Text = text,
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(22, 34, 51),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private async Task StartMonitoringAsync()
        {
            _timer.Start();
            _startButton.Enabled = false;
            _stopButton.Enabled = true;
            _statusLabel.Text = "Мониторинг запущен. Отправка каждые 5 секунд.";
            AddLog("Мониторинг запущен.");
            await CaptureAndSendAsync();
        }

        private void StopMonitoring()
        {
            _timer.Stop();
            _startButton.Enabled = true;
            _stopButton.Enabled = false;
            _statusLabel.Text = "Мониторинг остановлен.";
            AddLog("Мониторинг остановлен.");
        }

        private async Task CaptureAndSendAsync()
        {
            if (_isSending)
            {
                return;
            }

            try
            {
                _isSending = true;
                ActivitySnapshot snapshot = await _monitorService.CaptureSnapshotAsync();
                _activeWindowLabel.Text = $"Активное окно: {FormatValue(snapshot.ActiveWindow)}";
                _processCountLabel.Text = $"Процессов найдено: {snapshot.Processes.Count}";

                await _activityService.SubmitActivityAsync(snapshot, _accessToken);
                AddDebugLog(snapshot);
                _statusLabel.Text = $"Последняя отправка: {DateTime.Now:HH:mm:ss}";
                AddLog($"Отправлено: {snapshot.Processes.Count} процессов, окно: {FormatValue(snapshot.ActiveWindow)}");
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Ошибка отправки активности.";
                AddLog($"Ошибка: {ex.Message}");
            }
            finally
            {
                _isSending = false;
            }
        }

        private void AddLog(string message)
        {
            _logList.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
            while (_logList.Items.Count > 100)
            {
                _logList.Items.RemoveAt(_logList.Items.Count - 1);
            }
        }

        private void AddDebugLog(ActivitySnapshot snapshot)
        {
            string source = string.IsNullOrWhiteSpace(snapshot.DebugSource) ? "unknown" : snapshot.DebugSource;
            string mode = snapshot.IsFallback ? "fallback" : "live";
            AddLog($"debug: source={source}, mode={mode}, script={FormatValue(snapshot.ScriptPath)}");

            if (!string.IsNullOrWhiteSpace(snapshot.DebugMessage))
            {
                AddLog($"debug: {snapshot.DebugMessage}");
            }
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }
    }
}
