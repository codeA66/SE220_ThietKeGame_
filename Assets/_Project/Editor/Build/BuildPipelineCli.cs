using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DeadCells.Build.Editor
{
    public readonly struct BuildRequest
    {
        public BuildRequest(BuildTarget target, string outputPath, string version, BuildOptions options)
        {
            Target = target;
            OutputPath = outputPath;
            Version = version;
            Options = options;
        }

        public BuildTarget Target { get; }
        public string OutputPath { get; }
        public string Version { get; }
        public BuildOptions Options { get; }
    }

    /// <summary>
    /// Parameterized Editor & headless CI build entrypoint for Windows x64, WebGL, Linux x64, and macOS.
    /// Compatible with both `unity build --execute-method` (-buildOutput) and raw Unity CLI (-out / -buildTarget).
    /// </summary>
    public static class BuildPipelineCli
    {
        private static readonly HashSet<string> ValueFlags = new(StringComparer.OrdinalIgnoreCase)
        {
            "-buildTarget",
            "-buildOutput",
            "-out",
            "-buildVersion",
            "-version",
        };

        [MenuItem("Tools/DeadCells/Build/Build Windows x64")]
        public static void BuildWindows64Menu()
        {
            ExecuteBuild(new BuildRequest(
                BuildTarget.StandaloneWindows64,
                ResolveDefaultOutputPath(BuildTarget.StandaloneWindows64),
                PlayerSettings.bundleVersion,
                BuildOptions.None));
        }

        [MenuItem("Tools/DeadCells/Build/Build WebGL")]
        public static void BuildWebGLMenu()
        {
            ExecuteBuild(new BuildRequest(
                BuildTarget.WebGL,
                ResolveDefaultOutputPath(BuildTarget.WebGL),
                PlayerSettings.bundleVersion,
                BuildOptions.None));
        }

        public static void BuildFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (!TryParseArguments(args, PlayerSettings.bundleVersion, out BuildRequest request, out string error))
            {
                Debug.LogError($"[BuildPipelineCli] Invalid arguments: {error}");
                ExitIfBatchMode(2);
                return;
            }

            bool ok = ExecuteBuild(request);
            ExitIfBatchMode(ok ? 0 : 1);
        }

        public static bool ExecuteBuild(BuildRequest request)
        {
            if (!CollectEnabledScenes(EditorBuildSettings.scenes, File.Exists, out string[] scenes, out string sceneError))
            {
                Debug.LogError($"[BuildPipelineCli] Scene validation failed: {sceneError}");
                return false;
            }

            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(request.Target);
            if (EditorUserBuildSettings.activeBuildTarget != request.Target)
            {
                Debug.Log($"[BuildPipelineCli] Switching active build target to {request.Target} ({group})...");
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(group, request.Target))
                {
                    Debug.LogError($"[BuildPipelineCli] Failed to switch build target to {request.Target}. Ensure platform module is installed.");
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Version) &&
                !string.Equals(PlayerSettings.bundleVersion, request.Version, StringComparison.Ordinal))
            {
                PlayerSettings.bundleVersion = request.Version;
            }

            EnsureParentDirectory(request.Target, request.OutputPath);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = request.OutputPath,
                target = request.Target,
                targetGroup = group,
                options = request.Options,
            };

            Debug.Log($"[BuildPipelineCli] Starting {request.Target} v{PlayerSettings.bundleVersion} -> '{request.OutputPath}' ({scenes.Length} scene(s))...");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report != null ? report.summary : default;

            if (report != null && summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildPipelineCli] SUCCEEDED: {request.Target} -> '{summary.outputPath}' ({summary.totalSize} bytes, {summary.totalTime.TotalSeconds:F1}s, warnings={summary.totalWarnings}).");
                return true;
            }

            Debug.LogError($"[BuildPipelineCli] FAILED: {request.Target} result={summary.result}, errors={summary.totalErrors}.");
            return false;
        }

        public static bool TryParseArguments(string[] args, string fallbackVersion, out BuildRequest request, out string error)
        {
            request = default;
            error = null;

            BuildTarget target = BuildTarget.StandaloneWindows64;
            string outputPath = null;
            string version = string.IsNullOrWhiteSpace(fallbackVersion) ? "0.1.0" : fallbackVersion.Trim();
            BuildOptions options = BuildOptions.None;

            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string token = args[i];
                    if (string.IsNullOrWhiteSpace(token))
                        continue;

                    if (ValueFlags.Contains(token))
                    {
                        if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]) || args[i + 1].StartsWith("-", StringComparison.Ordinal))
                        {
                            error = $"Missing value after flag '{token}'.";
                            return false;
                        }

                        string val = args[++i].Trim();
                        if (token.Equals("-buildTarget", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!TryParseBuildTarget(val, out target))
                            {
                                error = $"Unsupported -buildTarget '{val}'. Supported: Win64, WebGL, Linux64, OSX.";
                                return false;
                            }
                        }
                        else if (token.Equals("-buildOutput", StringComparison.OrdinalIgnoreCase) ||
                                 token.Equals("-out", StringComparison.OrdinalIgnoreCase))
                        {
                            outputPath = val;
                        }
                        else if (token.Equals("-buildVersion", StringComparison.OrdinalIgnoreCase) ||
                                 token.Equals("-version", StringComparison.OrdinalIgnoreCase))
                        {
                            version = val;
                        }
                    }
                    else if (token.Equals("-dev", StringComparison.OrdinalIgnoreCase) ||
                             token.Equals("-development", StringComparison.OrdinalIgnoreCase))
                    {
                        options |= BuildOptions.Development;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(outputPath))
                outputPath = ResolveDefaultOutputPath(target);

            request = new BuildRequest(target, outputPath, version, options);
            return true;
        }

        public static bool TryParseBuildTarget(string raw, out BuildTarget target)
        {
            switch (raw?.Trim().ToLowerInvariant())
            {
                case "win64":
                case "windows":
                case "standalonewindows64":
                    target = BuildTarget.StandaloneWindows64;
                    return true;
                case "webgl":
                case "web":
                    target = BuildTarget.WebGL;
                    return true;
                case "linux":
                case "linux64":
                case "standalonelinux64":
                    target = BuildTarget.StandaloneLinux64;
                    return true;
                case "osx":
                case "mac":
                case "macos":
                case "standaloneosx":
                    target = BuildTarget.StandaloneOSX;
                    return true;
                default:
                    target = default;
                    return false;
            }
        }

        public static string ResolveDefaultOutputPath(BuildTarget target) => target switch
        {
            BuildTarget.StandaloneWindows64 => "Build/Windows/DeadCells.exe",
            BuildTarget.WebGL => "Build/WebGL",
            BuildTarget.StandaloneLinux64 => "Build/Linux/DeadCells.x86_64",
            BuildTarget.StandaloneOSX => "Build/macOS/DeadCells.app",
            _ => $"Build/{target}/DeadCells",
        };

        public static bool CollectEnabledScenes(
            EditorBuildSettingsScene[] configuredScenes,
            Func<string, bool> fileExists,
            out string[] scenePaths,
            out string error)
        {
            scenePaths = Array.Empty<string>();
            error = null;

            if (configuredScenes == null || configuredScenes.Length == 0)
            {
                error = "EditorBuildSettings.scenes is empty.";
                return false;
            }

            Func<string, bool> checkExists = fileExists ?? File.Exists;
            var list = new List<string>(configuredScenes.Length);
            for (int i = 0; i < configuredScenes.Length; i++)
            {
                EditorBuildSettingsScene scene = configuredScenes[i];
                if (scene == null || !scene.enabled || string.IsNullOrWhiteSpace(scene.path))
                    continue;

                if (!checkExists(scene.path))
                {
                    error = $"Enabled build scene at index {i} does not exist on disk: '{scene.path}'.";
                    return false;
                }

                list.Add(scene.path);
            }

            if (list.Count == 0)
            {
                error = "No enabled scenes found in EditorBuildSettings.scenes.";
                return false;
            }

            scenePaths = list.ToArray();
            return true;
        }

        private static void EnsureParentDirectory(BuildTarget target, string outputPath)
        {
            string dir = target == BuildTarget.WebGL ? outputPath : Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private static void ExitIfBatchMode(int exitCode)
        {
            if (Application.isBatchMode)
                EditorApplication.Exit(exitCode);
        }
    }
}
