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
/// Script for managing enemy/AI hand of cards.
/// Automatically fetches 6 cards and picks 1 random card for the AI to "play"
/// Attach this to the AIHandContainer for Player2
/// </summary>
public class CardFetcherEnemy : MonoBehaviour
{
    [Header("AI Hand Settings")]
    [SerializeField] private int handSize = 6;
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool autoSelectCardOnSetup = false; // Will be triggered by Turn Manager
    
    [Header("Character Type Filter")]
    [SerializeField] private bool useCharacterTypeFilter = true;
    [SerializeField] private string characterTypeFilter = "Warrior";
    [SerializeField] private CharacterData specificCharacterFilter = null;
    [SerializeField] private bool usePlayer2FromGameManagerForSpecificFilter = true;
    
    [Header("UI Elements - Manual Assignment")]
    [SerializeField] private List<Image> cardBackgrounds = new List<Image>();
    [SerializeField] private List<Image> cardBorders = new List<Image>();
    [SerializeField] private List<Image> cardArtworks = new List<Image>();
    [SerializeField] private List<TextMeshProUGUI> cardNameTexts = new List<TextMeshProUGUI>();
    [SerializeField] private List<TextMeshProUGUI> cardTypeTexts = new List<TextMeshProUGUI>();
    [SerializeField] private List<TextMeshProUGUI> damageTexts = new List<TextMeshProUGUI>();
    [SerializeField] private List<TextMeshProUGUI> manaTexts = new List<TextMeshProUGUI>();
    
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
    
    // AI hand of cards
    private List<CardData> aiHand = new List<CardData>();
    private CardData selectedCard = null;
    
    // Cached list of all available cards
    private static List<CardData> allCards = null;
    
    // Event when AI selects a card
    public System.Action<CardData> OnAICardSelected;
    
    void Start()
    {
        if (showDebugInfo)
        {
            Debug.Log($"[AIEnemy] Start on '{gameObject.name}' | handSize={handSize} | autoSetupOnStart={autoSetupOnStart} | autoSelectCardOnSetup={autoSelectCardOnSetup}", this);
        }

        if (autoSetupOnStart)
        {
            SetupAIHand();
            if (autoSelectCardOnSetup)
            {
                SelectRandomCard();
            }
        }
    }
    
    /// <summary>
    /// Sets up the AI hand by fetching initial cards
    /// </summary>
    [ContextMenu("Setup AI Hand")]
    public void SetupAIHand()
    {
        if (showDebugInfo)
        {
            Debug.Log("[AIEnemy] SetupAIHand() called", this);
        }

        SyncSpecificCharacterFilterFromGameManager();

        aiHand.Clear();
        List<CardData> availableCards = GetFilteredCards();

        if (availableCards.Count == 0)
        {
            Debug.LogWarning($"[AIEnemy] SetupAIHand found 0 available cards. useCharacterTypeFilter={useCharacterTypeFilter}, characterTypeFilter='{characterTypeFilter}', specificCharacterFilter={(specificCharacterFilter != null ? specificCharacterFilter.characterName : "<null>")}", this);
        }
        
        for (int i = 0; i < handSize && availableCards.Count > 0; i++)
        {
            CardData randomCard = availableCards[Random.Range(0, availableCards.Count)];
            aiHand.Add(randomCard);
            
            if (i < cardNameTexts.Count)
            {
                DisplayCardInSlot(randomCard, i);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[AIEnemy] AI hand setup with {aiHand.Count} cards");
            if (useCharacterTypeFilter)
            {
                Debug.Log($"[AIEnemy] Using character type filter: {characterTypeFilter}");
            }
            if (specificCharacterFilter != null)
            {
                Debug.Log($"[AIEnemy] Using specific character filter: {specificCharacterFilter.characterName}");
            }

            if (aiHand.Count > 0)
            {
                string handPreview = string.Join(", ", aiHand.Select(c => c != null ? c.Title : "<null>").ToArray());
                Debug.Log($"[AIEnemy] Hand cards: {handPreview}", this);
            }
        }
    }

    [ContextMenu("Sync Specific Character Filter From GameManager")]
    public void SyncSpecificCharacterFilterFromGameManager()
    {
        if (!usePlayer2FromGameManagerForSpecificFilter)
        {
            if (showDebugInfo)
            {
                Debug.Log("[AIEnemy] Skipping Player2 sync because usePlayer2FromGameManagerForSpecificFilter is disabled.", this);
            }
            return;
        }

        if (GameManager.Instance == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("[AIEnemy] GameManager.Instance is null; cannot sync Player2 filter yet.", this);
            }
            return;
        }

        if (!GameManager.Instance.TryGetPlayerCharacters(out Character _, out Character player2) || player2 == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("[AIEnemy] Could not resolve Player2 from GameManager for specific character filter.");
            }
            return;
        }

