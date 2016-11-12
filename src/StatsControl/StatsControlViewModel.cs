using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Documents;
using ForegroundLogger.Annotations;

namespace ForegroundLogger.StatsControl
{
    public class StatsControlViewModel : INotifyPropertyChanged
    {
        private string _seenLabelSearch;

        public string SeenLabelSearch
        {
            get { return _seenLabelSearch; }
            set
            {
                if (value == _seenLabelSearch) return;
                _seenLabelSearch = value;
                OnPropertyChanged();
                UpdateSeenLabelSearch();
            }
        }
    
        public ObservableCollection<StatsGridItem> StatsGridItems { get; set; }
        public CollectionView StatsGridItemsView { get; set; }

        public StatsControlViewModel()
        {
            StatsGridItems = new ObservableCollection<StatsGridItem>();
            StatsGridItemsView = (CollectionView)CollectionViewSource.GetDefaultView(StatsGridItems);
            StatsGridItemsView.SortDescriptions.Add(new SortDescription("TotalTime", ListSortDirection.Descending));
        }

        public void SetStats(Logger logger, IEnumerable<LogItem> selectedLogItems)
        {
            StatsGridItems.Clear();

            DateTime? prevTimestamp = null;
            var newStatsItems = new List<StatsGridItem>();            

            foreach (var logItem in selectedLogItems.OrderBy(li => li.Date))
            {
                try
                {
                    var contents = logger.GetLogContents(logItem);
                    if (string.IsNullOrWhiteSpace(contents))
                        continue;

                    var lines = contents.Split(new [] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var line in lines)
                    {
                        // TODO: move me
                        // TODO: pre-parse pass to reorder lines by date, they could become out of date
                        // if user imports

                        // format:
                        // 11/12/2016 08:39:24.924,powershell.exe,posh~git ~ fg-window-logger [stats]
                        // import thing, always treat chunks[2:] as one thing in case a title had a comma :-(                       
                        var chunks = line.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);

                        // if its remotely bogus toss the whole thing
                        if (chunks.Length < 3)
                            continue;
                        
                        var timestr = chunks[0].Trim();
                        var exename = chunks[1].Trim();
                        
                        DateTime time;
                        // once again, anything amiss give up the whole line
                        if (!DateTime.TryParseExact(timestr, Logger.CSV_TIMEFORMAT, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out time))
                            continue;
                        
                        // now we have our time, if this is first entry, set our roll time to it, otherwise we can calculate time spent
                        TimeSpan prevTimeSpan = TimeSpan.Zero;
                        if (prevTimestamp != null)
                        {
                            prevTimeSpan = time - prevTimestamp.Value;
                            // update previous entry
                            newStatsItems.Last().TotalTime = prevTimeSpan;
                        }

                        prevTimestamp = time;
                        // make sure to suck rest of any chunks for title
                        newStatsItems.Add(new StatsGridItem(exename, TimeSpan.Zero, chunks.Skip(2).ToArray()));
                    }
                }
                catch (IOException)
                {
                    
                }
            }

            // take our raw list of new stat items (just each raw line)
            // and actually sum em up and then use that remaining stat item on UI
            var statsByExe = new Dictionary<string, StatsGridItem>();
            foreach (var statItem in newStatsItems)
            {
                var key = statItem.ExeName;
                if (!statsByExe.ContainsKey(key))
                {
                    // make a new item instead of just stuffing this reference in, because will we be modifying it
                    statsByExe[key] = new StatsGridItem(key, statItem.TotalTime, statItem.SeenTitles.ToArray());
                }
                else
                {
                    statsByExe[key].TotalTime += statItem.TotalTime;
                    statsByExe[key].SeenTitles = statItem.SeenTitles.Union(statsByExe[key].SeenTitles).Distinct().ToList();
                }
            }

            foreach (var kvp in statsByExe)
                StatsGridItems.Add(kvp.Value);
        }

        private void UpdateSeenLabelSearch()
        {
            // dont hit property, that calls us
            if (string.IsNullOrWhiteSpace(_seenLabelSearch))
                StatsGridItemsView.Filter = null;
            else
            {
                StatsGridItemsView.Filter = o =>
                {
                    var item = o as StatsGridItem;
                    if (item == null)
                        return true;
                    return item.SeenTitles != null && item.SeenTitles.Any(t => t.ToUpperInvariant().Contains(_seenLabelSearch.ToUpperInvariant()));
                };
            }
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
