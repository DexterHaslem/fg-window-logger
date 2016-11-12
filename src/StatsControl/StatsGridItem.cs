using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForegroundLogger.StatsControl
{
    public class StatsGridItem
    {
        public string ExeName { get; set; }
        public TimeSpan TotalTime { get; set; }
        public List<string> SeenTitles { get; set; }

        public List<TimeSpan> Durations { get; set; }

        public TimeSpan AverageDuration
        {
            get { return new TimeSpan(Convert.ToInt64(Durations.Average(t => t.Ticks))); }
        }

        public StatsGridItem()
        {
            SeenTitles = new List<string>();
            Durations = new List<TimeSpan>();
        }

        public StatsGridItem(string exe, TimeSpan time) : this()
        {
            ExeName = exe;
            TotalTime = time;
        }

        public StatsGridItem(string exe, TimeSpan time, params string[] titles) : this(exe, time)
        {
            SeenTitles = new List<string>(titles);
        }
    }
}