        CharacterData player2Data = player2.characterData;
        if (player2Data == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("[AIEnemy] Player2 exists but has no CharacterData assigned.");
            }
            return;
        }

        specificCharacterFilter = player2Data;

        if (showDebugInfo)
        {
            Debug.Log($"[AIEnemy] Specific character filter synced from GameManager Player2: {specificCharacterFilter.characterName}");
        }
    }
    
    /// <summary>
    /// AI automatically selects a random card from its hand
    /// </summary>
    [ContextMenu("Select Random Card")]
    public void SelectRandomCard()
    {
        if (showDebugInfo)
        {
            int subscriberCount = OnAICardSelected != null ? OnAICardSelected.GetInvocationList().Length : 0;
            Debug.Log($"[AIEnemy] SelectRandomCard() called | handCount={aiHand.Count} | eventSubscribers={subscriberCount}", this);
        }

        if (aiHand.Count == 0)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("[AIEnemy] No cards in AI hand to select!");
            }
            return;
        }
        
        // Pick random card from hand
        selectedCard = aiHand[Random.Range(0, aiHand.Count)];
        
        if (showDebugInfo)
        {
            Debug.Log($"[AIEnemy] AI selected card: {selectedCard.Title}");
        }
        
        // Invoke event
        OnAICardSelected?.Invoke(selectedCard);

        if (showDebugInfo)
        {
            Debug.Log("[AIEnemy] OnAICardSelected event invoked.", this);
        }
    }
    
    /// <summary>
    /// Gets the currently selected card
    /// </summary>
    public CardData GetSelectedCard()
    {
        return selectedCard;
    }
    
    /// <summary>
    /// Gets all cards in AI hand
    /// </summary>
    public List<CardData> GetAIHand()
    {
        return new List<CardData>(aiHand);
    }
    
    /// <summary>
    /// Gets cards filtered by character type and obtainment status
    /// </summary>
    List<CardData> GetFilteredCards(string characterType = null)
    {
        if (characterType == null)
        {
            characterType = characterTypeFilter;
        }
        
        List<CardData> allAvailableCards = GetAllCards();
        List<CardData> filtered = new List<CardData>();
        
        // Apply character filter
        if (specificCharacterFilter != null)
        {
            filtered = allAvailableCards.Where(card => card.CanBeUsedByCharacter(specificCharacterFilter) && card.IsObtained).ToList();

            if (showDebugInfo)
            {
                Debug.Log($"[AIEnemy] Filter mode: specific character '{specificCharacterFilter.characterName}'", this);
            }
        }
        else if (useCharacterTypeFilter && !string.IsNullOrEmpty(characterType))
        {
            filtered = allAvailableCards.Where(card => 
                (card.IsUniversalCard || card.CompatibleCharacterTypes.Contains(characterType)) && card.IsObtained).ToList();

            if (showDebugInfo)
            {
                Debug.Log($"[AIEnemy] Filter mode: character type '{characterType}'", this);
            }
        }
        else
        {
            filtered = allAvailableCards.Where(card => card.IsObtained).ToList();

            if (showDebugInfo)
            {
                Debug.Log("[AIEnemy] Filter mode: obtained-only (no character filter)", this);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[AIEnemy] Filtered cards available: {filtered.Count}");

            if (filtered.Count > 0)
            {
                string filteredPreview = string.Join(", ", filtered.Take(10).Select(c => c != null ? c.Title : "<null>").ToArray());
                Debug.Log($"[AIEnemy] Filtered preview (up to 10): {filteredPreview}", this);
            }
        }
        
        return filtered;
    }
    
    /// <summary>
    /// Gets all available CardData assets from the CardData folder
    /// </summary>
    List<CardData> GetAllCards()
    {
        if (allCards == null)
        {
            #if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CardData", new[] { "Assets/CardData" });
            allCards = new List<CardData>();
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                CardData card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card != null)
                {
                    allCards.Add(card);
                }
            }
            #else
            allCards = Resources.LoadAll<CardData>("CardData").ToList();
            #endif
            
            if (showDebugInfo)
            {
                Debug.Log($"[AIEnemy] Loaded {allCards.Count} total cards from database");
            }
        }
        
        return allCards;
    }
    
    /// <summary>
    /// Displays a card in a specific slot
    /// </summary>
    void DisplayCardInSlot(CardData card, int slotIndex)
    {
        if (card == null) return;
        
        if (slotIndex < cardNameTexts.Count && cardNameTexts[slotIndex] != null)
            cardNameTexts[slotIndex].text = card.Title;
        
        if (slotIndex < cardTypeTexts.Count && cardTypeTexts[slotIndex] != null)
            cardTypeTexts[slotIndex].text = card.Type.ToString();
        
        if (slotIndex < damageTexts.Count && damageTexts[slotIndex] != null)
            damageTexts[slotIndex].text = card.Damage.ToString();
        
        if (slotIndex < manaTexts.Count && manaTexts[slotIndex] != null)
            manaTexts[slotIndex].text = card.ManaDeduction.ToString();
        
        // Update background color based on card type
        if (slotIndex < cardBackgrounds.Count && cardBackgrounds[slotIndex] != null)
            cardBackgrounds[slotIndex].color = GetCardTypeColor(card.Type);
        
        // Update card artwork
        if (slotIndex < cardArtworks.Count && cardArtworks[slotIndex] != null)
        {
            UpdateCardArtwork(card, cardArtworks[slotIndex]);
        }
    }
    
    /// <summary>
    /// Updates card artwork with priority system: CardSprite -> ImageUrl -> Default
    /// </summary>
    void UpdateCardArtwork(CardData card, Image artworkImage)
    {
        if (artworkImage == null) return;
        
        if (!prioritizeCardData)
        {
            if (card.CardSprite != null)
            {
                artworkImage.sprite = card.CardSprite;
                return;
            }
        }
        
        // Priority 1: CardData sprite
        if (card.CardSprite != null)
        {
            artworkImage.sprite = card.CardSprite;
            if (showDebugInfo)
            {
                Debug.Log($"[AIEnemy] Loaded sprite from CardData for {card.Title}");
            }
            return;
        }
        
        // Priority 2: ImageUrl
        if (!string.IsNullOrEmpty(card.ImageUrl) && loadFromUrlIfSpriteNull)
        {
            StartCoroutine(LoadImageFromUrl(card.ImageUrl, card.Title, artworkImage));
            if (showDebugInfo)
            {
                Debug.Log($"[AIEnemy] Loading image from URL for {card.Title}");
            }
            return;
        }
        
        // Priority 3: Default sprite
        if (defaultCardSprite != null)
        {
            artworkImage.sprite = defaultCardSprite;
            if (showDebugInfo)
            {
                Debug.Log($"[AIEnemy] Using default sprite for {card.Title}");
            }
        }
    }
    
    /// <summary>
    /// Loads an image from URL and applies it to the card artwork
    /// </summary>
    IEnumerator LoadImageFromUrl(string url, string cardTitle, Image targetImage)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            request.timeout = (int)imageLoadTimeout;
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success && targetImage != null)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                if (texture != null)
                {
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                    targetImage.sprite = sprite;
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"[AIEnemy] Successfully loaded image from URL for {cardTitle}");
                    }
                }
            }
            else
            {
                HandleImageLoadFailure(cardTitle, request.error, targetImage);
            }
        }
    }
    
    /// <summary>
    /// Handles image loading failures with fallback behavior
    /// </summary>
    void HandleImageLoadFailure(string cardTitle, string error, Image targetImage)
    {
        if (showDebugInfo)
        {
            Debug.LogWarning($"[AIEnemy] Failed to load image for {cardTitle}: {error}");
        }
        
        if (targetImage != null && defaultCardSprite != null)
        {
            targetImage.sprite = defaultCardSprite;
            if (showDebugInfo)
            {
                Debug.Log($"[AIEnemy] Using default sprite fallback for {cardTitle}");
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
    /// Refreshes the entire AI hand with new random cards
    /// </summary>
    [ContextMenu("Refresh AI Hand")]
    public void RefreshAIHand()
    {
        if (showDebugInfo)
        {
            Debug.Log("[AIEnemy] RefreshAIHand() called", this);
        }

        SetupAIHand();
    }

    [ContextMenu("Debug: Dump AI State")]
    public void DebugDumpAIState()
    {
        int subscriberCount = OnAICardSelected != null ? OnAICardSelected.GetInvocationList().Length : 0;
        string selected = selectedCard != null ? selectedCard.Title : "<none>";
        string specific = specificCharacterFilter != null ? specificCharacterFilter.characterName : "<null>";
        string hand = aiHand.Count > 0 ? string.Join(", ", aiHand.Select(c => c != null ? c.Title : "<null>").ToArray()) : "<empty>";

        Debug.Log($"[AIEnemy] Dump | object='{gameObject.name}' | activeInHierarchy={gameObject.activeInHierarchy} | handSizeSetting={handSize} | handCount={aiHand.Count} | selected={selected} | useCharacterTypeFilter={useCharacterTypeFilter} | characterTypeFilter='{characterTypeFilter}' | specificCharacterFilter={specific} | usePlayer2FromGameManagerForSpecificFilter={usePlayer2FromGameManagerForSpecificFilter} | subscribers={subscriberCount} | hand=[{hand}]", this);
    }
    
    /// <summary>
    /// Sets the character type filter for card fetching
    /// </summary>
    public void SetCharacterTypeFilter(string characterType)
    {
        characterTypeFilter = characterType;
        
        if (showDebugInfo)
        {
            Debug.Log($"[AIEnemy] Character type filter set to: {characterType}");
        }
    }
    
    /// <summary>
    /// Sets the specific character filter for card fetching
    /// </summary>
    public void SetCharacterFilter(CharacterData character)
    {
        specificCharacterFilter = character;
        useCharacterTypeFilter = false;
        
        if (showDebugInfo)
        {
            Debug.Log($"[AIEnemy] Character filter set to: {character.characterName}");
        }
    }
    
    #if UNITY_EDITOR
    [ContextMenu("Auto Find Card UI Elements")]
    public void AutoFindCardUIElements()
    {
        cardBackgrounds.Clear();
        cardBorders.Clear();
        cardArtworks.Clear();
        cardNameTexts.Clear();
        cardTypeTexts.Clear();
        damageTexts.Clear();
        manaTexts.Clear();
        
        // Find all child objects with common naming patterns
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        
        foreach (Transform child in allChildren)
        {
            string childName = child.name.ToLower();
            
            // Pattern: CardX where X is a number
            if (childName.StartsWith("card") && char.IsDigit(childName[childName.Length - 1]))
            {
                // Find Background
                Transform bgTransform = child.Find("Background");
                if (bgTransform != null)
                {
                    Image bgImage = bgTransform.GetComponent<Image>();
                    if (bgImage != null) cardBackgrounds.Add(bgImage);
                }
                
                // Find Border
                Transform borderTransform = child.Find("Border");
                if (borderTransform != null)
                {
                    Image borderImage = borderTransform.GetComponent<Image>();
                    if (borderImage != null) cardBorders.Add(borderImage);
                }
                
                // Find Artwork
                Transform artworkTransform = child.Find("Artwork");
                if (artworkTransform != null)
                {
                    Image artworkImage = artworkTransform.GetComponent<Image>();
                    if (artworkImage != null) cardArtworks.Add(artworkImage);
                }
                
                // Find Text elements
                Transform nameTransform = child.Find("Name");
                if (nameTransform != null)
                {
                    TextMeshProUGUI nameText = nameTransform.GetComponent<TextMeshProUGUI>();
                    if (nameText != null) cardNameTexts.Add(nameText);
                }
                
                Transform typeTransform = child.Find("Type");
                if (typeTransform != null)
                {
                    TextMeshProUGUI typeText = typeTransform.GetComponent<TextMeshProUGUI>();
                    if (typeText != null) cardTypeTexts.Add(typeText);
                }
                
                Transform damageTransform = child.Find("Damage");
                if (damageTransform != null)
                {
                    TextMeshProUGUI damageText = damageTransform.GetComponent<TextMeshProUGUI>();
                    if (damageText != null) damageTexts.Add(damageText);
                }
                
                Transform manaTransform = child.Find("Mana");
                if (manaTransform != null)
                {
                    TextMeshProUGUI manaText = manaTransform.GetComponent<TextMeshProUGUI>();
                    if (manaText != null) manaTexts.Add(manaText);
                }
            }
        }
        
        Debug.Log($"[AIEnemy] Auto-found UI elements: {cardNameTexts.Count} cards");
    }
    #endif
}
