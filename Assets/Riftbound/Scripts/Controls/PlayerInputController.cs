using System;
using System.Collections.Generic;
using Riftbound.Config;
using Riftbound.Simulation;
using Riftbound.View;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Riftbound.Controls
{
    /// <summary>
    /// Human controller: tap a card (UI button → <see cref="SelectSlot"/>), then tap the map.
    /// Touch and mouse behave the same: press shows a preview (green = valid, red = invalid),
    /// dragging moves it, releasing plays the card. Presses that start on UI are ignored.
    /// Produces <see cref="PlayCardCommand"/>s exactly like the bot.
    /// Uses the legacy Input Manager (Project Settings > Player > Active Input Handling = Input Manager or Both).
    /// </summary>
    public class PlayerInputController
    {
        readonly Match match;
        readonly MatchView view;
        readonly Camera cam;
        readonly Team team;

        bool pressing;
        Vector2 lastWorld;

        public int SelectedSlot { get; private set; } = -1;

        /// <summary>Feedback message for the HUD: (text, isError).</summary>
        public event Action<string, bool> Feedback;

        public PlayerInputController(Match match, MatchView view, Camera cam, Team team)
        {
            this.match = match;
            this.view = view;
            this.cam = cam;
            this.team = team;
        }

        public CardDefinition SelectedCard => SelectedSlot >= 0 ? match.GetDeck(team).GetSlot(SelectedSlot) : null;

        /// <summary>Called by the card buttons. Tapping the selected card again deselects it.</summary>
        public void SelectSlot(int slot)
        {
            if (!match.IsRunning) return;
            SelectedSlot = SelectedSlot == slot ? -1 : slot;
            pressing = false;
            var card = SelectedCard;
            if (card != null && !match.GetFlux(team).CanAfford(card.fluxCost))
                Feedback?.Invoke("NOT ENOUGH FLUX (" + card.fluxCost + ")", true);
        }

        public void Deselect()
        {
            SelectedSlot = -1;
            pressing = false;
        }

        public void Tick()
        {
            var card = SelectedCard;
            if (!match.IsRunning || card == null)
            {
                if (!match.IsRunning) SelectedSlot = -1;
                view.ShowDeployZones(team, false);
                view.HideGhost();
                pressing = false;
                return;
            }

            view.ShowDeployZones(team, card.kind == CardKind.Unit);

#if ENABLE_LEGACY_INPUT_MANAGER
            ReadPointer(out bool down, out bool held, out bool up, out Vector2 screen, out bool overUi, out bool hover);

            if (down)
            {
                pressing = !overUi && cam.pixelRect.Contains(screen);
                if (pressing) lastWorld = ToWorld(screen);
            }
            else if (held && pressing) lastWorld = ToWorld(screen);

            if (pressing || (hover && !overUi && cam.pixelRect.Contains(screen)))
            {
                Vector2 w = pressing ? lastWorld : ToWorld(screen);
                bool valid = match.Cards.ValidateCard(card, team, w, false) == PlayResult.Ok;
                view.ShowGhost(card, w, valid, SnapPoint(card, w));
            }
            else view.HideGhost();

            if (up && pressing)
            {
                pressing = false;
                // Releasing outside the battlefield (e.g. dragging back onto the cards) cancels the play.
                if (cam.pixelRect.Contains(screen)) TryPlay(lastWorld);
                else view.HideGhost();
            }
#endif
        }

        void TryPlay(Vector2 world)
        {
            var card = SelectedCard;
            if (card == null) return;
            var result = match.Submit(new PlayCardCommand(team, SelectedSlot, world));
            if (result == PlayResult.Ok)
            {
                Deselect();
                view.HideGhost();
                view.ShowDeployZones(team, false);
            }
            else
            {
                view.FlashInvalid(world);
                Feedback?.Invoke(PlayResultText.Describe(result), true);
            }
        }

        /// <summary>Where a spell would actually land (Rift centre / targeted unit) for the preview.</summary>
        Vector2 SnapPoint(CardDefinition card, Vector2 w)
        {
            if (card.kind == CardKind.PulseSpell)
            {
                var r = match.Cards.FindRiftAt(w);
                return r != null ? r.Position : w;
            }
            if (card.kind == CardKind.ParasiteSpell)
            {
                var u = match.Units.FindEnemyNear(team, w, card.targetPickRadius);
                return u != null ? u.Position : w;
            }
            return w;
        }

        Vector2 ToWorld(Vector2 screen)
        {
            Vector3 p = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            return new Vector2(p.x, p.y);
        }

static readonly List<RaycastResult> raycastBuffer = new List<RaycastResult>();
        static PointerEventData pointerData;
        static EventSystem pointerDataOwner;

        /// <summary>
        /// Robust "is this screen point over UI?" (IsPointerOverGameObject is unreliable for touches on the
        /// frame they begin, depending on script order). Only called on press/hover, the cost is negligible.
        /// </summary>
        static bool IsOverUi(Vector2 screen)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            if (pointerData == null || pointerDataOwner != es)
            {
                pointerData = new PointerEventData(es);
                pointerDataOwner = es;
            }
            pointerData.position = screen;
            raycastBuffer.Clear();
            es.RaycastAll(pointerData, raycastBuffer);
            return raycastBuffer.Count > 0;
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        static void ReadPointer(out bool down, out bool held, out bool up, out Vector2 screen, out bool overUi, out bool hover)
        {
            if (UnityEngine.Input.touchCount > 0)
            {
                var t = UnityEngine.Input.GetTouch(0);
                screen = t.position;
                down = t.phase == TouchPhase.Began;
                up = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
                held = !down && !up;
                overUi = down && IsOverUi(screen);
                hover = false;
                return;
            }

            screen = UnityEngine.Input.mousePosition;
            down = UnityEngine.Input.GetMouseButtonDown(0);
            up = UnityEngine.Input.GetMouseButtonUp(0);
            held = UnityEngine.Input.GetMouseButton(0);
            // Mouse hover preview is an Editor/desktop convenience only; touch never relies on it.
            hover = !held && !up && !UnityEngine.Input.touchSupported;
            overUi = (down || hover) && IsOverUi(screen);
        }
#endif
    }
}
