using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.ManagedSpy;

namespace ManagedSpy {
	/// <summary>
	/// This is the main window of ManagedSpy.
	/// Its a fairly simple Form containing a TreeView and TabControl.
	/// The TreeView contains processes and thier windows
	/// The TabControl contains properties and events.
	/// </summary>
	public partial class MainForm : Form
	{
		private const uint CWP_SKIPINVISIBLE = 0x0001;
		private const uint CWP_SKIPDISABLED = 0x0002;
		private const uint CWP_SKIPTRANSPARENT = 0x0004;
		private const int VK_LBUTTON = 0x01;
		private const int VK_ESCAPE = 0x1B;
		private const uint GW_HWNDPREV = 3;
		private const uint GA_ROOT = 2;
		private const uint RDW_INVALIDATE = 0x0001;
		private const uint RDW_ALLCHILDREN = 0x0080;
		private const uint RDW_UPDATENOW = 0x0100;
		private const uint RDW_FRAME = 0x0400;
		private const string ChildPlaceholderNodeKey = "__managedspy_placeholder";

		/// <summary>
		/// Currently selected proxy -- used for event logging.
		/// </summary>
		private ControlProxy currentProxy = null;
		private readonly EventFilterDialog dialog = new EventFilterDialog();
		private readonly System.Windows.Forms.Timer elementFinderTimer = new System.Windows.Forms.Timer();
		private readonly HighlightOverlayForm highlightOverlay = new HighlightOverlayForm();
		private readonly System.Windows.Forms.Timer persistentHighlightTimer = new System.Windows.Forms.Timer();
		private readonly HighlightOverlayForm layoutHighlightOverlay = new HighlightOverlayForm(true, LayoutViewControl.GetSectionAccentColor(LayoutSection.Element));
		private ToolStripButton tsButtonFindElement = null;
		private ToolStripButton tsButtonApplyProperty = null;
		private ToolStripMenuItem findElementToolStripMenuItem = null;
		private ToolStripMenuItem applyPropertyToolStripMenuItem = null;
		private ToolStripMenuItem refreshSubtreeToolStripMenuItem = null;
		private ToolStripMenuItem keepHighlightedToolStripMenuItem = null;
		private bool isElementFinderActive = false;
		private bool isLeftButtonPressed = false;
		private bool isUpdatingFinderUiState = false;
		private bool isExpandingElementFinderPath = false;
		private bool isProcessingFinderSelection = false;
		private TreeNode treeMenuTargetNode = null;
		private IntPtr highlightedWindowHandle = IntPtr.Zero;
		private Rectangle highlightedWindowRectangle = Rectangle.Empty;
		private readonly Dictionary<IntPtr, PersistentHighlightTarget> persistentHighlights = new Dictionary<IntPtr, PersistentHighlightTarget>();
		private readonly Color[] persistentHighlightPalette =
		{
			Color.FromArgb(0, 153, 255),
			Color.FromArgb(255, 140, 0),
			Color.FromArgb(137, 87, 229),
			Color.FromArgb(0, 170, 85),
			Color.FromArgb(230, 82, 140),
			Color.FromArgb(0, 180, 180),
			Color.FromArgb(185, 140, 0),
			Color.FromArgb(100, 150, 255)
		};
		private int nextPersistentHighlightColorIndex = 0;
		private ControlProxy currentLayoutProxy = null;
		private ControlLayoutInfo currentLayoutInfo = null;
		private readonly Dictionary<int, Process> trackedProcesses = new Dictionary<int, Process>();
		private CancellationTokenSource refreshCancellationSource = null;
		private Task currentRefreshTask = Task.CompletedTask;
		private bool isRefreshRunning = false;
		private static readonly object persistentHighlightDiagnosticsSync = new object();
		private static readonly string persistentHighlightDiagnosticsPath = Path.Combine(
			AppDomain.CurrentDomain.BaseDirectory,
			"ManagedSpy-highlight-diagnostics.log");

        public MainForm() {
			InitializeComponent();
			InitializeElementFinder();
			InitializePropertyApply();
			InitializeTreeContextMenu();
			ControlProxy.WindowDestroyed += ControlProxy_WindowDestroyed;
			ControlProxy.HandleChanged += ControlProxy_HandleChanged;
			layoutView.HoveredSectionChanged += layoutView_HoveredSectionChanged;
			tabControl1.SelectedIndexChanged += tabControl1_SelectedIndexChanged;
			Deactivate += MainForm_Deactivate;
        }

		private sealed class RefreshSnapshot
		{
			public RefreshSnapshot(List<RefreshWindowSnapshot> windows)
			{
				Windows = windows;
			}

			public List<RefreshWindowSnapshot> Windows { get; private set; }
		}

		private sealed class RefreshWindowSnapshot
		{
			public ControlProxy Proxy { get; set; }

			public int ProcessId { get; set; }

			public string ProcessName { get; set; }

			public string MainWindowTitle { get; set; }

			public string NodeText { get; set; }
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;

