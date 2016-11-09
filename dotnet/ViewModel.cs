using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ForegroundLogger_Managed.Annotations;

namespace ForegroundLogger_Managed
{
    public class ViewModel : INotifyPropertyChanged
    {
        private bool _started = false;
        private const string DEFAULT_FILENAME = "fglog.csv";
        private HookManager _hookManager;
        private Logger _logger;
        private Timer _updateTimer;
        private const int UPDATE_MS = 1000;
        //private readonly string DEFAULT_PATH;
        private MainWindow _owner;
        private string _statusBarText;
        private string _startStopButtonText;
        private bool _isStartStopButtonEnabled;

        public ObservableCollection<LogItem> Logs { get; set; }

        public string StatusBarText
        {
            get { return _statusBarText; }
            set
            {
                if (value == _statusBarText) return;
                _statusBarText = value;
                OnPropertyChanged();
            }
        }

        public string StartStopButtonText
        {
            get { return _startStopButtonText; }
            set
            {
                if (value == _startStopButtonText) return;
                _startStopButtonText = value;
                OnPropertyChanged();
            }
        }

        public bool IsStartStopButtonEnabled
        {
            get { return _isStartStopButtonEnabled; }
            set
            {
                if (value == _isStartStopButtonEnabled) return;
                _isStartStopButtonEnabled = value;
                OnPropertyChanged();
            }
        }

        public ViewModel(MainWindow owner)
        {
            _hookManager = new HookManager();
            //DEFAULT_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), DEFAULT_FILENAME);
            _hookManager.ForegroundWindowChanged += OnForegroundWindowChanged;
            _updateTimer = new Timer(OnTimerTick, null, UPDATE_MS, UPDATE_MS);
            //_logger = new CsvLogWriter(txtOutputPath.Text);
            //txtOutputPath.TextChanged += OnPathChanged;
            _owner = owner;
            _owner.Closing += (o, e) =>
            {
                _logger.Flush();
                _updateTimer.Dispose();
            };
        }

        private void OnTimerTick(object state)
        {
            _logger?.Flush();
            UpdateStatusBarText();
        }

        private void OnForegroundWindowChanged(ForegroundChangedEventArgs e)
        {
            _logger.QueueEvent(e);
        }

        private void UpdateStatusBarText()
        {
            string logLine = !_started ? "Idle..." : $"Running, logged {_logger.LinesLogged} changes...";
            //_owner.Dispatcher.Invoke(() => StatusBarTex = logLine);
            StatusBarText = logLine;
        }

        public void OnStartStopClick(object sender, RoutedEventArgs e)
        {
            _started = !_started;
            StartStopButtonText = _started ? "Stop" : "Start";
            //txtOutputPath.IsEnabled = btnFindPath.IsEnabled = !_started;
            _hookManager.SetHookEnabled(_started);
            UpdateStatusBarText();
        }

        //private void OnPathChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        //{
        //    if (!new FileInfo(txtOutputPath.Text).Directory.Exists)
        //        txtOutputPath.Text = DEFAULT_PATH;
        //    _logger.UpdateFilePath(txtOutputPath.Text);
        //}

        //private void OnFindPathClick(object sender, RoutedEventArgs e)
        //{
        //    Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
        //    dlg.FileName = "fglog"; // Default file name
        //    dlg.DefaultExt = ".csv"; // Default file extension
        //    dlg.OverwritePrompt = false;
        //    dlg.Filter = "Comma Separated Values|*.csv"; // Filter files by extension 
        //    if (dlg.ShowDialog() == true)
        //    {
        //        // path changed will take care of logger for us
        //        //txtOutputPath.Text = dlg.FileName;
        //    }
        //}

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
