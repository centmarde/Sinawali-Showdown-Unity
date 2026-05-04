using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Manages the player's selected deck across scenes.
/// Loads available CardData assets and stores a selected deck list.
/// </summary>
public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    [Header("Deck Settings")]
    [SerializeField] private int deckSizeLimit = 6;
    [SerializeField] private bool onlyUseObtainedCards = true;
    [SerializeField] private bool persistAcrossScenes = true;
    [SerializeField] private bool showDebugInfo = true;

    [Header("Deck Builder Filters")]
    [Tooltip("If enabled, only show cards compatible with the currently selected character in GameManager.")]
    [SerializeField] private bool filterBySelectedCharacter = true;

    [Header("Selected Deck")]
    [SerializeField] private List<CardData> selectedCards = new List<CardData>();

    private static List<CardData> cachedAllCards;

    public event Action<IReadOnlyList<CardData>> OnDeckChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public void InitializeFrom(DeckManager source)
    {
        if (source == null)
        {
            return;
        }

        deckSizeLimit = source.deckSizeLimit;
        onlyUseObtainedCards = source.onlyUseObtainedCards;
        persistAcrossScenes = source.persistAcrossScenes;
        showDebugInfo = source.showDebugInfo;
        filterBySelectedCharacter = source.filterBySelectedCharacter;

        selectedCards = source.selectedCards != null
            ? new List<CardData>(source.selectedCards)
            : new List<CardData>();

        Instance = this;

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    public int GetDeckLimit()
    {
        return Mathf.Max(1, deckSizeLimit);
    }

    public bool HasSelectedDeck()
    {
        return selectedCards != null && selectedCards.Count > 0;
    }

    public IReadOnlyList<CardData> GetSelectedCards()
    {
        return selectedCards;
    }

    public IReadOnlyList<CardData> GetAvailableCards()
    {
        List<CardData> allCards = LoadAllCards();

        if (showDebugInfo)
        {
            Debug.Log($"DeckManager: Loaded {allCards.Count} total cards (before filters).");
        }

        if (onlyUseObtainedCards)
        {
            allCards = allCards.Where(card => card != null && card.IsObtained).ToList();
        }
        else
        {
            allCards = allCards.Where(card => card != null).ToList();
        }

        if (filterBySelectedCharacter)
        {
            CharacterData selected = GetSelectedCharacterForFiltering();
            if (selected != null)
            {
                allCards = allCards.Where(card => card.CanBeUsedByCharacter(selected)).ToList();

                if (showDebugInfo)
                {
                    Debug.Log($"DeckManager: Filtered available cards for '{selected.characterName}' -> {allCards.Count} cards.");
                }
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning("DeckManager: filterBySelectedCharacter is enabled but no selected character was found.");
            }
        }

        if (showDebugInfo)
        {
            string names = allCards.Count > 0
                ? string.Join(", ", allCards.Select(card => card != null ? card.Title : "<null>"))
                : "<none>";
            Debug.Log($"DeckManager: Available cards -> {names}");
        }

        return allCards;
    }

    public void RefreshAllCards()
    {
        cachedAllCards = null;
        LoadAllCards();
    }

    public bool IsCardSelected(CardData card)
    {
        if (card == null) return false;
        return selectedCards.Contains(card);
    }

    public bool AddCardToDeck(CardData card)
    {
        if (card == null) return false;

        if (selectedCards.Contains(card))
        {
            return false;
        }

        if (selectedCards.Count >= GetDeckLimit())
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"DeckManager: Deck is full ({GetDeckLimit()} cards). Cannot add '{card.Title}'.");
            }
            return false;
        }

        selectedCards.Add(card);
        NotifyDeckChanged();
        return true;
    }

    public bool RemoveCardFromDeck(CardData card)
    {
        if (card == null) return false;

        bool removed = selectedCards.Remove(card);
        if (removed)
        {
            NotifyDeckChanged();
        }
        return removed;
    }

    public bool ToggleCard(CardData card)
    {
        if (card == null) return false;

        if (selectedCards.Contains(card))
        {
            return RemoveCardFromDeck(card);
        }

        return AddCardToDeck(card);
    }

    public void ClearDeck()
    {
        selectedCards.Clear();
        NotifyDeckChanged();
    }

    public void SetDeckLimit(int newLimit)
    {
        deckSizeLimit = Mathf.Max(1, newLimit);
        TrimDeckToLimit();
        NotifyDeckChanged();
    }

    private void TrimDeckToLimit()
    {
        int limit = GetDeckLimit();
        if (selectedCards.Count <= limit)
        {
            return;
        }

        selectedCards.RemoveRange(limit, selectedCards.Count - limit);
    }

    private void NotifyDeckChanged()
    {
        OnDeckChanged?.Invoke(selectedCards);

        if (showDebugInfo)
        {
            Debug.Log($"DeckManager: Deck now has {selectedCards.Count}/{GetDeckLimit()} cards.");
        }
    }

    private static List<CardData> LoadAllCards()
    {
        if (cachedAllCards != null)
        {
            return cachedAllCards;
        }

        cachedAllCards = new List<CardData>();

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:CardData", new[] { "Assets/CardData" });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
            if (card != null)
            {
                cachedAllCards.Add(card);
            }
        }
#else
        CardData[] cards = Resources.LoadAll<CardData>("CardData");
        if (cards != null)
        {
            cachedAllCards.AddRange(cards);
        }
#endif

        return cachedAllCards;
    }

    private static CharacterData GetSelectedCharacterForFiltering()
    {
        if (GameManager.Instance == null)
        {
            return null;
        }

        return GameManager.Instance.GetSelectedCharacter();
    }
}
