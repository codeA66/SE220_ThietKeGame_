using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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
}
