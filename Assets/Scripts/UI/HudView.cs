using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.UI
{
    public enum MessageTone
    {
        Neutral,
        Win,
        Bonus,
        Warning
    }

    /// <summary>
    /// The on-screen readouts: credits, bet (with the arrow buttons), last win,
    /// jackpot pool, free-spin counter and the message banner. It only
    /// displays what it is told and reports button presses; it holds no rules.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [Header("Readouts")]
        [SerializeField] private TMP_Text creditsValue;
        [SerializeField] private TMP_Text betValue;
        [SerializeField] private TMP_Text winValue;
        [SerializeField] private TMP_Text jackpotValue;
        [SerializeField] private TMP_Text jackpotHint;
        [SerializeField] private TMP_Text freeSpinsLabel;
        [SerializeField] private TMP_Text message;

        [Header("Bet controls")]
        [SerializeField] private Button betDown;
        [SerializeField] private Button betUp;

        [Header("Message colours")]
        [SerializeField] private Color neutralColor = new Color(0.86f, 0.9f, 1f);
        [SerializeField] private Color winColor = new Color(1f, 0.84f, 0.3f);
        [SerializeField] private Color bonusColor = new Color(0.55f, 1f, 0.75f);
        [SerializeField] private Color warningColor = new Color(1f, 0.45f, 0.4f);

        public event Action BetDownClicked;
        public event Action BetUpClicked;

        private int shownCredits;
        private bool canDecreaseBet, canIncreaseBet, betControlsEnabled = true;

        private void Awake()
        {
            betDown.onClick.AddListener(() => BetDownClicked?.Invoke());
            betUp.onClick.AddListener(() => BetUpClicked?.Invoke());
        }

        public void SetCredits(int credits)
        {
            shownCredits = credits;
            creditsValue.text = Format(credits);
        }

        /// <summary>Counts the credit readout up to <paramref name="target"/>; onStep fires as digits change (coin ticks).</summary>
        public IEnumerator CountCreditsTo(int target, float duration, Action onStep)
        {
            int from = shownCredits;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = 1f - Mathf.Pow(1f - t / duration, 2f);
                int value = Mathf.RoundToInt(Mathf.Lerp(from, target, k));
                if (value != shownCredits)
                {
                    SetCredits(value);
                    onStep?.Invoke();
                }
                yield return null;
            }
            SetCredits(target);
        }

        public void SetBet(int bet, bool canDecrease, bool canIncrease, bool isMaxBet, int maxBet)
        {
            betValue.text = Format(bet);
            canDecreaseBet = canDecrease;
            canIncreaseBet = canIncrease;
            jackpotHint.text = isMaxBet ? "JACKPOT ACTIVE" : "BET " + Format(maxBet) + " TO WIN IT";
            jackpotHint.color = isMaxBet ? bonusColor : neutralColor;
            RefreshBetButtons();
        }

        public void SetBetControlsEnabled(bool enabled)
        {
            betControlsEnabled = enabled;
            RefreshBetButtons();
        }

        public void SetLastWin(int amount) => winValue.text = Format(amount);

        public void SetJackpot(int pool) => jackpotValue.text = Format(pool);

        public void SetFreeSpins(int remaining, int bet)
        {
            freeSpinsLabel.gameObject.SetActive(remaining > 0);
            if (remaining > 0)
                freeSpinsLabel.text = "FREE SPINS: " + remaining + "  (BET " + Format(bet) + ")";
        }

        public void ShowMessage(string text, MessageTone tone)
        {
            message.text = text;
            message.color = tone switch
            {
                MessageTone.Win => winColor,
                MessageTone.Bonus => bonusColor,
                MessageTone.Warning => warningColor,
                _ => neutralColor,
            };
        }

        private void RefreshBetButtons()
        {
            betDown.interactable = betControlsEnabled && canDecreaseBet;
            betUp.interactable = betControlsEnabled && canIncreaseBet;
        }

        public static string Format(int value) => value.ToString("N0", CultureInfo.InvariantCulture);
    }
}
