using Riftbound.Simulation;
using UnityEngine;

namespace Riftbound.View
{
    /// <summary>Big rectangle Core with its Stability. Flashes yellow while vulnerable (Rift Break).</summary>
    public class CoreView : MonoBehaviour
    {
        Core core;
        SpriteRenderer body, border;
        TextMesh label, vulnText;
        int shownStability = -1;
        Color teamColor;

        public void Build(Core c)
        {
            core = c;
            teamColor = Palette.Of(c.Team);
            transform.localPosition = new Vector3(c.Position.x, c.Position.y, 0f);

            border = ShapeLibrary.CreateSprite(transform, "Border", ShapeLibrary.Shape.Pixel, Color.black, 30);
            border.transform.localScale = new Vector3(c.Size.x + 0.12f, c.Size.y + 0.12f, 1f);
            body = ShapeLibrary.CreateSprite(transform, "Body", ShapeLibrary.Shape.Pixel, teamColor, 31);
            body.transform.localScale = new Vector3(c.Size.x, c.Size.y, 1f);

            label = ShapeLibrary.CreateText(transform, "Label", "", 0.32f, Color.white, 35);
            vulnText = ShapeLibrary.CreateText(transform, "Vulnerable", "VULNERABLE!", 0.3f, Palette.Contested, 35);
            // Text on the field side of the core.
            float side = c.Team == Team.Player ? 1f : -1f;
            vulnText.transform.localPosition = new Vector3(0f, side * (c.Size.y * 0.5f + 0.25f), 0f);
            vulnText.gameObject.SetActive(false);
        }

        public void Sync(float time)
        {
            int s = Mathf.CeilToInt(core.Stability);
            if (s != shownStability)
            {
                shownStability = s;
                label.text = (core.Team == Team.Player ? "PLAYER CORE\n" : "ENEMY CORE\n") + s;
            }

            bool vuln = core.Vulnerable;
            vulnText.gameObject.SetActive(vuln);
            float blink = Mathf.PingPong(time * 5f, 1f);
            border.color = vuln ? Color.Lerp(Palette.Contested, Color.black, blink * 0.6f) : Color.black;
            body.color = core.TimeSinceHit < 0.1f ? Color.white : vuln ? Color.Lerp(teamColor, Palette.Contested, 0.25f * blink) : teamColor;
        }
    }
}
