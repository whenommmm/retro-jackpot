using System.Collections.Generic;
using UnityEngine;

namespace SlotMachine.UI
{
    /// <summary>One line of the paytable, as plain display data.</summary>
    public readonly struct PaytableLine
    {
        public readonly Sprite Icon;
        public readonly string Payout;
        public readonly string Note;

        public PaytableLine(Sprite icon, string payout, string note)
        {
            Icon = icon;
            Payout = payout;
            Note = note;
        }
    }

    /// <summary>
    /// The paytable panel. Lines are generated from the machine config at
    /// start-up, so changing a payout in the Inspector updates the table too.
    /// </summary>
    public sealed class PaytableView : MonoBehaviour
    {
        [SerializeField] private PaytableRow[] rows;

        public void Show(IReadOnlyList<PaytableLine> lines)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                bool used = i < lines.Count;
                rows[i].gameObject.SetActive(used);
                if (used) rows[i].Set(lines[i]);
            }
        }
    }
}
