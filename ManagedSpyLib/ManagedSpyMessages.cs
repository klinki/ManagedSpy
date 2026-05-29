using System;

namespace Microsoft.ManagedSpy
{
    internal static class ManagedSpyMessages
    {
        internal static readonly uint GetProxy = NativeMethods.RegisterWindowMessage("MSFT_ManagedSpy_GETPROXY");
        internal static readonly uint IsManaged = NativeMethods.RegisterWindowMessage("MSFT_ManagedSpy_ISMANAGED");
        internal static readonly uint ReleaseMemory = NativeMethods.RegisterWindowMessage("MSFT_ManagedSpy_RELEASEMEMORY");
        internal static readonly uint SetManagedProperty = NativeMethods.RegisterWindowMessage("MSFT_ManagedSpy_SETMGDPROPERTY");
        internal static readonly uint GetManagedProperty = NativeMethods.RegisterWindowMessage("MSFT_ManagedSpy_GETMGDPROPERTY");
        internal static readonly uint GetManagedScreenRect = NativeMethods.RegisterWindowMessage("MSFT_ManagedSpy_GETMGDSCREENRECT");
        internal static readonly uint GetManagedLayout = NativeMethods.RegisterWindowMessage("MSFT_ManagedSpy_GETMGDLAYOUT");
        internal static readonly uint ResetManagedProperty = NativeMethods.RegisterWindowMessage("MSFT_ManagedSpy_RESETMGDPROPERTY");
        internal static readonly uint SubscribeEvent = NativeMethods.RegisterWindowMessage("MSFT_ManagedSpy_SUBSCRIBEEVENT");
        internal static readonly uint UnsubscribeEvent = NativeMethods.RegisterWindowMessage("MSFT_ManagedSpy_UNSUBSCRIBEEVENT");
        internal static readonly uint EventFired = NativeMethods.RegisterWindowMessage("MSFT_ManagedSpy_EVENTFIRED");
        internal static readonly uint WindowDestroyed = NativeMethods.RegisterWindowMessage("MSFT_ManagedSpy_WINDOWDESTROYED");
        internal static readonly uint HandleChanged = NativeMethods.RegisterWindowMessage("MSFT_ManagedSpy_HANDLECHANGED");
    }
}
