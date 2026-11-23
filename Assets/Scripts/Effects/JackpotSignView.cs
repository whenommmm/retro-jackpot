using UnityEngine;

namespace SlotMachine.Effects
{
    /// <summary>Flashes the "JACKPOT" sign on top of the machine through its Animator.</summary>
    public sealed class JackpotSignView : MonoBehaviour
    {
        private static readonly int FlashTrigger = Animator.StringToHash("Flash");

        [SerializeField] private Animator animator;

        public void Flash() => animator.SetTrigger(FlashTrigger);
    }
}