			public Rectangle ToRectangle()
			{
				return Rectangle.FromLTRB(Left, Top, Right, Bottom);
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct POINT
		{
			public int X;
			public int Y;

			public Point ToPoint()
			{
				return new Point(X, Y);
			}
		}

		[DllImport("user32.dll")]
		private static extern IntPtr WindowFromPoint(Point point);

		[DllImport("user32.dll")]
		private static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

		[DllImport("user32.dll")]
		private static extern IntPtr ChildWindowFromPointEx(IntPtr hwndParent, Point pt, uint uFlags);

		[DllImport("user32.dll")]
		private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

		[DllImport("user32.dll")]
		private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

		[DllImport("user32.dll")]
		private static extern IntPtr GetParent(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("user32.dll")]
		private static extern uint GetDpiForWindow(IntPtr hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool PhysicalToLogicalPointForPerMonitorDPI(IntPtr hWnd, ref POINT lpPoint);

		[DllImport("user32.dll")]
		private static extern bool GetCursorPos(out POINT lpPoint);

		[DllImport("user32.dll")]
		private static extern short GetAsyncKeyState(int vKey);

		private sealed class PersistentHighlightDebugInfo
		{
			public IntPtr CoordinateReferenceHandle = IntPtr.Zero;
			public IntPtr ProxyHandle = IntPtr.Zero;
			public IntPtr ParentWindow = IntPtr.Zero;
			public IntPtr RootWindow = IntPtr.Zero;
			public uint TargetProcessId;
			public int ManagedSpyProcessId;
			public int ManagedChildPathLength;
			public string ManagedChildPath = String.Empty;
			public Rectangle PreferredSourceRectangle = Rectangle.Empty;
			public Rectangle PreferredRectangle = Rectangle.Empty;
			public bool PreferredNormalizationChanged;
			public bool PreferredRawWindowFallback;
			public Rectangle NonAccessibleSourceRectangle = Rectangle.Empty;
			public Rectangle NonAccessibleRectangle = Rectangle.Empty;
			public bool NonAccessibleNormalizationChanged;
			public bool NonAccessibleRawWindowFallback;
			public Rectangle RawWindowRectangle = Rectangle.Empty;
			public Rectangle ParentWindowRectangle = Rectangle.Empty;
			public Rectangle RootWindowRectangle = Rectangle.Empty;
			public Rectangle ChosenRectangle = Rectangle.Empty;
			public string ChosenSource = "none";
			public bool RawEqualsParent;
			public bool RawEqualsRoot;
			public string CoordinateReferenceDpi = "unknown";
			public string ProxyDpi = "unknown";
			public string ParentDpi = "unknown";
			public string RootDpi = "unknown";
		}

		private sealed class PersistentHighlightTarget
		{
			public PersistentHighlightTarget(ControlProxy proxy, Color color)
			{
				Proxy = proxy;
				Color = color;
				Overlay = new HighlightOverlayForm(false, color);
			}

			public ControlProxy Proxy;
			public Color Color;
			public Rectangle Rectangle = Rectangle.Empty;
			public HighlightOverlayForm Overlay;
		}

		private void InitializeElementFinder()
		{
			elementFinderTimer.Interval = 80;
			elementFinderTimer.Tick += new EventHandler(elementFinderTimer_Tick);

			findElementToolStripMenuItem = new ToolStripMenuItem("Find Element");
			findElementToolStripMenuItem.CheckOnClick = true;
			findElementToolStripMenuItem.ToolTipText = "Select an element on screen and focus it in the tree.";
			findElementToolStripMenuItem.Click += new EventHandler(findElementToolStripMenuItem_Click);
			viewToolStripMenuItem.DropDownItems.Insert(2, findElementToolStripMenuItem);

			tsButtonFindElement = new ToolStripButton();
			tsButtonFindElement.CheckOnClick = true;
			tsButtonFindElement.DisplayStyle = ToolStripItemDisplayStyle.Image;
			tsButtonFindElement.Image = ManagedSpy.Properties.Resources.search;
			tsButtonFindElement.ImageTransparentColor = Color.Magenta;
			tsButtonFindElement.ToolTipText = "Find element on screen";
			tsButtonFindElement.Click += new EventHandler(tsButtonFindElement_Click);
			toolStrip1.Items.Insert(2, tsButtonFindElement);
		}

		private void InitializePropertyApply()
		{
			applyPropertyToolStripMenuItem = new ToolStripMenuItem("Apply Property");
			applyPropertyToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Enter;
			applyPropertyToolStripMenuItem.ToolTipText = "Apply selected property value to the target control.";
			applyPropertyToolStripMenuItem.Click += new EventHandler(applyPropertyToolStripMenuItem_Click);
			viewToolStripMenuItem.DropDownItems.Insert(3, applyPropertyToolStripMenuItem);

			tsButtonApplyProperty = new ToolStripButton();
			tsButtonApplyProperty.DisplayStyle = ToolStripItemDisplayStyle.Text;
			tsButtonApplyProperty.Text = "Apply";
			tsButtonApplyProperty.ToolTipText = "Apply selected property value";
			tsButtonApplyProperty.Click += new EventHandler(tsButtonApplyProperty_Click);
			toolStrip1.Items.Insert(3, tsButtonApplyProperty);
		}

		private void InitializeTreeContextMenu()
		{
			refreshSubtreeToolStripMenuItem = new ToolStripMenuItem("Refresh Subtree");
			refreshSubtreeToolStripMenuItem.ToolTipText = "Refresh selected component and all of its descendants.";
			refreshSubtreeToolStripMenuItem.Click += new EventHandler(refreshSubtreeToolStripMenuItem_Click);

			keepHighlightedToolStripMenuItem = new ToolStripMenuItem("Keep Highlighted");
			keepHighlightedToolStripMenuItem.CheckOnClick = true;
			keepHighlightedToolStripMenuItem.ToolTipText = "Keep the selected component highlighted on screen.";
			keepHighlightedToolStripMenuItem.Click += new EventHandler(keepHighlightedToolStripMenuItem_Click);

			treeMenuStrip.Items.Add(new ToolStripSeparator());
			treeMenuStrip.Items.Add(refreshSubtreeToolStripMenuItem);
			treeMenuStrip.Items.Add(keepHighlightedToolStripMenuItem);
			treeMenuStrip.Opening += new CancelEventHandler(treeMenuStrip_Opening);
			treeMenuStrip.Closed += new ToolStripDropDownClosedEventHandler(treeMenuStrip_Closed);

			persistentHighlightTimer.Interval = 80;
			persistentHighlightTimer.Tick += new EventHandler(persistentHighlightTimer_Tick);
		}

		private TreeNode GetTreeMenuTargetNode()
		{
			return treeMenuTargetNode ?? treeWindow.SelectedNode;
		}

		private void SetTreeMenuTargetNode(TreeNode node)
		{
			treeMenuTargetNode = node;
			if (node != null && treeWindow.SelectedNode != node)
			{
				treeWindow.SelectedNode = node;
			}
		}

		private static ControlProxy GetNodeProxy(TreeNode node)
		{
			return node == null ? null : node.Tag as ControlProxy;
		}

		private static string GetProxyNodeText(ControlProxy proxy)
		{
			string name = String.IsNullOrEmpty(proxy.GetComponentName()) ? "<noname>" : proxy.GetComponentName();
			return name + "     [" + proxy.GetClassName() + "]";
		}

		private static TreeNode CreateProxyNode(ControlProxy proxy)
		{
			TreeNode node = new TreeNode(GetProxyNodeText(proxy));
			node.Name = proxy.Handle.ToString();
			node.Tag = proxy;
			AddChildPlaceholder(node);
			return node;
		}

		private static void AddChildPlaceholder(TreeNode node)
		{
			if (node != null && node.Nodes.Count == 0)
			{
				node.Nodes.Add(ChildPlaceholderNodeKey, String.Empty);
			}
		}

		private static TreeNode AddProxyNodeIfMissing(TreeNodeCollection nodes, ControlProxy proxy)
		{
			if (proxy == null)
			{
				return null;
			}

			string key = proxy.Handle.ToString();
			if (nodes.ContainsKey(key))
			{
				return null;
			}

			TreeNode node = CreateProxyNode(proxy);
			nodes.Add(node);
			return node;
		}

		private static void CaptureExpandedControlHandles(TreeNode node, HashSet<IntPtr> handles)
		{
			foreach (TreeNode child in node.Nodes)
			{
				ControlProxy childProxy = GetNodeProxy(child);
				if (childProxy != null && child.IsExpanded)
				{
					handles.Add(childProxy.Handle);
				}
				CaptureExpandedControlHandles(child, handles);
			}
		}

		private static void RestoreExpandedControlHandles(TreeNode node, HashSet<IntPtr> handles)
		{
			foreach (TreeNode child in node.Nodes)
			{
				ControlProxy childProxy = GetNodeProxy(child);
				if (childProxy != null && handles.Contains(childProxy.Handle))
				{
					child.Expand();
				}
				RestoreExpandedControlHandles(child, handles);
			}
		}

		private static TreeNode FindNodeByHandle(TreeNode node, IntPtr handle)
		{
			ControlProxy proxy = GetNodeProxy(node);
			if (proxy != null && proxy.Handle == handle)
			{
				return node;
			}

			foreach (TreeNode child in node.Nodes)
			{
				TreeNode match = FindNodeByHandle(child, handle);
				if (match != null)
				{
					return match;
				}
			}

			return null;
		}

		private void RebuildControlSubtree(TreeNode parentNode)
		{
			ControlProxy parentProxy = GetNodeProxy(parentNode);
			if (parentProxy == null)
			{
				return;
			}

			parentNode.Nodes.Clear();
			foreach (ControlProxy childProxy in GetProxyChildren(parentProxy))
			{
				TreeNode childNode = AddProxyNodeIfMissing(parentNode.Nodes, childProxy);
				if (childNode != null)
				{
					RebuildControlSubtree(childNode);
				}
			}
		}

		private static void PopulateProxyChildren(TreeNode parentNode)
		{
			ControlProxy parentProxy = GetNodeProxy(parentNode);
			if (parentProxy == null)
			{
				return;
			}

			parentNode.Nodes.Clear();
			foreach (ControlProxy childProxy in GetProxyChildren(parentProxy))
			{
				AddProxyNodeIfMissing(parentNode.Nodes, childProxy);
			}
		}

		private static ControlProxy[] GetProxyChildren(ControlProxy proxy)
		{
			if (proxy == null)
			{
				return Array.Empty<ControlProxy>();
			}

			return proxy.Children ?? Array.Empty<ControlProxy>();
		}

		private void RefreshSelectedSubtree()
		{
			RefreshSubtree(GetTreeMenuTargetNode());
		}

		private void RefreshSubtree(TreeNode rootNode)
		{
			ControlProxy rootProxy = GetNodeProxy(rootNode);
			if (rootProxy == null)
			{
				return;
			}

			IntPtr selectedHandle = rootProxy.Handle;
			HashSet<IntPtr> expandedHandles = new HashSet<IntPtr>();
			CaptureExpandedControlHandles(rootNode, expandedHandles);
			if (rootNode.IsExpanded)
			{
				expandedHandles.Add(rootProxy.Handle);
			}

			treeWindow.BeginUpdate();
			try
			{
				RebuildControlSubtree(rootNode);
				RestoreExpandedControlHandles(rootNode, expandedHandles);
				if (expandedHandles.Contains(rootProxy.Handle))
				{
					rootNode.Expand();
				}
			}
			finally
			{
				treeWindow.EndUpdate();
			}

			TreeNode selectedNode = FindNodeByHandle(rootNode, selectedHandle);
			if (selectedNode != null)
			{
				treeWindow.SelectedNode = selectedNode;
				selectedNode.EnsureVisible();
			}

			toolStripStatusLabel1.Text = "Refreshed subtree: " + rootNode.Text;
		}

		private void RunAfterTreeMenuClose(Action action)
		{
			if (action == null)
			{
				return;
			}

			BeginInvoke((MethodInvoker)delegate
			{
				action();
			});
		}

		private void EnablePersistentHighlight(ControlProxy proxy)
		{
			if (proxy == null || proxy.Handle == IntPtr.Zero)
			{
				return;
			}

			if (persistentHighlights.ContainsKey(proxy.Handle))
			{
				return;
			}

			PersistentHighlightTarget target = new PersistentHighlightTarget(proxy, GetNextPersistentHighlightColor());
			persistentHighlights.Add(proxy.Handle, target);
			UpdatePersistentHighlight(target, true, IntPtr.Zero);
			persistentHighlightTimer.Start();
		}

		private void DisablePersistentHighlight(ControlProxy proxy)
		{
			if (proxy == null)
			{
				return;
			}

			RemovePersistentHighlight(proxy.Handle);
		}

		private void DisableAllPersistentHighlights()
		{
			foreach (PersistentHighlightTarget target in new List<PersistentHighlightTarget>(persistentHighlights.Values))
			{
				DisposePersistentHighlightTarget(target);
			}

			persistentHighlights.Clear();
			persistentHighlightTimer.Stop();
		}

		private bool RemovePersistentHighlight(IntPtr handle)
		{
			if (!persistentHighlights.TryGetValue(handle, out PersistentHighlightTarget target))
			{
				return false;
			}

			persistentHighlights.Remove(handle);
			DisposePersistentHighlightTarget(target);
			RequestTargetWindowRedraw(handle);
			if (persistentHighlights.Count == 0)
			{
				persistentHighlightTimer.Stop();
			}

			return true;
		}

		private bool IsPersistentHighlightEnabled(ControlProxy proxy)
		{
			return proxy != null && persistentHighlights.ContainsKey(proxy.Handle);
		}

		private Color GetNextPersistentHighlightColor()
		{
			HashSet<int> activeColors = new HashSet<int>();
			foreach (PersistentHighlightTarget target in persistentHighlights.Values)
			{
				activeColors.Add(target.Color.ToArgb());
			}

			for (int offset = 0; offset < persistentHighlightPalette.Length; offset++)
			{
				int paletteIndex = (nextPersistentHighlightColorIndex + offset) % persistentHighlightPalette.Length;
				Color paletteColor = persistentHighlightPalette[paletteIndex];
				if (!activeColors.Contains(paletteColor.ToArgb()))
				{
					nextPersistentHighlightColorIndex = paletteIndex + 1;
					return paletteColor;
				}
			}

			Color generatedColor;
			int attempts = 0;
			do
			{
				generatedColor = CreateGeneratedPersistentHighlightColor(nextPersistentHighlightColorIndex++);
				attempts++;
			}
			while (activeColors.Contains(generatedColor.ToArgb()) && attempts < 720);

			return generatedColor;
		}

		private static Color CreateGeneratedPersistentHighlightColor(int index)
		{
			double hue = (index * 137.508) % 360;
			return ColorFromHsl(hue, 0.85, 0.45);
		}

		private static Color ColorFromHsl(double hue, double saturation, double lightness)
		{
			double chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
			double huePrime = hue / 60.0;
			double secondary = chroma * (1 - Math.Abs(huePrime % 2 - 1));
			double red = 0;
			double green = 0;
			double blue = 0;

			if (huePrime < 1)
			{
				red = chroma;
				green = secondary;
			}
			else if (huePrime < 2)
			{
				red = secondary;
				green = chroma;
			}
			else if (huePrime < 3)
			{
				green = chroma;
				blue = secondary;
			}
			else if (huePrime < 4)
			{
				green = secondary;
				blue = chroma;
			}
			else if (huePrime < 5)
			{
				red = secondary;
				blue = chroma;
			}
			else
			{
				red = chroma;
				blue = secondary;
			}

			double match = lightness - chroma / 2;
			return Color.FromArgb(
				255,
				(int)Math.Round((red + match) * 255),
				(int)Math.Round((green + match) * 255),
				(int)Math.Round((blue + match) * 255));
		}

		private static void DisposePersistentHighlightTarget(PersistentHighlightTarget target)
		{
			if (target == null || target.Overlay == null)
			{
				return;
			}

			target.Overlay.HideHighlight();
			target.Overlay.Dispose();
			target.Overlay = null;
			target.Rectangle = Rectangle.Empty;
		}

		private void ControlProxy_WindowDestroyed(IntPtr destroyedHandle)
		{
			if (destroyedHandle == IntPtr.Zero || IsDisposed)
			{
				return;
			}

			if (InvokeRequired)
			{
				if (IsHandleCreated)
				{
					BeginInvoke((MethodInvoker)delegate
					{
						ControlProxy_WindowDestroyed(destroyedHandle);
					});
				}
				return;
			}

			bool wasPersistentHighlightTarget = RemovePersistentHighlight(destroyedHandle);
			if (wasPersistentHighlightTarget)
			{
				toolStripStatusLabel1.Text = "Persistent highlight target closed.";
			}

			if (currentLayoutProxy != null && currentLayoutProxy.Handle == destroyedHandle)
			{
				ClearLayoutTab();
			}

			if (RemoveProxyNodesByHandle(destroyedHandle) && !wasPersistentHighlightTarget)
			{
				toolStripStatusLabel1.Text = "Removed closed target from tree.";
			}
		}

		private Process TrackProcess(Process process)
		{
			if (process == null)
			{
				return null;
			}

			if (trackedProcesses.TryGetValue(process.Id, out Process trackedProcess))
			{
				process.Dispose();
				return trackedProcess;
			}

			try
			{
				process.EnableRaisingEvents = true;
				process.Exited += targetProcess_Exited;
				trackedProcesses.Add(process.Id, process);
				return process;
			}
			catch (InvalidOperationException)
			{
				process.Dispose();
				return null;
			}
		}

		private void UntrackProcess(int processId)
		{
			if (!trackedProcesses.TryGetValue(processId, out Process process))
			{
				return;
			}

			trackedProcesses.Remove(processId);
			process.Exited -= targetProcess_Exited;
			process.Dispose();
		}

		private void ClearTrackedProcesses()
		{
			List<int> processIds = new List<int>(trackedProcesses.Keys);
			foreach (int processId in processIds)
			{
				UntrackProcess(processId);
			}
		}

		private void targetProcess_Exited(object sender, EventArgs e)
		{
			Process process = sender as Process;
			if (process == null || IsDisposed)
			{
				return;
			}

			int processId;
			try
			{
				processId = process.Id;
			}
			catch (InvalidOperationException)
			{
				return;
			}

			if (InvokeRequired)
			{
				if (IsHandleCreated)
				{
					BeginInvoke((MethodInvoker)delegate
					{
						DisconnectExitedTargetProcess(processId);
					});
				}
				return;
			}

			DisconnectExitedTargetProcess(processId);
		}

		private void DisconnectExitedTargetProcess(int processId)
		{
			UntrackProcess(processId);
			ControlProxy.DisconnectProcess(processId);

			TreeNode processNode = treeWindow.Nodes[processId.ToString()];
			if (processNode == null)
			{
				return;
			}

			bool removedSelection = IsNodeOrDescendant(processNode, treeWindow.SelectedNode);
			bool removedCurrentProxy =
				currentProxy != null &&
				FindNodeByHandle(processNode, currentProxy.Handle) != null;
			bool removedPersistentHighlight = RemovePersistentHighlightsInProcessNode(processNode);
			bool removedLayout =
				currentLayoutProxy != null &&
				FindNodeByHandle(processNode, currentLayoutProxy.Handle) != null;

			if (removedCurrentProxy)
			{
				StopLogging();
				currentProxy = null;
				eventGrid.Rows.Clear();
			}

			if (removedPersistentHighlight)
			{
				toolStripStatusLabel1.Text = "Persistent highlight target closed.";
			}

			if (removedLayout)
			{
				ClearLayoutTab();
			}

			processNode.Remove();
			if (removedSelection)
			{
				propertyGrid.SelectedObject = null;
			}

			toolStripStatusLabel1.Text = "Disconnected exited target process.";
		}

		private void ControlProxy_HandleChanged(IntPtr oldHandle, IntPtr newHandle)
		{
			if (oldHandle == IntPtr.Zero || IsDisposed)
			{
				return;
			}

			if (InvokeRequired)
			{
				if (IsHandleCreated)
				{
					BeginInvoke((MethodInvoker)delegate
					{
						ControlProxy_HandleChanged(oldHandle, newHandle);
					});
				}
				return;
			}

			UpdateProxyHandleReferences(oldHandle, newHandle);
			UpdatePersistentHighlightHandle(oldHandle, newHandle);
			if (currentLayoutProxy != null && (currentLayoutProxy.Handle == oldHandle || currentLayoutProxy.Handle == newHandle))
			{
				currentLayoutProxy.Handle = newHandle;
				UpdateLayoutTab(currentLayoutProxy);
			}
		}

		private void UpdatePersistentHighlightHandle(IntPtr oldHandle, IntPtr newHandle)
		{
			if (!persistentHighlights.TryGetValue(oldHandle, out PersistentHighlightTarget target))
			{
				return;
			}

			persistentHighlights.Remove(oldHandle);
			target.Proxy.Handle = newHandle;
			target.Rectangle = Rectangle.Empty;
			target.Overlay.HideHighlight();
			if (newHandle != IntPtr.Zero)
			{
				persistentHighlights[newHandle] = target;
				UpdatePersistentHighlight(target, true, oldHandle);
			}

			RequestTargetWindowRedraw(oldHandle);
			RequestTargetWindowRedraw(newHandle);
			if (persistentHighlights.Count == 0)
			{
				persistentHighlightTimer.Stop();
			}
		}

		private bool RemovePersistentHighlightsInProcessNode(TreeNode processNode)
		{
			List<IntPtr> handlesToRemove = new List<IntPtr>();
			foreach (IntPtr handle in persistentHighlights.Keys)
			{
				if (FindNodeByHandle(processNode, handle) != null)
				{
					handlesToRemove.Add(handle);
				}
			}

			foreach (IntPtr handle in handlesToRemove)
			{
				RemovePersistentHighlight(handle);
			}

			return handlesToRemove.Count > 0;
		}

		private bool RemoveProxyNodesByHandle(IntPtr handle)
		{
			bool removedSelectedNode = false;
			bool removedAnyNode;
			treeWindow.BeginUpdate();
			try
			{
				removedAnyNode = RemoveProxyNodesByHandle(treeWindow.Nodes, handle, ref removedSelectedNode);
			}
			finally
			{
				treeWindow.EndUpdate();
			}

			if (removedSelectedNode)
			{
				propertyGrid.SelectedObject = treeWindow.SelectedNode == null ? null : treeWindow.SelectedNode.Tag;
			}

			return removedAnyNode;
		}

		private bool RemoveProxyNodesByHandle(TreeNodeCollection nodes, IntPtr handle, ref bool removedSelectedNode)
		{
			bool removedAnyNode = false;
			for (int i = nodes.Count - 1; i >= 0; i--)
			{
				TreeNode node = nodes[i];
				if (RemoveProxyNodesByHandle(node.Nodes, handle, ref removedSelectedNode))
				{
					removedAnyNode = true;
				}

				ControlProxy proxy = GetNodeProxy(node);
				if (proxy == null || proxy.Handle != handle)
				{
					continue;
				}

				if (IsNodeOrDescendant(node, treeWindow.SelectedNode))
				{
					removedSelectedNode = true;
				}
				node.Remove();
				removedAnyNode = true;
			}

			return removedAnyNode;
		}

		private void UpdateProxyHandleReferences(IntPtr oldHandle, IntPtr newHandle)
		{
			bool removedSelectedNode = false;
			treeWindow.BeginUpdate();
			try
			{
				UpdateProxyHandleReferences(treeWindow.Nodes, oldHandle, newHandle);
				RemoveDuplicateProxyNodes(treeWindow.Nodes, ref removedSelectedNode);
			}
			finally
			{
				treeWindow.EndUpdate();
			}

			if (removedSelectedNode)
			{
				propertyGrid.SelectedObject = treeWindow.SelectedNode == null ? null : treeWindow.SelectedNode.Tag;
			}
		}

		private void UpdateProxyHandleReferences(TreeNodeCollection nodes, IntPtr oldHandle, IntPtr newHandle)
		{
			string oldKey = oldHandle.ToString();
			string newKey = newHandle.ToString();
			foreach (TreeNode node in nodes)
			{
				ControlProxy proxy = GetNodeProxy(node);
				if (proxy != null && (proxy.Handle == oldHandle || node.Name == oldKey))
				{
					proxy.Handle = newHandle;
					node.Name = newKey;
				}

				UpdateProxyHandleReferences(node.Nodes, oldHandle, newHandle);
			}
		}

		private bool RemoveDuplicateProxyNodes(TreeNodeCollection nodes, ref bool removedSelectedNode)
		{
			bool removedAnyNode = false;
			HashSet<IntPtr> siblingHandles = new HashSet<IntPtr>();
			for (int i = 0; i < nodes.Count; i++)
			{
				TreeNode node = nodes[i];
				ControlProxy proxy = GetNodeProxy(node);
				if (proxy != null &&
					proxy.Handle != IntPtr.Zero &&
					!siblingHandles.Add(proxy.Handle))
				{
					if (IsNodeOrDescendant(node, treeWindow.SelectedNode))
					{
						removedSelectedNode = true;
					}

					node.Remove();
					removedAnyNode = true;
					i--;
					continue;
				}

				if (RemoveDuplicateProxyNodes(node.Nodes, ref removedSelectedNode))
				{
					removedAnyNode = true;
				}
			}

			return removedAnyNode;
		}

		private static bool IsNodeOrDescendant(TreeNode node, TreeNode candidate)
		{
			for (TreeNode current = candidate; current != null; current = current.Parent)
			{
				if (current == node)
				{
					return true;
				}
			}

			return false;
		}

		private void UpdatePersistentHighlight()
		{
			foreach (PersistentHighlightTarget target in new List<PersistentHighlightTarget>(persistentHighlights.Values))
			{
				UpdatePersistentHighlight(target, false, IntPtr.Zero);
			}
		}

		private void UpdatePersistentHighlight(PersistentHighlightTarget target, bool targetChanged, IntPtr previousHandle)
		{
			if (target == null || target.Proxy == null || target.Overlay == null)
			{
				return;
			}

			IntPtr windowHandle = target.Proxy.Handle;
			if (windowHandle == IntPtr.Zero)
			{
				return;
			}

			Rectangle rectangle;
			PersistentHighlightDebugInfo debugInfo;
			Rectangle previousTargetRectangle = target.Rectangle;
			if (!TryGetPersistentHighlightRectangle(this.Handle, target.Proxy, previousTargetRectangle, out rectangle, out debugInfo))
			{
				target.Overlay.HideHighlight();
				target.Rectangle = Rectangle.Empty;
				return;
			}

			IntPtr insertAfterWindow = GetPersistentHighlightInsertAfterWindow(windowHandle);
			if (targetChanged || rectangle != target.Rectangle)
			{
				LogPersistentHighlightDiagnostics(target.Proxy, previousTargetRectangle, debugInfo, targetChanged);
			}
			target.Rectangle = rectangle;
			target.Overlay.ShowHighlight(rectangle, insertAfterWindow);
			if (targetChanged)
			{
				RequestTargetWindowRedraw(previousHandle);
			}
			RequestTargetWindowRedraw(windowHandle);
		}

		private static bool TryGetPersistentHighlightRectangle(
			IntPtr coordinateReferenceHandle,
			ControlProxy proxy,
			Rectangle previousRectangle,
			out Rectangle rectangle,
			out PersistentHighlightDebugInfo debugInfo)
		{
			rectangle = Rectangle.Empty;
			debugInfo = new PersistentHighlightDebugInfo();
			if (proxy == null)
			{
				return false;
			}

			debugInfo.CoordinateReferenceHandle = coordinateReferenceHandle;
			debugInfo.ProxyHandle = proxy.Handle;
			debugInfo.ManagedChildPathLength = proxy.ManagedChildPathLength;
			debugInfo.ManagedChildPath = proxy.ManagedChildPath;
			using (Process currentProcess = Process.GetCurrentProcess())
			{
				debugInfo.ManagedSpyProcessId = currentProcess.Id;
			}
			GetWindowThreadProcessId(proxy.Handle, out debugInfo.TargetProcessId);

			debugInfo.ParentWindow = GetParent(proxy.Handle);
			TryGetWindowRectangle(proxy.Handle, out debugInfo.RawWindowRectangle);
			if (debugInfo.ParentWindow != IntPtr.Zero)
			{
				TryGetWindowRectangle(debugInfo.ParentWindow, out debugInfo.ParentWindowRectangle);
			}
			debugInfo.RootWindow = GetAncestor(proxy.Handle, GA_ROOT);
			if (debugInfo.RootWindow == IntPtr.Zero)
			{
				debugInfo.RootWindow = proxy.Handle;
			}
			TryGetWindowRectangle(debugInfo.RootWindow, out debugInfo.RootWindowRectangle);
			debugInfo.RawEqualsParent =
				debugInfo.ParentWindow != IntPtr.Zero &&
				debugInfo.RawWindowRectangle == debugInfo.ParentWindowRectangle;
			debugInfo.RawEqualsRoot =
				debugInfo.RootWindow != IntPtr.Zero &&
				debugInfo.RawWindowRectangle == debugInfo.RootWindowRectangle;
			debugInfo.CoordinateReferenceDpi = FormatDiagnosticDpi(coordinateReferenceHandle);
			debugInfo.ProxyDpi = FormatDiagnosticDpi(proxy.Handle);
			debugInfo.ParentDpi = FormatDiagnosticDpi(debugInfo.ParentWindow);
			debugInfo.RootDpi = FormatDiagnosticDpi(debugInfo.RootWindow);

			try
			{
				debugInfo.PreferredSourceRectangle = proxy.GetScreenBounds();
				debugInfo.PreferredRectangle = NormalizeRectangleToLocalCoordinates(
					coordinateReferenceHandle,
					debugInfo.PreferredSourceRectangle,
					debugInfo.RootWindowRectangle);
				debugInfo.PreferredNormalizationChanged =
					debugInfo.PreferredSourceRectangle != debugInfo.PreferredRectangle;
				bool preferredUsesRawWindowFallback;
				Rectangle preferredRectangle = ResolveRawWindowDpiFallback(
					debugInfo.PreferredRectangle,
					debugInfo.RawWindowRectangle,
					debugInfo.RootWindowRectangle,
					out preferredUsesRawWindowFallback);
				debugInfo.PreferredRawWindowFallback = preferredUsesRawWindowFallback;
				bool hasPreferredRectangle = preferredRectangle.Width > 0 && preferredRectangle.Height > 0;
				bool hasPreviousRectangle = previousRectangle.Width > 0 && previousRectangle.Height > 0;
				if (hasPreviousRectangle)
				{
					debugInfo.NonAccessibleSourceRectangle = proxy.GetScreenBounds(false);
					debugInfo.NonAccessibleRectangle = NormalizeRectangleToLocalCoordinates(
						coordinateReferenceHandle,
						debugInfo.NonAccessibleSourceRectangle,
						debugInfo.RootWindowRectangle);
					debugInfo.NonAccessibleNormalizationChanged =
						debugInfo.NonAccessibleSourceRectangle != debugInfo.NonAccessibleRectangle;
					bool nonAccessibleUsesRawWindowFallback;
					Rectangle nonAccessibleRectangle = ResolveRawWindowDpiFallback(
						debugInfo.NonAccessibleRectangle,
						debugInfo.RawWindowRectangle,
						debugInfo.RootWindowRectangle,
						out nonAccessibleUsesRawWindowFallback);
					debugInfo.NonAccessibleRawWindowFallback = nonAccessibleUsesRawWindowFallback;
					bool hasNonAccessibleRectangle =
						nonAccessibleRectangle.Width > 0 &&
						nonAccessibleRectangle.Height > 0;

					if (hasNonAccessibleRectangle && nonAccessibleRectangle != previousRectangle)
					{
						rectangle = nonAccessibleRectangle;
						debugInfo.ChosenRectangle = rectangle;
						debugInfo.ChosenSource = nonAccessibleUsesRawWindowFallback
							? "raw-window-dpi-fallback-non-accessible"
							: "non-accessible-switch";
						return true;
					}

					if (hasPreferredRectangle)
					{
						rectangle = preferredRectangle;
						debugInfo.ChosenRectangle = rectangle;
						debugInfo.ChosenSource = preferredUsesRawWindowFallback
							? "raw-window-dpi-fallback-preferred"
							: "preferred";
						return true;
					}

					if (hasNonAccessibleRectangle)
					{
						rectangle = nonAccessibleRectangle;
						debugInfo.ChosenRectangle = rectangle;
						debugInfo.ChosenSource = nonAccessibleUsesRawWindowFallback
							? "raw-window-dpi-fallback-non-accessible"
							: "non-accessible-switch";
						return true;
					}
				}

				if (hasPreferredRectangle)
				{
					rectangle = preferredRectangle;
					debugInfo.ChosenSource = preferredUsesRawWindowFallback
						? "raw-window-dpi-fallback-preferred"
						: "preferred";
					debugInfo.ChosenRectangle = rectangle;
					return true;
				}
			}
			catch (ArgumentException)
			{
			}
			catch (InvalidOperationException)
			{
			}

			if (TryGetWindowRectangle(proxy.Handle, out rectangle))
			{
				debugInfo.RawWindowRectangle = rectangle;
				debugInfo.ChosenRectangle = rectangle;
				debugInfo.ChosenSource = "raw-window";
				return true;
			}

			return false;
		}

		// DPI-aware target apps can return physical screen rectangles that must be translated
		// into ManagedSpy's local overlay coordinate space before comparison and drawing.
		private static Rectangle NormalizeRectangleToLocalCoordinates(
			IntPtr referenceHandle,
			Rectangle candidateRectangle,
			Rectangle rootWindowRectangle)
		{
			if (referenceHandle == IntPtr.Zero ||
				candidateRectangle.Width <= 0 ||
				candidateRectangle.Height <= 0)
			{
				return candidateRectangle;
			}

			POINT topLeft = new POINT { X = candidateRectangle.Left, Y = candidateRectangle.Top };
			POINT bottomRight = new POINT { X = candidateRectangle.Right, Y = candidateRectangle.Bottom };
			if (!PhysicalToLogicalPointForPerMonitorDPI(referenceHandle, ref topLeft) ||
				!PhysicalToLogicalPointForPerMonitorDPI(referenceHandle, ref bottomRight))
			{
				return candidateRectangle;
			}

			Rectangle logicalRectangle = Rectangle.FromLTRB(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
			if (logicalRectangle.Width <= 0 || logicalRectangle.Height <= 0)
			{
				return candidateRectangle;
			}

			if (rootWindowRectangle.Width <= 0 || rootWindowRectangle.Height <= 0)
			{
				return logicalRectangle;
			}

			long candidateScore = GetIntersectionArea(candidateRectangle, rootWindowRectangle);
			long logicalScore = GetIntersectionArea(logicalRectangle, rootWindowRectangle);
			if (rootWindowRectangle.Contains(logicalRectangle) && !rootWindowRectangle.Contains(candidateRectangle))
			{
				return logicalRectangle;
			}

			if (candidateScore == 0 && logicalScore > 0)
			{
				return logicalRectangle;
			}

			if (logicalScore > candidateScore)
			{
				return logicalRectangle;
			}

			return candidateRectangle;
		}

		private static long GetIntersectionArea(Rectangle rectangle, Rectangle container)
		{
			Rectangle intersection = Rectangle.Intersect(rectangle, container);
			return (long)intersection.Width * intersection.Height;
		}

		private static Rectangle ResolveRawWindowDpiFallback(
			Rectangle candidateRectangle,
			Rectangle rawWindowRectangle,
			Rectangle rootWindowRectangle,
			out bool usedFallback)
		{
			usedFallback = false;
			if (!ScreenBoundsHelper.ShouldUseRawWindowDpiFallback(candidateRectangle, rawWindowRectangle, rootWindowRectangle))
			{
				return candidateRectangle;
			}

			usedFallback = true;
			return rawWindowRectangle;
		}

		private static IntPtr GetPersistentHighlightInsertAfterWindow(IntPtr windowHandle)
		{
			IntPtr rootWindow = GetAncestor(windowHandle, GA_ROOT);
			if (rootWindow == IntPtr.Zero)
			{
				rootWindow = windowHandle;
			}

			return GetWindow(rootWindow, GW_HWNDPREV);
		}

		private static void RequestTargetWindowRedraw(IntPtr windowHandle)
		{
			if (windowHandle == IntPtr.Zero)
			{
				return;
			}

			IntPtr rootWindow = GetAncestor(windowHandle, GA_ROOT);
			if (rootWindow == IntPtr.Zero)
			{
				rootWindow = windowHandle;
			}

			RedrawWindow(rootWindow, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_ALLCHILDREN | RDW_FRAME | RDW_UPDATENOW);
		}

		private static void LogPersistentHighlightDiagnostics(
			ControlProxy proxy,
			Rectangle previousRectangle,
			PersistentHighlightDebugInfo debugInfo,
			bool targetChanged)
		{
			if (proxy == null || debugInfo == null)
			{
				return;
			}

			string componentName = String.IsNullOrEmpty(proxy.GetComponentName()) ? "<noname>" : proxy.GetComponentName();
			string className = String.IsNullOrEmpty(proxy.GetClassName()) ? "<unknown>" : proxy.GetClassName();
			string line =
				DateTime.Now.ToString("O") +
				"\tdiagnosticVersion=2" +
				"\ttargetChanged=" + targetChanged +
				"\thandle=" + proxy.Handle +
				"\tproxyHandle=" + FormatDiagnosticHandle(debugInfo.ProxyHandle) +
				"\tparentHandle=" + FormatDiagnosticHandle(debugInfo.ParentWindow) +
				"\trootHandle=" + FormatDiagnosticHandle(debugInfo.RootWindow) +
				"\tcoordinateReferenceHandle=" + FormatDiagnosticHandle(debugInfo.CoordinateReferenceHandle) +
				"\tmanagedSpyProcessId=" + debugInfo.ManagedSpyProcessId +
				"\ttargetProcessId=" + debugInfo.TargetProcessId +
				"\tmanagedChildPathLength=" + debugInfo.ManagedChildPathLength +
				"\tmanagedChildPath=" + debugInfo.ManagedChildPath +
				"\tdpiReference=" + debugInfo.CoordinateReferenceDpi +
				"\tdpiProxy=" + debugInfo.ProxyDpi +
				"\tdpiParent=" + debugInfo.ParentDpi +
				"\tdpiRoot=" + debugInfo.RootDpi +
				"\tcomponent=" + componentName +
				"\tclass=" + className +
				"\tprevious=" + FormatDiagnosticRectangle(previousRectangle) +
				"\tpreferredSource=" + FormatDiagnosticRectangle(debugInfo.PreferredSourceRectangle) +
				"\tpreferred=" + FormatDiagnosticRectangle(debugInfo.PreferredRectangle) +
				"\tpreferredNormalizationChanged=" + debugInfo.PreferredNormalizationChanged +
				"\tpreferredRawFallback=" + debugInfo.PreferredRawWindowFallback +
				"\tpreferredFallbackMetrics=" + FormatRawWindowFallbackMetrics(debugInfo.PreferredRectangle, debugInfo.RawWindowRectangle, debugInfo.RootWindowRectangle) +
				"\tnonAccessibleSource=" + FormatDiagnosticRectangle(debugInfo.NonAccessibleSourceRectangle) +
				"\tnonAccessible=" + FormatDiagnosticRectangle(debugInfo.NonAccessibleRectangle) +
				"\tnonAccessibleNormalizationChanged=" + debugInfo.NonAccessibleNormalizationChanged +
				"\tnonAccessibleRawFallback=" + debugInfo.NonAccessibleRawWindowFallback +
				"\tnonAccessibleFallbackMetrics=" + FormatRawWindowFallbackMetrics(debugInfo.NonAccessibleRectangle, debugInfo.RawWindowRectangle, debugInfo.RootWindowRectangle) +
				"\traw=" + FormatDiagnosticRectangle(debugInfo.RawWindowRectangle) +
				"\tparent=" + FormatDiagnosticRectangle(debugInfo.ParentWindowRectangle) +
				"\troot=" + FormatDiagnosticRectangle(debugInfo.RootWindowRectangle) +
				"\trawEqualsParent=" + debugInfo.RawEqualsParent +
				"\trawEqualsRoot=" + debugInfo.RawEqualsRoot +
				"\tchosen=" + FormatDiagnosticRectangle(debugInfo.ChosenRectangle) +
				"\tsource=" + debugInfo.ChosenSource +
				Environment.NewLine;

			try
			{
				lock (persistentHighlightDiagnosticsSync)
				{
					File.AppendAllText(persistentHighlightDiagnosticsPath, line);
				}
			}
			catch (IOException exception)
			{
				Debug.WriteLine("Persistent highlight diagnostics logging failed: " + exception.Message);
			}
			catch (UnauthorizedAccessException exception)
			{
				Debug.WriteLine("Persistent highlight diagnostics logging failed: " + exception.Message);
			}
		}

		private static string FormatDiagnosticRectangle(Rectangle rectangle)
		{
			return rectangle == Rectangle.Empty
				? "empty"
				: rectangle.Left + "," + rectangle.Top + "," + rectangle.Width + "," + rectangle.Height;
		}

		private static string FormatDiagnosticHandle(IntPtr handle)
		{
			return handle == IntPtr.Zero
				? "zero"
				: "0x" + handle.ToInt64().ToString("X", CultureInfo.InvariantCulture);
		}

		private static string FormatDiagnosticDpi(IntPtr handle)
		{
			if (handle == IntPtr.Zero)
			{
				return "none";
			}

			try
			{
				return GetDpiForWindow(handle).ToString(CultureInfo.InvariantCulture);
			}
			catch (EntryPointNotFoundException)
			{
				return "unavailable";
			}
		}

		private static string FormatRawWindowFallbackMetrics(Rectangle candidateRectangle, Rectangle rawWindowRectangle, Rectangle rootWindowRectangle)
		{
			if (candidateRectangle.Width <= 0 ||
				candidateRectangle.Height <= 0 ||
				rawWindowRectangle.Width <= 0 ||
				rawWindowRectangle.Height <= 0)
			{
				return "unavailable";
			}

			double scaleX = (double)candidateRectangle.Width / rawWindowRectangle.Width;
			double scaleY = (double)candidateRectangle.Height / rawWindowRectangle.Height;
			long candidateArea = GetRectangleArea(candidateRectangle);
			long rawArea = GetRectangleArea(rawWindowRectangle);
			long candidateIntersection = GetIntersectionArea(candidateRectangle, rootWindowRectangle);
			long rawIntersection = GetIntersectionArea(rawWindowRectangle, rootWindowRectangle);
			double candidateCoverage = GetIntersectionCoverage(candidateRectangle, rootWindowRectangle);
			double rawCoverage = GetIntersectionCoverage(rawWindowRectangle, rootWindowRectangle);

			return
				"scaleX=" + FormatDiagnosticDouble(scaleX) +
				",scaleY=" + FormatDiagnosticDouble(scaleY) +
				",candidateArea=" + candidateArea +
				",rawArea=" + rawArea +
				",candidateRootIntersection=" + candidateIntersection +
				",rawRootIntersection=" + rawIntersection +
				",candidateRootCoverage=" + FormatDiagnosticDouble(candidateCoverage) +
				",rawRootCoverage=" + FormatDiagnosticDouble(rawCoverage);
		}

		private static long GetRectangleArea(Rectangle rectangle)
		{
			if (rectangle.Width <= 0 || rectangle.Height <= 0)
			{
				return 0;
			}

			return (long)rectangle.Width * rectangle.Height;
		}

		private static double GetIntersectionCoverage(Rectangle rectangle, Rectangle container)
		{
			long area = GetRectangleArea(rectangle);
			if (area == 0)
			{
				return 0;
			}

			return (double)GetIntersectionArea(rectangle, container) / area;
		}

		private static string FormatDiagnosticDouble(double value)
		{
			return value.ToString("0.###", CultureInfo.InvariantCulture);
		}

		private void treeMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			TreeNode targetNode = GetTreeMenuTargetNode();
			if (targetNode != null && treeWindow.SelectedNode != targetNode)
			{
				treeWindow.SelectedNode = targetNode;
			}

			ControlProxy proxy = GetNodeProxy(targetNode);
			bool hasControlProxy = proxy != null;

			showWindowToolStripMenuItem.Enabled = hasControlProxy;
			refreshSubtreeToolStripMenuItem.Enabled = hasControlProxy;
			keepHighlightedToolStripMenuItem.Enabled = hasControlProxy;
			keepHighlightedToolStripMenuItem.Checked = hasControlProxy && IsPersistentHighlightEnabled(proxy);
		}

		private void treeMenuStrip_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			treeMenuTargetNode = null;
		}

		private void refreshSubtreeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			TreeNode targetNode = GetTreeMenuTargetNode();
			RunAfterTreeMenuClose(() => RefreshSubtree(targetNode));
		}

