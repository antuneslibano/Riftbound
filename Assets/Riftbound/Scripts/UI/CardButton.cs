using Riftbound.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Riftbound.UI
{
    /// <summary>One hand slot: name + cost (+ type), a Flux progress fill and a selection outline.</summary>
    public class CardButton
    {
        public readonly Button Button;
        readonly Image background;
        readonly Image progress;
        readonly Image[] outline;
        readonly Text nameText, costText, typeText;
        readonly RectTransform progressRt;

        CardDefinition shownCard;
        int shownState = -1;

        public CardButton(Transform parent, int slot, System.Action<int> onClick)
        {
            Button = UIFactory.Button(parent, "Card" + slot, "", 10, new Color(0.16f, 0.18f, 0.24f), () => onClick(slot), out var dummy);
            dummy.text = "";
            background = (Image)Button.targetGraphic;

            progress = UIFactory.Panel(Button.transform, "FluxProgress", new Color(0.3f, 0.45f, 0.9f, 0.25f));
            progressRt = progress.rectTransform;
            progressRt.Anchor(Vector2.zero, new Vector2(1f, 0f), Vector2.zero, Vector2.zero);

            nameText = UIFactory.Label(Button.transform, "Name", "", 46, Color.white);
            nameText.rectTransform.Anchor(new Vector2(0f, 0.38f), new Vector2(1f, 0.78f), Vector2.zero, Vector2.zero);
            costText = UIFactory.Label(Button.transform, "Cost", "", 64, new Color(0.55f, 0.8f, 1f));
            costText.rectTransform.Anchor(new Vector2(0f, 0.02f), new Vector2(1f, 0.4f), Vector2.zero, Vector2.zero);
            typeText = UIFactory.Label(Button.transform, "Type", "", 24, new Color(0.7f, 0.72f, 0.75f), TextAnchor.MiddleCenter, FontStyle.Normal);
            typeText.rectTransform.Anchor(new Vector2(0f, 0.78f), new Vector2(1f, 0.98f), Vector2.zero, Vector2.zero);

            outline = UIFactory.Outline(Button.transform, Color.clear, 8f);
        }

        /// <param name="flux">current Flux (for affordability/progress)</param>
        public void Refresh(CardDefinition card, float flux, bool selected)
        {
            if (card != shownCard)
            {
                shownCard = card;
                shownState = -1;
                nameText.text = card != null ? card.displayName : "-";
                costText.text = card != null ? card.fluxCost.ToString() : "";
                typeText.text = card == null ? "" : card.kind == CardKind.Unit
                    ? (card.spawnCount > 1 ? "UNIT x" + card.spawnCount : "UNIT")
                    : "SPELL";
            }
            if (card == null) return;

            bool affordable = flux + 0.0001f >= card.fluxCost;
            float p = card.fluxCost > 0 ? Mathf.Clamp01(flux / card.fluxCost) : 1f;
            progressRt.anchorMax = new Vector2(1f, affordable ? 0f : p);

            int state = (affordable ? 1 : 0) + (selected ? 2 : 0);
            if (state == shownState) return;
            shownState = state;

            background.color = selected ? new Color(0.28f, 0.3f, 0.12f) : affordable ? new Color(0.16f, 0.2f, 0.3f) : new Color(0.12f, 0.12f, 0.14f);
            nameText.color = affordable ? Color.white : new Color(0.55f, 0.55f, 0.58f);
            costText.color = affordable ? new Color(0.55f, 0.8f, 1f) : new Color(0.45f, 0.45f, 0.5f);
            UIFactory.SetColor(outline, selected ? Palette.Contested : affordable ? new Color(0.35f, 0.55f, 0.95f, 0.8f) : new Color(0.25f, 0.25f, 0.28f, 0.8f));
        }
    }
}
