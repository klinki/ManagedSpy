using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedSpy.Tests
{
    [TestClass]
    public class ManagedSpyEndToEndTests
    {
        [TestMethod]
        [TestCategory("EndToEnd")]
        [Timeout(90000)]
        public void ManagedSpy_InspectsManagedWinFormsTarget_WithUiAutomation()
        {
            ArtifactTestEnvironment.RequireInteractiveDesktop();
            ArtifactTestEnvironment.RequireRunnableManagedSpyOutput();
            if (!File.Exists(ArtifactTestEnvironment.TestTargetExecutablePath))
            {
                Assert.Inconclusive("ManagedSpy.TestTarget.exe is required for the UIAutomation workflow.");
            }

            using ProcessScope target = ProcessScope.Start(ArtifactTestEnvironment.TestTargetExecutablePath);
            AutomationNode targetWindow = WaitForMainWindow(target.Process, ArtifactTestEnvironment.TestTargetWindowTitle);
            Assert.AreEqual(ArtifactTestEnvironment.TestTargetWindowTitle, targetWindow.Name);

            using ProcessScope spy = ProcessScope.Start(ArtifactTestEnvironment.ManagedSpyExecutablePath);
            AutomationNode spyWindow = WaitForMainWindow(spy.Process, ArtifactTestEnvironment.ManagedSpyWindowTitle);
            Assert.AreEqual(ArtifactTestEnvironment.ManagedSpyWindowTitle, spyWindow.Name);

            AutomationNode tree = WaitForDescendant(spyWindow, NativeUiAutomation.TreeControlTypeId, null);
            Assert.IsNotNull(tree, "ManagedSpy should expose its inspection tree through UIAutomation.");
            RefreshManagedSpyTree(spy.Process.MainWindowHandle);

            AutomationNode targetTreeItem = WaitForDescendant(
                tree,
                NativeUiAutomation.TreeItemControlTypeId,
                ArtifactTestEnvironment.TestTargetWindowTitle);
            if (targetTreeItem == null)
            {
                targetTreeItem = WaitForDescendant(
                    tree,
                    NativeUiAutomation.TreeItemControlTypeId,
                    "ManagedSpy.TestTarget");
            }
            Assert.IsNotNull(
                targetTreeItem,
                "ManagedSpy should list the target application in the inspection tree. Tree items: " +
                String.Join("; ", FindDescendantNames(tree, NativeUiAutomation.TreeItemControlTypeId, 20)));

            AutomationNode propertiesTab = WaitForDescendant(spyWindow, NativeUiAutomation.TabItemControlTypeId, "Properties");
            Assert.IsNotNull(propertiesTab, "ManagedSpy should expose the Properties tab through UIAutomation.");

            AutomationNode propertyGrid = WaitForDescendantByAutomationId(spyWindow, "propertyGrid");
            Assert.IsNotNull(propertyGrid, "ManagedSpy should expose the property grid surface.");
        }

        private static AutomationNode WaitForMainWindow(Process process, string expectedTitle)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(20);

            try
            {
                process.WaitForInputIdle(10000);
            }
            catch (InvalidOperationException)
            {
            }

            while (DateTime.UtcNow < deadline)
            {
                process.Refresh();
                Assert.IsFalse(process.HasExited, expectedTitle + " exited during startup.");

                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    AutomationNode window = NativeUiAutomation.FromHandle(process.MainWindowHandle);
                    if (window != null && window.Name == expectedTitle)
                    {
                        return window;
                    }
                }

                Thread.Sleep(200);
            }

            Assert.Fail("Timed out waiting for " + expectedTitle + " main window.");
            return null;
        }

        private static AutomationNode WaitForDescendant(AutomationNode root, int controlType, string nameContains)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(30);

            while (DateTime.UtcNow < deadline)
            {
                foreach (AutomationNode match in root.Descendants())
                {
                    if (match.ControlType != controlType)
                    {
                        continue;
                    }

                    if (nameContains == null || match.Name.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return match;
                    }
                }

                Thread.Sleep(250);
            }

            return null;
        }

        private static AutomationNode WaitForDescendantByAutomationId(AutomationNode root, string automationId)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(10);

            while (DateTime.UtcNow < deadline)
            {
                foreach (AutomationNode match in root.Descendants())
                {
                    if (String.Equals(match.AutomationId, automationId, StringComparison.Ordinal))
                    {
                        return match;
                    }
                }

                Thread.Sleep(250);
            }

            return null;
        }

        private static void RefreshManagedSpyTree(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            SetForegroundWindow(windowHandle);
            Thread.Sleep(250);
            SendKeys.SendWait("%v");
            Thread.Sleep(250);
            SendKeys.SendWait("r");
            Thread.Sleep(1000);
        }

        private static IEnumerable<string> FindDescendantNames(AutomationNode root, int controlType, int maximumCount)
        {
            int count = 0;
            foreach (AutomationNode match in root.Descendants())
            {
                if (match.ControlType != controlType)
                {
                    continue;
                }

                yield return String.IsNullOrEmpty(match.Name) ? "<empty>" : match.Name;
                count++;
                if (count >= maximumCount)
                {
                    yield break;
                }
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private sealed class ProcessScope : IDisposable
        {
            private ProcessScope(Process process)
            {
                Process = process;
            }

            public Process Process { get; }

            public static ProcessScope Start(string executablePath)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    WorkingDirectory = Path.GetDirectoryName(executablePath),
                    UseShellExecute = false
                };

                Process process = Process.Start(startInfo);
                Assert.IsNotNull(process, "Failed to start " + executablePath);
                return new ProcessScope(process);
            }

            public void Dispose()
            {
                if (Process == null)
                {
                    return;
                }

                try
                {
                    if (!Process.HasExited)
                    {
                        Process.CloseMainWindow();
                        if (!Process.WaitForExit(5000))
                        {
                            Process.Kill();
                            Process.WaitForExit(5000);
                        }
                    }
                }
                finally
                {
                    Process.Dispose();
                }
            }
        }
    }

    internal sealed class AutomationNode
    {
        private readonly NativeUiAutomation.IUIAutomationElement element;

        public AutomationNode(NativeUiAutomation.IUIAutomationElement element)
        {
            this.element = element;
        }

        public string Name
        {
            get { return GetStringProperty(NativeUiAutomation.NamePropertyId); }
        }

        public string AutomationId
        {
            get { return GetStringProperty(NativeUiAutomation.AutomationIdPropertyId); }
        }

        public int ControlType
        {
            get { return GetIntProperty(NativeUiAutomation.ControlTypePropertyId); }
        }

        public IEnumerable<AutomationNode> Descendants()
        {
            NativeUiAutomation.IUIAutomationElementArray matches;
            element.FindAll(
                NativeUiAutomation.TreeScope.Descendants,
                NativeUiAutomation.TrueCondition,
                out matches);

            int length;
            matches.get_Length(out length);
            for (int i = 0; i < length; i++)
            {
                NativeUiAutomation.IUIAutomationElement child;
                matches.GetElement(i, out child);
                if (child != null)
                {
                    yield return new AutomationNode(child);
                }
            }
        }

        private string GetStringProperty(int propertyId)
        {
            object value = GetProperty(propertyId);
            return value as string ?? String.Empty;
        }

        private int GetIntProperty(int propertyId)
        {
            object value = GetProperty(propertyId);
            return value is int ? (int)value : 0;
        }

        private object GetProperty(int propertyId)
        {
            object value;
            element.GetCurrentPropertyValue(propertyId, out value);
            return value;
        }
    }

    internal static class NativeUiAutomation
    {
        public const int NamePropertyId = 30005;
        public const int ControlTypePropertyId = 30003;
        public const int AutomationIdPropertyId = 30011;
        public const int TreeControlTypeId = 50023;
        public const int TreeItemControlTypeId = 50024;
        public const int TabItemControlTypeId = 50019;

        private static readonly IUIAutomation Automation = (IUIAutomation)new CUIAutomation();
        private static IUIAutomationCondition trueCondition;

        public static IUIAutomationCondition TrueCondition
        {
            get
            {
                if (trueCondition == null)
                {
                    Automation.CreateTrueCondition(out trueCondition);
                }
                return trueCondition;
            }
        }

        public static AutomationNode FromHandle(IntPtr windowHandle)
        {
            IUIAutomationElement element;
            Automation.ElementFromHandle(windowHandle, out element);
            return element == null ? null : new AutomationNode(element);
        }

        [Flags]
        public enum TreeScope
        {
            Element = 1,
            Children = 2,
            Descendants = 4,
            Subtree = Element | Children | Descendants
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UiaPoint
        {
            public int X;
            public int Y;
        }

        [ComImport]
        [Guid("FF48DBA4-60EF-4201-AA87-54103EEF594E")]
        private class CUIAutomation
        {
        }

        [ComImport]
        [Guid("30CBE57D-D9D0-452A-AB13-7AC5AC4825EE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IUIAutomation
        {
            void CompareElements(
                IUIAutomationElement element1,
                IUIAutomationElement element2,
                [MarshalAs(UnmanagedType.Bool)] out bool areSame);

            void CompareRuntimeIds(
                IntPtr runtimeId1,
                IntPtr runtimeId2,
                [MarshalAs(UnmanagedType.Bool)] out bool areSame);

            void GetRootElement(out IUIAutomationElement root);

            void ElementFromHandle(IntPtr hwnd, out IUIAutomationElement element);

            void ElementFromPoint(UiaPoint pt, out IUIAutomationElement element);

            void GetFocusedElement(out IUIAutomationElement element);

            void GetRootElementBuildCache(object cacheRequest, out IUIAutomationElement root);

            void ElementFromHandleBuildCache(IntPtr hwnd, object cacheRequest, out IUIAutomationElement element);

            void ElementFromPointBuildCache(UiaPoint pt, object cacheRequest, out IUIAutomationElement element);

            void GetFocusedElementBuildCache(object cacheRequest, out IUIAutomationElement element);

            void CreateTreeWalker(IUIAutomationCondition condition, out object walker);

            void get_ControlViewWalker(out object walker);

            void get_ContentViewWalker(out object walker);

            void get_RawViewWalker(out object walker);

            void get_RawViewCondition(out IUIAutomationCondition condition);

            void get_ControlViewCondition(out IUIAutomationCondition condition);

            void get_ContentViewCondition(out IUIAutomationCondition condition);

            void CreateCacheRequest(out object cacheRequest);

            void CreateTrueCondition(out IUIAutomationCondition condition);
        }

        [ComImport]
        [Guid("352FFBA8-0973-437C-A61F-F64CAFD81DF9")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IUIAutomationCondition
        {
        }

        [ComImport]
        [Guid("D22108AA-8AC5-49A5-837B-37BBB3D7591E")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IUIAutomationElement
        {
            void SetFocus();

            void GetRuntimeId(out IntPtr runtimeId);

            void FindFirst(TreeScope scope, IUIAutomationCondition condition, out IUIAutomationElement found);

            void FindAll(TreeScope scope, IUIAutomationCondition condition, out IUIAutomationElementArray found);

            void FindFirstBuildCache(TreeScope scope, IUIAutomationCondition condition, object cacheRequest, out IUIAutomationElement found);

            void FindAllBuildCache(TreeScope scope, IUIAutomationCondition condition, object cacheRequest, out IUIAutomationElementArray found);

            void BuildUpdatedCache(object cacheRequest, out IUIAutomationElement updatedElement);

            void GetCurrentPropertyValue(int propertyId, [MarshalAs(UnmanagedType.Struct)] out object retVal);
        }

        [ComImport]
        [Guid("14314595-B4BC-4055-95F2-58F2E42C9855")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IUIAutomationElementArray
        {
            void get_Length(out int length);

            void GetElement(int index, out IUIAutomationElement element);
        }
    }
}
