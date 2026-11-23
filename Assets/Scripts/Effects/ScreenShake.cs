using System.Collections;
using UnityEngine;

namespace SlotMachine.Effects
{
    /// <summary>Shakes the stage for big moments; offsets stay on whole pixels.</summary>
    public sealed class ScreenShake : MonoBehaviour
    {
        [SerializeField] private RectTransform target;

        private Vector2 rest;
        private Coroutine shaking;

        private void Awake() => rest = target.anchoredPosition;

        public void Shake(float duration, float magnitude)
        {
            if (shaking != null) StopCoroutine(shaking);
            shaking = StartCoroutine(Run(duration, magnitude));
        }

        private IEnumerator Run(float duration, float magnitude)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float strength = magnitude * (1f - t / duration);
                var offset = new Vector2(Mathf.Round(Random.Range(-strength, strength)),
                                         Mathf.Round(Random.Range(-strength, strength)));
                target.anchoredPosition = rest + offset;
                yield return null;
            }
            target.anchoredPosition = rest;
            shaking = null;
        }
    }
}
