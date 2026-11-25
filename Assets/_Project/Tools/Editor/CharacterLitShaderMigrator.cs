using System;
using UnityEditor;
using UnityEngine;

namespace DeadCells.Tools.Editor
{
    public static class CharacterLitShaderMigrator
    {
        private const string PaletteSwapLitShaderName = "DeadCells/PaletteSwapLit2D";
        private const string MobFleshSwapLitShaderName = "DeadCells/MobFleshSwapLit2D";
        private const float DefaultSelfLitWeight = 0.45f;
        private const float PlayerTargetSelfLitWeight = 0.52f;
        private const float MinSelfLitWeight = 0.35f;
        private const float MaxSelfLitWeight = 0.55f;

        private const string PlayerMaterialPath = "Assets/_Project/Art/Characters/Player/Player_PaletteSwap.mat";

        private static readonly string[] MobMaterialPaths =
        {
            "Assets/_Project/Features/Enemies/Materials/Mob_zombie.mat",
            "Assets/_Project/Features/Enemies/Materials/Mob_zombieShield.mat",
            "Assets/_Project/Features/Enemies/Materials/Mob_deadArcher.mat",
            "Assets/_Project/Features/Enemies/Materials/Mob_grenadier.mat",
            "Assets/_Project/Features/Enemies/Materials/Mob_Rat.mat"
        };

        private static readonly int PaletteTexId = Shader.PropertyToID("_PaletteTex");
        private static readonly int PaletteRowId = Shader.PropertyToID("_PaletteRow");
        private static readonly int PaletteRowsId = Shader.PropertyToID("_PaletteRows");
        private static readonly int FleshColorId = Shader.PropertyToID("_FleshColor");
        private static readonly int FlashId = Shader.PropertyToID("_Flash");
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
        private static readonly int SelfLitWeightId = Shader.PropertyToID("_SelfLitWeight");

        [MenuItem("Tools/DeadCells/Visual/Migrate Character Materials to Lit-Hybrid")]
        public static void ApplyLitHybridToCharacterMaterials()
        {
            Shader paletteLitShader = Shader.Find(PaletteSwapLitShaderName);
            if (paletteLitShader == null)
                throw new InvalidOperationException($"Could not find shader '{PaletteSwapLitShaderName}'.");

            Shader mobLitShader = Shader.Find(MobFleshSwapLitShaderName);
            if (mobLitShader == null)
                throw new InvalidOperationException($"Could not find shader '{MobFleshSwapLitShaderName}'.");

            int migratedCount = 0;

            if (MigratePlayerMaterial(PlayerMaterialPath, paletteLitShader))
                migratedCount++;

            for (int i = 0; i < MobMaterialPaths.Length; i++)
            {
                if (MigrateMobMaterial(MobMaterialPaths[i], mobLitShader))
                    migratedCount++;
            }

            if (migratedCount > 0)
                AssetDatabase.SaveAssets();

            Debug.Log($"[CharacterLitShaderMigrator] Processed 6 character/enemy materials (updated: {migratedCount}).");
        }

        private static bool MigratePlayerMaterial(string assetPath, Shader targetShader)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
                throw new InvalidOperationException($"Missing player material at '{assetPath}'.");

            Texture paletteTex = mat.HasProperty(PaletteTexId) ? mat.GetTexture(PaletteTexId) : null;
            float paletteRow = mat.HasProperty(PaletteRowId) ? mat.GetFloat(PaletteRowId) : 1f;
            float paletteRows = mat.HasProperty(PaletteRowsId) ? mat.GetFloat(PaletteRowsId) : 4f;
            float flash = mat.HasProperty(FlashId) ? mat.GetFloat(FlashId) : 0f;

            bool shaderChanged = mat.shader != targetShader;
            float selfLit = PlayerTargetSelfLitWeight;

            if (!shaderChanged && Mathf.Approximately(mat.GetFloat(SelfLitWeightId), selfLit))
                return false;

            mat.shader = targetShader;
            if (paletteTex != null)
                mat.SetTexture(PaletteTexId, paletteTex);
            mat.SetFloat(PaletteRowId, paletteRow);
            mat.SetFloat(PaletteRowsId, paletteRows);
            mat.SetFloat(FlashId, flash);
            mat.SetFloat(SelfLitWeightId, selfLit);
            EditorUtility.SetDirty(mat);
            return true;
        }

        private static bool MigrateMobMaterial(string assetPath, Shader targetShader)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
                throw new InvalidOperationException($"Missing enemy material at '{assetPath}'.");

            Color fleshColor = mat.HasProperty(FleshColorId) ? mat.GetColor(FleshColorId) : Color.magenta;
            Color flashColor = mat.HasProperty(FlashColorId) ? mat.GetColor(FlashColorId) : Color.white;
            float flash = mat.HasProperty(FlashId) ? mat.GetFloat(FlashId) : 0f;

            bool shaderChanged = mat.shader != targetShader;
            float selfLit = ResolveTargetSelfLitWeight(mat, shaderChanged);

            if (!shaderChanged && Mathf.Approximately(mat.GetFloat(SelfLitWeightId), selfLit))
                return false;

            mat.shader = targetShader;
            mat.SetColor(FleshColorId, fleshColor);
            mat.SetColor(FlashColorId, flashColor);
            mat.SetFloat(FlashId, flash);
            mat.SetFloat(SelfLitWeightId, selfLit);
            EditorUtility.SetDirty(mat);
            return true;
        }

        private static float ResolveTargetSelfLitWeight(Material mat, bool shaderChanged)
        {
            if (shaderChanged || !mat.HasProperty(SelfLitWeightId))
                return DefaultSelfLitWeight;

            float current = mat.GetFloat(SelfLitWeightId);
            if (current >= MinSelfLitWeight && current <= MaxSelfLitWeight)
                return current;

            return DefaultSelfLitWeight;
        }
    }
}
