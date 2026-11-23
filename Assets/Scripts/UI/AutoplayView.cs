using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.UI
{
    /// <summary>
    /// The autoplay panel under the paytable: arrows to pick how many spins,
    /// the count, and a START / STOP button. Like the HUD, it only displays
    /// and reports presses.
    /// </summary>
    public sealed class AutoplayView : MonoBehaviour
    {
        [SerializeField] private Button fewer;
        [SerializeField] private Button more;
        [SerializeField] private Button toggle;
        [SerializeField] private TMP_Text count;
        [SerializeField] private TMP_Text toggleLabel;
        [SerializeField] private TMP_Text caption;
        [SerializeField] private TMP_Text hint;

        public event Action FewerClicked;
        public event Action MoreClicked;
        public event Action ToggleClicked;

        private void Awake()
        {
            fewer.onClick.AddListener(() => FewerClicked?.Invoke());
            more.onClick.AddListener(() => MoreClicked?.Invoke());
            toggle.onClick.AddListener(() => ToggleClicked?.Invoke());
        }

        /// <summary>States the automatic stops, so the player knows them before starting.</summary>
        public void ShowStopRules(int lossLimitPercent) =>
            hint.text = "STOPS FOR BIG WINS OR IF DOWN " + lossLimitPercent + "%";

        /// <summary>Not running: shows the chosen number of spins, ready to start.</summary>
        public void ShowReady(int spins, bool canFewer, bool canMore, bool canStart)
        {
            caption.text = "AUTOPLAY";
            count.text = spins.ToString();
            toggleLabel.text = "START";
            fewer.interactable = canStart && canFewer;
            more.interactable = canStart && canMore;
            toggle.interactable = canStart;
        }

        /// <summary>Running: shows the spins left and offers STOP (greyed once a stop is on its way).</summary>
        public void ShowRunning(int spinsLeft, bool canStop)
        {
            caption.text = canStop ? "AUTOPLAY ON" : "STOPPING...";
            count.text = spinsLeft.ToString();
            toggleLabel.text = "STOP";
            fewer.interactable = false;
            more.interactable = false;
            toggle.interactable = canStop;
        }
    }
}
