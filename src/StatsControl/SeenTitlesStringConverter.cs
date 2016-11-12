using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ForegroundLogger.StatsControl
{
    internal class SeenTitlesStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var seenValues = value as List<string>;
            if (seenValues == null || seenValues.Count < 1)
                return string.Empty;
            return string.Join(", ", seenValues);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
