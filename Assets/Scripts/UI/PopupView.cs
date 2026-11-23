using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.UI
{
    /// <summary>
    /// One reusable popup built from the provided panel art: an info card with
    /// a close (X) button, or a question with YES / NO. The show and hide
    /// motion lives in an Animator; this class only manages content and input.
    /// </summary>
    public sealed class PopupView : MonoBehaviour
    {
        private static readonly int ShowTrigger = Animator.StringToHash("Show");
        private static readonly int HideTrigger = Animator.StringToHash("Hide");

        [SerializeField] private CanvasGroup group;
        [SerializeField] private Animator animator;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        private Action onClosed, onYes, onNo;

        public bool IsOpen { get; private set; }

        /// <summary>True when the open popup is a YES/NO question (it can't be dismissed with Space).</summary>
        public bool IsQuestion { get; private set; }

        /// <summary>Raised whenever a popup appears (used for its sound).</summary>
        public event Action Opened;

        private void Awake()
        {
            closeButton.onClick.AddListener(Dismiss);
            yesButton.onClick.AddListener(() => Answer(true));
            noButton.onClick.AddListener(() => Answer(false));
            SetInteractive(false);
        }

        public void ShowInfo(string heading, string text, Action closed = null)
        {
            onClosed = closed;
            onYes = onNo = null;
            Open(heading, text, question: false);
        }

        public void ShowQuestion(string heading, string text, Action yes, Action no)
        {
            onYes = yes;
            onNo = no;
            onClosed = null;
            Open(heading, text, question: true);
        }

        /// <summary>Closes an info popup (X button, Space or Enter).</summary>
        public void Dismiss()
        {
            if (!IsOpen || IsQuestion) return;
            var callback = onClosed;
            Close();
            callback?.Invoke();
        }

        public IEnumerator WaitUntilClosed()
        {
            while (IsOpen) yield return null;
        }

        private void Answer(bool yes)
        {
            if (!IsOpen) return;
            var callback = yes ? onYes : onNo;
            Close();
            callback?.Invoke();
        }

        private void Open(string heading, string text, bool question)
        {
            title.text = heading;
            body.text = text;
            IsQuestion = question;
            closeButton.gameObject.SetActive(!question);
            yesButton.gameObject.SetActive(question);
            noButton.gameObject.SetActive(question);

            IsOpen = true;
            SetInteractive(true);
            animator.ResetTrigger(HideTrigger);
            animator.SetTrigger(ShowTrigger);
            Opened?.Invoke();
        }

        private void Close()
        {
            IsOpen = false;
            onClosed = onYes = onNo = null;
            SetInteractive(false);
            animator.ResetTrigger(ShowTrigger);
            animator.SetTrigger(HideTrigger);
        }

        private void SetInteractive(bool on)
        {
            group.interactable = on;
            group.blocksRaycasts = on;
        }
    }
}
