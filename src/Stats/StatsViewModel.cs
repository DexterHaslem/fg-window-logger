using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ForegroundLogger.Annotations;

namespace ForegroundLogger.Stats
{
    public class StatsViewModel : INotifyPropertyChanged
    {
        private string _myText;

        public string MyText
        {
            get { return _myText; }
            set
            {
                if (value == _myText) return;
                _myText = value;
                OnPropertyChanged();
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
