using System;
using UnityEditor;
using UnityEngine;

namespace DeadCells.Tools.Editor
{
    public static class EnvironmentMaterialBuilder
    {
        public const string MaterialDirectory = "Assets/_Project/Art/Materials/Environment";

        private const string ParallaxGradientShaderName = "DeadCells/ParallaxGradientMap2D";
        private const string EmissiveHdrShaderName = "DeadCells/EmissiveHdr2D";
        private const string AdditiveGodRayShaderName = "DeadCells/AdditiveGodRay2D";
        private const string VertexSwayShaderName = "DeadCells/VertexSway2D";
        private const string GroundMistShaderName = "DeadCells/GroundMist2D";

        public static readonly string[] MaterialPaths =
        {
            $"{MaterialDirectory}/Mat_ParallaxFar_Prison.mat",
            $"{MaterialDirectory}/Mat_ParallaxMid_Prison.mat",
            $"{MaterialDirectory}/Mat_GodRay_Additive.mat",
            $"{MaterialDirectory}/Mat_Banner_Sway.mat",
            $"{MaterialDirectory}/Mat_Vine_Sway.mat",
            $"{MaterialDirectory}/Mat_Emissive_Flame.mat",
            $"{MaterialDirectory}/Mat_Emissive_Mushroom.mat",
            $"{MaterialDirectory}/Mat_Emissive_Scroll.mat",
            $"{MaterialDirectory}/Mat_Emissive_Exit.mat",
            $"{MaterialDirectory}/Mat_GroundMist_Prison.mat",
            $"{MaterialDirectory}/Mat_RoomBanner_Sway.mat",
            $"{MaterialDirectory}/Mat_Emissive_MushroomHalo.mat"
        };

        private static readonly int ColorDeepId = Shader.PropertyToID("_ColorDeep");
        private static readonly int ColorMidDarkId = Shader.PropertyToID("_ColorMidDark");
        private static readonly int ColorMidLightId = Shader.PropertyToID("_ColorMidLight");
        private static readonly int ColorHighlightId = Shader.PropertyToID("_ColorHighlight");
        private static readonly int ScrollSpeedId = Shader.PropertyToID("_ScrollSpeed");
        private static readonly int LitBlendId = Shader.PropertyToID("_LitBlend");

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int ShimmerStrengthId = Shader.PropertyToID("_ShimmerStrength");

        private static readonly int SwayAmplitudeId = Shader.PropertyToID("_SwayAmplitude");
        private static readonly int SwayFrequencyId = Shader.PropertyToID("_SwayFrequency");
        private static readonly int SwayPhaseScaleId = Shader.PropertyToID("_SwayPhaseScale");
        private static readonly int UseWorldYPinId = Shader.PropertyToID("_UseWorldYPin");
        private static readonly int PinTopWorldYId = Shader.PropertyToID("_PinTopWorldY");
        private static readonly int SwaySpanYId = Shader.PropertyToID("_SwaySpanY");

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
        private static readonly int PulseAmountId = Shader.PropertyToID("_PulseAmount");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");

        private static readonly int MistColorId = Shader.PropertyToID("_MistColor");
        private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");

