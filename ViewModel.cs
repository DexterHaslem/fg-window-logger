using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using ForegroundLogger.Annotations;

namespace ForegroundLogger
{
    public class ViewModel : INotifyPropertyChanged
    {
        private bool _isStarted = false;
        //private const string DEFAULT_FILENAME = "fglog.csv";
        private readonly HookManager _hookManager;
        private Logger _logger;
        private readonly Timer _updateTimer;
        private const int TIMER_TICK_MS = 1000;
        private int timerCount = 0;
        // update these every X timer ticks
        //private const int STATUS_UPDATE_RATE = 1;
        private const int FILELOG_UPDATE_RATE = 5;

        // update all files in list every hour. this will fix display when running it overnight
        private int updateAllCount = 0;
        private const int UPDATE_ALL_FILES_RATE = 60 * 60;

        private const string START_TEXT = "Start logging";
        private const string STOP_TEXT = "Stop logging";

        //private readonly string DEFAULT_PATH;
        private MainWindow _owner;
        private string _statusBarText;
        private string _startStopButtonText;
        private bool _isStartStopButtonEnabled;
        private LogItem _selectedLogItem;
        private bool _isExportEnabled;

        public ObservableCollection<LogItem> LogItems { get; set; }

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

        public CollectionView LogItemsView { get; }

        //public ObservableCollection<LogItem> SelectedLogItems { get; set; }
        public IEnumerable<LogItem> SelectedLogItems => LogItems.Where(li => li.IsSelected);

        //public LogItem SelectedLogItem
        //{
        //    get { return _selectedLogItem; }
        //    set
        //    {
        //        if (Equals(value, _selectedLogItem)) return;
        //        _selectedLogItem = value;
        //        IsExportEnabled = value != null;
        //        OnPropertyChanged();
        //    }
        //}

        public bool IsExportEnabled
        {
            get { return _isExportEnabled; }
            set
            {
                if (value == _isExportEnabled) return;
                _isExportEnabled = value;
                OnPropertyChanged();
            }
        }

        public ViewModel(MainWindow owner)
        {
            _hookManager = new HookManager();
            _hookManager.ForegroundWindowChanged += OnForegroundWindowChanged;
            _owner = owner;
            _owner.Closing += (o, e) =>
            {
                _logger?.WriteQueued();
                _updateTimer.Dispose();
            };
            _logger = new Logger();

            LogItems = new ObservableCollection<LogItem>(_logger.GetAllLogs());
            LogItemsView = (CollectionView)CollectionViewSource.GetDefaultView(LogItems);

            // TODO: adorners and clickable
            LogItemsView.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Descending));
            StartStopButtonText = START_TEXT;
            _updateTimer = new Timer(OnTimerTick, null, 25, TIMER_TICK_MS);

            LogItems.CollectionChanged += (o, e) =>
            {
                Debug.WriteLine("LogItems.CollectionChanged");
            };
        }

        private void OnTimerTick(object state)
        {
            _logger?.WriteQueued();
            ++timerCount;
            ++updateAllCount;

            if (_isStarted && timerCount % FILELOG_UPDATE_RATE == 0)
            {
                _logger?.UpdateLogItemsLineCount(LogItems.Where(l => l.IsCurrentLogItem));
                timerCount = 0;
            }

            if (updateAllCount % UPDATE_ALL_FILES_RATE == 0)
            {
                updateAllCount = 0;
                LogItems.Clear();
                foreach (var li in _logger.GetAllLogs())
                    LogItems.Add(li);
            }
            UpdateStatusBarText();
        }

        private void OnForegroundWindowChanged(ForegroundChangedEventArgs e)
        {
            _logger?.QueueEvent(e);
        }

        private void UpdateStatusBarText()
        {
            string logLine = !_isStarted ? "Idle" : $"Running, logged {_logger?.LinesLogged} changes this session";
            //_owner.Dispatcher.Invoke(() => StatusBarTex = logLine);
            StatusBarText = logLine;
        }

        public void OnStartStopClick(object sender, RoutedEventArgs e)
        {
            _isStarted = !_isStarted;
            StartStopButtonText = _isStarted ? STOP_TEXT : START_TEXT;
            _hookManager.SetHookEnabled(_isStarted);
            if (_isStarted && LogItems.Count == 0 || LogItems.All(li => !li.IsCurrentLogItem))
                LogItems.Add(new LogItem(_logger?.GetFilePathFormat(DateTime.Now)));
            UpdateStatusBarText();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public void OnDeleteLogClick()
        {
            var selectedItemsCopy = SelectedLogItems.ToList();
            if (selectedItemsCopy.Count < 1)
                return;

            var itemsChunk = selectedItemsCopy.Count > 1 ? $"selected {selectedItemsCopy.Count} log items?" : $"{selectedItemsCopy[0].DateString} log item?";
            var result = MessageBox.Show(_owner, "Are you sure you want to delete the " + itemsChunk, "Warning", MessageBoxButton.YesNoCancel);
            if (result != MessageBoxResult.Yes)
                return;

            foreach (var log2delete in selectedItemsCopy)
            {
                _logger?.DeleteLog(log2delete);
                LogItems.Remove(log2delete);
            }
        }

        public void OnExportLogClick()
        {
            // TODO: just let user save somewhere more friendly then isolated storage
            // if they select multiple logs, squish em into one file
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "exportfglog"; // Default file name
            dlg.DefaultExt = ".csv"; // Default file extension
            dlg.OverwritePrompt = false;
            dlg.Filter = "Comma Separated Values|*.csv"; // Filter files by extension 
            if (dlg.ShowDialog() == true)
                _logger?.SaveLogs(SelectedLogItems.ToList(), dlg.FileName);
        }

        public void OnLogSelectionChanged()
        {
            IsExportEnabled = SelectedLogItems.Any();
        }

        public void OnSelectAll(bool selected)
        {
            foreach (var li in LogItems)
                li.IsSelected = selected;
        }
    }
}
