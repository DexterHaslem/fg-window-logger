using System;

namespace ForegroundLogger
{
    public class ForegroundChangedEventArgs : EventArgs
    {
        public string Executable { get; private set; }
        public string WindowTitle { get; private set; }
        public DateTime Timestamp { get; private set; }
        public ForegroundChangedEventArgs(string exe, string windowTitle, DateTime timestamp) : base()
        {
            Executable = exe;
            WindowTitle = windowTitle;
            Timestamp = timestamp;
        }
    }
}
