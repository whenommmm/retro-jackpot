using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.Effects
{
    /// <summary>
    /// The glow behind the payline row. Hidden normally; blinks gold on a win.
    /// </summary>
    public sealed class PaylineView : MonoBehaviour
    {
        [SerializeField] private Image glow;
        [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0.3f, 0.55f);
        [SerializeField] private float blinkInterval = 0.12f;

        private Coroutine blinking;

        private void Awake() => SetVisible(false);

        public void Flash(float duration)
        {
            if (blinking != null) StopCoroutine(blinking);
            blinking = StartCoroutine(Blink(duration));
        }

        public void Clear()
        {
            if (blinking != null) StopCoroutine(blinking);
            blinking = null;
            SetVisible(false);
        }

        private IEnumerator Blink(float duration)
        {
            float elapsed = 0f;
            bool on = true;
            while (elapsed < duration)
            {
                SetVisible(on);
                on = !on;
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }
            // End lit, so the winning line stays marked until the next spin.
            SetVisible(true);
            blinking = null;
        }

        private void SetVisible(bool visible)
        {
            glow.color = visible ? glowColor : new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
        }
    }
}
