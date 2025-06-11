using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    [Header("Game Settings")]
    public int winsToWin = 5;
    public float cardRevealDelay = 1f;
    
    [Header("Player Stats")]
    public int playerWins = 0;
    public int aiWins = 0;
    public int currentRound = 0;
    
    [Header("UI References")]
    public GameObject winScreen;
    public TMPro.TextMeshProUGUI playerScoreText;
    public TMPro.TextMeshProUGUI aiScoreText;
    public TMPro.TextMeshProUGUI roundResultText;
    public TMPro.TextMeshProUGUI roundCounterText;
    
    [Header("Card Display")]
    public Image playerCardDisplay;
    public Image aiCardDisplay;
    public TMPro.TextMeshProUGUI playerCardText;
    public TMPro.TextMeshProUGUI aiCardText;
    public Button drawCardButton;
    
    [Header("Card Sprites")]
    public Sprite[] heartSprites = new Sprite[13]; // 2-Ace
    public Sprite[] diamondSprites = new Sprite[13];
    public Sprite[] clubSprites = new Sprite[13];
    public Sprite[] spadeSprites = new Sprite[13];
    public Sprite cardBackImage;
    
    private CardDeck deck;
    private bool gameInProgress = false;
    private bool roundInProgress = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        deck = new CardDeck();
        drawCardButton.onClick.AddListener(PlayRound);
        UpdateScoreUI();
        ResetCardDisplay();
    }
    
    public void StartNewGame()
    {
        playerWins = 0;
        aiWins = 0;
        currentRound = 0;
        deck.Shuffle();
        gameInProgress = true;
        roundInProgress = false;
        UpdateScoreUI();
        ResetCardDisplay();
        winScreen.SetActive(false);
        drawCardButton.interactable = true;
        roundResultText.text = "Click 'Draw Cards' to start!";
    }
    
    public void PlayRound()
    {
        if (!gameInProgress || roundInProgress) return;
        
        roundInProgress = true;
        drawCardButton.interactable = false;
        currentRound++;
        
        Card playerCard = deck.DrawCard();
        Card aiCard = deck.DrawCard();
        
        StartCoroutine(RevealCards(playerCard, aiCard));
        
        // Show interstitial ad every 3 rounds
        if (currentRound % 3 == 0 && AdManager.Instance != null)
        {
            AdManager.Instance.ShowInterstitialAd();
        }
    }
    
    private IEnumerator RevealCards(Card playerCard, Card aiCard)
    {
        roundResultText.text = "Drawing cards...";
        
        // Show card backs first
        ShowCardBack(playerCardDisplay);
        ShowCardBack(aiCardDisplay);
        playerCardText.text = "???";
        aiCardText.text = "???";
        
        yield return new WaitForSeconds(cardRevealDelay);
        
        // Reveal player card
        ShowCard(playerCard, playerCardDisplay, playerCardText);
        yield return new WaitForSeconds(cardRevealDelay * 0.5f);
        
        // Reveal AI card
        ShowCard(aiCard, aiCardDisplay, aiCardText);
        yield return new WaitForSeconds(cardRevealDelay * 0.5f);
        
        // Determine winner
        int result = CompareCards(playerCard, aiCard);
        
        if (result > 0)
        {
            playerWins++;
            roundResultText.text = $"You Win! {playerCard} beats {aiCard}";
        }
        else if (result < 0)
        {
            aiWins++;
            roundResultText.text = $"AI Wins! {aiCard} beats {playerCard}";
        }
        else
        {
            roundResultText.text = $"Tie! Both played {playerCard}";
        }
        
        UpdateScoreUI();
        
        yield return new WaitForSeconds(2f);
        
        CheckForGameEnd();
        
        if (gameInProgress)
        {
            roundInProgress = false;
            drawCardButton.interactable = true;
            roundResultText.text = "Ready for next round!";
        }
    }
    
    private void ShowCard(Card card, Image cardDisplay, TMPro.TextMeshProUGUI cardText)
    {
        Sprite cardSprite = GetCardSprite(card);
        if (cardSprite != null)
        {
            cardDisplay.sprite = cardSprite;
        }
        else
        {
            cardDisplay.sprite = cardBackImage; // Fallback
        }
        
        cardText.text = card.ToString();
    }
    
    private void ShowCardBack(Image cardDisplay)
    {
        cardDisplay.sprite = cardBackImage;
    }
    
    private Sprite GetCardSprite(Card card)
    {
        Sprite[] suitSprites = null;
        
        switch (card.suit)
        {
            case Card.Suit.Hearts:
                suitSprites = heartSprites;
                break;
            case Card.Suit.Diamonds:
                suitSprites = diamondSprites;
                break;
            case Card.Suit.Clubs:
                suitSprites = clubSprites;
                break;
            case Card.Suit.Spades:
                suitSprites = spadeSprites;
                break;
        }
        
        if (suitSprites != null)
        {
            int index = (int)card.rank - 2; // Convert rank to array index (2=0, 3=1, etc.)
            if (index >= 0 && index < suitSprites.Length && suitSprites[index] != null)
            {
                return suitSprites[index];
            }
        }
        
        return null; // Return null if sprite not found
    }
    
    private void ResetCardDisplay()
    {
        if (playerCardDisplay) ShowCardBack(playerCardDisplay);
        if (aiCardDisplay) ShowCardBack(aiCardDisplay);
        if (playerCardText) playerCardText.text = "";
        if (aiCardText) aiCardText.text = "";
        if (roundCounterText) roundCounterText.text = "Round: 0";
    }
    
    private int CompareCards(Card card1, Card card2)
    {
        return card1.value.CompareTo(card2.value);
    }
    
    private void CheckForGameEnd()
    {
        if (playerWins >= winsToWin)
        {
            EndGame("🎉 Congratulations! You Won! 🎉");
        }
        else if (aiWins >= winsToWin)
        {
            EndGame("💀 Game Over! AI Wins! 💀");
        }
    }
    
    private void EndGame(string message)
    {
        gameInProgress = false;
        roundInProgress = false;
        winScreen.SetActive(true);
        winScreen.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = message;
        drawCardButton.interactable = false;
    }
    
    private void UpdateScoreUI()
    {
        if (playerScoreText) playerScoreText.text = $"Player: {playerWins}";
        if (aiScoreText) aiScoreText.text = $"AI: {aiWins}";
        if (roundCounterText) roundCounterText.text = $"Round: {currentRound}";
    }
    
    public void ReturnToMainMenu()
    {
        if (AdManager.Instance != null)
        {
            AdManager.Instance.HideBannerAd();
        }
        SceneManager.LoadScene("MainMenu");
    }
    
    public void PlayAgain()
    {
        StartNewGame();
    }
}