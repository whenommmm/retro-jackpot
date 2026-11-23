using System;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.Gameplay
{
    /// <summary>
    /// The machine's pull lever. Clicking the handle asks to spin; the
    /// controller decides whether a spin can start and then plays the pull
    /// animation (an Animator clip swapping the up and down lever sprites).
    /// </summary>
    public sealed class LeverView : MonoBehaviour
    {
        private static readonly int PullTrigger = Animator.StringToHash("Pull");

        [SerializeField] private Animator animator;
        [Tooltip("Invisible button covering the lever handle.")]
        [SerializeField] private Button handle;

        /// <summary>Raised when the player clicks the lever.</summary>
        public event Action Clicked;

        public bool Interactable
        {
            get => handle.interactable;
            set => handle.interactable = value;
        }

        private void Awake() => handle.onClick.AddListener(() => Clicked?.Invoke());

        public void PlayPull() => animator.SetTrigger(PullTrigger);
    }
}
