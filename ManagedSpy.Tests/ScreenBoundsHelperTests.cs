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
                return ScreenBoundsHelper.GetControlScreenBounds(form, preferAccessibility: true);
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

                return ScreenBoundsHelper.GetControlScreenBounds(label, preferAccessibility: true);
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

                return ScreenBoundsHelper.GetControlScreenBounds(label, preferAccessibility: true);
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

                return ScreenBoundsHelper.GetControlScreenBounds(control, preferAccessibility: true);
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

        [TestMethod]
        public void GetControlScreenBounds_FallsBackToNativeBounds_WhenAccessibilityBoundsDoNotMatchControl()
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
                    baseBounds.Left,
                    baseBounds.Bottom + 80,
                    baseBounds.Width,
                    baseBounds.Height));

                return ScreenBoundsHelper.GetControlScreenBounds(control, preferAccessibility: true);
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

                return GetClientBoundsOnScreen(control);
            });

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ShouldUseRawWindowDpiFallback_ReturnsTrue_ForOptionButtonLogSample()
        {
            Rectangle candidate = new Rectangle(1224, 755, 1391, 120);
            Rectangle raw = new Rectangle(699, 431, 795, 69);
            Rectangle root = new Rectangle(676, 246, 842, 695);

            Assert.IsTrue(ScreenBoundsHelper.ShouldUseRawWindowDpiFallback(candidate, raw, root));
        }

        [TestMethod]
        public void ShouldUseRawWindowDpiFallback_ReturnsTrue_ForButtonLogSample()
        {
            Rectangle candidate = new Rectangle(2361, 1570, 122, 54);
            Rectangle raw = new Rectangle(1349, 897, 70, 31);
            Rectangle root = new Rectangle(676, 246, 842, 695);

            Assert.IsTrue(ScreenBoundsHelper.ShouldUseRawWindowDpiFallback(candidate, raw, root));
        }

        [TestMethod]
        public void ShouldUseRawWindowDpiFallback_ReturnsFalse_WhenScaleSignatureIsMissing()
        {
            Rectangle candidate = new Rectangle(900, 520, 950, 120);
            Rectangle raw = new Rectangle(699, 431, 795, 69);
            Rectangle root = new Rectangle(676, 246, 842, 695);

            Assert.IsFalse(ScreenBoundsHelper.ShouldUseRawWindowDpiFallback(candidate, raw, root));
        }

        [TestMethod]
        public void ShouldUseRawWindowDpiFallback_ReturnsFalse_WhenRawDoesNotFitRootBetter()
        {
            Rectangle candidate = new Rectangle(1224, 755, 1391, 120);
            Rectangle raw = new Rectangle(699, 431, 795, 69);
            Rectangle root = new Rectangle(0, 0, 3000, 2000);

            Assert.IsFalse(ScreenBoundsHelper.ShouldUseRawWindowDpiFallback(candidate, raw, root));
        }

        [TestMethod]
        public void ShouldUseRawWindowDpiFallback_ReturnsTrue_WhenScaledCandidateOverlapsButSpillsOutsideRoot()
        {
            Rectangle candidate = new Rectangle(365, 1003, 1801, 161);
            Rectangle raw = new Rectangle(209, 573, 1029, 92);
            Rectangle root = new Rectangle(56, 237, 1265, 1043);

            Assert.IsTrue(ScreenBoundsHelper.ShouldUseRawWindowDpiFallback(candidate, raw, root));
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
