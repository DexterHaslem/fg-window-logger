using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ForegroundLogger
{
    public static class NativeMethods
    {
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        public const uint WINEVENT_OUTOFCONTEXT = 0x0000; // Events are ASYNC
        public const uint WINEVENT_SKIPOWNTHREAD = 0x0001; // Don't call back for events on installer's thread    
        public const uint WINEVENT_SKIPOWNPROCESS = 0x0002; // Don't call back for events on installer's process
        public const uint WINEVENT_INCONTEXT = 0x0004; // Events are SYNC, this causes your dll to be injected into every process
        public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;

        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType,
                                                IntPtr hwnd, int idObject, int idChild,
                                                uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr
                                                hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess,
                                                uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("psapi.dll")]
        public static extern uint GetProcessImageFileName(IntPtr hProcess, 
            [Out] StringBuilder lpImageFileName, [In] [MarshalAs(UnmanagedType.U4)] int nSize);

        [DllImport("user32.dll", SetLastError=true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError=true, CharSet=CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);
    }
}
