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
using ForegroundLogger.LogControl;
using ForegroundLogger.Stats;
using ForegroundLogger.StatsControl;

namespace ForegroundLogger
{
    internal class MainViewModel : INotifyPropertyChanged
    {
        private bool _isStatsTabVisible;
        private int _selectedTabIndex;
        private string _statusBarText;

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

        public bool IsStatsTabVisible
        {
            get { return _isStatsTabVisible; }
            set
            {
                if (value == _isStatsTabVisible) return;
                _isStatsTabVisible = value;
                OnPropertyChanged();
            }
        }

        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set
            {
                if (value == _selectedTabIndex) return;
                _selectedTabIndex = value;
                OnPropertyChanged();
            }
        }

        public LogControlViewModel LogControlViewModel { get; private set; }
        public StatsControlViewModel StatsControlViewModel { get; private set; }

        public MainViewModel(MainWindow owner)
        {
            LogControlViewModel = new LogControlViewModel(owner);
            StatsControlViewModel = new StatsControlViewModel();

            owner.CommandBindings.Add(new CommandBinding(LogControlViewModel.StatsCommand, OnLogStats));
        }

        private void OnLogStats(object sender, ExecutedRoutedEventArgs e)
        {
            StatsControlViewModel.SetStats(LogControlViewModel.Logger, LogControlViewModel.SelectedLogItems);
            IsStatsTabVisible = true;
            //StatsControlViewModel.MyText = "farts";
            SelectedTabIndex = 1;
        }


        //private void UpdateStatusBarText()
        //{
        //    string logLine = !_isStarted ? "Idle" : $"Running, logged {Logger?.LinesLogged} changes this session";
        //    //_owner.Dispatcher.Invoke(() => StatusBarTex = logLine);
        //    StatusBarText = logLine;
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
