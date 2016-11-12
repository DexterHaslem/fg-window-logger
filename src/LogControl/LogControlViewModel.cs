using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ForegroundLogger.Annotations;

namespace ForegroundLogger.LogControl
{
    public class LogControlViewModel : INotifyPropertyChanged
    {
        private bool _isStarted;
        private readonly HookManager _hookManager;
        
        private readonly Timer _updateTimer;
        private const int TIMER_TICK_MS = 1000;
        // update these every X timer ticks
        //private const int STATUS_UPDATE_RATE = 1;
        private const int FILELOG_UPDATE_RATE = 5;
        // update all files in list every hour. this will fix display when running it overnight
        private const int UPDATE_ALL_FILES_RATE = 60 * 60;
        private int _updateAllCount;

        private const string START_TEXT = "Start logging";
        private const string STOP_TEXT = "Stop logging";
        
        private readonly MainWindow _owner;
        private string _startStopButtonText;
        private bool _areLogButtonsEnabled;
        private string _statusBarText;

        public Logger Logger { get; private set; }

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
        
        public IEnumerable<LogItem> SelectedLogItems => LogItems.Where(li => li.IsSelected);

        public bool AreLogButtonsEnabled
        {
            get { return _areLogButtonsEnabled; }
            set
            {
                if (value == _areLogButtonsEnabled) return;
                _areLogButtonsEnabled = value;
                OnPropertyChanged();
            }
        }

        public int TimerCount { get; private set; }

        // note to self: DONT use C# => shorthand getters, will return new instance of routed command every get. WPF hits em several times
        public ICommand DeleteCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand StatsCommand { get; }
        public ICommand LogSelectAllChangedCommand { get; }
        public ICommand LogSelectionChangedCommand { get; }
        public ICommand StartStopLoggingCommand { get; }

        public LogControlViewModel(MainWindow owner)
        {
            StartStopLoggingCommand = new RoutedCommand("StartStopLogging", typeof(LogControl));
            DeleteCommand = new RoutedCommand("Delete", typeof(LogControl));
            ExportCommand = new RoutedCommand("Export", typeof(LogControl));
            StatsCommand = new RoutedCommand("Stats", typeof(LogControl));
            LogSelectAllChangedCommand = new RoutedCommand("LogSelectAllChanged", typeof(LogControl));
            LogSelectionChangedCommand = new RoutedCommand("LogSelectionChanged", typeof(LogControl));

            _hookManager = new HookManager();
            _hookManager.ForegroundWindowChanged += OnForegroundWindowChanged;
            _owner = owner;
            _owner.Closing += (o, e) =>
            {
                Logger?.WriteQueued();
                _updateTimer.Dispose();
            };
            Logger = new Logger();

            LogItems = new ObservableCollection<LogItem>(Logger.GetAllLogs());
            LogItemsView = (CollectionView)CollectionViewSource.GetDefaultView(LogItems);

            // TODO: adorners and clickable
            LogItemsView.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Descending));
            StartStopButtonText = START_TEXT;
            _updateTimer = new Timer(OnTimerTick, null, 25, TIMER_TICK_MS);

            owner.CommandBindings.Add(new CommandBinding(DeleteCommand, OnDeleteLog, (sender, args) => args.CanExecute = true));
            owner.CommandBindings.Add(new CommandBinding(ExportCommand, OnExportLog, (sender, args) => args.CanExecute = true));
            owner.CommandBindings.Add(new CommandBinding(StartStopLoggingCommand, OnStartStopLogging));
            owner.CommandBindings.Add(new CommandBinding(LogSelectAllChangedCommand, OnLogSelectAllChanged, (sender, args) => args.CanExecute = true));
            owner.CommandBindings.Add(new CommandBinding(LogSelectionChangedCommand, OnLogSelectionChanged, (sender, args) => args.CanExecute = true));
        }

        private void OnTimerTick(object state)
        {
            Logger?.WriteQueued();
            ++TimerCount;
            ++_updateAllCount;

            if (_isStarted && TimerCount % FILELOG_UPDATE_RATE == 0)
            {
                Logger?.UpdateLogItemsLineCount(LogItems.Where(l => l.IsCurrentLogItem));
                TimerCount = 0;
            }

            if (_updateAllCount % UPDATE_ALL_FILES_RATE == 0)
            {
                _updateAllCount = 0;
                LogItems.Clear();
                if (Logger != null)
                    foreach (var li in Logger.GetAllLogs())
                        LogItems.Add(li);
            }
            UpdateStatusBarText();
        }

        private void OnForegroundWindowChanged(ForegroundChangedEventArgs e)
        {
            Logger?.QueueEvent(e);
        }

        private void UpdateStatusBarText()
        {
            string logLine = !_isStarted ? "Idle" : $"Running, logged {Logger?.LinesLogged} changes this session";
            //_owner.Dispatcher.Invoke(() => StatusBarTex = logLine);
            StatusBarText = logLine;
        }

        public void OnStartStopLogging(object sender, RoutedEventArgs e)
        {
            _isStarted = !_isStarted;
            StartStopButtonText = _isStarted ? STOP_TEXT : START_TEXT;
            _hookManager.SetHookEnabled(_isStarted);
            if (_isStarted && LogItems.Count == 0 || LogItems.All(li => !li.IsCurrentLogItem))
                LogItems.Add(new LogItem(Logger?.GetFilePathFormat(DateTime.Now)));
            UpdateStatusBarText();
        }

        public void OnDeleteLog(object sender, ExecutedRoutedEventArgs e)
        {
            var selectedItemsCopy = SelectedLogItems.ToList();
            if (selectedItemsCopy.Count < 1)
                return;

            var itemsChunk = selectedItemsCopy.Count > 1 ? $"selected {selectedItemsCopy.Count} log items?" : $"{selectedItemsCopy[0].DateString} log item?";
            var result = MessageBox.Show(_owner, "Are you sure you want to delete the " + itemsChunk, "Warning", MessageBoxButton.YesNoCancel);
            if (result != MessageBoxResult.Yes)
                return;

            foreach (var log2Delete in selectedItemsCopy)
            {
                Logger?.DeleteLog(log2Delete);
                LogItems.Remove(log2Delete);
            }
        }

        public void OnExportLog(object sender, ExecutedRoutedEventArgs e)
        {
            // TODO: just let user save somewhere more friendly then isolated storage
            // if they select multiple logs, squish em into one file
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "exportfglog"; // Default file name
            dlg.DefaultExt = ".csv"; // Default file extension
            dlg.OverwritePrompt = false;
            dlg.Filter = "Comma Separated Values|*.csv"; // Filter files by extension 
            if (dlg.ShowDialog() == true)
                Logger?.SaveLogs(SelectedLogItems.ToList(), dlg.FileName);
        }

        public void OnLogSelectionChanged(object sender, ExecutedRoutedEventArgs e)
        {
            AreLogButtonsEnabled = SelectedLogItems.Any();
        }

        private void OnLogSelectAllChanged(object sender, ExecutedRoutedEventArgs e)
        {
            foreach (var li in LogItems)
                li.IsSelected = (bool)e.Parameter;
        }

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
