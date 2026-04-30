using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedSpy.Tests
{
    internal static class ArtifactTestEnvironment
    {
        public const string ManagedSpyWindowTitle = "Managed Spy";
        public const string TestTargetWindowTitle = "ManagedSpy UIA Test Target";

        public static string OutputDirectory
        {
            get { return AppContext.BaseDirectory; }
        }

        public static string ManagedSpyExecutablePath
        {
            get { return Path.Combine(OutputDirectory, "ManagedSpy.exe"); }
        }

        public static string TestTargetExecutablePath
        {
            get { return Path.Combine(OutputDirectory, "ManagedSpy.TestTarget.exe"); }
        }

        public static bool HasRunnableManagedSpyOutput
        {
            get
            {
                return
                    File.Exists(ManagedSpyExecutablePath) &&
                    File.Exists(Path.Combine(OutputDirectory, "ManagedSpy.dll")) &&
                    File.Exists(Path.Combine(OutputDirectory, "ManagedSpy.runtimeconfig.json")) &&
                    File.Exists(Path.Combine(OutputDirectory, "ManagedSpyHook.dll")) &&
                    File.Exists(Path.Combine(OutputDirectory, "ManagedSpyHook.deps.json")) &&
                    File.Exists(Path.Combine(OutputDirectory, "ManagedSpyHook.runtimeconfig.json")) &&
                    File.Exists(Path.Combine(OutputDirectory, "Ijwhost.dll"));
            }
        }

        public static void RequireRunnableManagedSpyOutput()
        {
            if (!HasRunnableManagedSpyOutput)
            {
                Assert.Inconclusive(
                    "This test requires full MSBuild release artifacts. Run .\\build.ps1, then dotnet vstest artifacts\\release\\<platform>\\ManagedSpy.Tests.dll /Platform:<platform>.");
            }
        }

        public static void RequireInteractiveDesktop()
        {
            if (!Environment.UserInteractive)
            {
                Assert.Inconclusive("This test requires an interactive Windows desktop session.");
            }
        }
    }
}