        [MenuItem("Tools/DeadCells/Visual/Build Environment Materials")]
        public static void EnsureAll()
        {
            EnsureFolderHierarchy(MaterialDirectory);

            Shader parallaxShader = RequireShader(ParallaxGradientShaderName);
            Shader emissiveShader = RequireShader(EmissiveHdrShaderName);
            Shader godRayShader = RequireShader(AdditiveGodRayShaderName);
            Shader swayShader = RequireShader(VertexSwayShaderName);
            Shader mistShader = RequireShader(GroundMistShaderName);

            int updatedCount = 0;

            if (EnsureParallaxMaterial(
                    MaterialPaths[0],
                    parallaxShader,
                    ParseHex("#12242E"),
                    ParseHex("#25444C"),
                    ParseHex("#3D6866"),
                    ParseHex("#5E928A"),
                    scrollSpeed: 0.0f,
                    litBlend: 0.0f))
            {
                updatedCount++;
            }

            if (EnsureParallaxMaterial(
                    MaterialPaths[1],
                    parallaxShader,
                    ParseHex("#1C343D"),
                    ParseHex("#2E524E"),
                    ParseHex("#4A7A6E"),
                    ParseHex("#6FA293"),
                    scrollSpeed: 0.0f,
                    litBlend: 0.25f))
            {
                updatedCount++;
            }

            if (EnsureGodRayMaterial(
                    MaterialPaths[2],
                    godRayShader,
                    new Color(1.0f, 0.92f, 0.68f, 0.26f),
                    scrollSpeed: 0.14f,
                    shimmerStrength: 0.20f))
            {
                updatedCount++;
            }

            if (EnsureSwayMaterial(
                    MaterialPaths[3],
                    swayShader,
                    Color.white,
                    swayAmplitude: 0.09f,
                    swayFrequency: 1.6f,
                    swayPhaseScale: 0.7f,
                    useWorldYPin: 0.0f,
                    pinTopWorldY: 0.0f,
                    swaySpanY: 1.0f))
            {
                updatedCount++;
            }

            if (EnsureSwayMaterial(
                    MaterialPaths[4],
                    swayShader,
                    Color.white,
                    swayAmplitude: 0.12f,
                    swayFrequency: 1.2f,
                    swayPhaseScale: 0.5f,
                    useWorldYPin: 0.0f,
                    pinTopWorldY: 0.0f,
                    swaySpanY: 1.0f))
            {
                updatedCount++;
            }

            if (EnsureEmissiveMaterial(
                    MaterialPaths[5],
                    emissiveShader,
                    new Color(2.2f, 1.25f, 0.40f, 1.0f),
                    pulseSpeed: 3.5f,
                    pulseAmount: 0.12f))
            {
                updatedCount++;
            }

            if (EnsureEmissiveMaterial(
                    MaterialPaths[6],
                    emissiveShader,
                    new Color(0.45f, 2.20f, 1.95f, 1.0f),
                    pulseSpeed: 1.4f,
                    pulseAmount: 0.15f))
            {
                updatedCount++;
            }

            if (EnsureEmissiveMaterial(
                    MaterialPaths[7],
                    emissiveShader,
                    new Color(2.0f, 1.45f, 0.45f, 1.0f),
                    pulseSpeed: 2.2f,
                    pulseAmount: 0.18f))
            {
                updatedCount++;
            }

            if (EnsureEmissiveMaterial(
                    MaterialPaths[8],
                    emissiveShader,
                    new Color(2.1f, 1.75f, 0.85f, 1.0f),
                    pulseSpeed: 1.2f,
                    pulseAmount: 0.10f))
            {
                updatedCount++;
            }

            if (EnsureGroundMistMaterial(
                    MaterialPaths[9],
                    mistShader,
                    new Color(0.52f, 0.80f, 0.76f, 0.62f),
                    scrollSpeed: 0.04f,
                    noiseScale: 3.5f))
            {
                updatedCount++;
            }

            if (EnsureSwayMaterial(
                    MaterialPaths[10],
                    swayShader,
                    Color.white,
                    swayAmplitude: 0.09f,
                    swayFrequency: 1.6f,
                    swayPhaseScale: 0.7f,
                    useWorldYPin: 1.0f,
                    pinTopWorldY: 7.6f,
                    swaySpanY: 4.0f))
            {
                updatedCount++;
            }

            if (EnsureEmissiveMaterial(
                    MaterialPaths[11],
                    emissiveShader,
                    new Color(0.18f, 0.85f, 0.78f, 1.0f),
                    pulseSpeed: 1.4f,
                    pulseAmount: 0.15f,
                    dstBlend: 1.0f))
            {
                updatedCount++;
            }

            if (updatedCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[EnvironmentMaterialBuilder] Verified {MaterialPaths.Length} environment materials (created/updated: {updatedCount}).");
        }

        private static bool EnsureParallaxMaterial(
            string assetPath,
            Shader shader,
            Color deep,
            Color midDark,
            Color midLight,
            Color highlight,
            float scrollSpeed,
            float litBlend)
        {
            Material mat = GetOrCreateMaterial(assetPath, shader, out bool changed);
            changed |= SetColorIfChanged(mat, ColorDeepId, deep);
            changed |= SetColorIfChanged(mat, ColorMidDarkId, midDark);
            changed |= SetColorIfChanged(mat, ColorMidLightId, midLight);
            changed |= SetColorIfChanged(mat, ColorHighlightId, highlight);
            changed |= SetFloatIfChanged(mat, ScrollSpeedId, scrollSpeed);
            changed |= SetFloatIfChanged(mat, LitBlendId, litBlend);
            if (changed)
                EditorUtility.SetDirty(mat);
            return changed;
        }

        private static bool EnsureGodRayMaterial(
            string assetPath,
            Shader shader,
            Color tint,
            float scrollSpeed,
            float shimmerStrength)
        {
            Material mat = GetOrCreateMaterial(assetPath, shader, out bool changed);
            changed |= SetColorIfChanged(mat, ColorId, tint);
            changed |= SetFloatIfChanged(mat, ScrollSpeedId, scrollSpeed);
            changed |= SetFloatIfChanged(mat, ShimmerStrengthId, shimmerStrength);
            if (changed)
                EditorUtility.SetDirty(mat);
            return changed;
        }

        private static bool EnsureSwayMaterial(
            string assetPath,
            Shader shader,
            Color tint,
            float swayAmplitude,
            float swayFrequency,
            float swayPhaseScale,
            float useWorldYPin,
            float pinTopWorldY,
            float swaySpanY)
        {
            Material mat = GetOrCreateMaterial(assetPath, shader, out bool changed);
            changed |= SetColorIfChanged(mat, ColorId, tint);
            changed |= SetFloatIfChanged(mat, SwayAmplitudeId, swayAmplitude);
            changed |= SetFloatIfChanged(mat, SwayFrequencyId, swayFrequency);
            changed |= SetFloatIfChanged(mat, SwayPhaseScaleId, swayPhaseScale);
            changed |= SetFloatIfChanged(mat, UseWorldYPinId, useWorldYPin);
            changed |= SetFloatIfChanged(mat, PinTopWorldYId, pinTopWorldY);
            changed |= SetFloatIfChanged(mat, SwaySpanYId, swaySpanY);
            if (changed)
                EditorUtility.SetDirty(mat);
            return changed;
        }

        private static bool EnsureEmissiveMaterial(
            string assetPath,
            Shader shader,
            Color emissionHdr,
            float pulseSpeed,
            float pulseAmount,
            float dstBlend = 10f)
        {
            Material mat = GetOrCreateMaterial(assetPath, shader, out bool changed);
            changed |= SetColorIfChanged(mat, EmissionColorId, emissionHdr);
            changed |= SetFloatIfChanged(mat, PulseSpeedId, pulseSpeed);
            changed |= SetFloatIfChanged(mat, PulseAmountId, pulseAmount);
            changed |= SetFloatIfChanged(mat, DstBlendId, dstBlend);
            if (changed)
                EditorUtility.SetDirty(mat);
            return changed;
        }

        private static bool EnsureGroundMistMaterial(
            string assetPath,
            Shader shader,
            Color mistColor,
            float scrollSpeed,
            float noiseScale)
        {
            Material mat = GetOrCreateMaterial(assetPath, shader, out bool changed);
            changed |= SetColorIfChanged(mat, MistColorId, mistColor);
            changed |= SetFloatIfChanged(mat, ScrollSpeedId, scrollSpeed);
            changed |= SetFloatIfChanged(mat, NoiseScaleId, noiseScale);
            if (changed)
                EditorUtility.SetDirty(mat);
            return changed;
        }

        private static Material GetOrCreateMaterial(string assetPath, Shader shader, out bool createdOrShaderChanged)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, assetPath);
                createdOrShaderChanged = true;
                return mat;
            }

