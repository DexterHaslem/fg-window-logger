using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace ForegroundLogger_Managed
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _started = false;
        private const string DEFAULT_FILENAME = "fglog.csv";
        private HookManager _hookManager;
        private CsvLogWriter _logger;
        private Timer _updateTimer;
        private const int UPDATE_MS = 1000;
        private readonly string DEFAULT_PATH;

        public MainWindow()
        {
            InitializeComponent();
            _hookManager = new HookManager();
            DEFAULT_PATH = txtOutputPath.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), DEFAULT_FILENAME);
            _hookManager.ForegroundWindowChanged += OnForegroundWindowChanged;
            _updateTimer = new Timer(OnTimerTick, null, UPDATE_MS, UPDATE_MS);
            _logger = new CsvLogWriter(txtOutputPath.Text);
            txtOutputPath.TextChanged += OnPathChanged;
            Closing += (o,e) =>
            {
                _logger.Flush();
                _updateTimer.Dispose();
            };
        }

        private void OnTimerTick(object state)
        {
            _logger.Flush();
            UpdateStatusBarText();
        }

        private void OnForegroundWindowChanged(HookManager.ForegroundChangedEventArgs e)
        {
            _logger.QueueEvent(e);
        }

        private void UpdateStatusBarText()
        {
            string logLine = !_started ? "Idle..." : $"Running, logged {_logger.LinesLogged} changes...";
            Dispatcher.Invoke(()=> statusBarTxt.Text = logLine);
        }

        private void OnStartStopClick(object sender, RoutedEventArgs e)
        {
            _started = !_started;
            btnStartStop.Content = _started ? "Stop" : "Start";
            txtOutputPath.IsEnabled = btnFindPath.IsEnabled = !_started;
            _hookManager.SetHookEnabled(_started);
            UpdateStatusBarText();
        }

        private void OnPathChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!new FileInfo(txtOutputPath.Text).Directory.Exists)
                txtOutputPath.Text = DEFAULT_PATH;
            _logger.UpdateFilePath(txtOutputPath.Text);
        }

        private void OnFindPathClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "fglog"; // Default file name
            dlg.DefaultExt = ".csv"; // Default file extension
            dlg.OverwritePrompt = false;
            dlg.Filter = "Comma Separated Values|*.csv"; // Filter files by extension 
            if (dlg.ShowDialog() == true)
            {
                // path changed will take care of logger for us
                txtOutputPath.Text = dlg.FileName;
            }
        }
    }
}
