using System;
using System.Collections;
using SlotMachine.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.Gameplay
{
    /// <summary>
    /// Draws one reel strip inside its masked window and animates a spin:
    /// a small wind-up, acceleration to full speed (with motion-blurred art),
    /// then a stop that eases onto the chosen strip position with a slight
    /// overshoot and settle, like a mechanical reel.
    ///
    /// Position is measured in symbols along the strip and grows as the reel
    /// spins downward, so symbol k is drawn at (k - position) * spacing from
    /// the payline: higher strip indices arrive from the top.
    /// </summary>
    public sealed class ReelView : MonoBehaviour
    {
        private enum Phase { Idle, WindUp, Spinning, Decelerating, Settling }

        [Header("Layout")]
        [Tooltip("Symbol images, recycled as the strip scrolls. Five covers the window plus a spare.")]
        [SerializeField] private Image[] cells;
        [SerializeField] private float symbolSpacing = 96f;
        [Tooltip("Y of the payline (window centre), relative to the window's top edge.")]
        [SerializeField] private float paylineY = -105f;
        [Tooltip("Round symbol positions to whole pixels for the retro stepped look.")]
        [SerializeField] private bool pixelSnap = true;

        [Header("Motion (symbols per second)")]
        [SerializeField] private float maxSpeed = 20f;
        [SerializeField] private float acceleration = 90f;
        [SerializeField] private float windUpDistance = 0.3f;
        [SerializeField] private float windUpDuration = 0.12f;
        [SerializeField] private float blurSpeedThreshold = 9f;

        [Header("Stopping")]
        [Tooltip("Distance over which the reel brakes onto its target.")]
        [SerializeField] private float decelerationDistance = 2.5f;
        [Tooltip("Shorter braking used when the player slams the reels to a stop.")]
        [SerializeField] private float quickStopDistance = 1.1f;
        [SerializeField] private float overshoot = 0.16f;
        [SerializeField] private float settleDuration = 0.14f;

        /// <summary>Raised each time a symbol crosses the payline while moving (for tick sounds).</summary>
        public event Action<ReelView> SymbolPassed;

        /// <summary>Raised once the reel has settled on its target.</summary>
        public event Action<ReelView> Stopped;

        private ReelStrip strip;
        private SlotMachineConfig config;

        private Phase phase = Phase.Idle;
        private double position;
        private float speed;
        private double phaseFrom, phaseTo;
        private float phaseTime, phaseDuration;
        private int requestedStop = -1;
        private bool quickStop;
        private bool hasTarget;
        private double targetPosition;
        private int lastWholePosition;

        public bool IsSpinning => phase != Phase.Idle;

        /// <summary>The image currently sitting on the payline (valid when idle).</summary>
        public Image PaylineCell => cells[2];

        /// <summary>Places the strip on the reel at a resting stop position.</summary>
        public void Bind(ReelStrip reelStrip, SlotMachineConfig machineConfig, int restingStop)
        {
            strip = reelStrip;
            config = machineConfig;
            position = strip.Wrap(restingStop);
            lastWholePosition = (int)position;
            phase = Phase.Idle;
            speed = 0f;
            Render();
        }

        /// <summary>Starts spinning. The target is supplied later through <see cref="Stop"/>.</summary>
        public void StartSpin()
        {
            if (strip == null || phase != Phase.Idle) return;
            requestedStop = -1;
            hasTarget = false;
            quickStop = false;
            speed = 0f;
            BeginPhase(Phase.WindUp, position, position - windUpDistance, windUpDuration);
        }

        /// <summary>
        /// Asks the reel to land <paramref name="stopIndex"/> on the payline.
        /// It keeps spinning until it can brake naturally onto that position.
        /// </summary>
        public void Stop(int stopIndex, bool quick)
        {
            requestedStop = strip.Wrap(stopIndex);
            quickStop |= quick;
        }


        private void Update()
        {
            if (phase == Phase.Idle) return;

            float dt = Time.deltaTime;
            switch (phase)
            {
                case Phase.WindUp:
                    if (AdvancePhase(dt, EaseOutQuad))
                        phase = Phase.Spinning;
                    break;

                case Phase.Spinning:
                    speed = Mathf.Min(maxSpeed, speed + acceleration * dt);
                    position += speed * dt;
                    TryBeginBraking();
                    break;

                case Phase.Decelerating:
                    // Ease-out cubic: velocity starts at the cruise speed and falls to zero.
                    float before = (float)position;
                    if (AdvancePhase(dt, EaseOutCubic))
                        BeginPhase(Phase.Settling, position, targetPosition, settleDuration);
                    speed = dt > 0f ? Mathf.Abs(((float)position - before) / dt) : 0f;
                    break;

                case Phase.Settling:
                    if (AdvancePhase(dt, EaseInOutSine))
                        FinishStop();
                    break;
            }

            EmitTicks();
            Render();
        }

        /// <summary>
        /// Once a stop is requested (and the reel is up to speed, or the player
        /// slammed it), pick the landing position and start braking at once.
        ///
        /// While the reel is a motion-blurred streak, the strip is quietly
        /// re-aligned so the target sits exactly one braking distance ahead —
        /// the same trick video slots use. Without it a reel could need up to a
        /// full extra lap to reach its stop, so reels would finish out of order.
        /// </summary>
        private void TryBeginBraking()
        {
            if (requestedStop < 0 || hasTarget) return;
            // Normal stops wait for full speed; a slammed stop only needs the
            // reel to be blurred, so the re-alignment below stays invisible.
            bool readyToBrake = speed >= maxSpeed || (quickStop && speed > blurSpeedThreshold);
            if (!readyToBrake) return;

            float brakeDistance = quickStop ? quickStopDistance : decelerationDistance;
            int aligned = (int)Math.Ceiling(position + brakeDistance);
            int lapOffset = strip.Wrap(requestedStop - aligned);

            position += lapOffset; // invisible while blurred
            lastWholePosition = (int)Math.Floor(position);
            targetPosition = aligned + lapOffset;
            hasTarget = true;

            double end = targetPosition + overshoot;
            float duration = Mathf.Max(0.08f, 3f * (float)(end - position) / Mathf.Max(speed, 1f));
            BeginPhase(Phase.Decelerating, position, end, duration);
        }

        private void FinishStop()
        {
            // Snap exactly onto the target and fold the position back into one
            // lap of the strip so it never grows large enough to lose precision.
            position = strip.Wrap((int)Math.Round(targetPosition));
            lastWholePosition = (int)position;
            speed = 0f;
            phase = Phase.Idle;
            Render();
            Stopped?.Invoke(this);
        }

        private void BeginPhase(Phase next, double from, double to, float duration)
        {
            phase = next;
            phaseFrom = from;
            phaseTo = to;
            phaseTime = 0f;
            phaseDuration = Mathf.Max(0.0001f, duration);
        }

        /// <summary>Moves along the current tween; returns true when it has finished.</summary>
        private bool AdvancePhase(float dt, Func<float, float> ease)
        {
            phaseTime += dt;
            float t = Mathf.Clamp01(phaseTime / phaseDuration);
            position = phaseFrom + (phaseTo - phaseFrom) * ease(t);
            return t >= 1f;
        }

        private void EmitTicks()
        {
            int whole = (int)Math.Floor(position);
            if (whole == lastWholePosition) return;
            lastWholePosition = whole;
            SymbolPassed?.Invoke(this);
        }

        private void Render()
        {
            if (strip == null) return;

            int baseIndex = (int)Math.Floor(position);
            bool blurred = speed > blurSpeedThreshold;
            for (int i = 0; i < cells.Length; i++)
            {
                int k = baseIndex - 2 + i;
                float y = paylineY + (float)((k - position) * symbolSpacing);
                if (pixelSnap) y = Mathf.Round(y);

                var cell = cells[i];
                cell.rectTransform.anchoredPosition = new Vector2(cell.rectTransform.anchoredPosition.x, y);
                var symbol = config.GetSymbol(strip[k]);
                cell.sprite = blurred ? symbol.BlurredSprite : symbol.Sprite;
            }
        }

        /// <summary>Bounces the payline symbol a few times to celebrate a win.</summary>
        public IEnumerator PulsePaylineSymbol(int pulses, float pulseDuration)
        {
            var target = PaylineCell.rectTransform;
            for (int p = 0; p < pulses; p++)
            {
                for (float t = 0f; t < pulseDuration; t += Time.deltaTime)
                {
                    float s = 1f + 0.14f * Mathf.Sin(Mathf.PI * t / pulseDuration);
                    target.localScale = new Vector3(s, s, 1f);
                    yield return null;
                }
            }
            target.localScale = Vector3.one;
        }

        private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        private static float EaseInOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
    }
}
