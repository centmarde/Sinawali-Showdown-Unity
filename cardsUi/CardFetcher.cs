using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Script for fetching and displaying a random card from Assets/CardData
/// Attach this to a card UI object to automatically load and display a random card
/// </summary>
public class CardFetcher : MonoBehaviour
{
    [Header("Random Card Settings")]
    [SerializeField] private bool autoFetchOnStart = true;
    [SerializeField] private bool fetchNewCardOnClick = false;
    
    [Header("UI Elements - Auto Find")]
    [SerializeField] private bool autoFindUIElements = true;
    
    [Header("UI Elements - Manual Assignment")]
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image cardBorder;
    [SerializeField] private Image innerBorder;
    [SerializeField] private Image cardArtwork;
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI cardTypeText;
    [SerializeField] private TextMeshProUGUI cardDescriptionText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI manaText;
    
    [Header("Display Settings")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private Color attackCardColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color buffCardColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color healCardColor = new Color(0.8f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color specialCardColor = new Color(0.6f, 0.2f, 0.8f, 1f);
    
    // Cached list of all available cards
    private static List<CardData> allCards = null;
    
    void Start()
    {
        if (autoFindUIElements)
        {
            AutoFindUIElements();
        }
        
        // Add click listener for new random card
        if (fetchNewCardOnClick)
        {
            var button = GetComponent<Button>();
            if (button == null)
            {
                button = gameObject.AddComponent<Button>();
            }
            button.onClick.AddListener(FetchNewRandomCard);
        }
        
        if (autoFetchOnStart)
        {
            FetchAndDisplayCard();
        }
    }
    
    /// <summary>
    /// Automatically finds UI elements in the card hierarchy based on common naming patterns
    /// </summary>
    void AutoFindUIElements()
    {
        // Find elements by name patterns (case-insensitive search)
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        
        foreach (Transform child in allChildren)
        {
            string childName = child.name.ToLower();
            
            // Card background/border elements
            if (childName.Contains("background") || childName.Contains("card") && !childName.Contains("artwork") && !childName.Contains("name"))
            {
                if (cardBackground == null) cardBackground = child.GetComponent<Image>();
            }
            
            if (childName.Contains("border") && !childName.Contains("inner"))
            {
                if (cardBorder == null) cardBorder = child.GetComponent<Image>();
            }
            
            if (childName.Contains("inner") && childName.Contains("border"))
            {
                if (innerBorder == null) innerBorder = child.GetComponent<Image>();
            }
            
            // Card artwork
            if (childName.Contains("artwork") || childName.Contains("art") || childName.Contains("image"))
            {
                if (cardArtwork == null) cardArtwork = child.GetComponent<Image>();
            }
            
            // Text elements
            if (childName.Contains("name") && childName.Contains("card"))
            {
                if (cardNameText == null) cardNameText = child.GetComponent<TextMeshProUGUI>();
            }
            
            if (childName.Contains("type") && childName.Contains("card"))
            {
                if (cardTypeText == null) cardTypeText = child.GetComponent<TextMeshProUGUI>();
            }
            
            if (childName.Contains("description") && childName.Contains("card"))
            {
                if (cardDescriptionText == null) cardDescriptionText = child.GetComponent<TextMeshProUGUI>();
            }
            
            if (childName.Contains("damage"))
            {
                if (damageText == null) damageText = child.GetComponent<TextMeshProUGUI>();
            }
            
            if (childName.Contains("mana") || childName.Contains("cost"))
            {
                if (manaText == null) manaText = child.GetComponent<TextMeshProUGUI>();
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"CardFetcher: Auto-found UI elements on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Fetches and displays a random card from Assets/CardData
    /// </summary>
    [ContextMenu("Fetch Random Card")]
    public void FetchAndDisplayCard()
    {
        CardData cardToDisplay = GetRandomCard();
        
        if (cardToDisplay != null)
        {
            DisplayCard(cardToDisplay);
        }
        else
        {
            Debug.LogWarning($"CardFetcher: No cards found in Assets/CardData on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Gets all available CardData assets from the CardData folder
    /// </summary>
    List<CardData> GetAllCards()
    {
        if (allCards == null)
        {
            allCards = new List<CardData>();
            
            #if UNITY_EDITOR
            // Editor-time loading
            string[] guids = AssetDatabase.FindAssets("t:CardData", new[] { "Assets/CardData" });
            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
                if (card != null)
                {
                    allCards.Add(card);
                }
            }
            #else
            // Runtime loading using Resources (fallback)
            CardData[] cards = Resources.LoadAll<CardData>("CardData");
            allCards.AddRange(cards);
            #endif
            
            if (showDebugInfo)
            {
                Debug.Log($"CardFetcher: Loaded {allCards.Count} cards from Assets/CardData");
            }
        }
        
        return allCards;
    }
    

    
    /// <summary>
    /// Gets a random card from the available cards
    /// </summary>
    CardData GetRandomCard()
    {
        var cards = GetAllCards();
        if (cards.Count > 0)
        {
            int randomIndex = Random.Range(0, cards.Count);
            return cards[randomIndex];
        }
        return null;
    }
    
    /// <summary>
    /// Displays the card data on the UI elements
    /// </summary>
    void DisplayCard(CardData card)
    {
        if (card == null) return;
        
        // Update text elements
        if (cardNameText != null)
            cardNameText.text = card.Title;
            
        if (cardTypeText != null)
            cardTypeText.text = card.Type.ToString();
            
        if (cardDescriptionText != null)
            cardDescriptionText.text = card.Description;
            
        if (damageText != null)
            damageText.text = card.Damage.ToString();
            
        if (manaText != null)
            manaText.text = card.ManaDeduction.ToString();
        
        // Update card artwork
        if (cardArtwork != null && card.CardSprite != null)
        {
            cardArtwork.sprite = card.CardSprite;
        }
        
        // Update card colors based on type
        Color cardColor = GetCardTypeColor(card.Type);
        
        if (cardBackground != null)
            cardBackground.color = cardColor;
            
        if (cardBorder != null)
            cardBorder.color = cardColor;
        
        if (showDebugInfo)
        {
            Debug.Log($"CardFetcher: Displayed random card '{card.Title}' on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Gets the appropriate color for a card type
    /// </summary>
    Color GetCardTypeColor(CardType cardType)
    {
        switch (cardType)
        {
            case CardType.Attack:
                return attackCardColor;
            case CardType.Buff:
                return buffCardColor;
            case CardType.Heal:
                return healCardColor;
            default:
                return specialCardColor;
        }
    }
    
    /// <summary>
    /// Public method to fetch a new random card
    /// </summary>
    public void FetchNewRandomCard()
    {
        FetchAndDisplayCard();
    }
    
    #if UNITY_EDITOR
    [ContextMenu("List Available Cards")]
    public void ListAvailableCards()
    {
        var cards = GetAllCards();
        Debug.Log($"Available cards in Assets/CardData ({cards.Count}):");
        foreach (var card in cards)
        {
            Debug.Log($"- {card.Title} ({card.Type})");
        }
    }
    
    [ContextMenu("Refresh Card List")]
    public void RefreshCardList()
    {
        allCards = null;
        GetAllCards();
    }
    
    [ContextMenu("Fetch New Random Card")]
    public void FetchNewRandomCardMenu()
    {
        FetchNewRandomCard();
    }
    #endif
}