using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SlotMachine.Audio;
using SlotMachine.Core;
using SlotMachine.Effects;
using SlotMachine.UI;
using UnityEngine;

namespace SlotMachine.Gameplay
{
    /// <summary>
    /// Runs the machine. On every spin the engine decides the outcome first;
    /// everything this class does afterwards (lever, reels, sounds, lights,
    /// count-up, popups) only reveals that fixed result. It owns the flow and
    /// the input, and delegates rules to <see cref="SlotEngine"/> and display
    /// to the views.
    /// </summary>
    public sealed class SlotMachineController : MonoBehaviour
    {
        private enum State { Idle, Spinning, Presenting }

        [Header("Data")]
        [SerializeField] private SlotMachineConfig config;

        [Header("Machine")]
        [SerializeField] private ReelView[] reels;
        [SerializeField] private WindowGlow[] reelGlows;
        [SerializeField] private LeverView lever;
        [SerializeField] private PaylineView payline;
        [SerializeField] private JackpotSignView jackpotSign;

        [Header("Screen")]
        [SerializeField] private HudView hud;
        [SerializeField] private PaytableView paytable;
        [SerializeField] private PopupView popup;
        [SerializeField] private MarqueeLights lights;
        [SerializeField] private ScreenShake screenShake;
        [SerializeField] private AudioService audioService;
        [SerializeField] private RetroScreenEffect retroEffect;
        [SerializeField] private IconToggleView soundToggle;
        [SerializeField] private IconToggleView filterToggle;

        [Header("Timing (seconds)")]
        [SerializeField] private float leverToReels = 0.15f;
        [SerializeField] private float reelStartStagger = 0.06f;
        [SerializeField] private float minimumSpinTime = 0.7f;
        [SerializeField] private float reelStopStagger = 0.32f;
        [SerializeField] private float quickStopStagger = 0.06f;
        [Tooltip("Extra spin on the last reel when the first reels already match.")]
        [SerializeField] private float anticipationTime = 1.2f;
        [SerializeField] private float freeSpinPause = 0.8f;

        [Header("Presentation")]
        [Tooltip("Wins of at least this many times the bet get the BIG WIN popup.")]
        [SerializeField] private int bigWinMultiple = 25;

        private static readonly string[] LossLines =
        {
            "SO CLOSE! PULL AGAIN", "NO LUCK THIS TIME", "SPIN AGAIN!", "THE NEXT ONE COULD HIT!"
        };

        private SlotEngine engine;
        private State state = State.Idle;
        private bool slamRequested;
        private int freeSpinWinnings;

        private CreditBank Bank => engine.Bank;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private QA.QaRandomSource qaRandom;
#endif

        private void Awake()
        {
            var rules = config.BuildRules();
            IRandomSource random = new SystemRandomSource();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            random = qaRandom = new QA.QaRandomSource(random);
#endif
            engine = new SlotEngine(rules, new CreditBank(rules), random);

            for (int i = 0; i < reels.Length; i++)
            {
                var strip = engine.Strips[i];
                reels[i].Bind(strip, config, Random.Range(0, strip.Length)); // cosmetic resting position
                reels[i].SymbolPassed += _ => audioService.Play(Sfx.ReelTick);
                reels[i].Stopped += _ => audioService.Play(Sfx.ReelStop);
            }

            lever.Clicked += OnSpinInput;
            hud.BetDownClicked += () => ChangeBet(-1);
            hud.BetUpClicked += () => ChangeBet(+1);
            popup.Opened += () => audioService.Play(Sfx.PopupOpen);
            soundToggle.Clicked += ToggleSound;
            filterToggle.Clicked += ToggleFilter;
        }

        private void Start()
        {
            paytable.Show(BuildPaytableLines());
            hud.SetCredits(Bank.Credits);
            hud.SetJackpot(Bank.JackpotPool);
            hud.SetLastWin(0);
            hud.SetFreeSpins(0, 0);
            RefreshBet();
            hud.ShowMessage("PULL THE LEVER OR PRESS SPACE", MessageTone.Neutral);
            lights.SetShow(LightShow.Idle);
            soundToggle.SetOn(!audioService.Muted);
            filterToggle.SetOn(retroEffect.Pixelate);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (popup.IsOpen) popup.Dismiss();
                else OnSpinInput();
            }

