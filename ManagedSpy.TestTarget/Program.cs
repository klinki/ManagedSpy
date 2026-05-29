using System;
using System.Drawing;
using System.Windows.Forms;

namespace ManagedSpy.TestTarget
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TestTargetForm());
        }
    }

    internal sealed class TestTargetForm : Form
    {
        private SecondaryTargetForm secondaryForm;

        public TestTargetForm()
        {
            Text = "ManagedSpy UIA Test Target";
            Name = "ManagedSpyTestTargetMainForm";
            AccessibleName = Text;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(80, 80);
            Size = new Size(420, 260);

            Panel panel = new Panel
            {
                Name = "TestTargetPanel",
                AccessibleName = "Test target panel",
                Location = new Point(20, 20),
                Size = new Size(360, 160),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label label = new Label
            {
                Name = "TestTargetLabel",
                AccessibleName = "Test target label",
                Text = "Known label text",
                Location = new Point(16, 18),
                AutoSize = true
            };

            TextBox textBox = new TextBox
            {
                Name = "TestTargetTextBox",
                AccessibleName = "Test target text box",
                Text = "Known text box value",
                Location = new Point(16, 48),
                Width = 220
            };

            Button button = new Button
            {
                Name = "TestTargetButton",
                AccessibleName = "Test target button",
                Text = "Trigger",
                Location = new Point(16, 86),
                Size = new Size(100, 28)
            };
            button.Click += delegate
            {
                label.Text = "Button clicked";
            };

            panel.Controls.Add(label);
            panel.Controls.Add(textBox);
            panel.Controls.Add(button);
            Controls.Add(panel);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (secondaryForm == null || secondaryForm.IsDisposed)
            {
                secondaryForm = new SecondaryTargetForm();
                secondaryForm.Show();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (secondaryForm != null && !secondaryForm.IsDisposed)
            {
                secondaryForm.Close();
            }

            base.OnFormClosed(e);
        }
    }

    internal sealed class SecondaryTargetForm : Form
    {
        public SecondaryTargetForm()
        {
            Text = "ManagedSpy UIA Secondary Target";
            Name = "ManagedSpyTestTargetSecondaryForm";
            AccessibleName = Text;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(540, 80);
            Size = new Size(320, 180);

            Label label = new Label
            {
                Name = "SecondaryTargetLabel",
                AccessibleName = "Secondary target label",
                Text = "Second top-level form",
                Location = new Point(20, 20),
                AutoSize = true
            };

            Controls.Add(label);
        }
    }
}
