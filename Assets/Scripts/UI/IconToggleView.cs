using System;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.UI
{
    /// <summary>
    /// A square button with an icon that shows whether its setting is on or
    /// off (sound, pixel filter). It reports clicks and shows what it is told;
    /// the setting itself lives elsewhere.
    /// </summary>
    public sealed class IconToggleView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite offSprite;

        public event Action Clicked;

        private void Awake() => button.onClick.AddListener(() => Clicked?.Invoke());

        public void SetOn(bool on) => icon.sprite = on ? onSprite : offSprite;
    }
}
