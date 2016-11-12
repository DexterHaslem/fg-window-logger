using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ForegroundLogger.Annotations;

namespace ForegroundLogger.StatsControl
{
    public class StatsControlViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<StatsGridItem> StatsGridItems { get; set; }

        public StatsControlViewModel()
        {
            StatsGridItems = new ObservableCollection<StatsGridItem>();

            for (int i = 0; i < 25; ++i)
            {
                StatsGridItems.Add(new StatsGridItem(i.ToString(), TimeSpan.FromMinutes(i), "foo", "bar"));
            }
        }


        public void SetStats(IEnumerable<LogItem> logItems)
        {
            
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
