using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace DeadCells.Tools.Editor
{
    public static class SortingLayerSetup
    {
        private const string TagManagerAssetPath = "ProjectSettings/TagManager.asset";

        public static readonly string[] RequiredLayerOrder =
        {
            "Default",
            "ParallaxFar",
            "ParallaxMid",
            "Background",
            "Ground",
            "Actors",
            "VFX",
            "Foreground"
        };

        private readonly struct LayerEntry
        {
            public readonly string Name;
            public readonly int UniqueID;
            public readonly bool Locked;

            public LayerEntry(string name, int uniqueId, bool locked)
            {
                Name = name;
                UniqueID = uniqueId;
                Locked = locked;
            }
        }

        public static void EnsureDeadCellsSortingLayers()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(TagManagerAssetPath);
            if (assets == null || assets.Length == 0 || assets[0] == null)
                throw new InvalidOperationException($"Could not load '{TagManagerAssetPath}'.");

            var serializedObject = new SerializedObject(assets[0]);
            SerializedProperty sortingLayersProp = serializedObject.FindProperty("m_SortingLayers");
            if (sortingLayersProp == null || !sortingLayersProp.isArray)
                throw new InvalidOperationException("Property 'm_SortingLayers' not found on TagManager.");

            var existingEntries = ReadExistingEntries(sortingLayersProp);
            var existingByName = new Dictionary<string, LayerEntry>(StringComparer.Ordinal);
            var usedIds = new HashSet<int> { 0 };

            for (int i = 0; i < existingEntries.Count; i++)
            {
                LayerEntry entry = existingEntries[i];
                if (string.IsNullOrEmpty(entry.Name) || existingByName.ContainsKey(entry.Name))
                    continue;

                existingByName.Add(entry.Name, entry);
                if (entry.UniqueID != 0)
                    usedIds.Add(entry.UniqueID);
            }

            var requiredSet = new HashSet<string>(RequiredLayerOrder, StringComparer.Ordinal);
            var targetEntries = new List<LayerEntry>(RequiredLayerOrder.Length + existingEntries.Count);
            var assignedIds = new HashSet<int> { 0 };

            for (int i = 0; i < RequiredLayerOrder.Length; i++)
            {
                string layerName = RequiredLayerOrder[i];
                bool hasExisting = existingByName.TryGetValue(layerName, out LayerEntry existing);
                bool locked = hasExisting && existing.Locked;

                if (layerName == "Default")
                {
                    targetEntries.Add(new LayerEntry(layerName, 0, locked));
                    continue;
                }

                int id;
                if (hasExisting && existing.UniqueID != 0 && assignedIds.Add(existing.UniqueID))
                {
                    id = existing.UniqueID;
                }
                else
                {
                    id = CreateUniqueDeterministicId(layerName, usedIds, assignedIds);
                }

                targetEntries.Add(new LayerEntry(layerName, id, locked));
            }

            var seenExtras = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < existingEntries.Count; i++)
            {
                LayerEntry extra = existingEntries[i];
                if (string.IsNullOrEmpty(extra.Name) || requiredSet.Contains(extra.Name) || !seenExtras.Add(extra.Name))
                    continue;

                int id = extra.UniqueID != 0 && assignedIds.Add(extra.UniqueID)
                    ? extra.UniqueID
                    : CreateUniqueDeterministicId(extra.Name, usedIds, assignedIds);

                targetEntries.Add(new LayerEntry(extra.Name, id, extra.Locked));
            }

            if (EntriesMatch(existingEntries, targetEntries))
                return;

            sortingLayersProp.arraySize = targetEntries.Count;
            for (int i = 0; i < targetEntries.Count; i++)
            {
                SerializedProperty element = sortingLayersProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("name").stringValue = targetEntries[i].Name;
                element.FindPropertyRelative("uniqueID").intValue = targetEntries[i].UniqueID;
                WriteLockedProperty(element.FindPropertyRelative("locked"), targetEntries[i].Locked);
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        public static int GetSortingLayerId(string layerName)
        {
            return SortingLayer.NameToID(layerName);
        }

        private static List<LayerEntry> ReadExistingEntries(SerializedProperty sortingLayersProp)
        {
            var result = new List<LayerEntry>(sortingLayersProp.arraySize);
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                SerializedProperty element = sortingLayersProp.GetArrayElementAtIndex(i);
                string name = element.FindPropertyRelative("name")?.stringValue ?? string.Empty;
                int uniqueId = element.FindPropertyRelative("uniqueID")?.intValue ?? 0;
                bool locked = ReadLockedProperty(element.FindPropertyRelative("locked"));
                result.Add(new LayerEntry(name, uniqueId, locked));
            }

            return result;
        }

        private static bool ReadLockedProperty(SerializedProperty prop)
        {
            if (prop == null)
                return false;

            return prop.propertyType == SerializedPropertyType.Boolean
                ? prop.boolValue
                : prop.intValue != 0;
        }

        private static void WriteLockedProperty(SerializedProperty prop, bool locked)
        {
            if (prop == null)
                return;

            if (prop.propertyType == SerializedPropertyType.Boolean)
                prop.boolValue = locked;
            else
                prop.intValue = locked ? 1 : 0;
        }

        private static bool EntriesMatch(List<LayerEntry> current, List<LayerEntry> target)
        {
            if (current.Count != target.Count)
                return false;

            for (int i = 0; i < current.Count; i++)
            {
                if (!string.Equals(current[i].Name, target[i].Name, StringComparison.Ordinal) ||
                    current[i].UniqueID != target[i].UniqueID ||
                    current[i].Locked != target[i].Locked)
                {
                    return false;
                }
            }

            return true;
        }

        private static int CreateUniqueDeterministicId(string layerName, HashSet<int> usedIds, HashSet<int> assignedIds)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int i = 0; i < layerName.Length; i++)
                {
                    hash ^= layerName[i];
                    hash *= 16777619u;
                }

                int candidate = (int)(hash & 0x7FFFFFFFu);
                if (candidate == 0)
                    candidate = 1;

                while (usedIds.Contains(candidate) || assignedIds.Contains(candidate))
                {
                    candidate = candidate == int.MaxValue ? 1 : candidate + 1;
                }

                usedIds.Add(candidate);
                assignedIds.Add(candidate);
                return candidate;
            }
        }
    }

    public static class ReleaseSettingsSetup
    {
        private const string SandboxPlayerScenePath = "Assets/_Project/Scenes/Sandbox_Player.unity";
        private const string QualitySettingsAssetPath = "ProjectSettings/QualitySettings.asset";
        private const string ProjectSettingsAssetPath = "ProjectSettings/ProjectSettings.asset";
        private const string ManifestJsonPath = "Packages/manifest.json";
        private const double PackagePruneTimeoutSeconds = 600.0;

        // Verified 0-reference assets at HEAD (EnemyGoldDrops.cs excluded: still referenced by
        // Main_Level.unity, NhaGiamGiengChuong.unity, MainLevelIntegrityTests, MapWiringTests,
        // NhaGiamGiengChuongBuilder, and MainLevelSceneRemediator).
        private static readonly string[] DeadAssetPaths =
        {
            "Assets/_Project/Features/Combat/Scripts/TrainingDummy.cs",
            "Assets/_Project/Art/Shaders/MobFleshSwap2D.shader",
            "Assets/_Project/Art/Shaders/PaletteSwap2D.shader",
        };

        // Verified 0-dependency direct entries in Packages/manifest.json (zero script/scene/prefab/asmdef/package dependents).
        private static readonly string[] UnusedPackagesToRemove =
        {
            "com.unity.visualscripting",
            "com.unity.ai.inference",
            "com.unity.cinemachine",
            "com.unity.timeline",
            "com.unity.multiplayer.center",
            "com.unity.recorder",
            "com.unity.modules.cloth",
            "com.unity.modules.terrain",
            "com.unity.modules.terrainphysics",
            "com.unity.modules.vehicles",
            "com.unity.modules.wind",
            "com.unity.modules.xr",
        };

        private static AddAndRemoveRequest _pruneRequest;
        private static double _pruneDeadline;

        [MenuItem("Tools/Project/Fix Sandbox_Player URP 2D Lighting")]
        public static void ApplySandboxPlayerLighting()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.OpenScene(SandboxPlayerScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
                throw new InvalidOperationException($"Failed to open '{SandboxPlayerScenePath}'.");

            Camera mainCam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (mainCam == null)
                throw new InvalidOperationException($"Main Camera missing in '{SandboxPlayerScenePath}'.");

            bool changed = false;
            if (!mainCam.TryGetComponent<UniversalAdditionalCameraData>(out var uacd))
            {
                uacd = mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                EditorUtility.SetDirty(uacd);
                changed = true;
            }

            bool hasGlobalLight = false;
            Light2D[] lights = UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].lightType == Light2D.LightType.Global)
                {
                    hasGlobalLight = true;
                    break;
                }
            }

            if (!hasGlobalLight)
            {
                var lightGo = new GameObject("Global Light 2D");
                var globalLight = lightGo.AddComponent<Light2D>();
                globalLight.lightType = Light2D.LightType.Global;
                globalLight.intensity = 1f;
                globalLight.color = Color.white;
                EditorUtility.SetDirty(lightGo);
                EditorUtility.SetDirty(globalLight);
                changed = true;
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log($"[ReleaseSettingsSetup] Sandbox_Player URP 2D lighting verified (changed={changed}).");
        }

        [MenuItem("Tools/Project/Apply Release Quality & WebGL Settings")]
        public static void ApplyReleaseQualityAndWebGlSettings()
        {
            UnityEngine.Object[] qualityAssets = AssetDatabase.LoadAllAssetsAtPath(QualitySettingsAssetPath);
            if (qualityAssets == null || qualityAssets.Length == 0 || qualityAssets[0] == null)
                throw new InvalidOperationException($"Could not load '{QualitySettingsAssetPath}'.");

            UnityEngine.Object qualityAsset = qualityAssets[0];
            var soQuality = new SerializedObject(qualityAsset);
            SerializedProperty levelsProp = soQuality.FindProperty("m_QualitySettings");
            SerializedProperty perPlatformProp = soQuality.FindProperty("m_PerPlatformDefaultQuality");
            if (levelsProp == null || !levelsProp.isArray || perPlatformProp == null || !perPlatformProp.isArray)
                throw new InvalidOperationException("Unexpected QualitySettings serialization layout.");

            int highIndex = FindQualityLevelIndex(levelsProp, "High");
            int lowIndex = FindQualityLevelIndex(levelsProp, "Low");
            if (highIndex < 0)
                throw new InvalidOperationException("Quality level 'High' not found in QualitySettings.asset.");
            if (lowIndex < 0)
                throw new InvalidOperationException("Quality level 'Low' not found in QualitySettings.asset.");

            SetQualityLevelVSync(levelsProp.GetArrayElementAtIndex(highIndex), 1);
            SetQualityLevelVSync(levelsProp.GetArrayElementAtIndex(lowIndex), 0);

            string standaloneKey = NamedBuildTarget.Standalone.TargetName;
            string webGlKey = NamedBuildTarget.WebGL.TargetName;
            SetPerPlatformDefaultQuality(perPlatformProp, standaloneKey, highIndex);
            SetPerPlatformDefaultQuality(perPlatformProp, webGlKey, lowIndex);

            SerializedProperty currentQualityProp = soQuality.FindProperty("m_CurrentQuality");
            if (currentQualityProp != null)
                currentQualityProp.intValue = highIndex;

            soQuality.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(qualityAsset);

            if (!QualitySettings.IsPlatformIncluded(standaloneKey, highIndex))
                QualitySettings.TryIncludePlatformAt(standaloneKey, highIndex, out _);
            if (!QualitySettings.IsPlatformIncluded(webGlKey, lowIndex))
                QualitySettings.TryIncludePlatformAt(webGlKey, lowIndex, out _);

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.stripEngineCode = true;

            UnityEngine.Object[] projectAssets = AssetDatabase.LoadAllAssetsAtPath(ProjectSettingsAssetPath);
            if (projectAssets != null && projectAssets.Length > 0 && projectAssets[0] != null)
                EditorUtility.SetDirty(projectAssets[0]);

            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[ReleaseSettingsSetup] Quality & WebGL settings applied: " +
                $"{standaloneKey}={highIndex} (High, vSync=1), {webGlKey}={lowIndex} (Low, vSync=0), " +
                $"compression={PlayerSettings.WebGL.compressionFormat}, " +
                $"decompressionFallback={PlayerSettings.WebGL.decompressionFallback}, " +
                $"dataCaching={PlayerSettings.WebGL.dataCaching}, " +
                $"stripEngineCode={PlayerSettings.stripEngineCode}");
        }

        [MenuItem("Tools/Project/Delete Confirmed Dead Assets")]
        public static void DeleteDeadAssets()
        {
            if (!Application.isBatchMode &&
                !EditorUtility.DisplayDialog(
                    "Delete Confirmed Dead Assets",
                    "Delete the verified unused script/shader assets via AssetDatabase?",
                    "Delete",
                    "Cancel"))
            {
                return;
            }

            int deletedCount = 0;
            int skippedCount = 0;
            var failedPaths = new List<string>();

            for (int i = 0; i < DeadAssetPaths.Length; i++)
            {
                string assetPath = DeadAssetPaths[i];
                if (!File.Exists(assetPath) && string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath)))
                {
                    skippedCount++;
                    continue;
                }

                if (AssetDatabase.DeleteAsset(assetPath))
                {
                    deletedCount++;
                    Debug.Log($"[ReleaseSettingsSetup] Deleted dead asset: '{assetPath}'.");
                }
                else
                {
                    failedPaths.Add(assetPath);
                    Debug.LogError($"[ReleaseSettingsSetup] Failed to delete dead asset: '{assetPath}'.");
                }
            }

            if (deletedCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (failedPaths.Count > 0)
                throw new InvalidOperationException($"Failed to delete dead assets: {string.Join(", ", failedPaths)}");

            Debug.Log($"[ReleaseSettingsSetup] DeleteDeadAssets completed (deleted={deletedCount}, skipped={skippedCount}).");
        }

        // Invoke headless WITHOUT -quit:
        // -batchmode -nographics -projectPath <path> -executeMethod DeadCells.Tools.Editor.ReleaseSettingsSetup.PruneUnusedPackages -logFile -
        [MenuItem("Tools/Project/Prune Unused UPM Packages")]
        public static void PruneUnusedPackages()
        {
            if (_pruneRequest != null)
            {
                Debug.LogWarning("[ReleaseSettingsSetup] PruneUnusedPackages is already running.");
                return;
            }

            if (!Application.isBatchMode &&
                !EditorUtility.DisplayDialog(
                    "Prune Unused UPM Packages",
                    "Remove verified unused UPM packages via PackageManager.Client.AddAndRemove?",
                    "Prune",
                    "Cancel"))
            {
                return;
            }

            string[] toRemove = FilterPresentPackages(UnusedPackagesToRemove);
            if (toRemove.Length == 0)
            {
                Debug.Log("[ReleaseSettingsSetup] PruneUnusedPackages: all target packages already absent from Packages/manifest.json.");
                if (Application.isBatchMode)
                    EditorApplication.Exit(0);
                return;
            }

            Debug.Log($"[ReleaseSettingsSetup] Removing unused packages: {string.Join(", ", toRemove)}");
            _pruneRequest = Client.AddAndRemove(null, toRemove);
            _pruneDeadline = EditorApplication.timeSinceStartup + PackagePruneTimeoutSeconds;
            EditorApplication.update += PollPruneUnusedPackages;
        }

        private static string[] FilterPresentPackages(string[] candidates)
        {
            if (!File.Exists(ManifestJsonPath))
                return candidates;

            string manifestJson = File.ReadAllText(ManifestJsonPath);
            var present = new List<string>(candidates.Length);
            for (int i = 0; i < candidates.Length; i++)
            {
                string pkg = candidates[i];
                if (manifestJson.IndexOf($"\"{pkg}\"", StringComparison.Ordinal) >= 0)
                    present.Add(pkg);
            }
            return present.ToArray();
        }

        private static void PollPruneUnusedPackages()
        {
            if (_pruneRequest == null)
            {
                EditorApplication.update -= PollPruneUnusedPackages;
                return;
            }

            if (!_pruneRequest.IsCompleted)
            {
                if (EditorApplication.timeSinceStartup > _pruneDeadline)
                {
                    EditorApplication.update -= PollPruneUnusedPackages;
                    _pruneRequest = null;
                    Debug.LogError("[ReleaseSettingsSetup] PruneUnusedPackages timed out waiting for PackageManager.");
                    if (Application.isBatchMode)
                        EditorApplication.Exit(2);
                }
                return;
            }

            EditorApplication.update -= PollPruneUnusedPackages;
            AddAndRemoveRequest completed = _pruneRequest;
            _pruneRequest = null;

            if (completed.Status == StatusCode.Success)
            {
                string resolved = completed.Result != null
                    ? string.Join(", ", completed.Result.Select(p => $"{p.name}@{p.version}"))
                    : "none";
                Debug.Log($"[ReleaseSettingsSetup] PruneUnusedPackages succeeded. Resolved packages: {resolved}");
                if (Application.isBatchMode)
                    EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[ReleaseSettingsSetup] PruneUnusedPackages failed: {completed.Error?.message}");
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
            }
        }

        private static int FindQualityLevelIndex(SerializedProperty levelsProp, string levelName)
        {
            for (int i = 0; i < levelsProp.arraySize; i++)
            {
                SerializedProperty nameProp = levelsProp.GetArrayElementAtIndex(i).FindPropertyRelative("name");
                if (nameProp != null && string.Equals(nameProp.stringValue, levelName, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        private static void SetQualityLevelVSync(SerializedProperty levelProp, int vSyncCount)
        {
            SerializedProperty vsyncProp = levelProp.FindPropertyRelative("vSyncCount");
            if (vsyncProp == null)
                throw new InvalidOperationException("Property 'vSyncCount' not found on quality level.");

            vsyncProp.intValue = vSyncCount;
        }

        private static void SetPerPlatformDefaultQuality(SerializedProperty mapProp, string platformKey, int qualityIndex)
        {
            for (int i = 0; i < mapProp.arraySize; i++)
            {
                SerializedProperty entry = mapProp.GetArrayElementAtIndex(i);
                SerializedProperty keyProp = entry.FindPropertyRelative("first");
                if (keyProp != null && string.Equals(keyProp.stringValue, platformKey, StringComparison.Ordinal))
                {
                    entry.FindPropertyRelative("second").intValue = qualityIndex;
                    return;
                }
            }

            int insertIdx = mapProp.arraySize;
            mapProp.InsertArrayElementAtIndex(insertIdx);
            SerializedProperty newEntry = mapProp.GetArrayElementAtIndex(insertIdx);
            newEntry.FindPropertyRelative("first").stringValue = platformKey;
            newEntry.FindPropertyRelative("second").intValue = qualityIndex;
        }
    }
}