            if (Input.GetKeyDown(KeyCode.M)) ToggleSound();
            if (Input.GetKeyDown(KeyCode.P)) ToggleFilter();

            if (popup.IsOpen) return;
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.DownArrow)) ChangeBet(-1, fromKeyboard: true);
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.UpArrow)) ChangeBet(+1, fromKeyboard: true);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            HandleQaKeys();
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// QA shortcuts (Editor and development builds only): force the next
        /// spin's line to check rare presentations, or empty the wallet to
        /// test the refill flow.
        ///   1 = 7 7 7   2 = BAR x3   3 = BELL x3 (free spins)   4 = CHERRY x3
        ///   5 = 7 7 BAR (anticipation near-miss)   0 = empty the wallet
        /// </summary>
        private void HandleQaKeys()
        {
            if (state != State.Idle) return;
            if (Input.GetKeyDown(KeyCode.Alpha1)) QaForce(SymbolType.Seven, SymbolType.Seven, SymbolType.Seven);
            if (Input.GetKeyDown(KeyCode.Alpha2)) QaForce(SymbolType.Bar, SymbolType.Bar, SymbolType.Bar);
            if (Input.GetKeyDown(KeyCode.Alpha3)) QaForce(SymbolType.Bell, SymbolType.Bell, SymbolType.Bell);
            if (Input.GetKeyDown(KeyCode.Alpha4)) QaForce(SymbolType.Cherry, SymbolType.Cherry, SymbolType.Cherry);
            if (Input.GetKeyDown(KeyCode.Alpha5)) QaForce(SymbolType.Seven, SymbolType.Seven, SymbolType.Bar);
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                Bank.Withdraw(Bank.Credits);
                hud.SetCredits(Bank.Credits);
                hud.ShowMessage("QA: WALLET EMPTIED", MessageTone.Warning);
            }
        }

        private void QaForce(params SymbolType[] line)
        {
            qaRandom.ForceLine(engine.Strips, line);
            hud.ShowMessage("QA: NEXT SPIN FORCED", MessageTone.Warning);
        }