		private void keepHighlightedToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ControlProxy selectedProxy = GetNodeProxy(GetTreeMenuTargetNode());
			if (selectedProxy == null)
			{
				keepHighlightedToolStripMenuItem.Checked = false;
				return;
			}

			bool keepHighlighted = keepHighlightedToolStripMenuItem.Checked;
			RunAfterTreeMenuClose(() =>
			{
				if (keepHighlighted)
				{
					EnablePersistentHighlight(selectedProxy);
					toolStripStatusLabel1.Text = "Persistent highlight enabled.";
				}
				else if (IsPersistentHighlightEnabled(selectedProxy))
				{
					DisablePersistentHighlight(selectedProxy);
					toolStripStatusLabel1.Text = "Persistent highlight disabled.";
				}
			});
		}

		private void persistentHighlightTimer_Tick(object sender, EventArgs e)
		{
			UpdatePersistentHighlight();
		}

		private void UpdateLayoutTab(ControlProxy proxy)
		{
			HideLayoutHighlight();
			currentLayoutProxy = proxy;
			currentLayoutInfo = null;
			if (proxy == null)
			{
				layoutView.LayoutInfo = null;
				return;
			}

			try
			{
				currentLayoutInfo = proxy.GetLayoutInfo();
			}
			catch (ArgumentException)
			{
				currentLayoutInfo = null;
			}
			catch (InvalidOperationException)
			{
				currentLayoutInfo = null;
			}

			layoutView.LayoutInfo = currentLayoutInfo;
			if (currentLayoutInfo == null && tabControl1.SelectedTab == layoutPage)
			{
				toolStripStatusLabel1.Text = "Layout unavailable for selected target.";
			}
		}

