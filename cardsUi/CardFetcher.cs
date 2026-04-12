using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

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
    
    [Header("Character Type Filter")]
    [SerializeField] private bool useCharacterTypeFilter = false;
    [SerializeField] private string filterByCharacterType = "Warrior";
    [SerializeField] private CharacterData filterBySpecificCharacter = null; // Optional: filter by specific character
    
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
    
    [Header("Image Loading Settings")]
    [SerializeField] private bool prioritizeCardData = true;
    [SerializeField] private Sprite defaultCardSprite = null;
    [SerializeField] private bool loadFromUrlIfSpriteNull = true;
    [SerializeField] private float imageLoadTimeout = 5f;
    
    // Currently displayed card
    private CardData currentCard = null;
    
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
    /// Gets cards filtered by character type and obtainment status
    /// </summary>
    List<CardData> GetFilteredCards(string characterType = "Any")
    {
        var allCards = GetAllCards();
        
        // First filter by obtainment status (only include obtained cards)
        var obtainedCards = allCards.Where(card => card != null && card.IsObtained).ToList();
        
        // Validate character type input - no more "Any" allowed
        if (string.IsNullOrEmpty(characterType) || characterType.Equals("Any", System.StringComparison.OrdinalIgnoreCase))
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"CardFetcher: Invalid character type '{characterType}' - strict filtering requires specific character types. Returning no cards.");
            }
            return new List<CardData>(); // Return empty list for invalid types
        }
        
        // Filter by specific character type
        var filteredCards = obtainedCards.Where(card => 
        {
            try 
            {
                return card.CanBeUsedByCharacterType(characterType);
            }
            catch (System.Exception ex)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"CardFetcher: Error checking character type compatibility for card '{card.Title}': {ex.Message}");
                }
                return false;
            }
        }).ToList();
        
        if (showDebugInfo)
        {
            Debug.Log($"CardFetcher: Filtered by character type '{characterType}' - found {filteredCards.Count} compatible cards out of {obtainedCards.Count} obtained cards");
        }
        
        return filteredCards;
    }
    
    /// <summary>
    /// Gets cards filtered by specific character and obtainment status
    /// </summary>
    List<CardData> GetFilteredCardsForCharacter(CharacterData character)
    {
        var allCards = GetAllCards();
        
        // First filter by obtainment status (only include obtained cards)
        var obtainedCards = allCards.Where(card => card != null && card.IsObtained).ToList();
        
        if (character == null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"CardFetcher: No character specified - returning all {obtainedCards.Count} obtained cards");
            }
            return obtainedCards;
        }
        
        // Validate character data
        if (string.IsNullOrEmpty(character.characterType))
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"CardFetcher: Character '{character.characterName}' has no character type set - treating as universal");
            }
            return obtainedCards;
        }
        
        // Filter by specific character
        var filteredCards = obtainedCards.Where(card => 
        {
            try 
            {
                return card.CanBeUsedByCharacter(character);
            }
            catch (System.Exception ex)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"CardFetcher: Error checking character compatibility for card '{card.Title}' with character '{character.characterName}': {ex.Message}");
                }
                return false;
            }
        }).ToList();
        
        if (showDebugInfo)
        {
            Debug.Log($"CardFetcher: Filtered by character '{character.characterName}' (type: {character.characterType}) - found {filteredCards.Count} compatible cards out of {obtainedCards.Count} obtained cards");
        }
        
        return filteredCards;
    }
    
    /// <summary>
    /// Gets a random card from the available cards, optionally filtered by character type
    /// </summary>
    CardData GetRandomCard()
    {
        List<CardData> cards;
        string filterDescription = "";
        
        // Apply filters in order of priority: Specific Character > Character Type > Universal Cards Only
        if (filterBySpecificCharacter != null)
        {
            cards = GetFilteredCardsForCharacter(filterBySpecificCharacter);
            filterDescription = $"specific character '{filterBySpecificCharacter.characterName}' (type: {filterBySpecificCharacter.characterType})";
        }
        else if (useCharacterTypeFilter && !string.IsNullOrEmpty(filterByCharacterType))
        {
            cards = GetFilteredCards(filterByCharacterType);
            filterDescription = $"character type '{filterByCharacterType}'";
        }
        else
        {
            // Strict filtering: only return universal cards when no filter is set
            var allCards = GetAllCards();
            cards = allCards.Where(card => card != null && card.IsObtained && card.IsUniversalCard).ToList();
            filterDescription = "no character filter (universal cards only)";
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"CardFetcher: Getting random card with filter: {filterDescription} - {cards.Count} cards available");
        }
        
        if (cards.Count > 0)
        {
            int randomIndex = Random.Range(0, cards.Count);
            CardData selectedCard = cards[randomIndex];
            
            if (showDebugInfo)
            {
                string characterInfo = selectedCard.IsUniversalCard ? "Universal" : string.Join(", ", selectedCard.CompatibleCharacterTypes);
                Debug.Log($"CardFetcher: Selected random card '{selectedCard.Title}' (compatible with: {characterInfo})");
            }
            
            return selectedCard;
        }
        
        if (showDebugInfo)
        {
            Debug.LogWarning($"CardFetcher: No cards available with filter: {filterDescription}");
        }
        return null;
    }
    
    /// <summary>
    /// Displays the card data on the UI elements
    /// </summary>
    void DisplayCard(CardData card)
    {
        if (card == null) return;
        
        // Store the current card
        currentCard = card;
        
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
        
        // Update card artwork with priority system
        if (cardArtwork != null)
        {
            UpdateCardArtwork(card);
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
    /// Updates card artwork with priority system: CardSprite -> ImageUrl -> Default
    /// </summary>
    void UpdateCardArtwork(CardData card)
    {
        if (!prioritizeCardData)
        {
            // Use original behavior if prioritization is disabled
            if (card.CardSprite != null)
            {
                cardArtwork.sprite = card.CardSprite;
            }
            return;
        }
        
        // Priority 1: Use CardSprite if available
        if (card.CardSprite != null)
        {
            cardArtwork.sprite = card.CardSprite;
            if (showDebugInfo)
            {
                Debug.Log($"CardFetcher: Using CardSprite for '{card.Title}' on {gameObject.name}");
            }
            return;
        }
        
        // Priority 2: Load from ImageUrl if available
        if (!string.IsNullOrEmpty(card.ImageUrl) && loadFromUrlIfSpriteNull)
        {
            StartCoroutine(LoadImageFromUrl(card.ImageUrl, card.Title));
            if (showDebugInfo)
            {
                Debug.Log($"CardFetcher: Loading image from URL for '{card.Title}' on {gameObject.name}");
            }
            return;
        }
        
        // Priority 3: Use default sprite as fallback
        if (defaultCardSprite != null)
        {
            cardArtwork.sprite = defaultCardSprite;
            if (showDebugInfo)
            {
                Debug.Log($"CardFetcher: Using default sprite for '{card.Title}' on {gameObject.name}");
            }
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"CardFetcher: No image available for '{card.Title}' on {gameObject.name} - no sprite, URL, or default sprite provided");
            }
        }
    }
    
    /// <summary>
    /// Loads an image from URL and applies it to the card artwork
    /// </summary>
    IEnumerator LoadImageFromUrl(string url, string cardTitle)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        request.timeout = (int)imageLoadTimeout;
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            if (texture != null)
            {
                // Create sprite from downloaded texture
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                cardArtwork.sprite = sprite;
                
                if (showDebugInfo)
                {
                    Debug.Log($"CardFetcher: Successfully loaded image from URL for '{cardTitle}' on {gameObject.name}");
                }
            }
            else
            {
                HandleImageLoadFailure(cardTitle, "Downloaded texture is null");
            }
        }
        else
        {
            HandleImageLoadFailure(cardTitle, request.error);
        }
        
        request.Dispose();
    }
    
    /// <summary>
    /// Handles image loading failures with fallback behavior
    /// </summary>
    void HandleImageLoadFailure(string cardTitle, string error)
    {
        if (showDebugInfo)
        {
            Debug.LogWarning($"CardFetcher: Failed to load image from URL for '{cardTitle}' on {gameObject.name}: {error}");
        }
        
        // Fall back to default sprite if available
        if (defaultCardSprite != null)
        {
            cardArtwork.sprite = defaultCardSprite;
            if (showDebugInfo)
            {
                Debug.Log($"CardFetcher: Applied default sprite as fallback for '{cardTitle}' on {gameObject.name}");
            }
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
    /// Gets the currently displayed card data
    /// </summary>
    public CardData GetCurrentCard()
    {
        return currentCard;
    }
    
    /// <summary>
    /// Public method to fetch a new random card
    /// </summary>
    public void FetchNewRandomCard()
    {
        FetchAndDisplayCard();
    }
    
    /// <summary>
    /// Fetch a random card for a specific character type
    /// </summary>
    public void FetchCardForCharacterType(string characterType)
    {
        var cards = GetFilteredCards(characterType);
        
        if (cards.Count > 0)
        {
            int randomIndex = Random.Range(0, cards.Count);
            DisplayCard(cards[randomIndex]);
        }
        else
        {
            Debug.LogWarning($"CardFetcher: No cards found for character type '{characterType}'");
        }
    }
    
    /// <summary>
    /// Fetch a random card for a specific character
    /// </summary>
    public void FetchCardForCharacter(CharacterData character)
    {
        var cards = GetFilteredCardsForCharacter(character);
        
        if (cards.Count > 0)
        {
            int randomIndex = Random.Range(0, cards.Count);
            DisplayCard(cards[randomIndex]);
        }
        else
        {
            string characterName = character != null ? character.characterName : "null";
            Debug.LogWarning($"CardFetcher: No cards found for character '{characterName}'");
        }
    }
    
    /// <summary>
    /// Set character type filter and fetch a new card
    /// </summary>
    public void SetCharacterTypeFilter(string characterType)
    {
        // Normalize and validate input
        if (string.IsNullOrEmpty(characterType))
        {
            characterType = "Any";
        }
        
        // Only update and fetch if the filter actually changed
        if (!useCharacterTypeFilter || !filterByCharacterType.Equals(characterType, System.StringComparison.OrdinalIgnoreCase))
        {
            useCharacterTypeFilter = true;
            filterByCharacterType = characterType;
            filterBySpecificCharacter = null; // Clear specific character filter
            
            if (showDebugInfo)
            {
                Debug.Log($"CardFetcher: Set character type filter to '{characterType}' on {gameObject.name}");
            }
            
            FetchAndDisplayCard();
        }
        else if (showDebugInfo)
        {
            Debug.Log($"CardFetcher: Character type filter '{characterType}' already active on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Set specific character filter and fetch a new card
    /// </summary>
    public void SetCharacterFilter(CharacterData character)
    {
        // Validate input
        if (character != null && string.IsNullOrEmpty(character.characterType))
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"CardFetcher: Character '{character.characterName}' has no character type set - this may affect filtering");
            }
        }
        
        // Only update and fetch if the filter actually changed
        if (filterBySpecificCharacter != character)
        {
            filterBySpecificCharacter = character;
            useCharacterTypeFilter = false; // Clear type filter when using specific character
            
            if (showDebugInfo)
            {
                string characterInfo = character != null ? $"'{character.characterName}' (type: {character.characterType})" : "null";
                Debug.Log($"CardFetcher: Set specific character filter to {characterInfo} on {gameObject.name}");
            }
            
            FetchAndDisplayCard();
        }
        else if (showDebugInfo)
        {
            string characterInfo = character != null ? $"'{character.characterName}'" : "null";
            Debug.Log($"CardFetcher: Character filter {characterInfo} already active on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Clear all character filters and fetch a new card
    /// </summary>
    public void ClearCharacterFilters()
    {
        // Only update and fetch if filters were actually active
        if (useCharacterTypeFilter || filterBySpecificCharacter != null)
        {
            useCharacterTypeFilter = false;
            filterBySpecificCharacter = null;
            
            if (showDebugInfo)
            {
                Debug.Log($"CardFetcher: Cleared all character filters on {gameObject.name}");
            }
            
            FetchAndDisplayCard();
        }
        else if (showDebugInfo)
        {
            Debug.Log($"CardFetcher: No character filters to clear on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Get current filter status for debugging
    /// </summary>
    public string GetCurrentFilterStatus()
    {
        if (filterBySpecificCharacter != null)
        {
            return $"Character: {filterBySpecificCharacter.characterName} ({filterBySpecificCharacter.characterType})";
        }
        else if (useCharacterTypeFilter)
        {
            return $"Type: {filterByCharacterType}";
        }
        else
        {
            return "No Filter";
        }
    }
    
    /// <summary>
    /// Sets the default sprite to use as fallback when no card image is available
    /// </summary>
    public void SetDefaultSprite(Sprite sprite)
    {
        defaultCardSprite = sprite;
    }
    
    /// <summary>
    /// Enables or disables the card data prioritization system
    /// </summary>
    public void SetPrioritizeCardData(bool prioritize)
    {
        prioritizeCardData = prioritize;
    }
    
    /// <summary>
    /// Enables or disables URL loading when sprite is null
    /// </summary>
    public void SetLoadFromUrl(bool loadFromUrl)
    {
        loadFromUrlIfSpriteNull = loadFromUrl;
    }
    
    #if UNITY_EDITOR
    [ContextMenu("List Available Cards")]
    public void ListAvailableCards()
    {
        var allCards = GetAllCards();
        var obtainedCards = allCards.Where(card => card.IsObtained).ToList();
        var unobtainedCards = allCards.Where(card => !card.IsObtained).ToList();
        
        Debug.Log($"Total cards in Assets/CardData: {allCards.Count}");
        Debug.Log($"Obtained cards ({obtainedCards.Count}):");
        foreach (var card in obtainedCards)
        {
            string imageInfo = "";
            if (card.CardSprite != null) imageInfo += "[Sprite]";
            if (!string.IsNullOrEmpty(card.ImageUrl)) imageInfo += "[URL]";
            if (string.IsNullOrEmpty(imageInfo)) imageInfo = "[No Image]";
            
            string characterInfo = card.IsUniversalCard ? "Universal" : string.Join(", ", card.CompatibleCharacterTypes);
            Debug.Log($"- {card.Title} ({card.Type}, {characterInfo}) {imageInfo}");
        }
        
        if (unobtainedCards.Count > 0)
        {
            Debug.Log($"\nUnobtained cards ({unobtainedCards.Count}):");
            foreach (var card in unobtainedCards)
            {
                string characterInfo = card.IsUniversalCard ? "Universal" : string.Join(", ", card.CompatibleCharacterTypes);
                Debug.Log($"- {card.Title} ({card.Type}, {characterInfo}) [NOT OBTAINED]");
            }
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
    
    [ContextMenu("Debug: Set Warrior Filter")]
    public void DebugSetWarriorFilter()
    {
        SetCharacterTypeFilter("Warrior");
    }
    
    [ContextMenu("Debug: Set Mage Filter")]
    public void DebugSetMageFilter()
    {
        SetCharacterTypeFilter("Mage");
    }
    
    [ContextMenu("Debug: Show Universal Cards Only")]
    public void DebugShowUniversalCardsOnly()
    {
        ClearCharacterFilters(); // This will now show only universal cards
    }
    
    [ContextMenu("Debug: Set Rogue Filter")]
    public void DebugSetRogueFilter()
    {
        SetCharacterTypeFilter("Rogue");
    }
    
    [ContextMenu("Debug: Clear All Filters")]
    public void DebugClearAllFilters()
    {
        ClearCharacterFilters();
    }
    
    [ContextMenu("Test Image Priority System")]
    public void TestImagePrioritySystem()
    {
        if (currentCard != null)
        {
            Debug.Log($"Testing image priority for: {currentCard.Title}");
            Debug.Log($"- Has CardSprite: {currentCard.CardSprite != null}");
            Debug.Log($"- Has ImageUrl: {!string.IsNullOrEmpty(currentCard.ImageUrl)}");
            Debug.Log($"- Has Default Sprite: {defaultCardSprite != null}");
            Debug.Log($"- Priority Mode: {prioritizeCardData}");
            Debug.Log($"- Load from URL: {loadFromUrlIfSpriteNull}");
            Debug.Log($"- Character Types: {(currentCard.IsUniversalCard ? "Universal" : string.Join(", ", currentCard.CompatibleCharacterTypes))}");
            Debug.Log($"- Current Filter: {GetCurrentFilterStatus()}");
            
            // Re-apply the image to test the priority system
            if (cardArtwork != null)
            {
                UpdateCardArtwork(currentCard);
            }
        }
        else
        {
            Debug.LogWarning("No current card to test. Fetch a card first.");
        }
    }
    
    [ContextMenu("Test Character Filtering")]
    public void TestCharacterFiltering()
    {
        Debug.Log($"CardFetcher: Testing character filtering on {gameObject.name}");
        Debug.Log($"- Current Filter: {GetCurrentFilterStatus()}");
        
        var allCards = GetAllCards();
        var obtainedCards = allCards.Where(card => card != null && card.IsObtained).ToList();
        
        Debug.Log($"- Total Cards: {allCards.Count}");
        Debug.Log($"- Obtained Cards: {obtainedCards.Count}");
        
        if (filterBySpecificCharacter != null)
        {
            var filteredCards = GetFilteredCardsForCharacter(filterBySpecificCharacter);
            Debug.Log($"- Cards for Character '{filterBySpecificCharacter.characterName}': {filteredCards.Count}");
        }
        else if (useCharacterTypeFilter)
        {
            var filteredCards = GetFilteredCards(filterByCharacterType);
            Debug.Log($"- Cards for Type '{filterByCharacterType}': {filteredCards.Count}");
        }
        
        // Test a random fetch
        CardData randomCard = GetRandomCard();
        if (randomCard != null)
        {
            string compatibility = randomCard.IsUniversalCard ? "Universal" : string.Join(", ", randomCard.CompatibleCharacterTypes);
            Debug.Log($"- Random Card Result: '{randomCard.Title}' (Compatible with: {compatibility})");
        }
        else
        {
            Debug.Log("- Random Card Result: No cards available with current filter");
        }
    }
    #endif
}