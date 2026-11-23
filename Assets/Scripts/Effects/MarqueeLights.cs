using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.Effects
{
    public enum LightShow
    {
        Idle,
        Spinning,
        Win,
        Jackpot
    }

    /// <summary>
    /// Casino marquee bulbs framing the screen. A slow chase while idle, a fast
    /// chase while the reels spin, and all-bulb flashing on wins.
    /// </summary>
    public sealed class MarqueeLights : MonoBehaviour
    {
        [SerializeField] private Image[] bulbs;
        [SerializeField] private Color onColor = new Color(1f, 0.84f, 0.38f);
        [SerializeField] private Color offColor = new Color(0.32f, 0.2f, 0.16f);
        [SerializeField] private Color jackpotColor = new Color(1f, 0.35f, 0.3f);

        private LightShow show = LightShow.Idle;
        private float timer;
        private int step;

        public void SetShow(LightShow next)
        {
            if (show == next) return;
            show = next;
            timer = 0f;
            Apply();
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float interval = show switch
            {
                LightShow.Idle => 0.32f,
                LightShow.Spinning => 0.07f,
                LightShow.Win => 0.14f,
                _ => 0.08f,
            };
            if (timer < interval) return;
            timer -= interval;
            step++;
            Apply();
        }

        private void Apply()
        {
            for (int i = 0; i < bulbs.Length; i++)
            {
                Color c;
                switch (show)
                {
                    case LightShow.Idle:
                        c = (i + step) % 4 == 0 ? onColor : offColor; // one in four lit, chasing
                        break;
                    case LightShow.Spinning:
                        c = (i + step) % 3 == 0 ? onColor : offColor;
                        break;
                    case LightShow.Win:
                        c = (i + step) % 2 == 0 ? onColor : offColor; // alternate blink
                        break;
                    default:
                        c = (i + step) % 2 == 0 ? onColor : jackpotColor;
                        break;
                }
                bulbs[i].color = c;
            }
        }
    }
}
