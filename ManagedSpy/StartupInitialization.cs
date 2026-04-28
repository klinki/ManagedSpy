using System;

namespace ManagedSpy
{
    public static class StartupInitialization
    {
        public static void Initialize(
            Action enableVisualStyles,
            Action<bool> setCompatibleTextRenderingDefault,
            Action initializeMessageFilters)
        {
            if (enableVisualStyles == null)
            {
                throw new ArgumentNullException(nameof(enableVisualStyles));
            }

            if (setCompatibleTextRenderingDefault == null)
            {
                throw new ArgumentNullException(nameof(setCompatibleTextRenderingDefault));
            }

            if (initializeMessageFilters == null)
            {
                throw new ArgumentNullException(nameof(initializeMessageFilters));
            }

            enableVisualStyles();
            setCompatibleTextRenderingDefault(false);
            initializeMessageFilters();
        }
    }
}
