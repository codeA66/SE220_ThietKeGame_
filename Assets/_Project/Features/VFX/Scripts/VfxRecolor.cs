using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadCells.VFX
{
    // Builds a blue copy of a red effect at runtime, so baked blood sprites can match the blue enemy blood without
    // redrawing the art. Reads each frame's pixels through a render texture, so the source textures do not have to be
    // readable. Copies are cached per source definition.
    public static class VfxRecolor
    {
        // The blue ramp (sRGB), dark to bright: deep blue shadows, a strong mid blue, pale blue highlights.
        private static readonly Color BlueDark = new Color(0.03f, 0.15f, 0.55f);
        private static readonly Color BlueMid = new Color(0.10f, 0.45f, 1f);
        private static readonly Color BlueLight = new Color(0.62f, 0.88f, 1f);

        private static readonly Dictionary<VfxDefinition, VfxDefinition> Cache = new Dictionary<VfxDefinition, VfxDefinition>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Cache.Clear();

        /// <summary>The blue version of a red definition. Returns the source itself if it has no frames.</summary>
        public static VfxDefinition Blue(VfxDefinition source)
        {
            if (source == null || source.Frames == null || source.Frames.Length == 0)
                return source;
            if (Cache.TryGetValue(source, out VfxDefinition cached) && cached != null)
                return cached;

            var frames = new Sprite[source.Frames.Length];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = RecolorFrame(source.Frames[i]);

            VfxDefinition blue = VfxDefinition.Create(source.name + "_Blue", frames, source.Fps, source.Scale, source.SortingOrder, source.PrewarmCount);
            Cache[source] = blue;
            return blue;
        }

        private static Sprite RecolorFrame(Sprite frame)
        {
            Rect rect = frame.rect;
            Texture source = frame.texture;
            var target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            var pixels = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false, true)
            {
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave,
            };
            pixels.ReadPixels(rect, 0, 0);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);

            Color32[] data = pixels.GetPixels32();
            for (int i = 0; i < data.Length; i++)
            {
                Color32 c = data[i];
                // The brightness of each pixel picks a spot on a saturated blue ramp, so the result is a vivid blue
                // whatever tint the red source had. The texture holds linear values, so the ramp is converted to linear.
                float value = Mathf.LinearToGammaSpace(Mathf.Max(c.r, Mathf.Max(c.g, c.b)) / 255f);
                Color ramp = value < 0.55f
                    ? Color.Lerp(BlueDark, BlueMid, value / 0.55f)
                    : Color.Lerp(BlueMid, BlueLight, (value - 0.55f) / 0.45f);
                data[i] = new Color32(
                    (byte)(Mathf.GammaToLinearSpace(ramp.r) * 255f),
                    (byte)(Mathf.GammaToLinearSpace(ramp.g) * 255f),
                    (byte)(Mathf.GammaToLinearSpace(ramp.b) * 255f), c.a);
            }
            pixels.SetPixels32(data);
            pixels.Apply();
            return Sprite.Create(pixels, new Rect(0f, 0f, rect.width, rect.height),
                new Vector2(frame.pivot.x / rect.width, frame.pivot.y / rect.height), frame.pixelsPerUnit);
        }
    }
}