		private void ClearLayoutTab()
		{
			currentLayoutProxy = null;
			currentLayoutInfo = null;
			layoutView.LayoutInfo = null;
			HideLayoutHighlight();
		}

		private void layoutView_HoveredSectionChanged(object sender, EventArgs e)
		{
			LayoutSection hoveredSection = layoutView.HoveredSection;
			if (tabControl1.SelectedTab != layoutPage ||
				!TryGetLayoutHighlightRectangle(hoveredSection, out Rectangle rectangle))
			{
				HideLayoutHighlight();
				return;
			}

			layoutHighlightOverlay.BorderColor = LayoutViewControl.GetSectionAccentColor(hoveredSection);
			layoutHighlightOverlay.ShowHighlight(rectangle);
		}

		private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (tabControl1.SelectedTab != layoutPage)
			{
				HideLayoutHighlight();
			}
		}

		private void MainForm_Deactivate(object sender, EventArgs e)
		{
			HideLayoutHighlight();
		}

		private void HideLayoutHighlight()
		{
			layoutHighlightOverlay.HideHighlight();
		}

		private bool TryGetLayoutHighlightRectangle(LayoutSection section, out Rectangle rectangle)
		{
			rectangle = Rectangle.Empty;
			if (section == LayoutSection.None || currentLayoutInfo == null || currentLayoutProxy == null)
			{
				return false;
			}

			Rectangle sourceRectangle = currentLayoutInfo.GetSectionBounds(section);
			if (sourceRectangle.Width <= 0 || sourceRectangle.Height <= 0)
			{
				return false;
			}

			if (TryGetPersistentHighlightRectangle(
				this.Handle,
				currentLayoutProxy,
				Rectangle.Empty,
				out Rectangle elementRectangle,
				out _))
			{
				if (section == LayoutSection.Element)
				{
					rectangle = elementRectangle;
					return rectangle.Width > 0 && rectangle.Height > 0;
				}

				rectangle = MapLayoutSectionToOverlayRectangle(
					currentLayoutInfo.BoundsScreen,
					sourceRectangle,
					elementRectangle);
				if (rectangle.Width > 0 && rectangle.Height > 0)
				{
					return true;
				}
			}

			Rectangle rootWindowRectangle = Rectangle.Empty;
			IntPtr rootWindow = GetAncestor(currentLayoutProxy.Handle, GA_ROOT);
			if (rootWindow == IntPtr.Zero)
			{
				rootWindow = currentLayoutProxy.Handle;
			}

			if (rootWindow != IntPtr.Zero)
			{
				TryGetWindowRectangle(rootWindow, out rootWindowRectangle);
			}

			rectangle = NormalizeRectangleToLocalCoordinates(this.Handle, sourceRectangle, rootWindowRectangle);
			return rectangle.Width > 0 && rectangle.Height > 0;
		}

