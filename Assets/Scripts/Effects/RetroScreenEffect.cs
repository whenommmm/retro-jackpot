using UnityEngine;

namespace SlotMachine.Effects
{
    /// <summary>
    /// Gives the whole screen one retro pixel grid. The camera's frame is
    /// sampled down to a fixed virtual height (720 rows: the machine art's
    /// native size) with nearest-neighbour filtering, then scaled back up with
    /// hard edges, faint scanlines and a vignette. Text, panels and art all
    /// snap to the same pixels, so the look stays consistent at any window size.
    /// Input is unaffected: the UI is still laid out at full resolution.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class RetroScreenEffect : MonoBehaviour
    {
        private static readonly int VirtualHeightId = Shader.PropertyToID("_VirtualHeight");
        private static readonly int ScanlinesId = Shader.PropertyToID("_Scanlines");
        private static readonly int VignetteId = Shader.PropertyToID("_Vignette");

        [SerializeField] private bool pixelate = true;
        [Tooltip("Rows in the virtual pixel grid. 720 keeps the machine art at 1:1.")]
        [SerializeField, Min(90)] private int virtualHeight = 720;
        [SerializeField] private Material upscaleMaterial;
        [SerializeField, Range(0f, 0.5f)] private float scanlineStrength = 0.1f;
        [SerializeField, Range(0f, 1f)] private float vignetteStrength = 0.25f;

        public bool Pixelate
        {
            get => pixelate;
            set => pixelate = value;
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!pixelate || source.height <= 0)
            {
                Graphics.Blit(source, destination);
                return;
            }

            int height = Mathf.Min(virtualHeight, source.height);
            int width = Mathf.Max(1, Mathf.RoundToInt(height * (float)source.width / source.height));

            var low = RenderTexture.GetTemporary(width, height, 0, source.format);
            low.filterMode = FilterMode.Point;
            source.filterMode = FilterMode.Point;
            Graphics.Blit(source, low);

            if (upscaleMaterial != null)
            {
                upscaleMaterial.SetFloat(VirtualHeightId, height);
                upscaleMaterial.SetFloat(ScanlinesId, scanlineStrength);
                upscaleMaterial.SetFloat(VignetteId, vignetteStrength);
                Graphics.Blit(low, destination, upscaleMaterial);
            }
            else
            {
                Graphics.Blit(low, destination);
            }

            RenderTexture.ReleaseTemporary(low);
        }
    }
}
