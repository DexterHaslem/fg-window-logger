using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;

namespace ForegroundLogger_Managed
{
    public class Logger
    {
        // dont use invariant or other cultures with / or \, not valid in filenames
        public static readonly string FILEDATEFORMAT = "yyyy MM dd";// CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
        private const string CSV_TIMEFORMAT = "MM/dd/yyyy HH:mm:ss.fff";

        private readonly ConcurrentQueue<ForegroundChangedEventArgs> _logQueue;
        private readonly IsolatedStorageFile _isolatedStorage = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

        public int LinesLogged { get; private set; }
        public int QueuedEventsCount => _logQueue.Count;

        public Logger()
        {
            _logQueue = new ConcurrentQueue<ForegroundChangedEventArgs>();
        }

        public void WriteQueued()
        {
            if (_logQueue.IsEmpty)
                return;
            List<ForegroundChangedEventArgs> toLog = new List<ForegroundChangedEventArgs>(_logQueue.Count);
            ForegroundChangedEventArgs curLogItem;
            while (!_logQueue.IsEmpty && _logQueue.TryDequeue(out curLogItem))
                toLog.Add(curLogItem);
            WriteToLog(toLog);
        }

        private void WriteToLog(IEnumerable<ForegroundChangedEventArgs> items)
        {
            string filePath = GetFilePathFormat(DateTime.Now);
            using (var isoFile = new IsolatedStorageFileStream(filePath, FileMode.Append, _isolatedStorage))
            using (StreamWriter sw = new StreamWriter(isoFile))
                foreach (var l in items.Select(i => $"{i.Timestamp.ToString(CSV_TIMEFORMAT, CultureInfo.InvariantCulture)},{i.Executable},{i.WindowTitle}"))
                    sw.WriteLine(l);
            LinesLogged += items.Count();
        }

        public void QueueEvent(ForegroundChangedEventArgs e)
        {
            _logQueue.Enqueue(e);
        }

        public IEnumerable<LogItem> GetAllLogs()
        {
            // dont use an enumerable here, or else the update log items count will not work right
            // the lazy fetch fucks it up
            var ret = _isolatedStorage.GetFileNames().Select(f => new LogItem(f)).ToList();
            UpdateLogItemsLineCount(ret);
            return ret;
        }

        public string GetFilePathFormat(DateTime time)
        {
            return $"fglog-{time.ToString(FILEDATEFORMAT, CultureInfo.InvariantCulture)}.csv";
        }

        private string GetLogContents(LogItem logItem)
        {
            if (!_isolatedStorage.FileExists(logItem.FilePath))
                return string.Empty;

            using (var isoFile = new IsolatedStorageFileStream(logItem.FilePath, FileMode.Open, _isolatedStorage))
            using (StreamReader sr = new StreamReader(isoFile))
                return sr.ReadToEnd();
        }

        public void UpdateLogItemsLineCount(IEnumerable<LogItem> items)
        {
            foreach (LogItem item in items)
            {
                string contents = GetLogContents(item);
                item.ItemCount = string.IsNullOrWhiteSpace(contents) ? 0 :
                    contents.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Length;
            }
        }

        public void DeleteLog(LogItem selectedLogItem)
        {
            // warning already handled by this point
            if (_isolatedStorage.FileExists(selectedLogItem.FilePath))
                _isolatedStorage.DeleteFile(selectedLogItem.FilePath);
        }

        public void SaveLogs(List<LogItem> toList, string destFile)
        {
           //toList.OrderBy(i => i.Date).Select(GetLogContents)
           StringBuilder sb = new StringBuilder();
           foreach (LogItem i in toList)
           {
               var contents = GetLogContents(i);
                if (!string.IsNullOrWhiteSpace(contents))
                    sb.Append(contents);
           }

           File.WriteAllText(destFile, sb.ToString());
        }
    }
}
