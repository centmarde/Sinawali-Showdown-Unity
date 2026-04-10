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
    
    [Header("Character Class Filter")]
    [SerializeField] private bool useCharacterClassFilter = true;
    [SerializeField] private CharacterClass characterClassFilter = CharacterClass.Any;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
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
        if (autoSetupOnStart)
        {
            // Get character class from selected character if available
            SetCharacterClassFromGameManager();
            SetupHand();
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
            string filterInfo = useCharacterClassFilter ? $" with {characterClassFilter} filter" : " with no filter";
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
    /// Fetches cards for all active CardFetcher components with character class filtering
    /// </summary>
    public void FetchCardsForActiveHand()
    {
        foreach (CardFetcher fetcher in activeCardFetchers)
        {
            if (fetcher.enabled)
            {
                if (useCharacterClassFilter)
                {
                    fetcher.SetCharacterClassFilter(characterClassFilter);
                }
                fetcher.FetchAndDisplayCard();
            }
        }
        
        if (showDebugInfo)
        {
            string filterText = useCharacterClassFilter ? $" (filtered by {characterClassFilter})" : "";
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
                    if (useCharacterClassFilter)
                    {
                        fetcher.SetCharacterClassFilter(characterClassFilter);
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
    /// Sets the character class filter for card fetching
    /// </summary>
    public void SetCharacterClassFilter(CharacterClass characterClass)
    {
        characterClassFilter = characterClass;
        useCharacterClassFilter = true;
        
        if (showDebugInfo)
        {
            Debug.Log($"HandManager: Set character class filter to {characterClass}");
        }
    }
    
    /// <summary>
    /// Gets character class from GameManager's selected character
    /// </summary>
    void SetCharacterClassFromGameManager()
    {
        // Check if GameManager exists and has a selected character
        if (GameManager.Instance != null && GameManager.Instance.GetSelectedCharacter() != null)
        {
            CharacterData selectedCharacter = GameManager.Instance.GetSelectedCharacter();
            
            // Map character names to character classes (you can extend this logic)
            CharacterClass detectedClass = GetCharacterClass(selectedCharacter);
            
            if (detectedClass != CharacterClass.Any)
            {
                characterClassFilter = detectedClass;
                useCharacterClassFilter = true;
                
                if (showDebugInfo)
                {
                    Debug.Log($"HandManager: Auto-set character class filter to {characterClassFilter} from selected character '{selectedCharacter.characterName}'");
                }
            }
            else
            {
                if (showDebugInfo)
                {
                    Debug.Log($"HandManager: Could not determine character class for '{selectedCharacter.characterName}', using inspector settings");
                }
            }
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.Log("HandManager: No GameManager or selected character found, using inspector character class settings");
            }
        }
    }
    
    /// <summary>
    /// Determines character class based on character data
    /// You can extend this logic based on your character naming or add a CharacterClass field to CharacterData
    /// </summary>
    CharacterClass GetCharacterClass(CharacterData character)
    {
        if (character == null) return CharacterClass.Any;
        
        string charName = character.characterName.ToLower();
        
        // Check for warrior-related names
        if (charName.Contains("warrior") || charName.Contains("knight") || charName.Contains("fighter") || 
            charName.Contains("guard") || charName.Contains("soldier") || charName.Contains("paladin"))
        {
            return CharacterClass.Warrior;
        }
        
        // Check for mage-related names
        if (charName.Contains("mage") || charName.Contains("wizard") || charName.Contains("sorcerer") || 
            charName.Contains("witch") || charName.Contains("enchanter") || charName.Contains("elementalist"))
        {
            return CharacterClass.Mage;
        }
        
        // Add more character class detection logic as needed
        // For example: Rogue, Cleric, Archer, etc.
        
        return CharacterClass.Any; // Default to Any if no specific class detected
    }
    
    /// <summary>
    /// Disables character class filtering
    /// </summary>
    public void DisableCharacterClassFilter()
    {
        useCharacterClassFilter = false;
        
        if (showDebugInfo)
        {
            Debug.Log($"HandManager: Disabled character class filter");
        }
    }
    
    /// <summary>
    /// Fetches new cards for active hand with current character class filter
    /// </summary>
    public void RefreshHandWithFilter(CharacterClass characterClass)
    {
        SetCharacterClassFilter(characterClass);
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
        RefreshHandWithFilter(CharacterClass.Warrior);
    }
    
    [ContextMenu("Debug: Set Mage Filter")]
    public void DebugSetMageFilter()
    {
        RefreshHandWithFilter(CharacterClass.Mage);
    }
    
    [ContextMenu("Debug: Remove Filter")]
    public void DebugRemoveFilter()
    {
        DisableCharacterClassFilter();
        FetchCardsForActiveHand();
    }
    
    [ContextMenu("Debug: Refresh From GameManager")]
    public void DebugRefreshFromGameManager()
    {
        SetCharacterClassFromGameManager();
        FetchCardsForActiveHand();
    }
    
    [ContextMenu("Debug: Check Selected Character")]
    public void DebugCheckSelectedCharacter()
    {
        if (GameManager.Instance != null && GameManager.Instance.GetSelectedCharacter() != null)
        {
            CharacterData selected = GameManager.Instance.GetSelectedCharacter();
            CharacterClass detectedClass = GetCharacterClass(selected);
            Debug.Log($"Selected Character: {selected.characterName}, Detected Class: {detectedClass}, Current Filter: {characterClassFilter}");
        }
        else
        {
            Debug.Log("No selected character found in GameManager");
        }
    }
    #endif
}