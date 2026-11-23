using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotMachine.Audio
{
    public enum Sfx
    {
        ReelTick,
        ReelStop,
        LeverPull,
        ButtonClick,
        CoinTick,
        WinSmall,
        WinBig,
        Jackpot,
        FreeSpins,
        Anticipation,
        NoCredits,
        PopupOpen
    }

    [Serializable]
    public struct SfxClip
    {
        public Sfx id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    /// <summary>
    /// Plays every sound effect from one place, so gameplay code asks for
    /// "a reel stop" rather than juggling AudioSources. Rapid-fire sounds
    /// (reel ticks, coin ticks) are rate-limited so they never turn to noise.
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        private const string MutedKey = "slotmachine.muted";

        [SerializeField] private AudioSource effects;
        [Tooltip("Separate source for the looping anticipation sound.")]
        [SerializeField] private AudioSource loop;
        [SerializeField] private SfxClip[] clips;
        [SerializeField] private float tickInterval = 0.045f;

        private readonly Dictionary<Sfx, SfxClip> lookup = new Dictionary<Sfx, SfxClip>();
        private readonly Dictionary<Sfx, float> lastPlayed = new Dictionary<Sfx, float>();

        public bool Muted { get; private set; }

        private void Awake()
        {
            foreach (var c in clips) lookup[c.id] = c;
            Muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            ApplyMute();
        }

        public void Play(Sfx id)
        {
            if (!lookup.TryGetValue(id, out var c) || c.clip == null) return;

            if (id == Sfx.ReelTick || id == Sfx.CoinTick)
            {
                lastPlayed.TryGetValue(id, out float last);
                if (Time.unscaledTime - last < tickInterval) return;
                lastPlayed[id] = Time.unscaledTime;
            }
            effects.PlayOneShot(c.clip, c.volume);
        }

        public void PlayLoop(Sfx id)
        {
            if (!lookup.TryGetValue(id, out var c) || c.clip == null) return;
            loop.clip = c.clip;
            loop.volume = c.volume;
            loop.Play();
        }

        public void StopLoop() => loop.Stop();

        public void ToggleMute()
        {
            Muted = !Muted;
            PlayerPrefs.SetInt(MutedKey, Muted ? 1 : 0);
            ApplyMute();
        }

        private void ApplyMute() => AudioListener.volume = Muted ? 0f : 1f;
    }
}
