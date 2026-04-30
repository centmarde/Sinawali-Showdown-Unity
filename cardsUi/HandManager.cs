using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Script for managing cards in the HandContainer
/// Attach this to the HandContainer parent object to control CardFetcher components
/// Manages which cards are active and fetches initial hand of 6 cards
/// </summary>
public class HandManager : MonoBehaviour
{
    [Header("Hand Settings")]
    [SerializeField] private int initialHandSize = 6;
    [SerializeField] private int maxHandSize = 12;
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool fetchCardsOnSetup = true;
    
    [Header("Character Type Filter")]
    [SerializeField] private bool useCharacterTypeFilter = true;
    [SerializeField] private string characterTypeFilter = "Warrior";
    [SerializeField] private CharacterData specificCharacterFilter = null; // Optional: filter by specific character
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    [Header("Attack Resolution (QTE)")]
    [Tooltip("If enabled and an ArrowKeyAttackQTE is active in the scene, Attack card damage is deferred to that system (damage is not applied immediately on confirm).")]
    [SerializeField] private bool deferAttackDamageWhenQTEPresent = true;
    
    [Header("Card Management")]
    [SerializeField] private List<CardFetcher> allCardFetchers = new List<CardFetcher>();
    [SerializeField] private List<CardFetcher> activeCardFetchers = new List<CardFetcher>();
    
    // Events for UI updates
    public System.Action<int> OnHandSizeChanged;
    public System.Action<int> OnActiveCardsChanged;
    
    // Events for card selection and inspection
    public System.Action<CardInspector> OnCardSelected;
    public System.Action<CardInspector> OnCardDeselected;
    public System.Action<CardInspector, CardData> OnCardConfirmed;
    
    void Start()
    {
        // Listen to CardInspector confirmations (CardInspector invokes HandManager.OnCardConfirmed)
        // so we can apply gameplay effects when a card is played.
        OnCardConfirmed += HandleCardConfirmed;

        if (autoSetupOnStart)
        {
            // Get character type from selected character if available
            SetCharacterTypeFromGameManager();
            SetupHand();
        }
    }

    private void OnDestroy()
    {
        OnCardConfirmed -= HandleCardConfirmed;
    }

    private void HandleCardConfirmed(CardInspector inspector, CardData card)
    {
        if (card == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("HandManager: Confirmed card was null.");
            }
            return;
        }

