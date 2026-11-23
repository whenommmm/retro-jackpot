using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.Audio
{
    /// <summary>Plays the UI click whenever the attached button is pressed.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class ClickSound : MonoBehaviour
    {
        [SerializeField] private AudioService audioService;
        [SerializeField] private Sfx sound = Sfx.ButtonClick;

        private void Awake() =>
            GetComponent<Button>().onClick.AddListener(() =>
            {
                if (audioService != null) audioService.Play(sound);
            });
    }
}
