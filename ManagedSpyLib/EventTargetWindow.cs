using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace Microsoft.ManagedSpy
{
    public static class EventDispatchHelper
    {
        public static bool TryGetEventDescriptor(
            EventDescriptorCollection events,
            int eventIndex,
            out EventDescriptor eventDescriptor)
        {
            eventDescriptor = null;
            if (events == null || eventIndex < 0 || eventIndex >= events.Count)
            {
                return false;
            }

            eventDescriptor = events[eventIndex];
            return eventDescriptor != null;
        }
    }

    internal sealed class EventTargetWindow : Control
    {
        protected override void WndProc(ref Message m)
        {
            uint messageId = unchecked((uint)m.Msg);
            if (messageId == ManagedSpyMessages.EventFired)
            {
                MemoryStore store = MemoryStore.OpenStore((int)m.WParam, (int)m.LParam, true);
                if (store != null)
                {
                    try
                    {
                        List<object> parameters = store.GetParameters() as List<object>;
                        if (parameters != null && parameters.Count == 3)
                        {
                            IntPtr proxyHandle = (IntPtr)parameters[0];
                            if (Desktop.ProxyCache.TryGetValue(proxyHandle, out ControlProxy proxy))
                            {
                                EventDescriptorCollection events = proxy.GetEvents();
                                if (EventDispatchHelper.TryGetEventDescriptor(events, (int)parameters[1], out EventDescriptor eventDescriptor))
                                {
                                    proxy.RaiseEvent(new ProxyEventArgs(eventDescriptor, (EventArgs)parameters[2]));
                                }
                            }
                        }
                    }
                    finally
                    {
                        store.Release();
                    }
                }

                return;
            }

            if (messageId == ManagedSpyMessages.WindowDestroyed)
            {
                Desktop.ProxyCache.Remove(m.WParam);
                return;
            }

            if (messageId == ManagedSpyMessages.HandleChanged)
            {
                IntPtr oldHandle = m.WParam;
                IntPtr newHandle = m.LParam;
                if (Desktop.ProxyCache.TryGetValue(oldHandle, out ControlProxy proxy))
                {
                    Desktop.ProxyCache.Remove(oldHandle);
                    proxy.Handle = newHandle;
                    if (newHandle != IntPtr.Zero)
                    {
                        Desktop.ProxyCache[newHandle] = proxy;
                    }
                }

                return;
            }

            base.WndProc(ref m);
        }
    }
}
