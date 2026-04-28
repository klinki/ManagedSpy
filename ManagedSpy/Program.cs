using System;
using System.Windows.Forms;
using ManagedSpyLib;

namespace ManagedSpy {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            StartupInitialization.Initialize(
                Application.EnableVisualStyles,
                Application.SetCompatibleTextRenderingDefault,
                MessageFilters.Initialize);
            Application.Run(new MainForm());
        }
    }
}
