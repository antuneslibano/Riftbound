using Riftbound.Config;
using Riftbound.Simulation;
using UnityEngine;

namespace Riftbound.View
{
    /// <summary>
    /// Placeholder visual of a unit: team coloured shape, name above, HP bar + HP number,
    /// Parasite countdown, and AI info in Debug Mode. Pooled by <see cref="MatchView"/>.
    /// Text is only rebuilt when the displayed value changes (no per-frame string garbage).
    /// </summary>
    public class UnitView : MonoBehaviour
    {
        SpriteRenderer outline, body, hpBack, hpFill, attackLine;
        TextMesh nameText, hpText, statusText;
        Transform hpFillT;

        public Unit Unit { get; private set; }

        int shownHp = -1;
        int shownParasite = -1;
        float debugRefresh;
        float barWidth;
        Color teamColor;

        public void Build()
        {
            outline = ShapeLibrary.CreateSprite(transform, "Outline", ShapeLibrary.Shape.Circle, Color.black, 40);
            body = ShapeLibrary.CreateSprite(transform, "Body", ShapeLibrary.Shape.Circle, Color.white, 41);
            hpBack = ShapeLibrary.CreateSprite(transform, "HpBack", ShapeLibrary.Shape.Pixel, Palette.HpBack, 45);
            hpFill = ShapeLibrary.CreateSprite(hpBack.transform, "HpFill", ShapeLibrary.Shape.Pixel, Palette.HpGood, 46);
            hpFillT = hpFill.transform;
            attackLine = ShapeLibrary.CreateSprite(transform, "AttackLine", ShapeLibrary.Shape.Pixel, Color.white, 39);
            nameText = ShapeLibrary.CreateText(transform, "Name", "", 0.3f, Palette.Text, 50);
            hpText = ShapeLibrary.CreateText(transform, "Hp", "", 0.22f, Palette.TextDim, 50);
            statusText = ShapeLibrary.CreateText(transform, "Status", "", 0.24f, Palette.Parasite, 51);
        }

        public void Bind(Unit u)
        {
            Unit = u;
            gameObject.SetActive(true);
            var s = u.Stats;
            float r = s.bodyRadius;
            teamColor = Palette.Of(u.Team);

            var sprite = ShapeLibrary.Get(s.shape);
            body.sprite = sprite;
            outline.sprite = sprite;
            body.transform.localScale = Vector3.one * (r * 2f);
            outline.transform.localScale = Vector3.one * (r * 2f + 0.08f);
            // Enemy triangles point down (towards the player).
            float rot = u.Team == Team.Enemy ? 180f : 0f;
            body.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
            outline.transform.localRotation = body.transform.localRotation;
            body.color = teamColor;

            barWidth = Mathf.Max(0.55f, r * 2f);
            hpBack.transform.localPosition = new Vector3(0f, r + 0.1f, 0f);
            hpBack.transform.localScale = new Vector3(barWidth, 0.075f, 1f);

            nameText.text = u.Name;
            nameText.transform.localPosition = new Vector3(0f, r + 0.32f, 0f);
            nameText.color = Color.Lerp(teamColor, Color.white, 0.55f);
            hpText.transform.localPosition = new Vector3(0f, -r - 0.17f, 0f);
            statusText.transform.localPosition = new Vector3(0f, r + 0.6f, 0f);
            statusText.text = "";
            attackLine.enabled = false;

            shownHp = -1;
            shownParasite = -1;
            debugRefresh = 0f;
            Sync(false, 0f);
        }

        public void Unbind()
        {
            Unit = null;
            gameObject.SetActive(false);
        }

        public void Sync(bool debug, float unscaledDt)
        {
            var u = Unit;
            if (u == null) return;
            transform.localPosition = new Vector3(u.Position.x, u.Position.y, 0f);

            // HP bar (left anchored fill).
            float f = u.HpFraction;
            hpFillT.localScale = new Vector3(f, 1f, 1f);
            hpFillT.localPosition = new Vector3((f - 1f) * 0.5f, 0f, 0f);
            hpFill.color = f > 0.35f ? Palette.HpGood : Palette.HpLow;

            int hp = Mathf.CeilToInt(Mathf.Max(0f, u.Hp));
            if (hp != shownHp)
            {
                shownHp = hp;
                hpText.text = hp + " HP";
            }

            // Hit flash.
            body.color = u.TimeSinceHit < 0.08f ? Color.white : teamColor;

            // Attack line (very short flash towards the hit target).
            bool showLine = u.TimeSinceAttack < 0.1f;
            attackLine.enabled = showLine;
            if (showLine)
            {
                ShapeLibrary.SetLine(attackLine.transform, Vector2.zero, u.LastAttackPoint - u.Position, 0.05f);
                attackLine.color = Color.Lerp(teamColor, Color.white, 0.5f);
            }

            // Status line: Parasite countdown, or AI info in debug mode.
            if (u.Infected)
            {
                int secs = Mathf.CeilToInt(u.ParasiteTimer);
                if (secs != shownParasite)
                {
                    shownParasite = secs;
                    statusText.color = Palette.Parasite;
                    statusText.text = "PARASITE: " + secs + "s";
                }
            }
            else if (debug)
            {
                debugRefresh -= unscaledDt;
                if (debugRefresh <= 0f)
                {
                    debugRefresh = 0.25f;
                    shownParasite = -1;
                    statusText.color = Palette.Contested;
                    statusText.text = DebugLine(u);
                }
            }
            else if (shownParasite != -1 || statusText.text.Length > 0)
            {
                shownParasite = -1;
                statusText.text = "";
            }
        }

        static string DebugLine(Unit u)
        {
            string target = u.TargetUnit != null ? u.TargetUnit.Name + "#" + u.TargetUnit.Id
                : u.TargetingCore ? "CORE" : "-";
            string goal = u.HasGoal ? u.GoalNode.ToString().Replace("Rift", "") : "-";
            return u.State + " T:" + target + " G:" + goal + " DPS:" + u.Stats.Dps.ToString("0");
        }
    }
}
