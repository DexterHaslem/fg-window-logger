using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ForegroundLogger_Managed
{
    public class HookManager : IDisposable
    {
        private IntPtr hHook;
        // this is required so our callback doesnt get GCd
        // for some reason passing an instance method directly doesnt work
        private NativeMethods.WinEventDelegate _callbackFn;

        public class ForegroundChangedEventArgs  : EventArgs
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

        public delegate void ForegroundChangedDelegate(ForegroundChangedEventArgs e);
        public event ForegroundChangedDelegate ForegroundWindowChanged;
        
        public HookManager()
        {
            _callbackFn = new NativeMethods.WinEventDelegate(HookCallback);
        }

        public void SetHookEnabled(bool active)
        {
            if (active && _callbackFn != null)
            {
                hHook = NativeMethods.SetWinEventHook(NativeMethods.EVENT_SYSTEM_FOREGROUND, 
                    NativeMethods.EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _callbackFn, 0, 0,
                    NativeMethods.WINEVENT_OUTOFCONTEXT);
            }
            else if (hHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(hHook);
                hHook = IntPtr.Zero;
            }
        }

        private void HookCallback(IntPtr hWinEventHook, uint eventType,
                                  IntPtr hwnd, int idObject, int idChild,
                                  uint dwEventThread, uint dwmsEventTime)
        {
            string exeName = GetWindowProcessName(hwnd);
            string windowTitle = GetWindowText(hwnd);
            Debug.WriteLine("fglogger::hookcallback: hwnd=" + hwnd + " exeName="+ exeName + " title =" + windowTitle);
            RaiseForegroundChanged(exeName, windowTitle);
        }

        private void RaiseForegroundChanged(string exe, string title)
        {
            ForegroundWindowChanged?.Invoke(new ForegroundChangedEventArgs(exe, title, DateTime.UtcNow));
        }

        private static string GetWindowText(IntPtr hWnd)
        {
            int length = NativeMethods.GetWindowTextLength(hWnd);
            StringBuilder sb = new StringBuilder(length + 1);
            NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        private static string GetWindowProcessName(IntPtr hWnd)
        {
            // we need to get the process, open the process, then read image filename. wew lawd
            uint processId;
            NativeMethods.GetWindowThreadProcessId(hWnd, out processId);

            // we only need VM read and query, dont use all, that'd probably require admin.
            IntPtr processInfo = NativeMethods.OpenProcess(
                NativeMethods.ProcessAccessFlags.QueryInformation | NativeMethods.ProcessAccessFlags.VirtualMemoryRead, 
                true, (int) processId);

            var sb = new StringBuilder(1024);

            // it wants handle to process, not window
            NativeMethods.GetProcessImageFileName(processInfo, sb, 1024);

            string exeName = Path.GetFileName(sb.ToString());
            return exeName;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~HookManager()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ForegroundWindowChanged = null;
            }
            SetHookEnabled(false);
            _callbackFn = null;
        }
    }
}
