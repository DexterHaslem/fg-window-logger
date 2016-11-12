using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ForegroundLogger.Annotations;

namespace ForegroundLogger
{
    public class LogItem : INotifyPropertyChanged
    {
        private DateTime _date;
        private int _itemCount;
        private string _storagePath;
        private string _filePath;
        private bool _isSelected;

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

        // use current user culture
        public string DateString => Date.ToShortDateString();//Date.ToString(Logger.FILEDATEFORMAT);

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

        public string FilePath
        {
            get { return _filePath; }
            set
            {
                if (value == _filePath) return;
                _filePath = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public LogItem(string filePath)
        {
            _filePath = filePath;
            _date = ParseDateFromFileName(filePath);
        }

        private static DateTime ParseDateFromFileName(string path)
        {
            string timeChunk = path.Replace("fglog-", string.Empty).Split('.').FirstOrDefault();
            return DateTime.ParseExact(timeChunk, Logger.FILEDATEFORMAT, CultureInfo.InvariantCulture);
        }

        public bool IsCurrentLogItem => ParseDateFromFileName(_filePath).Date == DateTime.Now.Date;

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
