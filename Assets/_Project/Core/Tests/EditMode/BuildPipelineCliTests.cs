using System.IO;
using DeadCells.Build.Editor;
using NUnit.Framework;
using UnityEditor;

namespace DeadCells.Core.Tests
{
    [TestFixture]
    public sealed class BuildPipelineCliTests
    {
        [Test]
        public void TryParseArguments_DefaultsToWindows64ReleaseWithFallbackVersion()
        {
            bool ok = BuildPipelineCli.TryParseArguments(
                new[] { "Unity.exe", "-batchmode" },
                "0.3.1",
                out BuildRequest req,
                out string error);

            Assert.IsTrue(ok, error);
            Assert.AreEqual(BuildTarget.StandaloneWindows64, req.Target);
            Assert.AreEqual("Build/Windows/DeadCells.exe", req.OutputPath);
            Assert.AreEqual("0.3.1", req.Version);
            Assert.AreEqual(BuildOptions.None, req.Options);
        }

        [Test]
        public void TryParseArguments_ParsesWebGLBuildOutputVersionAndDevFlag()
        {
            string[] args =
            {
                "Unity.exe",
                "-buildTarget", "WebGL",
                "-buildOutput", "Build/WebOut",
                "-buildVersion", "1.2.0-rc1",
                "-dev",
            };

            bool ok = BuildPipelineCli.TryParseArguments(args, "0.1.0", out BuildRequest req, out string error);

            Assert.IsTrue(ok, error);
            Assert.AreEqual(BuildTarget.WebGL, req.Target);
            Assert.AreEqual("Build/WebOut", req.OutputPath);
            Assert.AreEqual("1.2.0-rc1", req.Version);
            Assert.AreEqual(BuildOptions.Development, req.Options);
        }

        [Test]
        public void TryParseArguments_RejectsUnsupportedTargetAndMissingFlagValue()
        {
            Assert.IsFalse(BuildPipelineCli.TryParseArguments(
                new[] { "-buildTarget", "Dreamcast" },
                "0.1.0",
                out _,
                out string badTargetErr));
            StringAssert.Contains("Unsupported -buildTarget", badTargetErr);

            Assert.IsFalse(BuildPipelineCli.TryParseArguments(
                new[] { "-out" },
                "0.1.0",
                out _,
                out string missingValErr));
            StringAssert.Contains("Missing value", missingValErr);
        }

        [Test]
        public void CollectEnabledScenes_FiltersDisabledAndRejectsMissingFiles()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/_Project/Scenes/A.unity", true),
                new EditorBuildSettingsScene("Assets/_Project/Scenes/Disabled.unity", false),
            };

            Assert.IsTrue(BuildPipelineCli.CollectEnabledScenes(scenes, p => p.EndsWith("A.unity"), out string[] valid, out string err), err);
            Assert.AreEqual(new[] { "Assets/_Project/Scenes/A.unity" }, valid);

            Assert.IsFalse(BuildPipelineCli.CollectEnabledScenes(scenes, _ => false, out _, out string missingErr));
            StringAssert.Contains("does not exist", missingErr);
        }

        [Test]
        public void CollectEnabledScenes_ProjectBuildSettingsAreValidOnDisk()
        {
            bool ok = BuildPipelineCli.CollectEnabledScenes(
                EditorBuildSettings.scenes,
                File.Exists,
                out string[] scenes,
                out string error);

            Assert.IsTrue(ok, error);
            Assert.GreaterOrEqual(scenes.Length, 1);
            Assert.AreEqual("Assets/_Project/Scenes/NhaGiamGiengChuong.unity", scenes[0]);
        }
    }
}
