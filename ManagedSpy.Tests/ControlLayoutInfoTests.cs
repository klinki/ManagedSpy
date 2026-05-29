using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.ManagedSpy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedSpy.Tests
{
    [TestClass]
    public class ControlLayoutInfoTests
    {
        [TestMethod]
        public void FromControl_ReturnsNormalizedBoxModelRectangles()
        {
            RunInSta(() =>
            {
                using Form form = new Form
                {
                    StartPosition = FormStartPosition.Manual,
                    Location = new Point(100, 120),
                    Size = new Size(360, 260)
                };
                Panel panel = new Panel
                {
                    Location = new Point(20, 30),
                    Size = new Size(200, 120),
                    Margin = new Padding(3, 5, 7, 11),
                    Padding = new Padding(13, 17, 19, 23),
                    AutoScroll = true,
                    AutoScrollMinSize = new Size(260, 180)
                };

                form.Controls.Add(panel);
                ShowForm(form);

                ControlLayoutInfo actual = ControlLayoutInfo.FromControl(panel);
                Assert.IsNotNull(actual);
                Assert.AreEqual(3, actual.Margin.Left);
                Assert.AreEqual(5, actual.Margin.Top);
                Assert.AreEqual(7, actual.Margin.Right);
                Assert.AreEqual(11, actual.Margin.Bottom);
                Assert.AreEqual(13, actual.Padding.Left);
                Assert.AreEqual(17, actual.Padding.Top);
                Assert.AreEqual(19, actual.Padding.Right);
                Assert.AreEqual(23, actual.Padding.Bottom);

                Rectangle expectedBounds = ScreenBounds(form, panel.Bounds, form);
                Rectangle expectedMarginBounds = ScreenBounds(
                    form,
                    Rectangle.FromLTRB(
                        panel.Bounds.Left - panel.Margin.Left,
                        panel.Bounds.Top - panel.Margin.Top,
                        panel.Bounds.Right + panel.Margin.Right,
                        panel.Bounds.Bottom + panel.Margin.Bottom),
                    form);

                Assert.AreEqual(expectedBounds, actual.BoundsScreen);
                Assert.AreEqual(expectedMarginBounds, actual.MarginBoundsScreen);
                Assert.AreEqual(ScreenBounds(panel, panel.ClientRectangle, panel), actual.ClientBoundsScreen);
                Assert.AreEqual(ScreenBounds(panel, panel.DisplayRectangle, panel), actual.ContentBoundsScreen);
                Assert.AreEqual(panel.Size, actual.ElementSize);
                Assert.AreEqual(panel.DisplayRectangle.Size, actual.ContentSize);
                Assert.AreEqual(actual.MarginBoundsScreen, actual.GetSectionBounds(LayoutSection.Margin));
                Assert.AreEqual(actual.BoundsScreen, actual.GetSectionBounds(LayoutSection.Element));
                Assert.AreEqual(actual.ClientBoundsScreen, actual.GetSectionBounds(LayoutSection.Padding));
                Assert.AreEqual(actual.ContentBoundsScreen, actual.GetSectionBounds(LayoutSection.Content));
            });
        }

        [TestMethod]
        public void FromControl_ClipsNestedLayoutRectanglesToVisibleAncestorArea()
        {
            RunInSta(() =>
            {
                using Form form = new Form
                {
                    StartPosition = FormStartPosition.Manual,
                    Location = new Point(120, 140),
                    ClientSize = new Size(240, 180)
                };
                Panel panel = new Panel
                {
                    Location = new Point(190, 28),
                    Size = new Size(110, 96),
                    Margin = new Padding(5, 7, 11, 13),
                    Padding = new Padding(9, 11, 13, 15),
                    BorderStyle = BorderStyle.FixedSingle,
                    AutoScroll = true,
                    AutoScrollMinSize = new Size(180, 140)
                };

                form.Controls.Add(panel);
                ShowForm(form);

                ControlLayoutInfo actual = ControlLayoutInfo.FromControl(panel);
                Rectangle expectedBounds = ScreenBounds(form, panel.Bounds, form);
                Rectangle expectedMarginBounds = ScreenBounds(
                    form,
                    Rectangle.FromLTRB(
                        panel.Bounds.Left - panel.Margin.Left,
                        panel.Bounds.Top - panel.Margin.Top,
                        panel.Bounds.Right + panel.Margin.Right,
                        panel.Bounds.Bottom + panel.Margin.Bottom),
                    form);
                Rectangle expectedClientBounds = ScreenBounds(panel, panel.ClientRectangle, panel);
                Rectangle expectedContentBounds = ScreenBounds(panel, panel.DisplayRectangle, panel);

                Assert.AreEqual(expectedBounds, actual.BoundsScreen);
                Assert.AreEqual(expectedMarginBounds, actual.MarginBoundsScreen);
                Assert.AreEqual(expectedClientBounds, actual.ClientBoundsScreen);
                Assert.AreEqual(expectedContentBounds, actual.ContentBoundsScreen);
                Assert.IsTrue(actual.BoundsScreen.Width < panel.Width, "Expected the clipped element bounds to be narrower than the full control width.");
                Assert.IsTrue(actual.ClientBoundsScreen.Width < panel.ClientSize.Width, "Expected the clipped client bounds to be narrower than the full client width.");
            });
        }

        [TestMethod]
        public void LayoutHighlightMapping_AnchorsSectionToResolvedElementRectangle()
        {
            MethodInfo method = typeof(global::ManagedSpy.MainForm).GetMethod(
                "MapLayoutSectionToOverlayRectangle",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);

            Rectangle sourceElement = new Rectangle(150, 200, 300, 180);
            Rectangle sourceContent = new Rectangle(180, 245, 210, 90);
            Rectangle resolvedElement = new Rectangle(100, 120, 200, 120);

            Rectangle actual = (Rectangle)method.Invoke(
                null,
                new object[] { sourceElement, sourceContent, resolvedElement });

            Assert.AreEqual(new Rectangle(120, 150, 140, 60), actual);
        }

        private static void RunInSta(Action action)
        {
            Exception exception = null;
            Thread thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception caughtException)
                {
                    exception = caughtException;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (exception != null)
            {
                throw exception;
            }
        }

        private static void ShowForm(Form form)
        {
            form.Show();
            Application.DoEvents();
        }

        private static Rectangle ScreenBounds(Control coordinateControl, Rectangle relativeBounds, Control clipStart)
        {
            Rectangle bounds = coordinateControl.RectangleToScreen(relativeBounds);
            for (Control ancestor = clipStart; ancestor != null; ancestor = ancestor.Parent)
            {
                bounds = Rectangle.Intersect(bounds, ancestor.RectangleToScreen(ancestor.ClientRectangle));
                if (bounds.Width <= 0 || bounds.Height <= 0)
                {
                    return Rectangle.Empty;
                }
            }

            return bounds;
        }
    }
}
