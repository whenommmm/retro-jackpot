using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.Gameplay
{
    /// <summary>
    /// A pulsing frame around one reel window, used for "anticipation" when
    /// the first reels already match and the last reel is still spinning.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class WindowGlow : MonoBehaviour
    {
        [SerializeField] private Color color = new Color(1f, 0.82f, 0.25f, 1f);
        [SerializeField] private float pulsesPerSecond = 4f;

        private Image image;
        private bool active;

        private void Awake()
        {
            image = GetComponent<Image>();
            SetActive(false);
        }

        public void SetActive(bool on)
        {
            active = on;
            if (image == null) image = GetComponent<Image>();
            image.enabled = on;
        }

        private void Update()
        {
            if (!active) return;
            // Step the alpha in a few levels rather than smoothly: it reads as a
            // blinking bulb frame, which suits the retro look.
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * pulsesPerSecond * Mathf.PI * 2f);
            float stepped = Mathf.Round(wave * 3f) / 3f;
            image.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.25f, 1f, stepped));
        }
    }
}
