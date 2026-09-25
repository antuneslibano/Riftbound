using Riftbound.Simulation;
using UnityEngine;

namespace Riftbound.View
{
    /// <summary>Hexagon Rift: fill colour = owner, bar = capture control (-1 red .. +1 blue), status text.</summary>
    public class RiftView : MonoBehaviour
    {
        Rift rift;
        SpriteRenderer fill, ring, barBack, barFill, centerMark;
        TextMesh nameText, statusText;
        Transform barFillT;
        string shownStatus = "";
        int shownPercent = -1;
        const float BarWidth = 1.6f;

        public void Build(Rift r)
        {
            rift = r;
            float d = r.Radius * 2f;
            fill = ShapeLibrary.CreateSprite(transform, "Fill", ShapeLibrary.Shape.Hexagon, Palette.Neutral, 20);
            fill.transform.localScale = Vector3.one * d;
            ring = ShapeLibrary.CreateSprite(transform, "Ring", ShapeLibrary.Shape.Ring, Palette.Neutral, 21);
            ring.transform.localScale = Vector3.one * (d + 0.1f);
            centerMark = ShapeLibrary.CreateSprite(transform, "Center", ShapeLibrary.Shape.Hexagon, Palette.Neutral, 21);
            centerMark.transform.localScale = Vector3.one * 0.25f;

            barBack = ShapeLibrary.CreateSprite(transform, "BarBack", ShapeLibrary.Shape.Pixel, Palette.HpBack, 22);
            barBack.transform.localPosition = new Vector3(0f, -r.Radius - 0.18f, 0f);
            barBack.transform.localScale = new Vector3(BarWidth, 0.12f, 1f);
            barFill = ShapeLibrary.CreateSprite(barBack.transform, "BarFill", ShapeLibrary.Shape.Pixel, Palette.Player, 23);
            barFillT = barFill.transform;
            var mid = ShapeLibrary.CreateSprite(barBack.transform, "Mid", ShapeLibrary.Shape.Pixel, Color.white, 24);
            mid.transform.localScale = new Vector3(0.02f, 1.4f, 1f);

            nameText = ShapeLibrary.CreateText(transform, "Name", r.Name, 0.38f, Palette.Text, 25);
            nameText.transform.localPosition = new Vector3(0f, r.Radius + 0.26f, 0f);
            statusText = ShapeLibrary.CreateText(transform, "Status", "NEUTRAL", 0.28f, Palette.TextDim, 25);
            statusText.transform.localPosition = new Vector3(0f, -r.Radius - 0.46f, 0f);
        }

        public void Sync(float time)
        {
            var p = rift.Position;
            transform.localPosition = new Vector3(p.x, p.y, 0f);

            Color owner = Palette.Of(rift.Owner);
            bool contested = rift.Contested;
            fill.color = owner.WithAlpha(rift.Owner == RiftOwner.Neutral ? 0.18f : 0.32f);
            ring.color = contested
                ? Color.Lerp(Palette.Contested, owner, Mathf.PingPong(time * 4f, 1f))
                : owner.WithAlpha(0.95f);
            centerMark.color = owner;

            float c = rift.Control;
            float w = Mathf.Abs(c);
            barFillT.localScale = new Vector3(w * 0.5f, 1f, 1f);
            barFillT.localPosition = new Vector3(Mathf.Sign(c) * w * 0.25f, 0f, 0f);
            barFill.color = c >= 0f ? Palette.Player : Palette.Enemy;

            string status;
            int percent = -1;
            if (contested) status = "CONTESTED";
            else if (rift.Presence[0] > 0 && !rift.Owner.Is(Team.Player) || rift.Presence[1] > 0 && !rift.Owner.Is(Team.Enemy))
            {
                status = "CAPTURING";
                percent = Mathf.RoundToInt(w * 100f);
            }
            else status = rift.Owner == RiftOwner.Neutral ? "NEUTRAL" : rift.Owner == RiftOwner.Player ? "PLAYER" : "ENEMY";

            if (status != shownStatus || percent != shownPercent)
            {
                shownStatus = status;
                shownPercent = percent;
                statusText.text = percent >= 0 ? status + " " + percent + "%" : status;
                statusText.color = contested ? Palette.Contested : Color.Lerp(owner, Color.white, 0.4f);
            }
        }
    }
}
