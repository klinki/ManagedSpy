using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Microsoft.ManagedSpy
{
    internal sealed class EventRegister
    {
        internal Control SourceWindow;
        internal int EventCode;
        internal EventInfo EventInfo;
        internal IntPtr TargetEventReceiver;

        public void OnEventFired(object sender, EventArgs args)
        {
            List<object> parameters = new List<object>
            {
                SourceWindow.Handle,
                EventCode
            };

            if (args == EventArgs.Empty || args.GetType().IsSerializable)
            {
                parameters.Add(args);
            }
            else if (args is MouseEventArgs mouseEventArgs)
            {
                parameters.Add(new SerializableMouseEventArgs(mouseEventArgs));
            }
            else
            {
                parameters.Add(new NonSerializableEventArgs(args));
            }

            Desktop.SendMarshaledMessage(TargetEventReceiver, ManagedSpyMessages.EventFired, parameters, false);
        }
    }

    public static class HookBridge
    {
        public static void MessageHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode == NativeMethods.HcAction)
            {
                Desktop.OnMessage(nCode, wParam, lParam);
            }
        }
    }

    internal static class Desktop
    {
        private static readonly List<int> managedProcesses = new List<int>();
        private static readonly List<int> unmanagedProcesses = new List<int>();
        private static readonly Dictionary<IntPtr, Dictionary<int, EventRegister>> eventCallbacks =
            new Dictionary<IntPtr, Dictionary<int, EventRegister>>();

        private static IntPtr messageHookHandle = IntPtr.Zero;
        private static IntPtr hookModuleHandle = IntPtr.Zero;

        internal static readonly EventTargetWindow EventWindow = new EventTargetWindow();
        internal static readonly Dictionary<IntPtr, ControlProxy> ProxyCache = new Dictionary<IntPtr, ControlProxy>();

        internal static void EnableHook(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            IntPtr moduleHandle = EnsureHookModuleLoaded();
            if (moduleHandle == IntPtr.Zero)
            {
                return;
            }

            IntPtr hookProc = NativeMethods.GetProcAddress(moduleHandle, "MessageHookProc");
            if (hookProc == IntPtr.Zero)
            {
                return;
            }

            DisableHook();

            uint threadId = NativeMethods.GetWindowThreadProcessId(windowHandle, out _);
            messageHookHandle = NativeMethods.SetWindowsHookEx(
                NativeMethods.WhCallWndProc,
                hookProc,
                moduleHandle,
                threadId);
        }

        internal static void DisableHook()
        {
            if (messageHookHandle != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(messageHookHandle);
                messageHookHandle = IntPtr.Zero;
            }
        }

        internal static object SendMarshaledMessage(IntPtr windowHandle, uint message, object parameter)
        {
            return SendMarshaledMessage(windowHandle, message, parameter, true);
        }

        internal static object SendMarshaledMessage(IntPtr windowHandle, uint message, object parameter, bool hookRequired)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return null;
            }

            MemoryStore store = MemoryStore.CreateStore(windowHandle);
            try
            {
                if (hookRequired)
                {
                    EnableHook(windowHandle);
                }

                return store.SendDataMessage(message, parameter);
            }
            finally
            {
                if (hookRequired)
                {
                    EnableHook(windowHandle);
                }

                store.Dispose();

                if (hookRequired)
                {
                    DisableHook();
                }
            }
        }

        internal static ControlProxy[] GetTopLevelWindows()
        {
            List<ControlProxy> topLevelWindows = new List<ControlProxy>();
            NativeMethods.EnumWindows(
                delegate (IntPtr handle, IntPtr lParam)
                {
                    topLevelWindows.Add(GetProxy(handle));
                    return true;
                },
                IntPtr.Zero);

            return topLevelWindows.ToArray();
        }

        internal static bool IsProcessAccessible(int processId)
        {
            if (processId == 0)
            {
                return false;
            }

            if (Environment.Is64BitOperatingSystem)
            {
                IntPtr handle = NativeMethods.OpenProcess(NativeMethods.ProcessAllAccess, false, processId);
                if (handle == IntPtr.Zero)
                {
                    return false;
                }

                try
                {
                    if (!NativeMethods.IsWow64Process(handle, out bool isWow64))
                    {
                        return false;
                    }

                    if (!isWow64 && !Environment.Is64BitProcess)
                    {
                        return false;
                    }
                }
                finally
                {
                    NativeMethods.CloseHandle(handle);
                }
            }

            try
            {
                using (Process process = Process.GetProcessById(processId))
                {
                    return process != null;
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        internal static bool IsManagedProcess(int processId)
        {
            if (managedProcesses.Contains(processId))
            {
                return true;
            }

            if (processId == 0 || unmanagedProcesses.Contains(processId))
            {
                return false;
            }

            Process process;
            ProcessModuleCollection modules;
            try
            {
                process = Process.GetProcessById(processId);
                modules = process.Modules;
            }
            catch (Win32Exception)
            {
                unmanagedProcesses.Add(processId);
                return false;
            }
            catch (ArgumentException)
            {
                unmanagedProcesses.Add(processId);
                return false;
            }
            catch (InvalidOperationException)
            {
                unmanagedProcesses.Add(processId);
                return false;
            }
            catch (NotSupportedException)
            {
                unmanagedProcesses.Add(processId);
                return false;
            }

            try
            {
                bool isManaged = false;
                bool isCompatibleRuntime = false;

                for (int i = 0; i < modules.Count; i++)
                {
                    ProcessModule module = modules[i];
                    string moduleName = module.ModuleName;

                    if (IsNetFrameworkRuntimeModule(moduleName))
                    {
                        isManaged = true;
                        isCompatibleRuntime = false;
                        break;
                    }

                    if (IsDotNetRuntimeModule(moduleName))
                    {
                        isManaged = true;
                        FileVersionInfo fileVersion = module.FileVersionInfo;
                        int runtimeMajorVersion = fileVersion == null ? 0 : fileVersion.FileMajorPart;
                        if (runtimeMajorVersion >= 10)
                        {
                            isCompatibleRuntime = true;
                            break;
                        }
                    }
                }

                if (isManaged && isCompatibleRuntime)
                {
                    managedProcesses.Add(processId);
                }
                else
                {
                    unmanagedProcesses.Add(processId);
                }

                return isManaged && isCompatibleRuntime;
            }
            finally
            {
                process.Dispose();
            }
        }

        internal static ControlProxy GetProxy(IntPtr windowHandle)
        {
            if (ProxyCache.TryGetValue(windowHandle, out ControlProxy cachedProxy))
            {
                return cachedProxy;
            }

            ControlProxy proxy = null;
            NativeMethods.GetWindowThreadProcessId(windowHandle, out uint processId);
            if (IsProcessAccessible((int)processId) && IsManagedProcess((int)processId))
            {
                List<object> parameters = new List<object> { EventWindow.Handle };
                proxy = SendMarshaledMessage(windowHandle, ManagedSpyMessages.GetProxy, parameters) as ControlProxy;
                if (proxy != null && !ProxyCache.ContainsKey(windowHandle))
                {
                    ProxyCache.Add(windowHandle, proxy);
                }
            }

            return proxy ?? new ControlProxy(windowHandle);
        }

        internal static void RemoveCachedProxiesForProcess(int processId)
        {
            if (processId == 0)
            {
                return;
            }

            managedProcesses.Remove(processId);
            unmanagedProcesses.Remove(processId);

            List<IntPtr> handlesToRemove = new List<IntPtr>();
            foreach (KeyValuePair<IntPtr, ControlProxy> proxyEntry in ProxyCache)
            {
                ControlProxy proxy = proxyEntry.Value;
                if (proxy != null && proxy.OwningProcessId == processId)
                {
                    handlesToRemove.Add(proxyEntry.Key);
                }
            }

            foreach (IntPtr handle in handlesToRemove)
            {
                ProxyCache.Remove(handle);
            }
        }

        internal static Delegate GetEventHandler(Type eventHandlerType, object instance)
        {
            if (instance == null || eventHandlerType == null)
            {
                return null;
            }

            return Delegate.CreateDelegate(
                eventHandlerType,
                instance,
                typeof(EventRegister).GetMethod(nameof(EventRegister.OnEventFired)),
                false);
        }

        internal static void SubscribeEvent(Control target, IntPtr eventWindow, string eventName, int eventCode)
        {
            EventRegister eventRegister = new EventRegister
            {
                EventCode = eventCode,
                SourceWindow = target,
                TargetEventReceiver = eventWindow,
                EventInfo = target?.GetType().GetEvent(eventName)
            };

            if (eventRegister.SourceWindow == null
                || eventRegister.TargetEventReceiver == IntPtr.Zero
                || eventRegister.EventInfo == null)
            {
                return;
            }

            if (!eventCallbacks.TryGetValue(target.Handle, out Dictionary<int, EventRegister> windowEventList))
            {
                windowEventList = new Dictionary<int, EventRegister>();
                eventCallbacks.Add(target.Handle, windowEventList);
            }

            if (windowEventList.ContainsKey(eventCode))
            {
                UnsubscribeEvent(target, eventCode);
            }

            Delegate handler = GetEventHandler(eventRegister.EventInfo.EventHandlerType, eventRegister);
            if (handler == null)
            {
                return;
            }

            eventRegister.EventInfo.AddEventHandler(eventRegister.SourceWindow, handler);
            windowEventList.Add(eventCode, eventRegister);
        }

        internal static void UnsubscribeEvent(Control target, int eventCode)
        {
            if (target == null || target.Handle == IntPtr.Zero)
            {
                return;
            }

            if (!eventCallbacks.TryGetValue(target.Handle, out Dictionary<int, EventRegister> windowEventList))
            {
                return;
            }

            if (!windowEventList.TryGetValue(eventCode, out EventRegister eventRegister))
            {
                return;
            }

            windowEventList.Remove(eventCode);
            if (eventRegister.EventInfo != null)
            {
                Delegate handler = GetEventHandler(eventRegister.EventInfo.EventHandlerType, eventRegister);
                if (handler != null)
                {
                    eventRegister.EventInfo.RemoveEventHandler(eventRegister.SourceWindow, handler);
                }
            }
        }

        internal static void OnMessage(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (lParam == IntPtr.Zero)
            {
                return;
            }

            NativeMethods.CwpStruct message = System.Runtime.InteropServices.Marshal.PtrToStructure<NativeMethods.CwpStruct>(lParam);

            if (message.message == ManagedSpyMessages.IsManaged)
            {
                Control control = Control.FromHandle(message.hwnd);
                MemoryStore store = MemoryStore.OpenStore(message);
                if (store != null)
                {
                    store.StoreReturnValue(control != null);
                }

                return;
            }

            if (message.message == ManagedSpyMessages.GetProxy)
            {
                MemoryStore store = MemoryStore.OpenStore(message);
                if (store == null)
                {
                    return;
                }

                List<object> parameters = store.GetParameters() as List<object>;
                if (parameters == null || parameters.Count != 1)
                {
                    return;
                }

                if (!ProxyCache.TryGetValue(message.hwnd, out ControlProxy proxy))
                {
                    Control control = Control.FromHandle(message.hwnd);
                    if (control != null)
                    {
                        proxy = new ControlProxy(control);
                    }
                }

                if (proxy != null)
                {
                    proxy.SetEventWindow((IntPtr)parameters[0]);
                    store.StoreReturnValue(proxy);
                }

                return;
            }

            if (message.message == ManagedSpyMessages.ReleaseMemory)
            {
                MemoryStore store = MemoryStore.OpenStore((int)message.wParam, (int)message.lParam, false);
                if (store != null)
                {
                    store.Release();
                }

                return;
            }

            if (message.message == ManagedSpyMessages.GetManagedProperty)
            {
                Control control = Control.FromHandle(message.hwnd);
                MemoryStore store = MemoryStore.OpenStore(message);
                if (control != null && store != null)
                {
                    string propertyName = store.GetParameters() as string;
                    PropertyDescriptor descriptor = TypeDescriptor.GetProperties(control)[propertyName];
                    if (descriptor != null)
                    {
                        store.StoreReturnValue(descriptor.GetValue(control));
                    }
                }

                return;
            }

            if (message.message == ManagedSpyMessages.GetManagedScreenRect)
            {
                Control control = Control.FromHandle(message.hwnd);
                MemoryStore store = MemoryStore.OpenStore(message);
                if (control != null && store != null)
                {
                    bool preferAccessibility;
                    Control target = ResolveManagedControlFromPath(control, store.GetParameters(), out preferAccessibility);
                    store.StoreReturnValue(ScreenBoundsHelper.GetControlScreenBounds(target, preferAccessibility));
                }

                return;
            }

            if (message.message == ManagedSpyMessages.ResetManagedProperty)
            {
                Control control = Control.FromHandle(message.hwnd);
                MemoryStore store = MemoryStore.OpenStore(message);
                if (control != null && store != null)
                {
                    string propertyName = store.GetParameters() as string;
                    PropertyDescriptor descriptor = TypeDescriptor.GetProperties(control)[propertyName];
                    descriptor?.ResetValue(control);
                }

                return;
            }

            if (message.message == ManagedSpyMessages.SetManagedProperty)
            {
                Control control = Control.FromHandle(message.hwnd);
                MemoryStore store = MemoryStore.OpenStore(message);
                if (control != null && store != null)
                {
                    List<object> parameters = store.GetParameters() as List<object>;
                    if (parameters != null && parameters.Count >= 2)
                    {
                        PropertyDescriptor descriptor = TypeDescriptor.GetProperties(control)[parameters[0] as string];
                        if (descriptor != null)
                        {
                            object value = parameters[1];
                            bool useInvariantString = parameters.Count >= 3 && parameters[2] is bool flag && flag;
                            if (useInvariantString)
                            {
                                string stringValue = value as string;
                                TypeConverter converter = descriptor.Converter;
                                if (stringValue == null)
                                {
                                    throw new ArgumentException("Invariant string payload was expected but not provided.");
                                }

                                if (converter == null || !converter.CanConvertFrom(typeof(string)))
                                {
                                    throw new InvalidOperationException(
                                        "Property '" + descriptor.Name + "' cannot convert from invariant string.");
                                }

                                value = converter.ConvertFromInvariantString(stringValue);
                            }

                            descriptor.SetValue(control, value);
                        }
                    }
                }

                return;
            }

            if (message.message == ManagedSpyMessages.SubscribeEvent)
            {
                Control control = Control.FromHandle(message.hwnd);
                MemoryStore store = MemoryStore.OpenStore(message);
                if (control != null && store != null)
                {
                    List<object> parameters = store.GetParameters() as List<object>;
                    if (parameters != null && parameters.Count == 3)
                    {
                        SubscribeEvent(control, (IntPtr)parameters[0], (string)parameters[1], (int)parameters[2]);
                    }
                }

                return;
            }

            if (message.message == ManagedSpyMessages.UnsubscribeEvent)
            {
                Control control = Control.FromHandle(message.hwnd);
                MemoryStore store = MemoryStore.OpenStore(message);
                if (control != null && store != null)
                {
                    List<object> parameters = store.GetParameters() as List<object>;
                    if (parameters != null && parameters.Count == 1)
                    {
                        UnsubscribeEvent(control, (int)parameters[0]);
                    }
                }
            }
        }

        private static IntPtr EnsureHookModuleLoaded()
        {
            if (hookModuleHandle != IntPtr.Zero)
            {
                return hookModuleHandle;
            }

            string hookPath = Path.Combine(AppContext.BaseDirectory, "ManagedSpyHook.dll");
            hookModuleHandle = NativeMethods.LoadLibrary(hookPath);
            return hookModuleHandle;
        }

        private static bool IsNetFrameworkRuntimeModule(string moduleName)
        {
            return string.Equals(moduleName, "mscorlib.dll", StringComparison.OrdinalIgnoreCase)
                || string.Equals(moduleName, "mscorlib.ni.dll", StringComparison.OrdinalIgnoreCase)
                || string.Equals(moduleName, "clr.dll", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDotNetRuntimeModule(string moduleName)
        {
            return string.Equals(moduleName, "coreclr.dll", StringComparison.OrdinalIgnoreCase)
                || string.Equals(moduleName, "System.Private.CoreLib.dll", StringComparison.OrdinalIgnoreCase)
                || string.Equals(moduleName, "System.Runtime.dll", StringComparison.OrdinalIgnoreCase);
        }

        private static Control GetManagedRoot(Control control)
        {
            Control current = control;
            while (current != null && current.Parent != null)
            {
                current = current.Parent;
            }

            return current;
        }

        private static Control ResolveManagedControlFromPath(Control source, object parameters, out bool preferAccessibility)
        {
            preferAccessibility = true;

            int[] path = parameters as int[];
            List<object> parameterList = parameters as List<object>;
            if (parameterList != null)
            {
                if (parameterList.Count > 0)
                {
                    path = parameterList[0] as int[];
                }

                if (parameterList.Count > 1 && parameterList[1] is bool prefer)
                {
                    preferAccessibility = prefer;
                }
            }

            if (source == null || path == null || path.Length == 0)
            {
                return source;
            }

            Control current = GetManagedRoot(source);
            if (current == null)
            {
                return source;
            }

            foreach (int childIndex in path)
            {
                if (childIndex < 0 || childIndex >= current.Controls.Count)
                {
                    return source;
                }

                current = current.Controls[childIndex];
                if (current == null)
                {
                    return source;
                }
            }

            return current;
        }
    }
}