		private static Rectangle MapLayoutSectionToOverlayRectangle(
			Rectangle sourceElementRectangle,
			Rectangle sourceSectionRectangle,
			Rectangle resolvedElementRectangle)
		{
			if (sourceElementRectangle.Width <= 0 ||
				sourceElementRectangle.Height <= 0 ||
				sourceSectionRectangle.Width <= 0 ||
				sourceSectionRectangle.Height <= 0 ||
				resolvedElementRectangle.Width <= 0 ||
				resolvedElementRectangle.Height <= 0)
			{
				return Rectangle.Empty;
			}

			double scaleX = (double)resolvedElementRectangle.Width / sourceElementRectangle.Width;
			double scaleY = (double)resolvedElementRectangle.Height / sourceElementRectangle.Height;
			int left = resolvedElementRectangle.Left + (int)Math.Round((sourceSectionRectangle.Left - sourceElementRectangle.Left) * scaleX);
			int top = resolvedElementRectangle.Top + (int)Math.Round((sourceSectionRectangle.Top - sourceElementRectangle.Top) * scaleY);
			int right = resolvedElementRectangle.Left + (int)Math.Round((sourceSectionRectangle.Right - sourceElementRectangle.Left) * scaleX);
			int bottom = resolvedElementRectangle.Top + (int)Math.Round((sourceSectionRectangle.Bottom - sourceElementRectangle.Top) * scaleY);
			if (right <= left || bottom <= top)
			{
				return Rectangle.Empty;
			}

			return Rectangle.FromLTRB(left, top, right, bottom);
		}