            if (mat.shader != shader)
            {
                mat.shader = shader;
                createdOrShaderChanged = true;
                return mat;
            }

            createdOrShaderChanged = false;
            return mat;
        }

        private static bool SetColorIfChanged(Material mat, int propertyId, Color target)
        {
            if (mat.HasProperty(propertyId) && ColorsApproximatelyEqual(mat.GetColor(propertyId), target))
                return false;

            mat.SetColor(propertyId, target);
            return true;
        }

        private static bool SetFloatIfChanged(Material mat, int propertyId, float target)
        {
            if (mat.HasProperty(propertyId) && Mathf.Abs(mat.GetFloat(propertyId) - target) <= 1e-5f)
                return false;

            mat.SetFloat(propertyId, target);
            return true;
        }

        private static bool ColorsApproximatelyEqual(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) <= 1e-4f
                && Mathf.Abs(a.g - b.g) <= 1e-4f
                && Mathf.Abs(a.b - b.b) <= 1e-4f
                && Mathf.Abs(a.a - b.a) <= 1e-4f;
        }

        private static Shader RequireShader(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
                throw new InvalidOperationException($"Could not find shader '{shaderName}'.");
            return shader;
        }

        private static Color ParseHex(string hex)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out Color color))
                throw new ArgumentException($"Invalid hex color '{hex}'.", nameof(hex));
            return color;
        }

        private static void EnsureFolderHierarchy(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    string guid = AssetDatabase.CreateFolder(current, parts[i]);
                    if (string.IsNullOrEmpty(guid))
                        throw new InvalidOperationException($"Failed to create folder '{next}'.");
                }
                current = next;
            }
        }
    }
}
