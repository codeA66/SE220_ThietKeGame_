using UnityEngine;

namespace DeadCells.VFX
{
    // Shared setup for effects drawn as runtime meshes (blood drops, trails, sparks): the pipeline's sprite
    // materials, a white and a soft-glow texture, and the colour-space fix for vertex colours.
    public static class RuntimeSprites
    {
        private const string UnlitShaderName = "Universal Render Pipeline/2D/Sprite-Unlit-Default";
        private const string VfxSortingLayerName = "VFX";
        private const int GlowSize = 64;

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        private static Material s_material;
        private static Material s_unlitMaterial;
        private static Texture2D s_glow;

        /// <summary>The VFX sorting layer ID, or 0 (Default) if the layer is not registered.</summary>
        public static int VfxSortingLayerId => SortingLayer.NameToID(VfxSortingLayerName);

        /// <summary>The pipeline's default sprite material, borrowed from a throwaway SpriteRenderer.</summary>
        public static Material DefaultMaterial
        {
            get
            {
                if (s_material == null)
                {
                    var probe = new GameObject("MaterialProbe") { hideFlags = HideFlags.HideAndDontSave };
                    s_material = probe.AddComponent<SpriteRenderer>().sharedMaterial;
                    if (Application.isPlaying)
                        Object.Destroy(probe);
                    else
                        Object.DestroyImmediate(probe);
                }
                return s_material;
            }
        }

        /// <summary>
        /// A sprite material that ignores the scene's 2D lights, so emissive effects stay bright in the dark.
        /// Falls back to the lit default if the shader is not available (for example stripped from a build).
        /// </summary>
        public static Material UnlitMaterial
        {
            get
            {
                if (s_unlitMaterial == null)
                {
                    Shader shader = Shader.Find(UnlitShaderName);
                    s_unlitMaterial = shader != null ? new Material(shader) { hideFlags = HideFlags.HideAndDontSave } : DefaultMaterial;
                }
                return s_unlitMaterial;
            }
        }

        /// <summary>A white disc that fades smoothly to transparent at the edge: the halo behind a glowing thing.</summary>
        public static Texture2D GlowTexture
        {
            get
            {
                if (s_glow == null)
                {
                    s_glow = new Texture2D(GlowSize, GlowSize, TextureFormat.RGBA32, false)
                    {
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        hideFlags = HideFlags.HideAndDontSave,
                    };
                    var pixels = new Color32[GlowSize * GlowSize];
                    float center = (GlowSize - 1) * 0.5f;
                    for (int y = 0; y < GlowSize; y++)
                    {
                        for (int x = 0; x < GlowSize; x++)
                        {
                            float distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center)) / center;
                            float falloff = Mathf.Clamp01(1f - distance);
                            byte alpha = (byte)Mathf.RoundToInt(255f * falloff * falloff);
                            pixels[y * GlowSize + x] = new Color32(255, 255, 255, alpha);
                        }
                    }
                    s_glow.SetPixels32(pixels);
                    s_glow.Apply();
                }
                return s_glow;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Statics survive between Play sessions when domain reload is off; rebuild from scratch.
            if (s_glow != null)
                Object.DestroyImmediate(s_glow);
            if (s_unlitMaterial != null && s_unlitMaterial != s_material)
                Object.DestroyImmediate(s_unlitMaterial);
            s_material = null;
            s_unlitMaterial = null;
            s_glow = null;
        }

        /// <summary>Gives a mesh renderer the sprite material, a texture (white by default) and a sorting slot.</summary>
        public static void Prepare(MeshRenderer meshRenderer, int sortingLayerId, int sortingOrder,
            Texture texture = null, bool unlit = false)
        {
            meshRenderer.sharedMaterial = unlit ? UnlitMaterial : DefaultMaterial;
            var block = new MaterialPropertyBlock();
            block.SetTexture(MainTexId, texture != null ? texture : Texture2D.whiteTexture);
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
