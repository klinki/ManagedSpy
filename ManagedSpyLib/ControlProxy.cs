using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Microsoft.ManagedSpy
{
    public sealed class ProxyEventArgs
    {
        internal ProxyEventArgs(EventDescriptor eventDescriptor, EventArgs eventArgs)
        {
            this.eventDescriptor = eventDescriptor;
            this.eventArgs = eventArgs;
        }

        public EventDescriptor eventDescriptor;
        public EventArgs eventArgs;
    }

    public delegate void ControlProxyEventHandler(object sender, ProxyEventArgs eventArgs);

    [Serializable]
    public sealed class ControlProxy : ICustomTypeDescriptor
    {
        private static List<Assembly> assemblies = new List<Assembly>();
        private static List<string> loadedAssemblies = new List<string>();
        private static bool subscribedAssemblyResolve;

        private string className;
        private string componentName;
        private string typeName;
        private int[] managedChildPath;
        private List<string> assemblyPaths;
        private int owningProcessId;

        [NonSerialized]
        private PropertyDescriptorCollection properties;

        [NonSerialized]
        private Type componentType;

        [NonSerialized]
        private EventDescriptorCollection eventsCache;

        [NonSerialized]
        private IntPtr eventWindowHandle;

        [NonSerialized]
        private IntPtr oldHandle;

        [field: NonSerialized]
        public event ControlProxyEventHandler EventFired;

        public static event Action<IntPtr> WindowDestroyed;

        public static event Action<IntPtr, IntPtr> HandleChanged;

        public ControlProxy()
        {
            EnsureAssemblyResolve();
        }

        internal ControlProxy(Control instance)
            : this()
        {
            if (instance == null)
            {
                return;
            }

            className = TypeDescriptor.GetClassName(instance);
            componentName = TypeDescriptor.GetComponentName(instance);
            if (string.IsNullOrEmpty(componentName))
            {
                PropertyDescriptor descriptor = TypeDescriptor.GetProperties(instance)["Name"];
                if (descriptor != null)
                {
                    componentName = descriptor.GetValue(instance) as string;
                }

                if (string.IsNullOrEmpty(componentName) && instance.Parent != null)
                {
                    FieldInfo[] fields = instance.Parent.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        object fieldValue = fields[i].GetValue(instance.Parent);
                        if (ReferenceEquals(fieldValue, instance))
                        {
                            componentName = fields[i].Name;
                        }
                    }
                }
            }

            Handle = instance.Handle;
            owningProcessId = Process.GetCurrentProcess().Id;
            typeName = instance.GetType().AssemblyQualifiedName;
            managedChildPath = BuildManagedChildPath(instance);

            Assembly[] domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            assemblyPaths = new List<string>(domainAssemblies.Length);
            foreach (Assembly assembly in domainAssemblies)
            {
                try
                {
                    assemblyPaths.Add(assembly.Location);
                }
                catch (NotSupportedException)
                {
                }
            }

            Desktop.ProxyCache[Handle] = this;

            instance.HandleCreated += OnHandleCreated;
            instance.HandleDestroyed += OnHandleDestroyed;
        }

        public ControlProxy(IntPtr windowHandle)
            : this()
        {
            Handle = windowHandle;
            owningProcessId = GetOwningProcessId(windowHandle);
            componentName = Handle.ToString();
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            StringBuilder classNameBuffer = new StringBuilder(255);
            if (NativeMethods.GetClassName(windowHandle, classNameBuffer, classNameBuffer.Capacity) != 0)
            {
                className = classNameBuffer.ToString();
            }
        }

        [Category("ManagedSpy Properties")]
        public IntPtr Handle { get; set; }

        [Browsable(false)]
        public int ManagedChildPathLength
        {
            get { return managedChildPath == null ? 0 : managedChildPath.Length; }
        }

        [Browsable(false)]
        public string ManagedChildPath
        {
            get
            {
                return managedChildPath == null || managedChildPath.Length == 0
                    ? String.Empty
                    : String.Join(".", managedChildPath);
            }
        }

        [Browsable(false)]
        public ControlProxy[] Children
        {
            get
            {
                if (Handle == IntPtr.Zero)
                {
                    return Array.Empty<ControlProxy>();
                }

                List<ControlProxy> childWindows = new List<ControlProxy>();
                NativeMethods.EnumChildWindows(
                    Handle,
                    delegate (IntPtr childHandle, IntPtr lParam)
                    {
                        if (NativeMethods.GetParent(childHandle) == Handle)
                        {
                            childWindows.Add(FromHandle(childHandle));
                        }

                        return true;
                    },
                    Handle);

                return childWindows.ToArray();
            }
        }

        [Browsable(false)]
        public int OwningProcessId
        {
            get
            {
                if (owningProcessId == 0)
                {
                    owningProcessId = GetOwningProcessId(Handle);
                }

                return owningProcessId;
            }
        }

        [Browsable(false)]
        public Process OwningProcess
        {
            get
            {
                try
                {
                    return Process.GetProcessById(OwningProcessId);
                }
                catch (ArgumentException)
                {
                    return null;
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
                catch (NotSupportedException)
                {
                    return null;
                }
            }
        }

        [Category("ManagedSpy Properties")]
        public bool IsManaged
        {
            get
            {
                if (Handle == IntPtr.Zero)
                {
                    return false;
                }

                using (Process process = OwningProcess)
                {
                    if (process == null)
                    {
                        return false;
                    }

                    if (!Desktop.IsProcessAccessible(process.Id) || !Desktop.IsManagedProcess(process.Id))
                    {
                        return false;
                    }
                }

                if (ComponentType != null || !string.IsNullOrEmpty(typeName))
                {
                    return true;
                }

                object result = Desktop.SendMarshaledMessage(Handle, ManagedSpyMessages.IsManaged, null);
                return result is bool isManaged && isManaged;
            }
        }

        [Category("ManagedSpy Properties")]
        public Type ComponentType
        {
            get
            {
                if (componentType == null && !string.IsNullOrEmpty(typeName))
                {
                    EnsureAssemblyResolve();
                    if (assemblyPaths != null)
                    {
                        foreach (string assemblyPath in assemblyPaths)
                        {
                            if (loadedAssemblies.Contains(assemblyPath))
                            {
                                continue;
                            }

                            loadedAssemblies.Add(assemblyPath);
                            try
                            {
                                assemblies.Add(LoadAssemblyWithoutLock(assemblyPath));
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }

                    componentType = Type.GetType(typeName);
                    typeName = null;
                }

                return componentType;
            }
        }

        [Browsable(false)]
        public ControlProxy Parent
        {
            get
            {
                IntPtr parentHandle = NativeMethods.GetParent(Handle);
                return parentHandle == IntPtr.Zero ? null : FromHandle(parentHandle);
            }
        }

        public void OnHandleCreated(object sender, EventArgs args)
        {
            Control control = sender as Control;
            if (control == null)
            {
                return;
            }

            Desktop.ProxyCache.Remove(control.Handle);
            if (oldHandle != IntPtr.Zero)
            {
                Desktop.ProxyCache.Remove(oldHandle);
            }

            Desktop.ProxyCache[control.Handle] = this;
            if (eventWindowHandle != IntPtr.Zero)
            {
                NativeMethods.SendMessage(eventWindowHandle, ManagedSpyMessages.HandleChanged, oldHandle, control.Handle);
            }

            oldHandle = IntPtr.Zero;
        }

        public void OnHandleDestroyed(object sender, EventArgs args)
        {
            Control control = sender as Control;
            if (control == null)
            {
                return;
            }

            if (control.RecreatingHandle)
            {
                oldHandle = control.Handle;
                return;
            }

            Desktop.ProxyCache.Remove(control.Handle);
            control.HandleCreated -= OnHandleCreated;
            control.HandleDestroyed -= OnHandleDestroyed;

            if (eventWindowHandle != IntPtr.Zero)
            {
                NativeMethods.SendMessage(eventWindowHandle, ManagedSpyMessages.WindowDestroyed, control.Handle, IntPtr.Zero);
            }
        }

        public string GetClassName()
        {
            return className;
        }

        public string GetComponentName()
        {
            return componentName;
        }

        public object GetValue(string propertyName)
        {
            PropertyDescriptor descriptor = GetProperties()[propertyName];
            return descriptor?.GetValue(this);
        }

        public void SetValue(string propertyName, object value)
        {
            PropertyDescriptor descriptor = GetProperties()[propertyName];
            descriptor?.SetValue(this, value);
        }

        public Rectangle GetScreenBounds()
        {
            return GetScreenBounds(true);
        }

        public Rectangle GetScreenBounds(bool preferAccessibility)
        {
            if (Handle == IntPtr.Zero)
            {
                return Rectangle.Empty;
            }

            List<object> parameters = null;
            if ((managedChildPath != null && managedChildPath.Length > 0) || !preferAccessibility)
            {
                parameters = new List<object>(2)
                {
                    managedChildPath,
                    preferAccessibility
                };
            }

            object result = Desktop.SendMarshaledMessage(Handle, ManagedSpyMessages.GetManagedScreenRect, parameters);
            return result is Rectangle rectangle ? rectangle : Rectangle.Empty;
        }

        public ControlLayoutInfo GetLayoutInfo()
        {
            if (Handle == IntPtr.Zero)
            {
                return null;
            }

            object parameters = null;
            if (managedChildPath != null && managedChildPath.Length > 0)
            {
                parameters = managedChildPath;
            }

            return Desktop.SendMarshaledMessage(Handle, ManagedSpyMessages.GetManagedLayout, parameters) as ControlLayoutInfo;
        }

        public Point PointToClient(Point point)
        {
            NativeMethods.PointNative[] points = { new NativeMethods.PointNative(point.X, point.Y) };
            NativeMethods.MapWindowPoints(IntPtr.Zero, Handle, points, 1);
            return points[0].ToPoint();
        }

        public Point PointToScreen(Point point)
        {
            NativeMethods.PointNative[] points = { new NativeMethods.PointNative(point.X, point.Y) };
            NativeMethods.MapWindowPoints(Handle, IntPtr.Zero, points, 1);
            return points[0].ToPoint();
        }

        public static ControlProxy FromHandle(IntPtr windowHandle)
        {
            return windowHandle == IntPtr.Zero ? null : Desktop.GetProxy(windowHandle);
        }

        public static ControlProxy[] TopLevelWindows => Desktop.GetTopLevelWindows();

        internal void RaiseEvent(ProxyEventArgs args)
        {
            EventFired?.Invoke(this, args);
        }

        internal static void NotifyWindowDestroyed(IntPtr windowHandle)
        {
            WindowDestroyed?.Invoke(windowHandle);
        }

        internal static void NotifyHandleChanged(IntPtr oldHandle, IntPtr newHandle)
        {
            HandleChanged?.Invoke(oldHandle, newHandle);
        }

        public static void DisconnectProcess(int processId)
        {
            Desktop.RemoveCachedProxiesForProcess(processId);
        }

        public void SubscribeEvent(EventDescriptor eventDescriptor)
        {
            EventDescriptorCollection eventDescriptors = GetEvents();
            if (eventDescriptors == null || eventDescriptors.Count == 0)
            {
                return;
            }

            int index = eventDescriptors.IndexOf(eventDescriptor);
            if (index < 0)
            {
                return;
            }

            List<object> parameters = new List<object>
            {
                Desktop.EventWindow.Handle,
                eventDescriptor.Name,
                index
            };
            Desktop.SendMarshaledMessage(Handle, ManagedSpyMessages.SubscribeEvent, parameters);
        }

        public void SubscribeEvent(string eventName)
        {
            SubscribeEvent(GetEvents()[eventName]);
        }

        public void UnsubscribeEvent(EventDescriptor eventDescriptor)
        {
            EventDescriptorCollection eventDescriptors = GetEvents();
            if (eventDescriptors == null || eventDescriptors.Count == 0)
            {
                return;
            }

            int index = eventDescriptors.IndexOf(eventDescriptor);
            if (index < 0)
            {
                return;
            }

            List<object> parameters = new List<object> { index };
            Desktop.SendMarshaledMessage(Handle, ManagedSpyMessages.UnsubscribeEvent, parameters);
        }

        public void UnsubscribeEvent(string eventName)
        {
            UnsubscribeEvent(GetEvents()[eventName]);
        }

        internal void SendMessage(int message, IntPtr wParam, IntPtr lParam)
        {
            NativeMethods.SendMessage(Handle, unchecked((uint)message), wParam, lParam);
        }

        internal void SetEventWindow(IntPtr eventWindow)
        {
            eventWindowHandle = eventWindow;
        }

        public AttributeCollection GetAttributes()
        {
            return ComponentType != null
                ? TypeDescriptor.GetAttributes(ComponentType)
                : AttributeCollection.Empty;
        }

        public TypeConverter GetConverter()
        {
            return ComponentType != null
                ? TypeDescriptor.GetConverter(ComponentType)
                : null;
        }

        public TypeConverter GetConverterFromRegisteredType()
        {
            return GetConverter();
        }

        public EventDescriptor GetDefaultEvent()
        {
            return ComponentType != null
                ? TypeDescriptor.GetDefaultEvent(ComponentType)
                : null;
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            if (ComponentType == null)
            {
                return null;
            }

            PropertyDescriptor descriptor = TypeDescriptor.GetDefaultProperty(ComponentType);
            return descriptor == null ? null : GetProperties()[descriptor.Name];
        }

        public object GetEditor(Type editorBaseType)
        {
            return ComponentType != null
                ? TypeDescriptor.GetEditor(ComponentType, editorBaseType)
                : null;
        }

        public EventDescriptorCollection GetEvents()
        {
            if (eventsCache == null)
            {
                eventsCache = ComponentType != null
                    ? TypeDescriptor.GetEvents(ComponentType)
                    : EventDescriptorCollection.Empty;
            }

            return eventsCache;
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return ComponentType != null
                ? TypeDescriptor.GetEvents(ComponentType, attributes)
                : EventDescriptorCollection.Empty;
        }

        public EventDescriptorCollection GetEventsFromRegisteredType()
        {
            return GetEvents();
        }

        public PropertyDescriptorCollection GetProperties()
        {
            return GetProperties(null);
        }

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            if (properties == null)
            {
                if (ComponentType != null)
                {
                    PropertyDescriptorCollection originalProperties = TypeDescriptor.GetProperties(ComponentType);
                    PropertyDescriptorCollection managedSpyProperties = TypeDescriptor.GetProperties(typeof(ControlProxy));
                    PropertyDescriptor[] combined = new PropertyDescriptor[originalProperties.Count + managedSpyProperties.Count];

                    int index = 0;
                    for (; index < originalProperties.Count; index++)
                    {
                        combined[index] = new PropertyDescriptorProxy(originalProperties[index]);
                    }

                    for (int propertyIndex = 0; propertyIndex < managedSpyProperties.Count; propertyIndex++, index++)
                    {
                        combined[index] = managedSpyProperties[propertyIndex];
                    }

                    properties = new PropertyDescriptorCollection(combined);
                }
                else
                {
                    properties = PropertyDescriptorCollection.Empty;
                }
            }

            PropertyDescriptorCollection filteredProperties = properties;
            if (properties != null && attributes != null && attributes.Length > 0)
            {
                List<PropertyDescriptor> visibleProperties = new List<PropertyDescriptor>();
                for (int i = 0; i < properties.Count; i++)
                {
                    bool hide = false;
                    for (int attributeIndex = 0; attributeIndex < attributes.Length; attributeIndex++)
                    {
                        if (ShouldHideMember(properties[i], attributes[attributeIndex]))
                        {
                            hide = true;
                            break;
                        }
                    }

                    if (!hide)
                    {
                        visibleProperties.Add(properties[i]);
                    }
                }

                if (visibleProperties.Count != properties.Count)
                {
                    filteredProperties = new PropertyDescriptorCollection(visibleProperties.ToArray());
                }
            }

            return filteredProperties;
        }

        public PropertyDescriptorCollection GetPropertiesFromRegisteredType()
        {
            return GetProperties();
        }

        public object GetPropertyOwner(PropertyDescriptor propertyDescriptor)
        {
            return this;
        }

        public bool? RequireRegisteredTypes => false;

        private static int[] BuildManagedChildPath(Control control)
        {
            if (control == null)
            {
                return Array.Empty<int>();
            }

            List<int> path = new List<int>();
            for (Control current = control; current != null && current.Parent != null; current = current.Parent)
            {
                int childIndex = current.Parent.Controls.IndexOf(current);
                if (childIndex < 0)
                {
                    return Array.Empty<int>();
                }

                path.Add(childIndex);
            }

            path.Reverse();
            return path.ToArray();
        }

        private static Assembly LoadAssemblyWithoutLock(string assemblyPath)
        {
            return Assembly.Load(File.ReadAllBytes(assemblyPath));
        }

        private static int GetOwningProcessId(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return 0;
            }

            NativeMethods.GetWindowThreadProcessId(windowHandle, out uint processId);
            return (int)processId;
        }

        private static void EnsureAssemblyResolve()
        {
            if (subscribedAssemblyResolve)
            {
                return;
            }

            subscribedAssemblyResolve = true;
            AppDomain.CurrentDomain.AssemblyResolve += ProxyResolveEventHandler;
        }

        private static Assembly ProxyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            if (args.Name == typeof(ControlProxy).Assembly.GetName().Name)
            {
                return typeof(ControlProxy).Assembly;
            }

            foreach (Assembly assembly in assemblies)
            {
                if (args.Name == assembly.FullName || args.Name == assembly.GetName().Name)
                {
                    return assembly;
                }
            }

            return null;
        }

        private static bool ShouldHideMember(MemberDescriptor member, Attribute attribute)
        {
            if (member == null || attribute == null)
            {
                return true;
            }

            Attribute memberAttribute = member.Attributes[attribute.GetType()];
            if (memberAttribute == null)
            {
                return !attribute.IsDefaultAttribute();
            }

            return !attribute.Match(memberAttribute);
        }
    }
}
