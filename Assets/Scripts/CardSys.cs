using UnityEngine;

[System.Serializable]
public class Card
{
    public enum Suit { Hearts, Diamonds, Clubs, Spades }
    public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }

    public Suit suit;
    public Rank rank;
    public int value;
    public Sprite cardSprite;

    public Card(Suit s, Rank r)
    {
        suit = s;
        rank = r;
        value = (int)rank;
    }

    public override string ToString()
    {
        return $"{rank} of {suit}";
    }
}

public class CardDeck
{
    private System.Collections.Generic.List<Card> cards;
    private int currentIndex = 0;

    public CardDeck()
    {
        InitializeDeck();
        Shuffle();
    }

    private void InitializeDeck()
    {
        cards = new System.Collections.Generic.List<Card>();

        foreach (Card.Suit suit in System.Enum.GetValues(typeof(Card.Suit)))
        {
            foreach (Card.Rank rank in System.Enum.GetValues(typeof(Card.Rank)))
            {
                cards.Add(new Card(suit, rank));
            }
        }
    }

    public void Shuffle()
    {
        currentIndex = 0;
        for (int i = 0; i < cards.Count; i++)
        {
            Card temp = cards[i];
            int randomIndex = Random.Range(i, cards.Count);
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }
    }

    public Card DrawCard()
    {
        if (currentIndex >= cards.Count)
        {
            Shuffle(); // Reshuffle if deck is empty
        }

        return cards[currentIndex++];
    }
}