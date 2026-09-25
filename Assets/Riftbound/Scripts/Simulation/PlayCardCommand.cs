using UnityEngine;

namespace Riftbound.Simulation
{
    /// <summary>
    /// The ONLY way a controller (human input, bot, or later a network peer) acts on the match.
    /// Commands are validated by <see cref="CardSystem"/> with exactly the same rules for everyone,
    /// so the bot can be swapped for a multiplayer command source without touching combat code.
    /// </summary>
    public struct PlayCardCommand
    {
        public Team Team;
        /// <summary>Hand slot (0..handSize-1) of the card being played.</summary>
        public int HandSlot;
        /// <summary>World position of the tap (unit deploy position / Rift / unit selection point).</summary>
        public Vector2 Position;

        public PlayCardCommand(Team team, int handSlot, Vector2 position)
        {
            Team = team;
            HandSlot = handSlot;
            Position = position;
        }
    }

    /// <summary>Result of validating or executing a <see cref="PlayCardCommand"/>.</summary>
    public enum PlayResult
    {
        Ok = 0,
        MatchNotRunning,
        InvalidSlot,
        NotEnoughFlux,
        InvalidPosition,
        UnitLimit,
        NoRiftThere,
        NoEnemyUnitThere,
    }

    public static class PlayResultText
    {
        public static string Describe(PlayResult r)
        {
            switch (r)
            {
                case PlayResult.Ok: return "OK";
                case PlayResult.MatchNotRunning: return "MATCH OVER";
                case PlayResult.InvalidSlot: return "NO CARD";
                case PlayResult.NotEnoughFlux: return "NOT ENOUGH FLUX";
                case PlayResult.InvalidPosition: return "INVALID POSITION";
                case PlayResult.UnitLimit: return "UNIT LIMIT REACHED";
                case PlayResult.NoRiftThere: return "TAP A RIFT";
                case PlayResult.NoEnemyUnitThere: return "TAP AN ENEMY UNIT";
                default: return r.ToString();
            }
        }
    }
}
