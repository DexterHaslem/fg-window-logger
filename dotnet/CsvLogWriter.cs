using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ForegroundLogger_Managed
{
    public class CsvLogWriter 
    {
        private static string _filePath;
        private ConcurrentQueue<HookManager.ForegroundChangedEventArgs> _logQueue;

        public int LinesLogged { get; private set; }
        public int QueuedEventsCount => _logQueue.Count;

        public CsvLogWriter(string path)
        {
            _filePath = path;
            _logQueue = new ConcurrentQueue<HookManager.ForegroundChangedEventArgs>();
            
        }

        public void Flush()
        {
            if (_logQueue.IsEmpty)
                return;

            List<HookManager.ForegroundChangedEventArgs> toLog = new List<HookManager.ForegroundChangedEventArgs>(_logQueue.Count);
            HookManager.ForegroundChangedEventArgs curLogItem;
            while (!_logQueue.IsEmpty && _logQueue.TryDequeue(out curLogItem))
                toLog.Add(curLogItem);
            WriteToLog(toLog);
        }

        private void WriteToLog(IEnumerable<HookManager.ForegroundChangedEventArgs> items)
        {
            File.AppendAllLines(_filePath, items.Select(i => $"{i.Timestamp.ToString(CultureInfo.InvariantCulture)},{i.Executable},{i.WindowTitle}"));
            LinesLogged += items.Count();
        }

        public void QueueEvent(HookManager.ForegroundChangedEventArgs e)
        {
            _logQueue.Enqueue(e);
        }

        public void UpdateFilePath(string newPath)
        {
            _filePath = newPath;
        }
    }
}
