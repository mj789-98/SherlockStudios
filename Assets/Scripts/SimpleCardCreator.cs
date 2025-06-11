using UnityEngine;
using UnityEngine.UI;

public class SimpleCardCreator : MonoBehaviour
{
    [Header("Card Creation")]
    public int cardWidth = 200;
    public int cardHeight = 300;
    
    [Header("Colors")]
    public Color redSuitColor = Color.red;
    public Color blackSuitColor = Color.black;
    public Color cardBackgroundColor = Color.white;
    public Color cardBackColor = new Color(0.2f, 0.4f, 0.8f);

    // This method creates simple colored sprites for cards if you don't have card assets
    public static Sprite CreateSimpleCardSprite(Card.Suit suit, Card.Rank rank, int width = 200, int height = 300)
    {
        // Create a texture
        Texture2D cardTexture = new Texture2D(width, height);
        
        // Fill background
        Color backgroundColor = Color.white;
        Color suitColor = (suit == Card.Suit.Hearts || suit == Card.Suit.Diamonds) ? Color.red : Color.black;
        
        // Fill the texture with background color
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Create border
                if (x < 5 || x >= width - 5 || y < 5 || y >= height - 5)
                {
                    cardTexture.SetPixel(x, y, Color.black);
                }
                else
                {
                    cardTexture.SetPixel(x, y, backgroundColor);
                }
            }
        }
        
        cardTexture.Apply();
        
        // Create sprite from texture
        Sprite cardSprite = Sprite.Create(cardTexture, new Rect(0, 0, width, height), Vector2.one * 0.5f);
        cardSprite.name = $"{rank}_of_{suit}";
        
        return cardSprite;
    }
    
    public static Sprite CreateCardBackSprite(int width = 200, int height = 300)
    {
        Texture2D cardTexture = new Texture2D(width, height);
        Color backColor = new Color(0.2f, 0.4f, 0.8f);
        
        // Fill the texture
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x < 5 || x >= width - 5 || y < 5 || y >= height - 5)
                {
                    cardTexture.SetPixel(x, y, Color.black);
                }
                else if ((x + y) % 20 < 10) // Simple pattern
                {
                    cardTexture.SetPixel(x, y, backColor);
                }
                else
                {
                    cardTexture.SetPixel(x, y, Color.Lerp(backColor, Color.white, 0.3f));
                }
            }
        }
        
        cardTexture.Apply();
        return Sprite.Create(cardTexture, new Rect(0, 0, width, height), Vector2.one * 0.5f);
    }
    
    // Call this method to auto-generate card sprites in the GameManager
    [ContextMenu("Generate Simple Card Sprites")]
    public void GenerateSimpleCardSprites()
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found in scene!");
            return;
        }
        
        // Generate card back
        gameManager.cardBackImage = CreateCardBackSprite();
        
        // Generate Hearts
        for (int i = 0; i < 13; i++)
        {
            Card.Rank rank = (Card.Rank)(i + 2);
            gameManager.heartSprites[i] = CreateSimpleCardSprite(Card.Suit.Hearts, rank);
        }
        
        // Generate Diamonds  
        for (int i = 0; i < 13; i++)
        {
            Card.Rank rank = (Card.Rank)(i + 2);
            gameManager.diamondSprites[i] = CreateSimpleCardSprite(Card.Suit.Diamonds, rank);
        }
        
        // Generate Clubs
        for (int i = 0; i < 13; i++)
        {
            Card.Rank rank = (Card.Rank)(i + 2);
            gameManager.clubSprites[i] = CreateSimpleCardSprite(Card.Suit.Clubs, rank);
        }
        
        // Generate Spades
        for (int i = 0; i < 13; i++)
        {
            Card.Rank rank = (Card.Rank)(i + 2);
            gameManager.spadeSprites[i] = CreateSimpleCardSprite(Card.Suit.Spades, rank);
        }
        
        Debug.Log("Simple card sprites generated successfully!");
    }
} 