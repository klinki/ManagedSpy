using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Microsoft.ManagedSpy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedSpy.Tests
{
    [TestClass]
    public class ScreenBoundsHelperTests
    {
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
                return form.Bounds;
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

                return label.RectangleToScreen(label.ClientRectangle);
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
