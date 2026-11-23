using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.UI
{
    /// <summary>A paytable row: three copies of the symbol, its payout and an optional note.</summary>
    public sealed class PaytableRow : MonoBehaviour
    {
        [SerializeField] private Image[] icons;
        [SerializeField] private TMP_Text payout;
        [SerializeField] private TMP_Text note;

        public void Set(PaytableLine line)
        {
            foreach (var icon in icons) icon.sprite = line.Icon;
            payout.text = line.Payout;
            note.text = line.Note;
            note.gameObject.SetActive(!string.IsNullOrEmpty(line.Note));
        }
    }
}
