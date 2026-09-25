using UnityEngine;

namespace Riftbound
{
    /// <summary>The two sides of a match. Player = bottom of the screen, Enemy = top.</summary>
    public enum Team
    {
        Player = 0,
        Enemy = 1,
    }

    /// <summary>Owner of a Rift.</summary>
    public enum RiftOwner
    {
        Neutral = 0,
        Player = 1,
        Enemy = 2,
    }

    public static class TeamUtil
    {
        public static Team Opponent(this Team t) => t == Team.Player ? Team.Enemy : Team.Player;

        /// <summary>+1 for the player (attacks upwards), -1 for the enemy.</summary>
        public static float Forward(this Team t) => t == Team.Player ? 1f : -1f;

        public static string Label(this Team t) => t == Team.Player ? "PLAYER" : "ENEMY";

        public static RiftOwner ToOwner(this Team t) => t == Team.Player ? RiftOwner.Player : RiftOwner.Enemy;

        public static bool Is(this RiftOwner owner, Team t) => owner == t.ToOwner();
    }

    /// <summary>All colours of the prototype in one place.</summary>
    public static class Palette
    {
        public static readonly Color Background = new Color(0.08f, 0.09f, 0.11f);
        public static readonly Color FieldTint = new Color(0.11f, 0.12f, 0.15f);
        public static readonly Color Player = new Color(0.25f, 0.55f, 1f);
        public static readonly Color Enemy = new Color(1f, 0.3f, 0.3f);
        public static readonly Color Neutral = new Color(0.55f, 0.57f, 0.6f);
        public static readonly Color Contested = new Color(1f, 0.8f, 0.2f);
        public static readonly Color Edge = new Color(0.45f, 0.48f, 0.55f, 0.7f);
        public static readonly Color EdgeNew = new Color(1f, 0.85f, 0.3f, 0.9f);
        public static readonly Color HpBack = new Color(0f, 0f, 0f, 0.75f);
        public static readonly Color HpGood = new Color(0.35f, 0.95f, 0.4f);
        public static readonly Color HpLow = new Color(1f, 0.6f, 0.2f);
        public static readonly Color Text = new Color(0.95f, 0.95f, 0.95f);
        public static readonly Color TextDim = new Color(0.7f, 0.72f, 0.75f);
        public static readonly Color Valid = new Color(0.3f, 1f, 0.45f, 0.9f);
        public static readonly Color Invalid = new Color(1f, 0.25f, 0.25f, 0.9f);
        public static readonly Color Parasite = new Color(0.75f, 1f, 0.2f);
        public static readonly Color Panel = new Color(0.05f, 0.06f, 0.08f, 0.96f);

        public static Color Of(Team t) => t == Team.Player ? Player : Enemy;

        public static Color Of(RiftOwner o)
        {
            switch (o)
            {
                case RiftOwner.Player: return Player;
                case RiftOwner.Enemy: return Enemy;
                default: return Neutral;
            }
        }

        public static Color WithAlpha(this Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