        // Resolve player characters via GameManager (preferred) so HP/Mana UI updates fire.
        Character player1 = null;
        Character player2 = null;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TryGetPlayerCharacters(out player1, out player2);
        }
        else
        {
            // Fallback if GameManager isn't present.
            HPTrackerBinder binder = FindObjectOfType<HPTrackerBinder>();
            if (binder != null)
            {
                if (binder.player1Object != null) player1 = binder.player1Object.GetComponent<Character>();
                if (binder.player2Object != null) player2 = binder.player2Object.GetComponent<Character>();
            }
        }

        // Spend mana on Player1
        if (player1 != null)
        {
            if (card.ManaDeduction > 0)
            {
                player1.SpendManaClamped(card.ManaDeduction);
            }
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning("HandManager: Could not resolve Player1 Character (mana not applied).");
        }

        // Deal damage to Player2
        if (player2 != null)
        {
            bool isAttackCard = card.Type == CardType.Attack;
            bool shouldDeferAttackDamage = deferAttackDamageWhenQTEPresent && isAttackCard && FindObjectOfType<ArrowKeyAttackQTE>() != null;

            if (!shouldDeferAttackDamage)
            {
                if (card.Damage > 0)
                {
                    player2.TakeDamage(card.Damage);
                }
            }
            else if (showDebugInfo)
            {
                Debug.Log("HandManager: Attack card damage deferred to ArrowKeyAttackQTE.");
            }
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning("HandManager: Could not resolve Player2 Character (damage not applied).");
        }

        // Store played card in graveyard
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCardToGraveyard(card);
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning("HandManager: No GameManager.Instance found (graveyard not updated).");
        }

        // Discard/replace the played card in-hand by drawing a new one into the same UI slot.
        RefillPlayedCardSlot(inspector);

        if (showDebugInfo)
        {
            string p1Name = player1 != null ? player1.GetCharacterName() : "<none>";
            string p2Name = player2 != null ? player2.GetCharacterName() : "<none>";
            Debug.Log($"HandManager: Played '{card.Title}' | Mana -{card.ManaDeduction} on {p1Name} | Damage {card.Damage} to {p2Name}");
        }
    }

    private void RefillPlayedCardSlot(CardInspector inspector)
    {
        if (inspector == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("HandManager: CardInspector was null (cannot refill card slot).");
            }
            return;
        }

        CardFetcher fetcher = inspector.GetComponent<CardFetcher>();
        if (fetcher == null)
        {
            fetcher = inspector.GetComponentInParent<CardFetcher>();
        }

        if (fetcher == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("HandManager: Could not find CardFetcher on confirmed card (cannot refill card slot).");
            }
            return;
        }

        // Keep the same filtering rules the hand currently uses.
        // CardFetcher filter setters may auto-fetch, so we only fetch once.
        string currentFilterStatus = fetcher.GetCurrentFilterStatus();

        if (specificCharacterFilter != null)
        {
            string expectedPrefix = $"Character: {specificCharacterFilter.characterName}";
            if (currentFilterStatus != null && currentFilterStatus.StartsWith(expectedPrefix))
            {
                fetcher.DiscardCurrentCardAndFetchReplacement();
            }
            else
            {
                fetcher.SetCharacterFilter(specificCharacterFilter); // auto-fetches when changed
            }
        }
        else if (useCharacterTypeFilter)
        {
            string expected = $"Type: {characterTypeFilter}";
            if (string.Equals(currentFilterStatus, expected, System.StringComparison.OrdinalIgnoreCase))
            {
                fetcher.DiscardCurrentCardAndFetchReplacement();
            }
            else
            {
                fetcher.SetCharacterTypeFilter(characterTypeFilter); // auto-fetches when changed
            }
        }
        else
        {
            if (string.Equals(currentFilterStatus, "No Filter", System.StringComparison.OrdinalIgnoreCase))
            {
                fetcher.DiscardCurrentCardAndFetchReplacement();
            }
            else
            {
                fetcher.ClearCharacterFilters(); // auto-fetches when changed
            }
        }
    }
    
    /// <summary>
    /// Sets up the hand by finding all CardFetcher components and enabling only the initial hand size
    /// </summary>
    [ContextMenu("Setup Hand")]
    public void SetupHand()
    {
        FindAllCardFetchers();
        SetInitialHandSize();
        
        if (fetchCardsOnSetup)
        {
            FetchCardsForActiveHand();
        }
        
        if (showDebugInfo)
        {
            string filterInfo = "";
            if (specificCharacterFilter != null)
            {
                filterInfo = $" with {specificCharacterFilter.characterName} character filter";
            }
            else if (useCharacterTypeFilter)
            {
                filterInfo = $" with {characterTypeFilter} type filter";
            }
            else
            {
                filterInfo = " with no filter";
            }
            Debug.Log($"HandManager: Hand setup complete. Active cards: {GetActiveCardCount()}/{GetTotalCardCount()}{filterInfo}");
        }
    }
    
    /// <summary>
    /// Finds all CardFetcher components in child objects (including inactive ones)
    /// Prioritizes card1-card6 for initial hand setup
    /// </summary>
    void FindAllCardFetchers()
    {
        allCardFetchers.Clear();
        
        // Get all CardFetcher components in children (includeInactive = true)
        CardFetcher[] fetchers = GetComponentsInChildren<CardFetcher>(true);
        
        // Sort them with card1-card6 first, then the rest
        var priorityCards = fetchers.Where(f => IsPriorityCard(f.name)).OrderBy(f => f.name).ToList();
        var otherCards = fetchers.Where(f => !IsPriorityCard(f.name)).OrderBy(f => f.name).ToList();
        
        allCardFetchers.AddRange(priorityCards);
        allCardFetchers.AddRange(otherCards);
        
        if (showDebugInfo)
        {
            Debug.Log($"HandManager: Found {allCardFetchers.Count} CardFetcher components");
            foreach (var fetcher in allCardFetchers)
            {
                Debug.Log($"- {fetcher.name}");
            }
        }
    }
    
    /// <summary>
    /// Sets the initial hand size by enabling/disabling CardFetcher components
    /// </summary>
    void SetInitialHandSize()
    {
        activeCardFetchers.Clear();
        
        for (int i = 0; i < allCardFetchers.Count; i++)
        {
            CardFetcher fetcher = allCardFetchers[i];
            
            if (i < initialHandSize)
            {
                // Enable the card and CardFetcher component
                fetcher.gameObject.SetActive(true);
                fetcher.enabled = true;
                activeCardFetchers.Add(fetcher);
                
                if (showDebugInfo)
                {
                    Debug.Log($"HandManager: Enabled {fetcher.name} (index {i})");
                }
            }
            else
            {
                // Disable the CardFetcher component and hide the entire card object
                fetcher.enabled = false;
                fetcher.gameObject.SetActive(false);
                
                if (showDebugInfo)
                {
                    Debug.Log($"HandManager: Disabled and hid {fetcher.name} (index {i})");
                }
            }
        }
        
        OnActiveCardsChanged?.Invoke(activeCardFetchers.Count);
    }
    
    /// <summary>
    /// Fetches cards for all active CardFetcher components with character type filtering
    /// </summary>
    public void FetchCardsForActiveHand()
    {
        foreach (CardFetcher fetcher in activeCardFetchers)
        {
            if (fetcher.enabled)
            {
                if (specificCharacterFilter != null)
                {
                    fetcher.SetCharacterFilter(specificCharacterFilter);
                }
                else if (useCharacterTypeFilter)
                {
                    fetcher.SetCharacterTypeFilter(characterTypeFilter);
                }
                else
                {
                    fetcher.ClearCharacterFilters();
                }
                fetcher.FetchAndDisplayCard();
            }
        }
        
        if (showDebugInfo)
        {
            string filterText = "";
            if (specificCharacterFilter != null)
            {
                filterText = $" (filtered by character {specificCharacterFilter.characterName})";
            }
            else if (useCharacterTypeFilter)
            {
                filterText = $" (filtered by type {characterTypeFilter})";
            }
            Debug.Log($"HandManager: Fetched cards for {activeCardFetchers.Count} active cards{filterText}");
        }
    }
    
    /// <summary>
    /// Adds a card to the hand (enables next disabled CardFetcher)
    /// </summary>
    public bool AddCardToHand()
    {
        if (activeCardFetchers.Count >= maxHandSize)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("HandManager: Cannot add card - hand is at maximum size");
            }
            return false;
        }
        
        // Find the next disabled CardFetcher
        for (int i = activeCardFetchers.Count; i < allCardFetchers.Count; i++)
        {
            CardFetcher fetcher = allCardFetchers[i];
            
            if (!fetcher.enabled)
            {
                fetcher.enabled = true;
                fetcher.gameObject.SetActive(true);
                activeCardFetchers.Add(fetcher);
                
                if (fetchCardsOnSetup)
                {
                    if (specificCharacterFilter != null)
                    {
                        fetcher.SetCharacterFilter(specificCharacterFilter);
                    }
                    else if (useCharacterTypeFilter)
                    {
                        fetcher.SetCharacterTypeFilter(characterTypeFilter);
                    }
                    else
                    {
                        fetcher.ClearCharacterFilters();
                    }
                    fetcher.FetchAndDisplayCard();
                }
                
                OnActiveCardsChanged?.Invoke(activeCardFetchers.Count);
                
                if (showDebugInfo)
                {
                    Debug.Log($"HandManager: Added card {fetcher.name} to hand. Active: {activeCardFetchers.Count}");
                }
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Removes a card from the hand (disables last active CardFetcher)
    /// </summary>
    public bool RemoveCardFromHand()
    {
        if (activeCardFetchers.Count <= 0)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("HandManager: Cannot remove card - hand is empty");
            }
            return false;
        }
        
        // Disable the last active card and hide it
        CardFetcher lastCard = activeCardFetchers[activeCardFetchers.Count - 1];
        lastCard.enabled = false;
        lastCard.gameObject.SetActive(false);
        
        activeCardFetchers.RemoveAt(activeCardFetchers.Count - 1);
        
        OnActiveCardsChanged?.Invoke(activeCardFetchers.Count);
        
        if (showDebugInfo)
        {
            Debug.Log($"HandManager: Removed card {lastCard.name} from hand. Active: {activeCardFetchers.Count}");
        }
        return true;
    }
    
    /// <summary>
    /// Sets the hand size to a specific number
    /// </summary>
    public void SetHandSize(int newSize)
    {
        newSize = Mathf.Clamp(newSize, 0, maxHandSize);
        
        while (activeCardFetchers.Count < newSize)
        {
            if (!AddCardToHand()) break;
        }
        
        while (activeCardFetchers.Count > newSize)
        {
            if (!RemoveCardFromHand()) break;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"HandManager: Set hand size to {activeCardFetchers.Count}");
        }
    }
    
    /// <summary>
    /// Gets the current number of active cards in hand
    /// </summary>
    public int GetActiveCardCount()
    {
        return activeCardFetchers.Count;
    }
    
    /// <summary>
    /// Gets the total number of card slots available
    /// </summary>
    public int GetTotalCardCount()
    {
        return allCardFetchers.Count;
    }
    
    /// <summary>
    /// Refresh all active cards (fetch new random cards)
    /// </summary>
    [ContextMenu("Refresh All Cards")]
    public void RefreshAllCards()
    {
        FetchCardsForActiveHand();
    }
    
    /// <summary>
    /// Get all active CardFetcher components
    /// </summary>
    public List<CardFetcher> GetActiveCards()
    {
        return new List<CardFetcher>(activeCardFetchers);
    }
    
    /// <summary>
    /// Get all CardFetcher components (active and inactive)
    /// </summary>
    public List<CardFetcher> GetAllCards()
    {
        return new List<CardFetcher>(allCardFetchers);
    }
    
    /// <summary>
    /// Checks if a card name is a priority card (card1-card6)
    /// </summary>
    bool IsPriorityCard(string cardName)
    {
        string lowerName = cardName.ToLower();
        return lowerName == "card1" || lowerName == "card2" || lowerName == "card3" || 
               lowerName == "card4" || lowerName == "card5" || lowerName == "card6";
    }
    
    /// <summary>
    /// Sets the character type filter for card fetching
    /// </summary>
    public void SetCharacterTypeFilter(string characterType)
    {
        characterTypeFilter = characterType;
        useCharacterTypeFilter = true;
        specificCharacterFilter = null; // Clear specific character filter
        
        if (showDebugInfo)
        {
            Debug.Log($"HandManager: Set character type filter to {characterType}");
        }
    }
    
    /// <summary>
    /// Sets the specific character filter for card fetching
    /// </summary>
    public void SetCharacterFilter(CharacterData character)
    {
        specificCharacterFilter = character;
        useCharacterTypeFilter = false; // Clear type filter when using specific character
        
        if (showDebugInfo)
        {
            string charName = character != null ? character.characterName : "null";
            Debug.Log($"HandManager: Set specific character filter to {charName}");
        }
    }
    
    /// <summary>
    /// Gets character type from GameManager's selected character
    /// </summary>
    void SetCharacterTypeFromGameManager()
    {
        // Check if GameManager exists and has a selected character
        if (GameManager.Instance != null && GameManager.Instance.GetSelectedCharacter() != null)
        {
            CharacterData selectedCharacter = GameManager.Instance.GetSelectedCharacter();
            
            // Use the character directly for filtering
            specificCharacterFilter = selectedCharacter;
            useCharacterTypeFilter = false; // Use specific character instead of type
            
            if (showDebugInfo)
            {
                Debug.Log($"HandManager: Auto-set character filter to '{selectedCharacter.characterName}' (type: {selectedCharacter.characterType})");
            }
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.Log("HandManager: No GameManager or selected character found, using inspector character type settings");
            }
        }
    }
    
    /// <summary>
    /// Gets the character type from character data
    /// Now uses the dynamic characterType field from CharacterData
    /// </summary>
    string GetCharacterType(CharacterData character)
    {
        if (character == null) return null; // Return null for invalid characters in strict mode
        
        // Use the characterType field from CharacterData
        if (!string.IsNullOrEmpty(character.characterType))
        {
            return character.characterType;
        }
        
        // Fallback to name-based detection if characterType is not set
        string charName = character.characterName.ToLower();
        
        // Check for warrior-related names
        if (charName.Contains("warrior") || charName.Contains("knight") || charName.Contains("fighter") || 
            charName.Contains("guard") || charName.Contains("soldier") || charName.Contains("paladin"))
        {
            return "Warrior";
        }
        
        // Check for mage-related names
        if (charName.Contains("mage") || charName.Contains("wizard") || charName.Contains("sorcerer") || 
            charName.Contains("witch") || charName.Contains("enchanter") || charName.Contains("elementalist"))
        {
            return "Mage";
        }
        
        // Add more character type detection logic as needed
        // For example: Rogue, Cleric, Archer, etc.
        
        return "Warrior"; // Default to Warrior if no specific type detected in strict mode
    }
    
    /// <summary>
    /// Disables character type filtering
    /// </summary>
    public void DisableCharacterTypeFilter()
    {
        useCharacterTypeFilter = false;
        specificCharacterFilter = null;
        
        if (showDebugInfo)
        {
            Debug.Log($"HandManager: Disabled character type filter");
        }
    }
    
    /// <summary>
    /// Fetches new cards for active hand with character type filter
    /// </summary>
    public void RefreshHandWithTypeFilter(string characterType)
    {
        SetCharacterTypeFilter(characterType);
        FetchCardsForActiveHand();
    }
    
    /// <summary>
    /// Fetches new cards for active hand with specific character filter
    /// </summary>
    public void RefreshHandWithCharacterFilter(CharacterData character)
    {
        SetCharacterFilter(character);
        FetchCardsForActiveHand();
    }
    
    #if UNITY_EDITOR
    [ContextMenu("Debug: List All Cards")]
    public void DebugListAllCards()
    {
        FindAllCardFetchers();
        Debug.Log($"HandManager Debug - Total Cards: {allCardFetchers.Count}, Active: {activeCardFetchers.Count}");
        
        for (int i = 0; i < allCardFetchers.Count; i++)
        {
            CardFetcher fetcher = allCardFetchers[i];
            string status = fetcher.enabled ? "ENABLED" : "DISABLED";
            string active = fetcher.gameObject.activeInHierarchy ? "ACTIVE" : "INACTIVE";
            string priority = IsPriorityCard(fetcher.name) ? "PRIORITY" : "EXTRA";
            Debug.Log($"Card {i + 1}: {fetcher.name} - {status} - {active} - {priority}");
        }
    }
    
    [ContextMenu("Debug: Add Card")]
    public void DebugAddCard()
    {
        AddCardToHand();
    }
    
    [ContextMenu("Debug: Remove Card")]
    public void DebugRemoveCard()
    {
        RemoveCardFromHand();
    }
    
    [ContextMenu("Reset Hand")]
    public void ResetHand()
    {
        SetupHand();
    }
    
    [ContextMenu("Debug: Set Warrior Filter")]
    public void DebugSetWarriorFilter()
    {
        RefreshHandWithTypeFilter("Warrior");
    }
    
    [ContextMenu("Debug: Set Mage Filter")]
    public void DebugSetMageFilter()
    {
        RefreshHandWithTypeFilter("Mage");
    }
    
    [ContextMenu("Debug: Remove Filter")]
    public void DebugRemoveFilter()
    {
        DisableCharacterTypeFilter();
        FetchCardsForActiveHand();
    }
    
    [ContextMenu("Debug: Refresh From GameManager")]
    public void DebugRefreshFromGameManager()
    {
        SetCharacterTypeFromGameManager();
        FetchCardsForActiveHand();
    }
    
    [ContextMenu("Debug: Check Selected Character")]
    public void DebugCheckSelectedCharacter()
    {
        if (GameManager.Instance != null && GameManager.Instance.GetSelectedCharacter() != null)
        {
            CharacterData selected = GameManager.Instance.GetSelectedCharacter();
            string detectedType = GetCharacterType(selected);
            string currentFilter = specificCharacterFilter != null ? specificCharacterFilter.characterName : characterTypeFilter;
            Debug.Log($"Selected Character: {selected.characterName}, Character Type: {selected.characterType}, Detected Type: {detectedType}, Current Filter: {currentFilter}");
        }
        else
        {
            Debug.Log("No selected character found in GameManager");
        }
    }
    #endif
}