#endif

        /// <summary>Lever click, Space or Enter: spin when idle, slam the reels to a stop while spinning.</summary>
        private void OnSpinInput()
        {
            if (popup.IsOpen) return;
            if (state == State.Idle) StartCoroutine(RunSpins());
            else if (state == State.Spinning) slamRequested = true;
        }

        private void ChangeBet(int direction, bool fromKeyboard = false)
        {
            if (state != State.Idle || engine.HasFreeSpins || popup.IsOpen) return;
            bool changed = direction < 0 ? Bank.DecreaseBet() : Bank.IncreaseBet();
            if (!changed) return;
            if (fromKeyboard) audioService.Play(Sfx.ButtonClick); // buttons play their own click
            RefreshBet();
        }

        private void ToggleSound()
        {
            audioService.ToggleMute();
            soundToggle.SetOn(!audioService.Muted);
        }

        private void ToggleFilter()
        {
            retroEffect.Pixelate = !retroEffect.Pixelate;
            filterToggle.SetOn(retroEffect.Pixelate);
        }

        /// <summary>One paid spin, followed by any free spins it triggers.</summary>
        private IEnumerator RunSpins()
        {
            do
            {
                yield return StartCoroutine(PlayOneSpin());
                if (engine.HasFreeSpins)
                {
                    hud.ShowMessage("FREE SPIN COMING UP...", MessageTone.Bonus);
                    yield return new WaitForSeconds(freeSpinPause);
                }
            } while (engine.HasFreeSpins);

            state = State.Idle;
            hud.SetBetControlsEnabled(true);

            if (popup.IsOpen) yield break; // a refill offer is already showing
            if (!Bank.CanAffordAnyBet) OfferRefill();
            else if (!Bank.CanAffordCurrentBet) LowerBetToCredits();
        }

        private IEnumerator PlayOneSpin()
        {
            if (!engine.HasFreeSpins && !Bank.CanAffordCurrentBet)
            {
                if (Bank.CanAffordAnyBet) LowerBetToCredits();
                else OfferRefill();
                yield break;
            }

            // The outcome is decided here; the rest of this method only reveals it.
            if (!engine.TrySpin(out var result)) yield break;

            state = State.Spinning;
            slamRequested = false;
            hud.SetBetControlsEnabled(false);
            payline.Clear();
            lights.SetShow(LightShow.Spinning);
            lever.PlayPull();
            audioService.Play(Sfx.LeverPull);

            hud.SetCredits(result.CreditsAfterBet);
            hud.SetJackpot(result.IsJackpot ? result.Payout : result.JackpotPoolAfter);
            if (result.IsFreeSpin)
            {
                hud.SetFreeSpins(result.FreeSpinsRemaining, result.Bet);
                hud.ShowMessage(result.FreeSpinsRemaining > 0
                    ? "FREE SPIN!  " + result.FreeSpinsRemaining + " MORE TO GO"
                    : "LAST FREE SPIN!", MessageTone.Bonus);
            }
            else
            {
                hud.ShowMessage("GOOD LUCK!", MessageTone.Neutral);
            }

            yield return new WaitForSeconds(leverToReels);
            foreach (var reel in reels)
            {
                reel.StartSpin();
                yield return new WaitForSeconds(reelStartStagger);
            }

            yield return WaitUnlessSlammed(minimumSpinTime, result, 0);

            for (int i = 0; i < reels.Length; i++)
            {
                bool lastReel = i == reels.Length - 1;
                if (lastReel && ShouldBuildAnticipation(result) && !slamRequested)
                    yield return Anticipate(i, result);

                reels[i].Stop(result.StopIndices[i], slamRequested);
                if (!lastReel)
                    yield return WaitUnlessSlammed(slamRequested ? quickStopStagger : reelStopStagger, result, i + 1);
            }

            while (reels.Any(r => r.IsSpinning))
            {
                if (slamRequested) HurryStops(result, reels.Length);
                yield return null;
            }

            state = State.Presenting;
            yield return StartCoroutine(PresentResult(result));
        }

        /// <summary>
        /// Waits, but cuts the wait short when the player slams the stop; any
        /// reels already told to stop are switched to a quick stop.
        /// </summary>
        private IEnumerator WaitUnlessSlammed(float seconds, SpinResult result, int reelsAlreadyStopping)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                if (slamRequested)
                {
                    HurryStops(result, reelsAlreadyStopping);
                    yield break;
                }
                yield return null;
            }
        }

        private void HurryStops(SpinResult result, int count)
        {
            for (int i = 0; i < count; i++)
                if (reels[i].IsSpinning) reels[i].Stop(result.StopIndices[i], quick: true);
        }

        /// <summary>When every reel but the last already matches, the last one keeps spinning a little longer.</summary>
        private bool ShouldBuildAnticipation(SpinResult result)
        {
            if (reels.Length < 3) return false;
            for (int i = 1; i < reels.Length - 1; i++)
                if (result.Symbols[i] != result.Symbols[0]) return false;
            return true;
        }

        private IEnumerator Anticipate(int reelIndex, SpinResult result)
        {
            // Let the earlier reels land first so the player sees the match.
            while (reels.Take(reelIndex).Any(r => r.IsSpinning)) yield return null;

            reelGlows[reelIndex].SetActive(true);
            audioService.PlayLoop(Sfx.Anticipation);
            hud.ShowMessage("COME ON...!", MessageTone.Bonus);

            yield return WaitUnlessSlammed(anticipationTime, result, reelIndex);

            reelGlows[reelIndex].SetActive(false);
            audioService.StopLoop();
        }

        private IEnumerator PresentResult(SpinResult result)
        {
            if (result.IsWin)
            {
                var symbol = config.GetSymbol(result.WinningSymbol);
                bool bigWin = result.IsJackpot || result.WinMultiple >= bigWinMultiple;

                lights.SetShow(result.IsJackpot ? LightShow.Jackpot : LightShow.Win);
                payline.Flash(1.2f);
                foreach (var reel in reels) reel.StartCoroutine(reel.PulsePaylineSymbol(3, 0.22f));
                audioService.Play(result.IsJackpot ? Sfx.Jackpot : bigWin ? Sfx.WinBig : Sfx.WinSmall);

                if (result.IsJackpot)
                {
                    hud.ShowMessage("JACKPOT!!!", MessageTone.Win);
                    screenShake.Shake(0.8f, 6f);
                    jackpotSign.Flash();
                }
                else
                {
                    hud.ShowMessage("3 x " + symbol.DisplayName.ToUpperInvariant() + "!  WIN " + HudView.Format(result.Payout),
                        MessageTone.Win);
                }

                float countDuration = Mathf.Clamp(0.5f + 0.04f * result.WinMultiple, 0.6f, 2.6f);
                yield return StartCoroutine(hud.CountCreditsTo(result.CreditsAfterPayout, countDuration,
                    () => audioService.Play(Sfx.CoinTick)));
                hud.SetLastWin(result.Payout);
                hud.SetJackpot(result.JackpotPoolAfter);
                if (result.IsFreeSpin) freeSpinWinnings += result.Payout;

                if (result.IsJackpot)
                {
                    popup.ShowInfo("JACKPOT!", "777 AT MAX BET\nYOU WIN " + HudView.Format(result.Payout) + " CREDITS");
                    yield return StartCoroutine(popup.WaitUntilClosed());
                }
                else if (bigWin)
                {
                    popup.ShowInfo("BIG WIN!", "3 x " + symbol.DisplayName.ToUpperInvariant() + "\n+" + HudView.Format(result.Payout) + " CREDITS");
                    yield return StartCoroutine(popup.WaitUntilClosed());
                }

                if (result.FreeSpinsAwarded > 0)
                {
                    if (!result.IsFreeSpin) freeSpinWinnings = 0;
                    audioService.Play(Sfx.FreeSpins);
                    hud.SetFreeSpins(engine.FreeSpinsRemaining, engine.FreeSpinBet);
                    popup.ShowInfo(result.IsFreeSpin ? "MORE FREE SPINS!" : "FREE SPINS!",
                        "+" + result.FreeSpinsAwarded + " FREE SPINS\nPLAYED AT " + HudView.Format(engine.FreeSpinBet) + " CREDITS");
                    yield return StartCoroutine(popup.WaitUntilClosed());
                }
            }
            else
            {
                lights.SetShow(LightShow.Idle);
                hud.ShowMessage(result.IsFreeSpin ? "NO WIN ON THIS FREE SPIN" : LossLines[Random.Range(0, LossLines.Length)],
                    MessageTone.Neutral);
            }

            if (result.IsFreeSpin && !engine.HasFreeSpins)
            {
                hud.SetFreeSpins(0, 0);
                popup.ShowInfo("FREE SPINS OVER", "YOU WON " + HudView.Format(freeSpinWinnings) + " CREDITS");
                yield return StartCoroutine(popup.WaitUntilClosed());
                hud.ShowMessage("PULL THE LEVER OR PRESS SPACE", MessageTone.Neutral);
            }
        }

        private void OfferRefill()
        {
            audioService.Play(Sfx.NoCredits);
            hud.ShowMessage("OUT OF CREDITS", MessageTone.Warning);
            popup.ShowQuestion("OUT OF CREDITS",
                "REFILL TO " + HudView.Format(engine.Rules.StartingCredits) + " CREDITS?",
                yes: () =>
                {
                    Bank.Refill();
                    hud.SetCredits(Bank.Credits);
                    RefreshBet();
                    hud.ShowMessage("CREDITS REFILLED - GOOD LUCK!", MessageTone.Bonus);
                },
                no: () => hud.ShowMessage("OUT OF CREDITS - PULL THE LEVER TO REFILL", MessageTone.Warning));
        }

        private void LowerBetToCredits()
        {
            Bank.ClampBetToCredits();
            RefreshBet();
            hud.ShowMessage("BET LOWERED TO " + HudView.Format(Bank.CurrentBet), MessageTone.Warning);
        }

        private void RefreshBet() =>
            hud.SetBet(Bank.CurrentBet, Bank.CanDecreaseBet, Bank.CanIncreaseBet, Bank.IsMaxBet, engine.Rules.MaxBet);

        /// <summary>Paytable rows built from the config, richest first.</summary>
        private List<PaytableLine> BuildPaytableLines()
        {
            var lines = new List<PaytableLine>();
            foreach (var symbol in config.Symbols.OrderByDescending(s => s.Multiplier))
            {
                string note = symbol.Type == config.JackpotSymbol
                    ? "MAX BET: JACKPOT"
                    : symbol.FreeSpins > 0 ? "+" + symbol.FreeSpins + " FREE SPINS" : "";
                lines.Add(new PaytableLine(symbol.Sprite, "x" + symbol.Multiplier, note));
            }
            return lines;
        }
    }
}
