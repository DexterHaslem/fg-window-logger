using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;

namespace ForegroundLogger_Managed
{
    public class Logger
    {
        public const string FILEDATEFORMAT = "yyyy mm dd";
        private static string _filePath;
        private readonly ConcurrentQueue<ForegroundChangedEventArgs> _logQueue;
        private IsolatedStorageFile _isolatedStorage = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

        public int LinesLogged { get; private set; }
        public int QueuedEventsCount => _logQueue.Count;

        public Logger(string path)
        {
            _filePath = path;
            _logQueue = new ConcurrentQueue<ForegroundChangedEventArgs>();
        }

        public void Flush()
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
            File.AppendAllLines(_filePath, items.Select(i => $"{i.Timestamp.ToString(CultureInfo.InvariantCulture)},{i.Executable},{i.WindowTitle}"));
            LinesLogged += items.Count();
        }

        public void QueueEvent(ForegroundChangedEventArgs e)
        {
            _logQueue.Enqueue(e);
        }

        public void UpdateFilePath(string newPath)
        {
            _filePath = newPath;
        }

        public IEnumerable<LogItem> GetAllLogs()
        {
            return _isolatedStorage.GetFileNames().Select(f => new LogItem(f));
        }

        
        public string GetFilePathFormat(DateTime time)
        {
            return $"fglog-{time.ToString(FILEDATEFORMAT, CultureInfo.InvariantCulture)}.csv";
        }
    }
}
