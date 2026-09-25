using System.Collections.Generic;
using Riftbound.Config;

namespace Riftbound.Simulation
{
    /// <summary>
    /// Rotating deck of one team: a hand of N visible slots plus a queue of waiting cards.
    /// Playing a slot sends that card to the END of the queue and the FRONT of the queue fills the slot.
    /// </summary>
    public class DeckController
    {
        readonly CardDefinition[] hand;
        readonly Queue<CardDefinition> queue = new Queue<CardDefinition>();

        public int HandSize => hand.Length;

        /// <summary>Increments whenever the hand changes (UI refreshes lazily).</summary>
        public int Version { get; private set; }

        public DeckController(IList<CardDefinition> deck, int handSize, bool shuffle, System.Random rng)
        {
            var cards = new List<CardDefinition>(deck);
            if (shuffle)
            {
                for (int i = cards.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    var tmp = cards[i];
                    cards[i] = cards[j];
                    cards[j] = tmp;
                }
            }

            if (handSize > cards.Count) handSize = cards.Count;
            hand = new CardDefinition[handSize];
            for (int i = 0; i < cards.Count; i++)
            {
                if (i < handSize) hand[i] = cards[i];
                else queue.Enqueue(cards[i]);
            }
        }

        public CardDefinition GetSlot(int slot) => slot >= 0 && slot < hand.Length ? hand[slot] : null;

        /// <summary>The card that will enter the hand next.</summary>
        public CardDefinition Next => queue.Count > 0 ? queue.Peek() : null;

        /// <summary>Rotates the played card to the back of the deck.</summary>
        public void Consume(int slot)
        {
            var played = hand[slot];
            if (queue.Count > 0)
            {
                hand[slot] = queue.Dequeue();
                queue.Enqueue(played);
            }
            Version++;
        }

        /// <summary>Finds the hand slot holding a card with this id, or -1.</summary>
        public int FindSlot(string cardId)
        {
            for (int i = 0; i < hand.Length; i++)
                if (hand[i] != null && hand[i].cardId == cardId) return i;
            return -1;
        }
    }
}
