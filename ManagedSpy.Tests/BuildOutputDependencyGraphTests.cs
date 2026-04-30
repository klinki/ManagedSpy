using System;
using System.IO;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedSpy.Tests
{
    [TestClass]
    public class BuildOutputDependencyGraphTests
    {
        [TestMethod]
        public void ManagedSpyDepsJson_UsesManagedSpyLibAsManagedRuntimeAsset()
        {
            string outputDirectory = AppContext.BaseDirectory;
            string managedSpyDepsPath = Path.Combine(outputDirectory, "ManagedSpy.deps.json");
            string managedSpyLibPath = Path.Combine(outputDirectory, "ManagedSpyLib.dll");

            Assert.IsTrue(File.Exists(managedSpyDepsPath), "ManagedSpy.deps.json should be present in the test output.");
            Assert.IsTrue(File.Exists(managedSpyLibPath), "ManagedSpyLib.dll should be present beside the built application.");

            using JsonDocument depsDocument = JsonDocument.Parse(File.ReadAllText(managedSpyDepsPath));
            string runtimeTargetName = depsDocument.RootElement
                .GetProperty("runtimeTarget")
                .GetProperty("name")
                .GetString();

            JsonElement runtimeSection = depsDocument.RootElement
                .GetProperty("targets")
                .GetProperty(runtimeTargetName)
                .GetProperty("ManagedSpyLib/1.0.0")
                .GetProperty("runtime");

            Assert.IsTrue(
                runtimeSection.TryGetProperty("ManagedSpyLib.dll", out _),
                "ManagedSpy.deps.json should expose ManagedSpyLib.dll as the managed runtime asset.");

            Assert.IsFalse(
                runtimeSection.TryGetProperty("ManagedSpyHook.dll", out _),
                "ManagedSpyHook.dll must not replace ManagedSpyLib.dll in ManagedSpy.deps.json.");
        }

        [TestMethod]
        public void RunnableOutput_DeploysNativeHookArtifacts()
        {
            ArtifactTestEnvironment.RequireRunnableManagedSpyOutput();

            string outputDirectory = AppContext.BaseDirectory;
            Assert.IsTrue(
                File.Exists(Path.Combine(outputDirectory, "ManagedSpyHook.dll")),
                "ManagedSpyHook.dll should be deployed beside the built application.");
            Assert.IsTrue(
                File.Exists(Path.Combine(outputDirectory, "ManagedSpyHook.deps.json")),
                "ManagedSpyHook.deps.json should be deployed beside the built application.");
            Assert.IsTrue(
                File.Exists(Path.Combine(outputDirectory, "ManagedSpyHook.runtimeconfig.json")),
                "ManagedSpyHook.runtimeconfig.json should be deployed beside the built application.");
            Assert.IsTrue(
                File.Exists(Path.Combine(outputDirectory, "Ijwhost.dll")),
                "Ijwhost.dll should be deployed beside the C++/CLI hook shim.");
        }
    }
}