		private void findElementToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!isUpdatingFinderUiState)
			{
				ToggleElementFinder();
			}
		}

		private void tsButtonFindElement_Click(object sender, EventArgs e)
		{
			if (!isUpdatingFinderUiState)
			{
				ToggleElementFinder();
			}
		}

		private void applyPropertyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ApplySelectedProperty();
		}

		private void tsButtonApplyProperty_Click(object sender, EventArgs e)
		{
			ApplySelectedProperty();
		}

		private void ApplySelectedProperty()
		{
			ControlProxy proxy = propertyGrid.SelectedObject as ControlProxy;
			if (proxy == null)
			{
				return;
			}

			GridItem rootPropertyItem = GetRootPropertyGridItem(propertyGrid.SelectedGridItem);
			if (rootPropertyItem == null || rootPropertyItem.PropertyDescriptor == null)
			{
				toolStripStatusLabel1.Text = "Select a property value first.";
				return;
			}

			PropertyDescriptor descriptor = rootPropertyItem.PropertyDescriptor;
			if (descriptor.IsReadOnly)
			{
				toolStripStatusLabel1.Text = descriptor.Name + " is read-only.";
				return;
			}

			try
			{
				descriptor.SetValue(proxy, rootPropertyItem.Value);
				propertyGrid.Refresh();
				toolStripStatusLabel1.Text = "Applied " + descriptor.Name + ".";
			}
			catch (ArgumentException exception)
			{
				MessageBox.Show(this, exception.Message, "Apply Property", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			catch (InvalidOperationException exception)
			{
				MessageBox.Show(this, exception.Message, "Apply Property", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		private static GridItem GetRootPropertyGridItem(GridItem selectedItem)
		{
			GridItem current = selectedItem;
			while (current != null && current.GridItemType != GridItemType.Property)
			{
				current = current.Parent;
			}

			if (current == null)
			{
				return null;
			}

			while (current.Parent != null && current.Parent.GridItemType == GridItemType.Property)
			{
				current = current.Parent;
			}

			return current;
		}

		private void ToggleElementFinder()
		{
			if (isElementFinderActive)
			{
				StopElementFinder();
			}
			else
			{
				StartElementFinder();
			}
		}

		private void StartElementFinder()
		{
			if (isElementFinderActive)
			{
				return;
			}

			isElementFinderActive = true;
			isLeftButtonPressed = IsLeftMouseButtonPressed();
			SetFinderUiState(true);
			elementFinderTimer.Start();
			toolStripStatusLabel1.Text = "Element finder: click target element, Esc to cancel.";
			Cursor = Cursors.Cross;
		}

		private void StopElementFinder()
		{
			if (!isElementFinderActive)
			{
				return;
			}

			elementFinderTimer.Stop();
			RemoveFinderHighlight();
			isElementFinderActive = false;
			SetFinderUiState(false);
			Cursor = Cursors.Default;
			if (treeWindow.SelectedNode != null)
			{
				toolStripStatusLabel1.Text = treeWindow.SelectedNode.Text;
			}
		}

		private void SetFinderUiState(bool isChecked)
		{
			isUpdatingFinderUiState = true;
			if (findElementToolStripMenuItem != null)
			{
				findElementToolStripMenuItem.Checked = isChecked;
			}
			if (tsButtonFindElement != null)
			{
				tsButtonFindElement.Checked = isChecked;
			}
			isUpdatingFinderUiState = false;
		}

		private async void elementFinderTimer_Tick(object sender, EventArgs e)
		{
			if (!isElementFinderActive || isProcessingFinderSelection)
			{
				return;
			}

			if (IsEscapePressed())
			{
				StopElementFinder();
				return;
			}

			IntPtr windowHandle = GetWindowHandleAtCursor();
			UpdateFinderHighlight(windowHandle);

			bool isPressed = IsLeftMouseButtonPressed();
			if (!isLeftButtonPressed && isPressed)
			{
				IntPtr clickedWindowHandle = GetWindowHandleAtCursor();
				StopElementFinder();
				clickedWindowHandle = GetFinderClickTarget(clickedWindowHandle, windowHandle);
				isProcessingFinderSelection = true;
				try
				{
					await FocusWindowInTreeAsync(clickedWindowHandle);
				}
				catch (Exception ex)
				{
					if (!IsDisposed)
					{
						toolStripStatusLabel1.Text = "Element finder failed.";
						MessageBox.Show(this, ex.Message, "Element finder failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
				finally
				{
					isProcessingFinderSelection = false;
				}
			}
			isLeftButtonPressed = isPressed;
		}

		private IntPtr GetFinderClickTarget(IntPtr clickedHandle, IntPtr highlightedHandle)
		{
			if (IsExternalWindowHandle(clickedHandle))
			{
				return clickedHandle;
			}

			IntPtr afterFinderHiddenHandle = GetWindowHandleAtCursor();
			if (IsExternalWindowHandle(afterFinderHiddenHandle))
			{
				return afterFinderHiddenHandle;
			}

			return IsExternalWindowHandle(highlightedHandle) ? highlightedHandle : clickedHandle;
		}

		private static bool IsExternalWindowHandle(IntPtr windowHandle)
		{
			if (windowHandle == IntPtr.Zero)
			{
				return false;
			}

			GetWindowThreadProcessId(windowHandle, out uint processId);
			return processId != 0 && processId != Process.GetCurrentProcess().Id;
		}

		private bool IsLeftMouseButtonPressed()
		{
			return (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;
		}

		private bool IsEscapePressed()
		{
			return (GetAsyncKeyState(VK_ESCAPE) & 0x8000) != 0;
		}

		private Point GetCursorScreenPosition()
		{
			POINT point;
			if (GetCursorPos(out point))
			{
				return point.ToPoint();
			}
			return Cursor.Position;
		}

		private IntPtr GetWindowHandleAtCursor()
		{
			Point cursorPosition = GetCursorScreenPosition();
			IntPtr currentHandle = WindowFromPoint(cursorPosition);
			while (currentHandle != IntPtr.Zero)
			{
				Point childPoint = cursorPosition;
				if (!ScreenToClient(currentHandle, ref childPoint))
				{
					break;
				}

				IntPtr childHandle = ChildWindowFromPointEx(currentHandle, childPoint,
					CWP_SKIPINVISIBLE | CWP_SKIPDISABLED | CWP_SKIPTRANSPARENT);
				if (childHandle == IntPtr.Zero || childHandle == currentHandle)
				{
					break;
				}
				currentHandle = childHandle;
			}
			return currentHandle;
		}

		private void UpdateFinderHighlight(IntPtr windowHandle)
		{
			Rectangle rectangle;
			if (!TryGetWindowRectangle(windowHandle, out rectangle))
			{
				RemoveFinderHighlight();
				return;
			}

			if (windowHandle == highlightedWindowHandle && rectangle == highlightedWindowRectangle)
			{
				return;
			}

			RemoveFinderHighlight();
			highlightedWindowHandle = windowHandle;
			highlightedWindowRectangle = rectangle;
			highlightOverlay.ShowHighlight(rectangle);
		}

		private void RemoveFinderHighlight()
		{
			highlightOverlay.HideHighlight();
			highlightedWindowRectangle = Rectangle.Empty;
			highlightedWindowHandle = IntPtr.Zero;
		}

		private static bool TryGetWindowRectangle(IntPtr windowHandle, out Rectangle rectangle)
		{
			RECT rect;
			if (windowHandle != IntPtr.Zero && GetWindowRect(windowHandle, out rect))
			{
				rectangle = rect.ToRectangle();
				if (rectangle.Width > 0 && rectangle.Height > 0)
				{
					return true;
				}
			}

			rectangle = Rectangle.Empty;
			return false;
		}

		private async Task FocusWindowInTreeAsync(IntPtr windowHandle)
		{
			if (isRefreshRunning && currentRefreshTask != null)
			{
				toolStripStatusLabel1.Text = "Waiting for refresh before selecting element...";
			}

			while (isRefreshRunning && currentRefreshTask != null)
			{
				await currentRefreshTask;
			}

			if (!IsDisposed)
			{
				FocusWindowInTree(windowHandle);
			}
		}

		private void FocusWindowInTree(IntPtr windowHandle)
		{
			if (windowHandle == IntPtr.Zero)
			{
				return;
			}

			ControlProxy targetProxy = ControlProxy.FromHandle(windowHandle);
			if (targetProxy == null)
			{
				return;
			}

			if (!ShowNative.Checked && !HasManagedAncestor(targetProxy))
			{
				toolStripStatusLabel1.Text = "Selected element is not managed. Enable Show Native Windows to include it.";
				return;
			}

			using (Process owningProcess = targetProxy.OwningProcess)
			{
				if (owningProcess == null || owningProcess.Id == Process.GetCurrentProcess().Id)
				{
					return;
				}
			}

			TreeNode node = FindOrAddProxyPathNode(targetProxy);

			if (node != null)
			{
				ExpandElementFinderPath(node);
				treeWindow.SelectedNode = node;
				node.EnsureVisible();
				tabControl1.SelectedTab = propertiesPage;
				if (WindowState == FormWindowState.Minimized)
				{
					WindowState = FormWindowState.Normal;
				}
				Activate();
				treeWindow.Focus();
				toolStripStatusLabel1.Text = "Selected element in tree.";
				FlashWindowHandle(windowHandle);
			}
			else
			{
				toolStripStatusLabel1.Text = "Unable to select element in tree.";
			}
		}

		private TreeNode FindOrAddProxyPathNode(ControlProxy proxy)
		{
			if (proxy == null)
			{
				return null;
			}

			Process process = proxy.OwningProcess;
			if (process == null)
			{
				return null;
			}

			try
			{
				if (process.Id == Process.GetCurrentProcess().Id)
				{
					return null;
				}

				int processId = process.Id;
				TreeNode processNode = treeWindow.Nodes[processId.ToString()];
				if (processNode == null)
				{
					Process trackedProcess = TrackProcess(process);
					process = null;
					if (trackedProcess == null)
					{
						return null;
					}

					processNode = treeWindow.Nodes.Add(trackedProcess.Id.ToString(),
						trackedProcess.ProcessName +
						"  " + trackedProcess.MainWindowTitle +
						" [" + trackedProcess.Id.ToString() + "]");
					processNode.Tag = trackedProcess;
				}

				PopulateProcessTopLevelWindows(processNode, processId);
				List<ControlProxy> chain = BuildProxyChain(proxy);
				TreeNode currentNode = processNode;
				foreach (ControlProxy chainProxy in chain)
				{
					if (GetNodeProxy(currentNode) != null)
					{
						PopulateProxyChildren(currentNode);
					}

					TreeNode childNode = FindChildNodeByHandle(currentNode, chainProxy.Handle);
					if (childNode == null)
					{
						childNode = CreateProxyNode(chainProxy);
						currentNode.Nodes.Add(childNode);
					}

					currentNode = childNode;
				}

				PopulateProxyChildren(currentNode);
				return currentNode;
			}
			finally
			{
				if (process != null)
				{
					process.Dispose();
				}
			}
		}

		private static List<ControlProxy> BuildProxyChain(ControlProxy proxy)
		{
			List<ControlProxy> chain = new List<ControlProxy>();
			ControlProxy current = proxy;
			while (current != null)
			{
				chain.Add(current);
				current = current.Parent;
			}

			chain.Reverse();
			return chain;
		}

		private bool HasManagedAncestor(ControlProxy proxy)
		{
			ControlProxy current = proxy;
			while (current != null)
			{
				if (current.IsKnownManagedProxy || current.IsManaged)
				{
					return true;
				}

				current = current.Parent;
			}

			return false;
		}

		private void PopulateProcessTopLevelWindows(TreeNode processNode, int processId)
		{
			if (processNode == null || processId == 0)
			{
				return;
			}

			ControlProxy[] topWindows = ControlProxy.GetTopLevelWindows(
				ControlProxy.EventWindowHandle,
				Process.GetCurrentProcess().Id);
			if (topWindows == null)
			{
				return;
			}

			bool includeAllProcessTopLevelWindows = ShowNative.Checked || ControlProxy.IsManagedProcess(processId);
			foreach (ControlProxy topWindow in topWindows)
			{
				if (topWindow == null ||
					topWindow.OwningProcessId != processId ||
					(!includeAllProcessTopLevelWindows && !IsManagedTreeRoot(topWindow)))
				{
					continue;
				}

				AddProxyNodeIfMissing(processNode.Nodes, topWindow);
			}
		}

		private static bool IsManagedTreeRoot(ControlProxy proxy)
		{
			return proxy != null && (proxy.IsKnownManagedProxy || proxy.IsManaged);
		}

		private void ExpandElementFinderPath(TreeNode node)
		{
			List<TreeNode> ancestors = new List<TreeNode>();
			TreeNode parent = node.Parent;
			while (parent != null)
			{
				ancestors.Add(parent);
				parent = parent.Parent;
			}

			ancestors.Reverse();
			isExpandingElementFinderPath = true;
			try
			{
				foreach (TreeNode ancestor in ancestors)
				{
					ancestor.Expand();
				}

				node.EnsureVisible();
			}
			finally
			{
				isExpandingElementFinderPath = false;
			}
		}

		private TreeNode FindProxyNode(ControlProxy proxy)
		{
			Process process = proxy.OwningProcess;
			if (process == null)
			{
				return null;
			}

			TreeNode processNode = treeWindow.Nodes[process.Id.ToString()];
			if (processNode == null)
			{
				return null;
			}
			processNode.Expand();

			List<ControlProxy> chain = BuildProxyChain(proxy);

			if (chain.Count == 0)
			{
				return null;
			}

			TreeNode currentNode = FindChildNodeByHandle(processNode, chain[0].Handle);
			if (currentNode == null)
			{
				return processNode;
			}

			for (int i = 1; i < chain.Count; i++)
			{
				currentNode.Expand();
				TreeNode child = FindChildNodeByHandle(currentNode, chain[i].Handle);
				if (child == null)
				{
					return currentNode;
				}
				currentNode = child;
			}

			return currentNode;
		}

		private TreeNode FindChildNodeByHandle(TreeNode parentNode, IntPtr handle)
		{
			string key = handle.ToString();
			TreeNode byKey = parentNode.Nodes[key];
			if (byKey != null)
			{
				return byKey;
			}

			foreach (TreeNode child in parentNode.Nodes)
			{
				ControlProxy childProxy = child.Tag as ControlProxy;
				if (childProxy != null && childProxy.Handle == handle)
				{
					return child;
				}
			}

			return null;
		}

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
			Application.Exit();
		}

		private async void MainForm_Load(object sender, EventArgs e) {
			await RefreshWindowsAsync();
		}

		private async void refreshToolStripMenuItem_Click(object sender, EventArgs e) {
			await RefreshWindowsAsync();
		}

		/// <summary>
		/// This rebuilds the window hierarchy
		/// </summary>
		private Task RefreshWindowsAsync() {
			if (isRefreshRunning)
			{
				return currentRefreshTask;
			}

			refreshCancellationSource?.Dispose();
			refreshCancellationSource = new CancellationTokenSource();
			isRefreshRunning = true;
			SetRefreshControlsEnabled(false);
			toolStripStatusLabel1.Text = "Refreshing windows...";
			currentRefreshTask = RefreshWindowsCoreAsync(refreshCancellationSource.Token);
			return currentRefreshTask;
		}

		private async Task RefreshWindowsCoreAsync(CancellationToken cancellationToken)
		{
			bool showNative = ShowNative.Checked;
			IntPtr eventWindowHandle = ControlProxy.EventWindowHandle;
			int currentProcessId = Process.GetCurrentProcess().Id;
			bool refreshSucceeded = false;

			try
			{
				RefreshSnapshot snapshot = await Task.Run(
					() => BuildRefreshSnapshot(showNative, eventWindowHandle, currentProcessId, cancellationToken),
					cancellationToken);

				if (!cancellationToken.IsCancellationRequested && !IsDisposed)
				{
					ApplyRefreshSnapshot(snapshot);
					refreshSucceeded = true;
				}
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex)
			{
				if (!IsDisposed)
				{
					toolStripStatusLabel1.Text = "Refresh failed.";
					MessageBox.Show(this, ex.Message, "Refresh failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			finally
			{
				if (!IsDisposed)
				{
					SetRefreshControlsEnabled(true);
					if (refreshSucceeded)
					{
						toolStripStatusLabel1.Text = "Refresh complete.";
					}
				}

				isRefreshRunning = false;
			}
		}

		private static RefreshSnapshot BuildRefreshSnapshot(bool showNative, IntPtr eventWindowHandle, int currentProcessId, CancellationToken cancellationToken)
		{
			List<RefreshWindowSnapshot> windows = new List<RefreshWindowSnapshot>();
			Dictionary<int, bool> managedProcessCache = new Dictionary<int, bool>();
			ControlProxy[] topWindows = ControlProxy.GetTopLevelWindows(eventWindowHandle, currentProcessId);
			if (topWindows != null && topWindows.Length > 0)
			{
				foreach (ControlProxy cproxy in topWindows)
				{
					cancellationToken.ThrowIfCancellationRequested();
					int processId = cproxy.OwningProcessId;
					if (processId == currentProcessId || processId == 0)
					{
						continue;
					}

					bool processIsManaged = IsManagedProcess(processId, managedProcessCache);
					if (!showNative && !processIsManaged && !cproxy.IsKnownManagedProxy && !cproxy.IsManaged)
					{
						continue;
					}

					string processName = String.Empty;
					string mainWindowTitle = String.Empty;
					using (Process proc = TryGetProcess(processId))
					{
						if (proc == null)
						{
							continue;
						}

						processName = proc.ProcessName;
						mainWindowTitle = proc.MainWindowTitle;
					}

					windows.Add(new RefreshWindowSnapshot
					{
						Proxy = cproxy,
						ProcessId = processId,
						ProcessName = processName,
						MainWindowTitle = mainWindowTitle,
						NodeText = GetProxyNodeText(cproxy)
					});
				}
			}

			return new RefreshSnapshot(windows);
		}

		private static bool IsManagedProcess(int processId, Dictionary<int, bool> managedProcessCache)
		{
			if (!managedProcessCache.TryGetValue(processId, out bool isManaged))
			{
				isManaged = ControlProxy.IsManagedProcess(processId);
				managedProcessCache.Add(processId, isManaged);
			}

			return isManaged;
		}

		private void ApplyRefreshSnapshot(RefreshSnapshot snapshot)
		{
			ClearTrackedProcesses();
			ClearLayoutTab();
			this.treeWindow.BeginUpdate();
			try
			{
				this.treeWindow.Nodes.Clear();
				foreach (RefreshWindowSnapshot window in snapshot.Windows) {
					TreeNode procnode;
					procnode = treeWindow.Nodes[window.ProcessId.ToString()];
					if (procnode == null) {
						Process proc = TryGetProcess(window.ProcessId);
						proc = TrackProcess(proc);
						if (proc == null)
						{
							continue;
						}

						procnode = treeWindow.Nodes.Add(window.ProcessId.ToString(),
							window.ProcessName +
							"  " + window.MainWindowTitle +
							" [" + window.ProcessId.ToString() + "]");
						procnode.Tag = proc;
					}

					TreeNode node = CreateProxyNode(window.Proxy);
					node.Text = window.NodeText;
					procnode.Nodes.Add(node);
				}
				if (treeWindow.Nodes.Count == 0) {
					treeWindow.Nodes.Add("No managed processes running.");
					treeWindow.Nodes.Add("Select View->Refresh.");
				}
			}
			finally
			{
				this.treeWindow.EndUpdate();
			}
		}

		private static Process TryGetProcess(int processId)
		{
			try
			{
				return Process.GetProcessById(processId);
			}
			catch (ArgumentException)
			{
				return null;
			}
			catch (InvalidOperationException)
			{
				return null;
			}
		}

		private void SetRefreshControlsEnabled(bool enabled)
		{
			tsbuttonRefresh.Enabled = enabled;
			refreshToolStripMenuItem.Enabled = enabled;
		}

		/// <summary>
		/// Called when the user selects a control in the treeview
		/// </summary>
		private void treeWindow_AfterSelect(object sender, TreeViewEventArgs e) {
			TreeNode selectedNode = e == null || e.Node == null ? treeWindow.SelectedNode : e.Node;
			if (selectedNode == null)
			{
				this.propertyGrid.SelectedObject = null;
				ClearLayoutTab();
				this.toolStripStatusLabel1.Text = String.Empty;
				StopLogging();
				this.eventGrid.Rows.Clear();
				return;
			}

			this.propertyGrid.SelectedObject = selectedNode.Tag;
			UpdateLayoutTab(GetNodeProxy(selectedNode));
			this.toolStripStatusLabel1.Text = selectedNode.Text;
			StopLogging();
			this.eventGrid.Rows.Clear();
			StartLogging();
		}

		/// <summary>
		/// This is called when the selected ControlProxy raises an event
		/// </summary>
		private void ProxyEventFired(object sender, ProxyEventArgs args) {
			eventGrid.FirstDisplayedScrollingRowIndex = this.eventGrid.Rows.Add(new object[] { args.eventDescriptor.Name, args.eventArgs.ToString() });
		}

		/// <summary>
		/// Used to lazily build the treeview as the user expands control nodes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void treeWindow_BeforeExpand(object sender, TreeViewCancelEventArgs e) {
			if (isExpandingElementFinderPath || e == null || e.Node == null)
			{
				return;
			}

			PopulateProxyChildren(e.Node);
		}

		private void flashWindow_Click(object sender, EventArgs e) {
			FlashCurrentWindow();
		}
		private void showWindowToolStripMenuItem_Click(object sender, EventArgs e) {
			TreeNode targetNode = GetTreeMenuTargetNode();
			RunAfterTreeMenuClose(() => FlashWindow(targetNode));
		}

		/// <summary>
		/// This highlights the given window with a temporary overlay frame.
		/// </summary>
		private void FlashCurrentWindow()
		{
			FlashWindow(treeWindow.SelectedNode);
		}

		private void FlashWindow(TreeNode node)
		{
			if (node == null)
			{
				return;
			}

			ControlProxy proxy = node.Tag as ControlProxy;
			if (proxy != null)
			{
				FlashWindowHandle(proxy.Handle);
			}
		}

		private void FlashWindowHandle(IntPtr windowHandle)
		{
			Rectangle rectangle;
			if (!TryGetWindowRectangle(windowHandle, out rectangle))
			{
				return;
			}

			for (int i = 0; i < 5; i++)
			{
				highlightOverlay.ShowHighlight(rectangle);
				Thread.Sleep(80);
				highlightOverlay.HideHighlight();
				Thread.Sleep(60);
			}
		}

		/// <summary>
		/// Starts event logging
		/// </summary>
		private void StartLogging() {
			if (tsButtonStartStop.Checked) {
				currentProxy = propertyGrid.SelectedObject as ControlProxy;
				if (currentProxy != null) {
					//unsubscribe from events.
					foreach (EventDescriptor ed in currentProxy.GetEvents()) {
						if (dialog.EventList[ed.Name].Display) {
							currentProxy.SubscribeEvent(ed);
						}
					}
					currentProxy.EventFired += new ControlProxyEventHandler(ProxyEventFired);
				}
			}
		}

		/// <summary>
		/// Stops event Logging
		/// </summary>
		private void StopLogging() {
			if (currentProxy != null) {
				//unsubscribe from events.
				foreach (EventDescriptor ed in currentProxy.GetEvents()) {
					currentProxy.UnsubscribeEvent(ed);
				}
				currentProxy.EventFired -= new ControlProxyEventHandler(ProxyEventFired);
			}
		}

		private void tsButtonStartStop_Click(object sender, EventArgs e) {
			StopLogging();
			StartLogging();
			if (tsButtonStartStop.Checked) {
				tsButtonStartStop.Image = ManagedSpy.Properties.Resources.Stop;
			}
			else {
				tsButtonStartStop.Image = ManagedSpy.Properties.Resources.Play;
			}
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
			refreshCancellationSource?.Cancel();
			ControlProxy.WindowDestroyed -= ControlProxy_WindowDestroyed;
			ControlProxy.HandleChanged -= ControlProxy_HandleChanged;
			ClearTrackedProcesses();
			StopElementFinder();
			highlightOverlay.Dispose();
			DisableAllPersistentHighlights();
			layoutHighlightOverlay.Dispose();
			StopLogging();
			refreshCancellationSource?.Dispose();
		}

		private async void tsbuttonRefresh_Click(object sender, EventArgs e) {
			await RefreshWindowsAsync();
		}

		private void tsButtonClear_Click(object sender, EventArgs e) {
			this.eventGrid.Rows.Clear();
		}

		private void tsbuttonFilterEvents_Click(object sender, EventArgs e) {
			dialog.ShowDialog();
			StopLogging();
			StartLogging();
		}

		private void filterEventsToolStripMenuItem_Click(object sender, EventArgs e) {
			dialog.ShowDialog();
			StopLogging();
			StartLogging();
		}

		private void aboutManagedSpyToolStripMenuItem_Click(object sender, EventArgs e) {
			HelpAbout about = new HelpAbout();
			about.ShowDialog();
		}

		private void treeWindow_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				SetTreeMenuTargetNode(e.Node);
				treeMenuStrip.Show(treeWindow, e.Location);
			}
		}
	}

	class HighlightOverlayForm : Form
	{
		private const int WM_NCHITTEST = 0x84;
		private const int HTTRANSPARENT = -1;
		private const int WS_EX_TRANSPARENT = 0x00000020;
		private const int WS_EX_TOOLWINDOW = 0x00000080;
		private const int WS_EX_TOPMOST = 0x00000008;
		private const int WS_EX_NOACTIVATE = 0x08000000;
		private const uint SWP_NOACTIVATE = 0x0010;
		private const uint SWP_SHOWWINDOW = 0x0040;
		private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
		private readonly bool isTopMost;
		private Color borderColor;

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

		public HighlightOverlayForm()
			: this(true)
		{
		}

		public HighlightOverlayForm(bool topMost)
			: this(topMost, Color.Red)
		{
		}

		public HighlightOverlayForm(bool topMost, Color borderColor)
		{
			isTopMost = topMost;
			this.borderColor = borderColor;
			FormBorderStyle = FormBorderStyle.None;
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.Manual;
			TopMost = topMost;
			BackColor = Color.Magenta;
			TransparencyKey = Color.Magenta;
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Color BorderColor
		{
			get { return borderColor; }
			set
			{
				if (borderColor == value)
				{
					return;
				}

				borderColor = value;
				Invalidate();
			}
		}

		protected override bool ShowWithoutActivation
		{
			get { return true; }
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams createParams = base.CreateParams;
				createParams.ExStyle |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
				if (isTopMost)
				{
					createParams.ExStyle |= WS_EX_TOPMOST;
				}
				return createParams;
			}
		}

		public void ShowHighlight(Rectangle screenBounds)
		{
			ShowHighlight(screenBounds, IntPtr.Zero);
		}

		public void ShowHighlight(Rectangle screenBounds, IntPtr insertAfterWindow)
		{
			if (screenBounds == Rectangle.Empty || screenBounds.Width <= 0 || screenBounds.Height <= 0)
			{
				HideHighlight();
				return;
			}

			Rectangle frameBounds = screenBounds;
			frameBounds.Inflate(2, 2);

			if (!Visible)
			{
				Show();
			}

			IntPtr zOrderReference = isTopMost ? HWND_TOPMOST : insertAfterWindow;
			SetWindowPos(Handle, zOrderReference, frameBounds.Left, frameBounds.Top, frameBounds.Width, frameBounds.Height, SWP_NOACTIVATE | SWP_SHOWWINDOW);
			Invalidate();
			Update();
		}

		public void HideHighlight()
		{
			if (Visible)
			{
				Hide();
			}
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_NCHITTEST)
			{
				m.Result = (IntPtr)HTTRANSPARENT;
				return;
			}

			base.WndProc(ref m);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			Rectangle rectangle = ClientRectangle;
			if (rectangle.Width <= 2 || rectangle.Height <= 2)
			{
				return;
			}

			rectangle.Inflate(-1, -1);
			using (Pen pen = new Pen(borderColor, 3))
			{
				e.Graphics.DrawRectangle(pen, rectangle);
			}
		}
	}

	/// <summary>
	/// This is to ensure when you click on the toolstrip, our application doesn't have to be
	/// active for the click to register.
	/// </summary>
	class ClickToolStrip : ToolStrip {
		const int WM_MOUSEACTIVATE = 0x0021;
		const int MA_ACTIVATE = 0x0001;

		protected override void WndProc(ref Message m) {
			if (m.Msg == WM_MOUSEACTIVATE) {
				m.Result = (IntPtr)MA_ACTIVATE;
			}
			else {
				base.WndProc(ref m);
			}
		}
	}
}
