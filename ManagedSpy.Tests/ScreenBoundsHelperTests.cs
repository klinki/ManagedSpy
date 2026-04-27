using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.ManagedSpy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedSpy.Tests
{
    [TestClass]
    public class ScreenBoundsHelperTests
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, [In, Out] POINT[] lpPoints, uint cPoints);

        [TestMethod]
        public void GetControlScreenBounds_ReturnsFormBounds_ForTopLevelForm()
        {
            Rectangle actual = RunInSta(() =>
            {
                using Form form = CreateForm();
                return ScreenBoundsHelper.GetControlScreenBounds(form);
            });

            Rectangle expected = RunInSta(() =>
            {
                using Form form = CreateForm();
                return GetWindowBounds(form);
            });

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetControlScreenBounds_ReturnsClientRectangleOnScreen_ForNestedLabel()
        {
            Rectangle actual = RunInSta(() =>
            {
                using Form form = CreateForm();
                Panel panel = new Panel
                {
                    Location = new Point(20, 30),
                    Size = new Size(220, 160)
                };
                Label label = new Label
                {
                    Location = new Point(15, 25),
                    Size = new Size(90, 24),
                    Text = "Sample"
                };

                panel.Controls.Add(label);
                form.Controls.Add(panel);
                ShowForm(form);

                return ScreenBoundsHelper.GetControlScreenBounds(label);
            });

            Rectangle expected = RunInSta(() =>
            {
                using Form form = CreateForm();
                Panel panel = new Panel
                {
                    Location = new Point(20, 30),
                    Size = new Size(220, 160)
                };
                Label label = new Label
                {
                    Location = new Point(15, 25),
                    Size = new Size(90, 24),
                    Text = "Sample"
                };

                panel.Controls.Add(label);
                form.Controls.Add(panel);
                ShowForm(form);

                return GetClientBoundsOnScreen(label);
            });

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetControlScreenBounds_ClipsOversizedChildToParentClientBounds()
        {
            Rectangle actual = RunInSta(() =>
            {
                using Form form = CreateForm();
                Panel panel = new Panel
                {
                    Location = new Point(20, 30),
                    Size = new Size(220, 160)
                };
                Label label = new Label
                {
                    Location = new Point(180, 20),
                    Size = new Size(120, 30),
                    Text = "Clipped"
                };

                panel.Controls.Add(label);
                form.Controls.Add(panel);
                ShowForm(form);

                return ScreenBoundsHelper.GetControlScreenBounds(label);
            });

            Rectangle expected = RunInSta(() =>
            {
                using Form form = CreateForm();
                Panel panel = new Panel
                {
                    Location = new Point(20, 30),
                    Size = new Size(220, 160)
                };
                Label label = new Label
                {
                    Location = new Point(180, 20),
                    Size = new Size(120, 30),
                    Text = "Clipped"
                };

                panel.Controls.Add(label);
                form.Controls.Add(panel);
                ShowForm(form);

                Rectangle labelBounds = GetClientBoundsOnScreen(label);
                Rectangle panelBounds = GetClientBoundsOnScreen(panel);
                return Rectangle.Intersect(labelBounds, panelBounds);
            });

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetControlScreenBounds_PrefersAccessibilityBounds_WhenAvailable()
        {
            Rectangle actual = RunInSta(() =>
            {
                using Form form = CreateForm();
                Panel panel = new Panel
                {
                    Location = new Point(20, 30),
                    Size = new Size(220, 160)
                };
                AccessibleBoundsTestControl control = new AccessibleBoundsTestControl
                {
                    Location = new Point(15, 25),
                    Size = new Size(120, 40)
                };

                panel.Controls.Add(control);
                form.Controls.Add(panel);
                ShowForm(form);

                Rectangle baseBounds = GetClientBoundsOnScreen(control);
                control.SetAccessibleBoundsOverride(new Rectangle(
                    baseBounds.Left + 8,
                    baseBounds.Top + 6,
                    baseBounds.Width - 16,
                    baseBounds.Height - 12));

                return ScreenBoundsHelper.GetControlScreenBounds(control);
            });

            Rectangle expected = RunInSta(() =>
            {
                using Form form = CreateForm();
                Panel panel = new Panel
                {
                    Location = new Point(20, 30),
                    Size = new Size(220, 160)
                };
                AccessibleBoundsTestControl control = new AccessibleBoundsTestControl
                {
                    Location = new Point(15, 25),
                    Size = new Size(120, 40)
                };

                panel.Controls.Add(control);
                form.Controls.Add(panel);
                ShowForm(form);

                Rectangle baseBounds = GetClientBoundsOnScreen(control);
                return new Rectangle(
                    baseBounds.Left + 8,
                    baseBounds.Top + 6,
                    baseBounds.Width - 16,
                    baseBounds.Height - 12);
            });

            Assert.AreEqual(expected, actual);
        }

        private static Form CreateForm()
        {
            Form form = new Form
            {
                StartPosition = FormStartPosition.Manual,
                Location = new Point(120, 140),
                Size = new Size(400, 300),
                ShowInTaskbar = false
            };

            ShowForm(form);
            return form;
        }

        private static void ShowForm(Form form)
        {
            form.Show();
            Application.DoEvents();
        }

        private static Rectangle GetWindowBounds(Control control)
        {
            Assert.IsTrue(GetWindowRect(control.Handle, out RECT rect), "GetWindowRect failed.");
            return Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        private static Rectangle GetClientBoundsOnScreen(Control control)
        {
            Assert.IsTrue(GetClientRect(control.Handle, out RECT rect), "GetClientRect failed.");
            POINT[] points =
            {
                new POINT { X = rect.Left, Y = rect.Top },
                new POINT { X = rect.Right, Y = rect.Bottom }
            };

            int mapped = MapWindowPoints(control.Handle, IntPtr.Zero, points, (uint)points.Length);
            Assert.IsTrue(mapped != 0 || Marshal.GetLastWin32Error() == 0, "MapWindowPoints failed.");
            return Rectangle.FromLTRB(points[0].X, points[0].Y, points[1].X, points[1].Y);
        }

        private sealed class AccessibleBoundsTestControl : Control
        {
            private Rectangle accessibleBoundsOverride;

            public void SetAccessibleBoundsOverride(Rectangle bounds)
            {
                accessibleBoundsOverride = bounds;
            }

            protected override AccessibleObject CreateAccessibilityInstance()
            {
                return new AccessibleBoundsAccessibleObject(this);
            }

            private sealed class AccessibleBoundsAccessibleObject : Control.ControlAccessibleObject
            {
                private readonly AccessibleBoundsTestControl owner;

                public AccessibleBoundsAccessibleObject(AccessibleBoundsTestControl owner)
                    : base(owner)
                {
                    this.owner = owner;
                }

                public override Rectangle Bounds => owner.accessibleBoundsOverride;
            }
        }

        private static T RunInSta<T>(Func<T> action)
        {
            Exception exception = null;
            T result = default;

            Thread thread = new Thread(() =>
            {
                try
                {
                    result = action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (exception != null)
            {
                throw new Exception("STA test execution failed.", exception);
            }

            return result;
        }
    }
}
