using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QuadralTimeApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _alarmsFile = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QuadralTimeApp",
            "alarms.json");
        // Handler for delete (X) button in alarm list
        private void DeleteAlarmButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Alarm alarm)
            {
                alarm.Active = false;
                RefreshAlarmsList();
                SaveAlarms();
            }
        }

        private readonly System.Windows.Threading.DispatcherTimer _timer;
        private readonly double _clockRadius = 150;
        private readonly double _centerX = 175;
        private readonly double _centerY = 175;
        private readonly string[] _cycleLabels = { "AM", "MM", "DM", "PM", "EM", "NM" };
        private readonly (int startHour, int endHour)[] _cycleRanges =
        {
            (0, 4),   // AM: 12am–4am
            (4, 8),   // MM: 4am–8am
            (8, 12),  // DM: 8am–12pm
            (12, 16), // PM: 12pm–4pm
            (16, 20), // EM: 4pm–8pm
            (20, 24)  // NM: 8pm–12am
        };

        // Alarm data
        private class Alarm
        {
            [JsonInclude]
            public int QuadralHour { get; set; } // 1-4
            [JsonInclude]
            public int QuadralMinute { get; set; } // 0, 15, 30, 45
            [JsonInclude]
            public string QuadralCycle { get; set; } = "AM";
            [JsonInclude]
            public TimeSpan Time { get; set; }
            [JsonInclude]
            public string Recurrence { get; set; } = "Once";
            [JsonInclude]
            public bool Active { get; set; } = true;
            [JsonInclude]
            public DateTime? LastTriggered { get; set; } = null;
            public override string ToString()
            {
                // Show as Quadral Time: 1:00 AM, 2:15 MM, etc
                string min = QuadralMinute.ToString("D2");
                return $"{QuadralHour}:{min} {QuadralCycle} ({Recurrence})";
            }
        }

        private readonly List<Alarm> _alarms = new();
        private bool _alarmRinging = false;
        private DateTime _alarmStartTime;
        private System.Media.SoundPlayer? _alarmPlayer;

        public MainWindow()
        {
            InitializeComponent();
            LoadAlarms();
            _timer = new System.Windows.Threading.DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            DrawClockFace();
            UpdateClock();

            AddAlarmButton.Click += AddAlarmButton_Click;
            StopAlarmButton.Click += StopAlarmButton_Click;
            AlarmsList.MouseDoubleClick += AlarmsList_MouseDoubleClick;
            this.Closing += MainWindow_Closing;
            RefreshAlarmsList();
        }


        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveAlarms();
            // Ensure all background tasks and timers are stopped
            _timer.Stop();
            Application.Current.Shutdown();
        }

        private void SaveAlarms()
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(_alarmsFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                var toSave = _alarms.Where(a => a.Active).ToList();
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(_alarmsFile, JsonSerializer.Serialize(toSave, options));
            }
            catch { /* Ignore errors */ }
        }

        private void LoadAlarms()
        {
            try
            {
                if (File.Exists(_alarmsFile))
                {
                    var json = File.ReadAllText(_alarmsFile);
                    var loaded = JsonSerializer.Deserialize<List<Alarm>>(json);
                    if (loaded != null)
                    {
                        _alarms.Clear();
                        _alarms.AddRange(loaded);
                        RefreshAlarmsList();
                    }
                }
            }
            catch { /* Ignore errors */ }
        }


    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateClock();
        CheckAlarms();
        if (_alarmRinging && (DateTime.Now - _alarmStartTime).TotalMinutes >= 5)
        {
            StopAlarm();
        }
    }
    // Alarm logic
    private void AddAlarmButton_Click(object sender, RoutedEventArgs e)
    {
        // Get hour from ComboBox
        if (!(AlarmHourBox.SelectedItem is ComboBoxItem hourItem) || !int.TryParse(hourItem.Content.ToString(), out int quadralHour) || quadralHour < 1 || quadralHour > 4)
        {
            MessageBox.Show("Select an hour (1-4) for Quadral Time.", "Invalid Hour", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        // Get minute from ComboBox
        if (!(AlarmMinuteBox.SelectedItem is ComboBoxItem minuteItem) || !int.TryParse(minuteItem.Content.ToString(), out int quadralMinute) || (quadralMinute != 0 && quadralMinute != 15 && quadralMinute != 30 && quadralMinute != 45))
        {
            MessageBox.Show("Select a minute (00, 15, 30, 45) for Quadral Time.", "Invalid Minute", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        // Get cycle from ComboBox
        string cycle = (AlarmCycleBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "AM";
        int cycleIndex = Array.IndexOf(_cycleLabels, cycle);
        if (cycleIndex == -1)
        {
            MessageBox.Show("Select a valid Quadral cycle.", "Invalid Cycle", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        // Convert Quadral Time to 24-hour time
        int normalHour = _cycleRanges[cycleIndex].startHour + (quadralHour - 1);
        var time = new TimeSpan(normalHour, quadralMinute, 0);
        string recurrence = (AlarmRecurrenceBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Once";
        var alarm = new Alarm {
            Time = time,
            Recurrence = recurrence,
            QuadralHour = quadralHour,
            QuadralMinute = quadralMinute,
            QuadralCycle = cycle
        };
        _alarms.Add(alarm);
        RefreshAlarmsList();
        SaveAlarms();
        AlarmHourBox.SelectedIndex = -1;
        AlarmMinuteBox.SelectedIndex = -1;
        AlarmCycleBox.SelectedIndex = -1;
    }

    private void RefreshAlarmsList()
    {
        AlarmsList.ItemsSource = null;
        AlarmsList.ItemsSource = _alarms.Where(a => a.Active).ToList();
    }

        private void AlarmsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AlarmsList.SelectedItem is Alarm alarm)
            {
                alarm.Active = false;
                RefreshAlarmsList();
                SaveAlarms();
            }
        }

    private void CheckAlarms()
    {
        if (_alarmRinging) return;
        DateTime now = DateTime.Now;
        foreach (var alarm in _alarms.Where(a => a.Active))
        {
            if (ShouldTriggerAlarm(alarm, now))
            {
                TriggerAlarm(alarm);
                break;
            }
        }
    }

    private bool ShouldTriggerAlarm(Alarm alarm, DateTime now)
    {
        // Only trigger if not already triggered this minute
        if (alarm.LastTriggered != null && alarm.LastTriggered.Value.Date == now.Date && alarm.LastTriggered.Value.Hour == now.Hour && alarm.LastTriggered.Value.Minute == now.Minute)
            return false;
        // Check recurrence
        bool match = false;
        switch (alarm.Recurrence)
        {
            case "Once":
                match = (now.TimeOfDay.Hours() == alarm.Time.Hours() && now.TimeOfDay.Minutes() == alarm.Time.Minutes() && (alarm.LastTriggered == null));
                break;
            case "Daily":
                match = (now.TimeOfDay.Hours() == alarm.Time.Hours() && now.TimeOfDay.Minutes() == alarm.Time.Minutes());
                break;
            case "Weekdays":
                match = (now.TimeOfDay.Hours() == alarm.Time.Hours() && now.TimeOfDay.Minutes() == alarm.Time.Minutes() && now.DayOfWeek >= DayOfWeek.Monday && now.DayOfWeek <= DayOfWeek.Friday);
                break;
            case "Weekends":
                match = (now.TimeOfDay.Hours() == alarm.Time.Hours() && now.TimeOfDay.Minutes() == alarm.Time.Minutes() && (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday));
                break;
        }
        return match;
    }

    private void TriggerAlarm(Alarm alarm)
    {
        alarm.LastTriggered = DateTime.Now;
        if (alarm.Recurrence == "Once")
            alarm.Active = false;
        RefreshAlarmsList();
        _alarmRinging = true;
        _alarmStartTime = DateTime.Now;
        StopAlarmButton.Visibility = Visibility.Visible;
        // Play sound (use built-in system sound as placeholder)
        System.Media.SystemSounds.Exclamation.Play();
        // For a real loud sound, you can use a .wav file:
        // _alarmPlayer = new System.Media.SoundPlayer("Assets/Sounds/alarm.wav");
        // _alarmPlayer.PlayLooping();
    }

    private void StopAlarmButton_Click(object sender, RoutedEventArgs e)
    {
        StopAlarm();
    }

    private void StopAlarm()
    {
        _alarmRinging = false;
        StopAlarmButton.Visibility = Visibility.Collapsed;
        if (_alarmPlayer != null)
        {
            _alarmPlayer.Stop();
            _alarmPlayer.Dispose();
            _alarmPlayer = null;
        }
    }


    private void UpdateClock()
    {
        // Get current time
        DateTime now = DateTime.Now;
        // Calculate 4-hour revolution
        int quadralHour = now.Hour % 4;
        int minute = now.Minute;
        int second = now.Second;
        double totalMinutes = quadralHour * 60 + minute + second / 60.0;
        double angle = (totalMinutes / 240.0) * 360.0; // 240 minutes in 4 hours

        // Redraw hand
        DrawClockHand(angle);

        // Update cycle counter
        int cycleIndex = GetCycleIndex(now.Hour);
        string cycleLabel = _cycleLabels[cycleIndex];
        var (start, end) = _cycleRanges[cycleIndex];
        string range = $"{FormatHour(start)}–{FormatHour(end)}";
        CycleCounter.Text = $"{cycleLabel}  ({range})";

        // Update date display
        DateDisplay.Text = now.ToString("dddd, MMMM d, yyyy");
    }

    private int GetCycleIndex(int hour)
    {
        for (int i = 0; i < _cycleRanges.Length; i++)
        {
            var (start, end) = _cycleRanges[i];
            if (hour >= start && hour < end)
                return i;
        }
        return 0;
    }

    private string FormatHour(int hour)
    {
        if (hour == 0) return "12am";
        if (hour == 12) return "12pm";
        if (hour < 12) return $"{hour}am";
        return $"{hour - 12}pm";
    }

    private void DrawClockFace()
    {
        ClockCanvas.Children.Clear();
        // Draw outer circle
        Ellipse circle = new Ellipse
        {
            Width = _clockRadius * 2,
            Height = _clockRadius * 2,
            Stroke = Brushes.SlateGray,
            StrokeThickness = 4
        };
        Canvas.SetLeft(circle, _centerX - _clockRadius);
        Canvas.SetTop(circle, _centerY - _clockRadius);
        ClockCanvas.Children.Add(circle);

        // Draw hour marks (1-4)
        for (int i = 0; i < 4; i++)
        {
            double angle = (i / 4.0) * 360.0 - 90;
            double x = _centerX + Math.Cos(angle * Math.PI / 180) * (_clockRadius - 30);
            double y = _centerY + Math.Sin(angle * Math.PI / 180) * (_clockRadius - 30);
            TextBlock hourMark = new TextBlock
            {
                Text = (i + 1).ToString(),
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.MidnightBlue
            };
            Canvas.SetLeft(hourMark, x - 12);
            Canvas.SetTop(hourMark, y - 18);
            ClockCanvas.Children.Add(hourMark);
        }

        // Draw 15-minute tick marks
        for (int i = 0; i < 16; i++)
        {
            double angle = (i / 16.0) * 360.0 - 90;
            double x1 = _centerX + Math.Cos(angle * Math.PI / 180) * (_clockRadius - 10);
            double y1 = _centerY + Math.Sin(angle * Math.PI / 180) * (_clockRadius - 10);
            double x2 = _centerX + Math.Cos(angle * Math.PI / 180) * (_clockRadius - 20);
            double y2 = _centerY + Math.Sin(angle * Math.PI / 180) * (_clockRadius - 20);
            Line tick = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = Brushes.Gray,
                StrokeThickness = 2
            };
            ClockCanvas.Children.Add(tick);
        }
    }

    private Line? _clockHand;
    private void DrawClockHand(double angle)
    {
        // Remove previous hand
        if (_clockHand != null)
            ClockCanvas.Children.Remove(_clockHand);

        double rad = (angle - 90) * Math.PI / 180;
        double x = _centerX + Math.Cos(rad) * (_clockRadius - 50);
        double y = _centerY + Math.Sin(rad) * (_clockRadius - 50);
        _clockHand = new Line
        {
            X1 = _centerX,
            Y1 = _centerY,
            X2 = x,
            Y2 = y,
            Stroke = Brushes.Crimson,
            StrokeThickness = 6,
            StrokeEndLineCap = PenLineCap.Round
        };
        ClockCanvas.Children.Add(_clockHand);
    }
}
}