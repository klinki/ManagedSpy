using System;
using Microsoft.ManagedSpy;

namespace ManagedSpyLib
{
    public static class MessageFilters
    {
        public static void Initialize()
        {
            IntPtr eventWindowHandle = Desktop.EventWindow.Handle;
            AllowMessage(eventWindowHandle, ManagedSpyMessages.ReleaseMemory);
            AllowMessage(eventWindowHandle, ManagedSpyMessages.EventFired);
            AllowMessage(eventWindowHandle, ManagedSpyMessages.WindowDestroyed);
            AllowMessage(eventWindowHandle, ManagedSpyMessages.HandleChanged);
        }

        private static void AllowMessage(IntPtr targetWindow, uint message)
        {
            NativeMethods.ChangeFilterStruct changeFilter = new NativeMethods.ChangeFilterStruct
            {
                cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.ChangeFilterStruct>()
            };

            NativeMethods.ChangeWindowMessageFilterEx(
                targetWindow,
                message,
                NativeMethods.MsgFltAllow,
                ref changeFilter);
        }
    }
}
