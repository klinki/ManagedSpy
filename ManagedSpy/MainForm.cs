using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
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

		/// <summary>
		/// Currently selected proxy -- used for event logging.
		/// </summary>
		private ControlProxy currentProxy = null;
		private readonly EventFilterDialog dialog = new EventFilterDialog();
		private readonly System.Windows.Forms.Timer elementFinderTimer = new System.Windows.Forms.Timer();
		private readonly HighlightOverlayForm highlightOverlay = new HighlightOverlayForm();
		private readonly System.Windows.Forms.Timer persistentHighlightTimer = new System.Windows.Forms.Timer();
		private readonly HighlightOverlayForm persistentHighlightOverlay = new HighlightOverlayForm(false);
		private ToolStripButton tsButtonFindElement = null;
		private ToolStripButton tsButtonApplyProperty = null;
		private ToolStripMenuItem findElementToolStripMenuItem = null;
		private ToolStripMenuItem applyPropertyToolStripMenuItem = null;
		private ToolStripMenuItem refreshSubtreeToolStripMenuItem = null;
		private ToolStripMenuItem keepHighlightedToolStripMenuItem = null;
		private bool isElementFinderActive = false;
		private bool isLeftButtonPressed = false;
		private bool isUpdatingFinderUiState = false;
		private IntPtr highlightedWindowHandle = IntPtr.Zero;
		private Rectangle highlightedWindowRectangle = Rectangle.Empty;
		private ControlProxy persistentHighlightProxy = null;
		private Rectangle persistentHighlightRectangle = Rectangle.Empty;

        public MainForm() {
			InitializeComponent();
			InitializeElementFinder();
			InitializePropertyApply();
			InitializeTreeContextMenu();
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

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

		[DllImport("user32.dll")]
		private static extern bool GetCursorPos(out POINT lpPoint);

		[DllImport("user32.dll")]
		private static extern short GetAsyncKeyState(int vKey);

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

			persistentHighlightTimer.Interval = 150;
			persistentHighlightTimer.Tick += new EventHandler(persistentHighlightTimer_Tick);
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
			foreach (ControlProxy childProxy in parentProxy.Children)
			{
				TreeNode childNode = CreateProxyNode(childProxy);
				parentNode.Nodes.Add(childNode);
				RebuildControlSubtree(childNode);
			}
		}

		private void RefreshSelectedSubtree()
		{
			TreeNode rootNode = treeWindow.SelectedNode;
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

		private void EnablePersistentHighlight(ControlProxy proxy)
		{
			persistentHighlightProxy = proxy;
			persistentHighlightRectangle = Rectangle.Empty;
			UpdatePersistentHighlight();
			persistentHighlightTimer.Start();
		}

		private void DisablePersistentHighlight()
		{
			persistentHighlightTimer.Stop();
			persistentHighlightProxy = null;
			persistentHighlightRectangle = Rectangle.Empty;
			persistentHighlightOverlay.HideHighlight();
		}

		private void UpdatePersistentHighlight()
		{
			if (persistentHighlightProxy == null)
			{
				return;
			}

			IntPtr windowHandle = persistentHighlightProxy.Handle;
			if (windowHandle == IntPtr.Zero)
			{
				return;
			}

			Rectangle rectangle;
			if (!TryGetWindowRectangle(windowHandle, out rectangle))
			{
				persistentHighlightOverlay.HideHighlight();
				persistentHighlightRectangle = Rectangle.Empty;
				return;
			}

			IntPtr insertAfterWindow = GetPersistentHighlightInsertAfterWindow(windowHandle);
			persistentHighlightRectangle = rectangle;
			persistentHighlightOverlay.ShowHighlight(rectangle, insertAfterWindow);
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

		private void treeMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			ControlProxy proxy = GetNodeProxy(treeWindow.SelectedNode);
			bool hasControlProxy = proxy != null;

			showWindowToolStripMenuItem.Enabled = hasControlProxy;
			refreshSubtreeToolStripMenuItem.Enabled = hasControlProxy;
			keepHighlightedToolStripMenuItem.Enabled = hasControlProxy;
			keepHighlightedToolStripMenuItem.Checked = hasControlProxy &&
				persistentHighlightProxy != null &&
				proxy.Handle == persistentHighlightProxy.Handle;
		}

		private void refreshSubtreeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			RefreshSelectedSubtree();
		}

		private void keepHighlightedToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ControlProxy selectedProxy = GetNodeProxy(treeWindow.SelectedNode);
			if (selectedProxy == null)
			{
				keepHighlightedToolStripMenuItem.Checked = false;
				return;
			}

			if (keepHighlightedToolStripMenuItem.Checked)
			{
				EnablePersistentHighlight(selectedProxy);
				toolStripStatusLabel1.Text = "Persistent highlight enabled.";
			}
			else if (persistentHighlightProxy != null && persistentHighlightProxy.Handle == selectedProxy.Handle)
			{
				DisablePersistentHighlight();
				toolStripStatusLabel1.Text = "Persistent highlight disabled.";
			}
		}

		private void persistentHighlightTimer_Tick(object sender, EventArgs e)
		{
			UpdatePersistentHighlight();
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

		private void elementFinderTimer_Tick(object sender, EventArgs e)
		{
			if (!isElementFinderActive)
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
				StopElementFinder();
				FocusWindowInTree(windowHandle);
			}
			isLeftButtonPressed = isPressed;
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
			Process owningProcess = targetProxy.OwningProcess;
			if (owningProcess == null || owningProcess.Id == Process.GetCurrentProcess().Id)
			{
				return;
			}

			TreeNode node = FindProxyNode(targetProxy);
			if (node == null)
			{
				RefreshWindows();
				node = FindProxyNode(targetProxy);
			}

			if (node != null)
			{
				treeWindow.SelectedNode = node;
				node.EnsureVisible();
				tabControl1.SelectedTab = propertiesPage;
				FlashWindowHandle(windowHandle);
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

			List<ControlProxy> chain = new List<ControlProxy>();
			ControlProxy current = proxy;
			while (current != null)
			{
				chain.Add(current);
				current = current.Parent;
			}
			chain.Reverse();

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

		private void MainForm_Load(object sender, EventArgs e) {
			RefreshWindows();
		}

		private void refreshToolStripMenuItem_Click(object sender, EventArgs e) {
			RefreshWindows();
		}

		/// <summary>
		/// This rebuilds the window hierarchy
		/// </summary>
		private void RefreshWindows() {
			this.treeWindow.BeginUpdate();
			this.treeWindow.Nodes.Clear();
			ControlProxy[] topWindows = Microsoft.ManagedSpy.ControlProxy.TopLevelWindows;
			if (topWindows != null && topWindows.Length > 0) {
				foreach (ControlProxy cproxy in topWindows) {
					TreeNode procnode;

					//only showing managed windows
					if (this.ShowNative.Checked || cproxy.IsManaged) {
						Process proc = cproxy.OwningProcess;
						if (proc != null && proc.Id != Process.GetCurrentProcess().Id) {
							procnode = treeWindow.Nodes[proc.Id.ToString()];
							if (procnode == null) {
								procnode = treeWindow.Nodes.Add(proc.Id.ToString(),
									proc.ProcessName +
									"  " + proc.MainWindowTitle +
									" [" + proc.Id.ToString() + "]");
								procnode.Tag = proc;
							}
							TreeNode node = CreateProxyNode(cproxy);
							procnode.Nodes.Add(node);
						}
					}
				}
			}
			if (treeWindow.Nodes.Count == 0) {
				treeWindow.Nodes.Add("No managed processes running.");
				treeWindow.Nodes.Add("Select View->Refresh.");
			}
			this.treeWindow.EndUpdate();
		}

		/// <summary>
		/// Called when the user selects a control in the treeview
		/// </summary>
		private void treeWindow_AfterSelect(object sender, TreeViewEventArgs e) {
			this.propertyGrid.SelectedObject = this.treeWindow.SelectedNode.Tag;
			this.toolStripStatusLabel1.Text = treeWindow.SelectedNode.Text;
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
		/// Used to build the treeview as the user expands nodes.
		/// We always stay one step ahead of the user to get the expand state set correctly.
		/// So, for instance, when we just show processes, we have already calculated all the top level windows.
		/// When the user expands a process -- we calculate the children of all top level windows
		/// And so on...
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void treeWindow_BeforeExpand(object sender, TreeViewCancelEventArgs e) {
			foreach (TreeNode child in e.Node.Nodes) {
				child.Nodes.Clear();
				ControlProxy proxy = child.Tag as ControlProxy;
				if (proxy != null) {
					foreach (ControlProxy proxychild in proxy.Children) {
						TreeNode node = CreateProxyNode(proxychild);
						child.Nodes.Add(node);
					}
				}
			}
		}

		private void flashWindow_Click(object sender, EventArgs e) {
			FlashCurrentWindow();
		}
		private void showWindowToolStripMenuItem_Click(object sender, EventArgs e) {
			FlashCurrentWindow();
		}

		/// <summary>
		/// This highlights the given window with a temporary overlay frame.
		/// </summary>
		private void FlashCurrentWindow()
		{
			if (treeWindow.SelectedNode == null)
			{
				return;
			}

			ControlProxy proxy = treeWindow.SelectedNode.Tag as ControlProxy;
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
			StopElementFinder();
			highlightOverlay.Dispose();
			DisablePersistentHighlight();
			persistentHighlightOverlay.Dispose();
			StopLogging();
		}

		private void tsbuttonRefresh_Click(object sender, EventArgs e) {
			RefreshWindows();
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
				treeWindow.SelectedNode = e.Node;
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

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

		public HighlightOverlayForm()
			: this(true)
		{
		}

		public HighlightOverlayForm(bool topMost)
		{
			isTopMost = topMost;
			FormBorderStyle = FormBorderStyle.None;
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.Manual;
			TopMost = topMost;
			BackColor = Color.Magenta;
			TransparencyKey = Color.Magenta;
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
			using (Pen pen = new Pen(Color.Red, 3))
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
