using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;

namespace ForegroundLogger
{
    public class Logger
    {
        // dont use invariant or other cultures with / or \, not valid in filenames
        public static readonly string FILEDATEFORMAT = "yyyy MM dd";// CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
        public const string CSV_TIMEFORMAT = "MM/dd/yyyy HH:mm:ss.fff";
        private const string APPDATA_FOLDERNAME = "fglogs";
        private readonly string _appDataFolder;
        private readonly ConcurrentQueue<ForegroundChangedEventArgs> _logQueue;
        //private readonly IsolatedStorageFile _isolatedStorage = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

        public int LinesLogged { get; private set; }
        public int QueuedEventsCount => _logQueue.Count;

        public Logger()
        {
            _logQueue = new ConcurrentQueue<ForegroundChangedEventArgs>();
            var appdataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _appDataFolder = Path.Combine(appdataFolder, APPDATA_FOLDERNAME);
            Directory.CreateDirectory(_appDataFolder);
        }

        public void WriteQueued()
        {
            if (_logQueue.IsEmpty)
                return;
            var toLog = new List<ForegroundChangedEventArgs>(_logQueue.Count);
            ForegroundChangedEventArgs curLogItem;
            while (!_logQueue.IsEmpty && _logQueue.TryDequeue(out curLogItem))
                toLog.Add(curLogItem);
            WriteToLog(toLog);
        }

        private void WriteToLog(IEnumerable<ForegroundChangedEventArgs> items)
        {
            string filePath = GetFileNameForLogDate(DateTime.Now);
            //using (var isoFile = new IsolatedStorageFileStream(filePath, FileMode.Append, _isolatedStorage))            
            //using (StreamWriter sw = new StreamWriter(isoFile))
            //    foreach (var l in items.Select(i => $"{i.Timestamp.ToString(CSV_TIMEFORMAT, CultureInfo.InvariantCulture)},{i.Executable},{i.WindowTitle}"))
            //        sw.WriteLine(l);
            File.AppendAllLines(Path.Combine(_appDataFolder,filePath), 
                items.Select(i => $"{i.Timestamp.ToString(CSV_TIMEFORMAT, CultureInfo.InvariantCulture)},{i.Executable},{i.WindowTitle}"));
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
            var ret = Directory.GetFiles(_appDataFolder).Select(f => new LogItem(f)).ToList();
            UpdateLogItemsLineCount(ret);
            return ret;
        }

        public string GetFileNameForLogDate(DateTime time)
        {
            return $"fglog-{time.ToString(FILEDATEFORMAT, CultureInfo.InvariantCulture)}.csv";
        }

        public string GetLogContents(LogItem logItem)
        {
            if (!File.Exists(logItem.FilePath))
                return string.Empty;

            //using (var isoFile = new IsolatedStorageFileStream(logItem.FilePath, FileMode.Open, _isolatedStorage))
            //using (StreamReader sr = new StreamReader(isoFile))
            //    return sr.ReadToEnd();
            return File.ReadAllText(logItem.FilePath);
        }

        public void CopyFileIntoLogs(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            var linesByDate = new Dictionary<DateTime, List<string>>();

            // we dont have to parse completely, just bucket into dates
            foreach (var line in lines)
            {
                var chunks = line.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
                if (chunks.Length < 1)
                    continue;

                DateTime timestamp;
                if (DateTime.TryParseExact(chunks[0], CSV_TIMEFORMAT, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out timestamp))
                {
                    if (!linesByDate.ContainsKey(timestamp))
                        linesByDate.Add(timestamp, new List<string>());
                    linesByDate[timestamp].Add(line);
                }
            }

            foreach (var kvp in linesByDate)
            {
                var fileName = GetFileNameForLogDate(kvp.Key);
                var path = Path.Combine(_appDataFolder, fileName);
                File.AppendAllLines(path, kvp.Value);
            }
        }

        public void UpdateLogItemsLineCount(IEnumerable<LogItem> items)
        {
            foreach (var item in items)
            {
                var contents = GetLogContents(item);
                item.ItemCount = string.IsNullOrWhiteSpace(contents) ? 0 :
                    contents.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Length;
            }
        }

        public void DeleteLog(LogItem selectedLogItem)
        {
            // UI warning already handled by this point
            //if (_isolatedStorage.FileExists(selectedLogItem.FilePath))
            //    _isolatedStorage.DeleteFile(selectedLogItem.FilePath);
            File.Delete(selectedLogItem.FilePath);
        }

        public void SaveLogs(List<LogItem> toList, string destFile)
        {
           var sb = new StringBuilder();
           foreach (var i in toList)
           {
                var contents = GetLogContents(i);
                if (!string.IsNullOrWhiteSpace(contents))
                    sb.Append(contents);
           }

           File.WriteAllText(destFile, sb.ToString());
        }
    }
}
