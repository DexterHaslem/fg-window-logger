using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ForegroundLogger_Managed.Annotations;

namespace ForegroundLogger_Managed
{
    public class LogItem : INotifyPropertyChanged
    {
        private DateTime _date;
        private int _itemCount;
        private string _storagePath;
        private bool _isActiveLog;

        public LogItem(string filePath)
        {
           _date = ParseDateFromFileName(filePath);
            _isActiveLog = GetIsLogActive(filePath);
            _itemCount = File.ReadAllLines(filePath).Length;
        }

        private static DateTime ParseDateFromFileName(string path)
        {
            var fileNameOnly = new FileInfo(path).Name;
            string timeChunk = fileNameOnly.Replace("fglog-", string.Empty).Split('.').FirstOrDefault();
            return DateTime.ParseExact(timeChunk, Logger.FILEDATEFORMAT, CultureInfo.InvariantCulture);
        }

        private bool GetIsLogActive(string fileName)
        {
            return ParseDateFromFileName(fileName).Date == DateTime.Now.Date;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public DateTime Date
        {
            get { return _date; }
            set
            {
                if (value.Equals(_date)) return;
                _date = value;
                OnPropertyChanged();
            }
        }

        public int ItemCount
        {
            get { return _itemCount; }
            set
            {
                if (value == _itemCount) return;
                _itemCount = value;
                OnPropertyChanged();
            }
        }

        public string StoragePath
        {
            get { return _storagePath; }
            set
            {
                if (value == _storagePath) return;
                _storagePath = value;
                OnPropertyChanged();
            }
        }

        public bool IsActiveLog
        {
            get { return _isActiveLog; }
            set
            {
                if (value == _isActiveLog) return;
                _isActiveLog = value;
                OnPropertyChanged();
            }
        }
    }
}
