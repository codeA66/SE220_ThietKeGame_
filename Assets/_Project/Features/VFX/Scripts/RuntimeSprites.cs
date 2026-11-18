using UnityEngine;

namespace DeadCells.VFX
{
    // Shared setup for effects drawn as runtime meshes (blood drops, trails): the default sprite
    // material, so they are lit like every other sprite, and the colour-space fix for vertex colours.
    public static class RuntimeSprites
    {
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        private static Material s_material;

        /// <summary>The pipeline's default sprite material, borrowed from a throwaway SpriteRenderer.</summary>
        public static Material DefaultMaterial
        {
            get
            {
                if (s_material == null)
                {
                    var probe = new GameObject("MaterialProbe");
                    s_material = probe.AddComponent<SpriteRenderer>().sharedMaterial;
                    Object.Destroy(probe);
                }
                return s_material;
            }
        }

        /// <summary>Gives a mesh renderer the sprite material, a white texture and a sorting slot.</summary>
        public static void Prepare(MeshRenderer meshRenderer, int sortingLayerId, int sortingOrder)
        {
            meshRenderer.sharedMaterial = DefaultMaterial;
            var block = new MaterialPropertyBlock();
            block.SetTexture(MainTexId, Texture2D.whiteTexture);
            meshRenderer.SetPropertyBlock(block);
            meshRenderer.sortingLayerID = sortingLayerId;
            meshRenderer.sortingOrder = sortingOrder;
        }

        /// <summary>
        /// In a Linear colour space project Unity converts texture and SpriteRenderer colours, but mesh
        /// vertex colours are used as they are, so sRGB values would show up washed out. Convert them.
        /// </summary>
        public static Color32 ToRenderSpace(Color32 srgb) =>
            QualitySettings.activeColorSpace == ColorSpace.Linear ? (Color32)((Color)srgb).linear : srgb;
    }
}
