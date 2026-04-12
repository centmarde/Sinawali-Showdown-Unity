using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Card Inspector component for showing detailed card information
/// Attach to individual card UI objects to enable selection highlighting and inspection dialog
/// Integrates with HandManager for card selection events
/// </summary>
public class CardInspector : MonoBehaviour, IPointerClickHandler
{
    [Header("Inspector Settings")]
    [SerializeField] private bool showInspectorOnClick = true;
    [SerializeField] private bool highlightOnSelection = true;
    [SerializeField] private bool requireConfirmation = true;
    
    [Header("Highlight Settings")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float highlightIntensity = 2f;
    [SerializeField] private float animationDuration = 0.3f;
    
    [Header("References")]
    [SerializeField] private CardFetcher cardFetcher;
    [SerializeField] private Image cardBackground;
    [SerializeField] private Outline cardOutline;
    
    [Header("Inspector UI References - Auto-Found")]
    [SerializeField] private GameObject inspectorPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI manaText;
    [SerializeField] private TextMeshProUGUI cardTypeText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private TextMeshProUGUI characterClassText;
    [SerializeField] private TextMeshProUGUI effectsText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button backgroundOverlay;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    // State tracking
    private bool isSelected = false;
    private bool isHighlighted = false;
    private Color originalBackgroundColor;
    private Vector3 originalScale;
    private HandManager handManager;
    
    // Static inspector management to prevent conflicts
    private static GameObject sharedInspectorPanel;
    private static CardInspector currentlySelectedCard;
    
    /// <summary>
    /// Ensure the inspector system is properly set up
    /// </summary>
    public static void EnsureInspectorSystem()
    {
        // Find or create the inspector panel
        FindSharedInspectorPanel();
        
        if (sharedInspectorPanel == null)
        {
            Debug.LogWarning("CardInspector: No inspector panel found. Trying to create one via CardInspectorAutoCreate...");
            
            // Try to find and trigger CardInspectorAutoCreate
            CardInspectorAutoCreate autoCreate = GameObject.FindObjectOfType<CardInspectorAutoCreate>();
            if (autoCreate != null)
            {
                autoCreate.SetupCompleteCardInspectionSystem();
                FindSharedInspectorPanel(); // Try again after creation
            }
            else
            {
                Debug.LogError("CardInspector: No CardInspectorAutoCreate found in scene! Please add one to create the inspector UI.");
            }
        }
    }
    
    // Events for card selection
    public System.Action<CardInspector> OnCardSelected;
    public System.Action<CardInspector> OnCardDeselected;
    public System.Action<CardInspector, CardData> OnCardConfirmed;
    public System.Action<CardInspector> OnCardCancelled;
    
    void Start()
    {
        Initialize();
    }
    
    /// <summary>
    /// Initialize the card inspector
    /// </summary>
    public void Initialize()
    {
        // Auto-find components if not assigned
        if (cardFetcher == null)
        {
            cardFetcher = GetComponent<CardFetcher>();
        }
        
        if (cardBackground == null)
        {
            cardBackground = GetComponent<Image>();
            if (cardBackground == null)
            {
                cardBackground = GetComponentInChildren<Image>();
            }
        }
        
        if (cardOutline == null)
        {
            cardOutline = GetComponent<Outline>();
        }
        
        // Find HandManager in parent hierarchy
        if (handManager == null)
        {
            handManager = GetComponentInParent<HandManager>();
        }
        
        // Store original values
        if (cardBackground != null)
        {
            originalBackgroundColor = cardBackground.color;
        }
        originalScale = transform.localScale;
        
        // Auto-find inspector UI elements
        AutoFindInspectorElements();
        
        // Setup inspector panel
        SetupInspectorPanel();
        
        if (showDebugInfo)
        {
            Debug.Log($"CardInspector: Initialized on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Auto-find inspector UI elements by common naming patterns
    /// </summary>
    void AutoFindInspectorElements()
    {
        // Always try to find the inspector panel (in case it was recreated)
        FindSharedInspectorPanel();
        
        // All cards use the same shared panel
        inspectorPanel = sharedInspectorPanel;
        
        if (inspectorPanel != null)
        {
            // Auto-find UI elements within the panel
            AutoFindUIElementsInPanel();
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning("CardInspector: No inspector panel found in scene. Make sure CardInspectorAutoCreate has run.");
        }
    }
    
    /// <summary>
    /// Find the shared inspector panel in the scene
    /// </summary>
    static void FindSharedInspectorPanel()
    {
        if (sharedInspectorPanel != null && sharedInspectorPanel != null) return;
        
        // Search all GameObjects (including children and inactive ones) by name
        GameObject[] allGameObjects = GameObject.FindObjectsOfType<GameObject>(true);
        string[] targetNames = { "CardInspectorPanel", "InspectorPanel", "CardDetailPanel" };
        
        GameObject foundPanel = null;
        foreach (string targetName in targetNames)
        {
            foundPanel = System.Array.Find(allGameObjects, go => go.name == targetName);
            if (foundPanel != null) break;
        }
        
        if (foundPanel != null)
        {
            sharedInspectorPanel = foundPanel;
            Debug.Log($"CardInspector: Found shared inspector panel: {foundPanel.name} (Parent: {(foundPanel.transform.parent != null ? foundPanel.transform.parent.name : "None")})");
        }
        else
        {
            var panelObjects = allGameObjects.Where(go => go.name.ToLower().Contains("inspector") || go.name.ToLower().Contains("panel")).ToArray();
            Debug.LogWarning($"CardInspector: Could not find inspector panel. Found {panelObjects.Length} potential panels: " + 
                           string.Join(", ", panelObjects.Select(go => $"{go.name}({(go.transform.parent != null ? go.transform.parent.name : "Root")})")));
        }
    }
    
    /// <summary>
    /// Auto-find UI elements within the inspector panel
    /// </summary>
    void AutoFindUIElementsInPanel()
    {
        if (inspectorPanel == null) return;
        
        // Find text elements by name patterns (case-insensitive)
        TextMeshProUGUI[] textElements = inspectorPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        
        foreach (TextMeshProUGUI textElement in textElements)
        {
            string elementName = textElement.name.ToLower();
            
            if (titleText == null && (elementName.Contains("title") || elementName.Contains("name")))
            {
                titleText = textElement;
            }
            else if (descriptionText == null && (elementName.Contains("description") || elementName.Contains("desc")))
            {
                descriptionText = textElement;
            }
            else if (damageText == null && elementName.Contains("damage"))
            {
                damageText = textElement;
            }
            else if (manaText == null && elementName.Contains("mana"))
            {
                manaText = textElement;
            }
            else if (cardTypeText == null && (elementName.Contains("type") || elementName.Contains("cardtype")))
            {
                cardTypeText = textElement;
            }
            else if (rarityText == null && elementName.Contains("rarity"))
            {
                rarityText = textElement;
            }
            else if (characterClassText == null && (elementName.Contains("class") || elementName.Contains("character")))
            {
                characterClassText = textElement;
            }
            else if (effectsText == null && (elementName.Contains("effect") || elementName.Contains("special")))
            {
                effectsText = textElement;
            }
        }
        
        // Find buttons
        Button[] buttons = inspectorPanel.GetComponentsInChildren<Button>(true);
        
        foreach (Button button in buttons)
        {
            string buttonName = button.name.ToLower();
            
            if (confirmButton == null && (buttonName.Contains("confirm") || buttonName.Contains("accept") || buttonName.Contains("ok")))
            {
                confirmButton = button;
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }
            else if (cancelButton == null && (buttonName.Contains("cancel") || buttonName.Contains("close") || buttonName.Contains("back")))
            {
                cancelButton = button;
                cancelButton.onClick.AddListener(OnCancelClicked);
            }
            else if (backgroundOverlay == null && (buttonName.Contains("background") || buttonName.Contains("overlay")))
            {
                backgroundOverlay = button;
                backgroundOverlay.onClick.AddListener(OnBackgroundClicked);
            }
        }
    }
    
    /// <summary>
    /// Setup inspector panel initial state
    /// </summary>
    void SetupInspectorPanel()
    {
        if (inspectorPanel != null)
        {
            // Hide inspector panel by default
            inspectorPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Handle pointer click events
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (showInspectorOnClick)
        {
            SelectCard();
        }
    }
    
    /// <summary>
    /// Select this card and show inspector
    /// </summary>
    [ContextMenu("Select Card")]
    public void SelectCard()
    {
        if (showDebugInfo)
        {
            Debug.Log($"CardInspector: SelectCard called on {gameObject.name}");
        }
        
        if (isSelected) 
        {
            if (showDebugInfo)
            {
                Debug.Log($"CardInspector: Card {gameObject.name} already selected, skipping");
            }
            return;
        }
        
        // Update static reference to currently selected card
        if (currentlySelectedCard != null && currentlySelectedCard != this)
        {
            if (showDebugInfo)
            {
                Debug.Log($"CardInspector: Deselecting previous card {currentlySelectedCard.gameObject.name}");
            }
            currentlySelectedCard.SilentDeselect();
        }
        currentlySelectedCard = this;
        
        isSelected = true;
        
        if (highlightOnSelection)
        {
            HighlightCard();
        }
        
        // Show inspector with current card's data
        if (showInspectorOnClick)
        {
            if (showDebugInfo)
            {
                Debug.Log($"CardInspector: Attempting to show inspector for {gameObject.name}");
            }
            
            // Ensure inspector system is set up before trying to show
            EnsureInspectorSystem();
            
            ShowInspectorForCard();
        }
        else if (showDebugInfo)
        {
            Debug.Log($"CardInspector: showInspectorOnClick is false, not showing inspector");
        }
        
        // Notify events
        OnCardSelected?.Invoke(this);
        
        // Notify HandManager if available
        if (handManager != null && handManager.OnCardSelected != null)
        {
            handManager.OnCardSelected.Invoke(this);
        }
        
        if (showDebugInfo)
        {
            CardData currentCard = GetCurrentCard();
            string cardName = currentCard != null ? currentCard.Title : "Unknown";
            Debug.Log($"CardInspector: Successfully selected card '{cardName}' on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Deselect this card with option to hide inspector
    /// </summary>
    /// <param name="hideInspector">Whether to hide the inspector when deselecting</param>
    public void DeselectCard(bool hideInspector = true)
    {
        if (!isSelected) return;
        
        isSelected = false;
        
        // Clear static reference if this was the selected card
        if (currentlySelectedCard == this)
        {
            currentlySelectedCard = null;
        }
        
        if (highlightOnSelection)
        {
            RemoveHighlight();
        }
        
        // Only close inspector if explicitly requested AND this is the last selected card
        if (hideInspector && currentlySelectedCard == null)
        {
            HideInspectorStatic();
        }
        
        // Notify events
        OnCardDeselected?.Invoke(this);
        
        // Notify HandManager if available
        if (handManager != null && handManager.OnCardDeselected != null)
        {
            handManager.OnCardDeselected.Invoke(this);
        }
        
        if (showDebugInfo)
        {
            string action = hideInspector ? "closed inspector and deselected" : "silently deselected";
            Debug.Log($"CardInspector: {action} card on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Deselect this card and hide inspector (for context menu)
    /// </summary>
    [ContextMenu("Deselect Card")]
    public void DeselectCardWithInspectorClose()
    {
        DeselectCard(true);
    }
    
    /// <summary>
    /// Silently deselect this card without affecting the inspector
    /// Used when switching between cards
    /// </summary>
    public void SilentDeselect()
    {
        DeselectCard(false);
    }
    
    /// <summary>
    /// This method is now handled by the centralized selection in SelectCard()
    /// Kept for backward compatibility but no longer needed
    /// </summary>
    void DeselectAllOtherCards()
    {
        // This is now handled automatically by the static currentlySelectedCard management
        // in the SelectCard() method, so this method is effectively a no-op
    }
    
    /// <summary>
    /// Highlight the card with visual effects
    /// </summary>
    void HighlightCard()
    {
        if (isHighlighted) return;
        
        isHighlighted = true;
        
        // Apply highlight color to background
        if (cardBackground != null)
        {
            Color highlightedColor = originalBackgroundColor * highlightColor * highlightIntensity;
            highlightedColor.a = originalBackgroundColor.a;
            cardBackground.color = highlightedColor;
        }
        
        // Add or enable outline
        if (cardOutline != null)
        {
            cardOutline.effectColor = highlightColor;
            cardOutline.enabled = true;
        }
        else
        {
            // Create outline component if it doesn't exist
            cardOutline = gameObject.AddComponent<Outline>();
            cardOutline.effectColor = highlightColor;
            cardOutline.effectDistance = new Vector2(2, 2);
        }
        
        // Scale animation
        StartCoroutine(AnimateScale(originalScale * 1.1f, animationDuration));
    }
    
    /// <summary>
    /// Remove highlight effects from the card
    /// </summary>
    void RemoveHighlight()
    {
        if (!isHighlighted) return;
        
        isHighlighted = false;
        
        // Restore original background color
        if (cardBackground != null)
        {
            cardBackground.color = originalBackgroundColor;
        }
        
        // Disable outline
        if (cardOutline != null)
        {
            cardOutline.enabled = false;
        }
        
        // Reset scale
        StartCoroutine(AnimateScale(originalScale, animationDuration));
    }
    
    /// <summary>
    /// Animate scale changes with easing
    /// </summary>
    System.Collections.IEnumerator AnimateScale(Vector3 targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            // Use ease-out cubic for smooth animation
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            
            transform.localScale = Vector3.Lerp(startScale, targetScale, easedProgress);
            yield return null;
        }
        
        transform.localScale = targetScale;
    }
    
    /// <summary>
    /// Show the inspector panel with card details (centralized method)
    /// </summary>
    public void ShowInspectorForCard()
    {
        // Try to find the panel if it's null
        if (sharedInspectorPanel == null)
        {
            FindSharedInspectorPanel();
        }
        
        if (sharedInspectorPanel == null)
        {
            if (showDebugInfo)
            {
                Debug.LogError("CardInspector: Cannot show inspector - no inspector panel found! Make sure CardInspectorAutoCreate has been run.");
            }
            return;
        }
        
        CardData currentCard = GetCurrentCard();
        if (currentCard == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("CardInspector: Cannot show inspector - no card data available");
            }
            return;
        }
        
        // Populate inspector UI with card data
        PopulateInspectorUI(currentCard);
        
        // Show inspector panel if not already active
        bool wasActive = sharedInspectorPanel.activeInHierarchy;
        sharedInspectorPanel.SetActive(true);
        
        // Only animate if inspector was not already active
        if (!wasActive && sharedInspectorPanel.transform is RectTransform rectTransform)
        {
            StartCoroutine(AnimateInspectorScale(rectTransform, Vector3.zero, Vector3.one, animationDuration));
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"CardInspector: Showing inspector for '{currentCard.Title}' - was already active: {wasActive}");
        }
    }
    
    /// <summary>
    /// Show the inspector panel with card details (legacy method for backward compatibility)
    /// </summary>
    public void ShowInspector()
    {
        ShowInspectorForCard();
    }
    
    /// <summary>
    /// Hide the inspector panel (static method to prevent conflicts)
    /// </summary>
    public static void HideInspectorStatic()
    {
        if (sharedInspectorPanel == null) return;
        
        // Animate inspector panel disappearance
        if (sharedInspectorPanel.transform is RectTransform rectTransform)
        {
            // Find any active CardInspector to handle the animation
            CardInspector activeInspector = currentlySelectedCard;
            if (activeInspector == null)
            {
                // Find any CardInspector component to handle animation
                activeInspector = FindObjectOfType<CardInspector>();
            }
            
            if (activeInspector != null)
            {
                activeInspector.StartCoroutine(activeInspector.AnimateInspectorScale(rectTransform, Vector3.one, Vector3.zero, activeInspector.animationDuration, () =>
                {
                    sharedInspectorPanel.SetActive(false);
                }));
            }
            else
            {
                sharedInspectorPanel.SetActive(false);
            }
        }
        else
        {
            sharedInspectorPanel.SetActive(false);
        }
        
        Debug.Log("CardInspector: Hiding shared inspector panel");
    }
    
    /// <summary>
    /// Hide the inspector panel (instance method for backward compatibility)
    /// </summary>
    public void HideInspector()
    {
        HideInspectorStatic();
    }
    
    /// <summary>
    /// Animate inspector panel scale
    /// </summary>
    System.Collections.IEnumerator AnimateInspectorScale(RectTransform rectTransform, Vector3 startScale, Vector3 targetScale, float duration, System.Action onComplete = null)
    {
        rectTransform.localScale = startScale;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            // Use ease-out cubic for smooth animation
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, easedProgress);
            yield return null;
        }
        
        rectTransform.localScale = targetScale;
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// Populate inspector UI with card data
    /// </summary>
    void PopulateInspectorUI(CardData card)
    {
        if (card == null) return;
        
        // Set title
        if (titleText != null)
        {
            titleText.text = card.Title;
            titleText.color = GetColorForCardType(card.Type);
        }
        
        // Set description
        if (descriptionText != null)
        {
            descriptionText.text = card.Description;
        }
        
        // Set damage
        if (damageText != null)
        {
            damageText.text = $"Damage: {card.Damage}";
            damageText.color = card.Damage > 0 ? Color.red : Color.gray;
        }
        
        // Set mana
        if (manaText != null)
        {
            manaText.text = $"Mana: {card.ManaDeduction}";
            manaText.color = Color.blue;
        }
        
        // Set card type
        if (cardTypeText != null)
        {
            cardTypeText.text = $"Type: {card.Type}";
            cardTypeText.color = GetColorForCardType(card.Type);
        }
        
        // Set rarity
        if (rarityText != null)
        {
            rarityText.text = $"Rarity: {card.Rarity}";
            rarityText.color = GetColorForRarity(card.Rarity);
        }
        
        // Set character compatibility
        if (characterClassText != null)
        {
            string characterInfo = "";
            
            if (card.IsUniversalCard)
            {
                characterInfo = "Universal";
            }
            else if (card.CompatibleCharacterTypes != null && card.CompatibleCharacterTypes.Count > 0)
            {
                characterInfo = string.Join(", ", card.CompatibleCharacterTypes);
            }
            else
            {
                characterInfo = "None";
            }
            
            characterClassText.text = $"Compatible: {characterInfo}";
        }
        
        // Set effects
        if (effectsText != null)
        {
            if (card.Effects != null && card.Effects.Count > 0)
            {
                string effectsString = string.Join(", ", card.Effects);
                effectsText.text = $"Effects: {effectsString}";
            }
            else
            {
                effectsText.text = "Effects: None";
            }
        }
    }
    
    /// <summary>
    /// Get color for card type
    /// </summary>
    Color GetColorForCardType(CardType cardType)
    {
        switch (cardType)
        {
            case CardType.Attack: return Color.red;
            case CardType.Buff: return Color.green;
            case CardType.Debuff: return Color.magenta;
            case CardType.Heal: return Color.cyan;
            case CardType.Shield: return Color.blue;
            case CardType.Special: return Color.yellow;
            default: return Color.white;
        }
    }
    
    /// <summary>
    /// Get color for rarity
    /// </summary>
    Color GetColorForRarity(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common: return Color.white;
            case Rarity.Uncommon: return Color.green;
            case Rarity.Rare: return Color.blue;
            case Rarity.Epic: return Color.magenta;
            case Rarity.Legendary: return Color.yellow;
            default: return Color.gray;
        }
    }
    
    /// <summary>
    /// Get the current card data from CardFetcher
    /// </summary>
    public CardData GetCurrentCard()
    {
        if (cardFetcher != null)
        {
            return cardFetcher.GetCurrentCard();
        }
        return null;
    }
    
    /// <summary>
    /// Handle confirm button click
    /// </summary>
    void OnConfirmClicked()
    {
        CardData currentCard = GetCurrentCard();
        if (currentCard != null)
        {
            // Notify events
            OnCardConfirmed?.Invoke(this, currentCard);
            
            // Notify HandManager if available
            if (handManager != null && handManager.OnCardConfirmed != null)
            {
                handManager.OnCardConfirmed.Invoke(this, currentCard);
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"CardInspector: Confirmed card '{currentCard.Title}'");
            }
        }
        
        // Hide inspector after confirmation
        DeselectCard(true);
    }
    
    /// <summary>
    /// Handle cancel button click
    /// </summary>
    void OnCancelClicked()
    {
        // Notify events
        OnCardCancelled?.Invoke(this);
        
        if (showDebugInfo)
        {
            Debug.Log("CardInspector: Cancelled card selection via cancel button");
        }
        
        // Close inspector and deselect card
        DeselectCard(true);
    }
    
    /// <summary>
    /// Handle background overlay click
    /// </summary>
    void OnBackgroundClicked()
    {
        if (showDebugInfo)
        {
            Debug.Log("CardInspector: Clicked background overlay - closing inspector");
        }
        
        // Close inspector when clicking outside
        if (currentlySelectedCard != null)
        {
            currentlySelectedCard.OnCancelClicked();
        }
        else
        {
            HideInspectorStatic();
        }
    }
    
    /// <summary>
    /// Force close the inspector (for close button functionality)
    /// </summary>
    public void ForceCloseInspector()
    {
        if (showDebugInfo)
        {
            Debug.Log("CardInspector: Force closing inspector via close button");
        }
        
        // Clear the currently selected card and close inspector
        if (currentlySelectedCard != null)
        {
            currentlySelectedCard.DeselectCard(true);
        }
        else
        {
            // Fallback: directly close the inspector
            HideInspectorStatic();
        }
    }
    
    /// <summary>
    /// Check if this card is currently selected
    /// </summary>
    public bool IsSelected()
    {
        return isSelected;
    }
    
    /// <summary>
    /// Check if this card is currently highlighted
    /// </summary>
    public bool IsHighlighted()
    {
        return isHighlighted;
    }

#if UNITY_EDITOR
    [ContextMenu("Debug: Show Inspector")]
    public void DebugShowInspector()
    {
        ShowInspector();
    }
    
    [ContextMenu("Debug: Hide Inspector")]
    public void DebugHideInspector()
    {
        HideInspector();
    }
    
    [ContextMenu("Debug: Toggle Highlight")]
    public void DebugToggleHighlight()
    {
        if (isHighlighted)
        {
            RemoveHighlight();
        }
        else
        {
            HighlightCard();
        }
    }
    
    [ContextMenu("Debug: Check Components")]
    public void DebugCheckComponents()
    {
        Debug.Log($"CardInspector Debug on {gameObject.name}:");
        Debug.Log($"- CardFetcher: {(cardFetcher != null ? "Found" : "Missing")}");
        Debug.Log($"- CardBackground: {(cardBackground != null ? "Found" : "Missing")}");
        Debug.Log($"- CardOutline: {(cardOutline != null ? "Found" : "Missing")}");
        Debug.Log($"- HandManager: {(handManager != null ? "Found" : "Missing")}");
        Debug.Log($"- Inspector Panel: {(inspectorPanel != null ? "Found" : "Missing")}");
        Debug.Log($"- Is Selected: {isSelected}");
        Debug.Log($"- Is Highlighted: {isHighlighted}");
        
        CardData currentCard = GetCurrentCard();
        Debug.Log($"- Current Card: {(currentCard != null ? currentCard.Title : "None")}");
    }
    
    [ContextMenu("Debug: Auto-Find Components")]
    public void DebugAutoFindComponents()
    {
        Initialize();
        Debug.Log("CardInspector: Auto-find components completed");
    }    
    [ContextMenu("Debug: Force Find Inspector Panel")]
    public void DebugForceFindInspectorPanel()
    {
        FindSharedInspectorPanel();
        Debug.Log($"CardInspector: Shared inspector panel: {(sharedInspectorPanel != null ? sharedInspectorPanel.name : "NULL")}");
    }
    
    [ContextMenu("Debug: Show All GameObjects")]
    public void DebugShowAllGameObjects()
    {
        var allGameObjects = GameObject.FindObjectsOfType<GameObject>(true);
        var panelObjects = allGameObjects.Where(go => go.name.ToLower().Contains("inspector") || go.name.ToLower().Contains("panel")).ToArray();
        
        Debug.Log($"CardInspector: Found {panelObjects.Length} potential panel objects:");
        foreach (var obj in panelObjects)
        {
            Debug.Log($"  - {obj.name} (Active: {obj.activeInHierarchy}, Parent: {(obj.transform.parent != null ? obj.transform.parent.name : "None")})");
        }
    }
    
    [ContextMenu("Debug: Test Card Selection")]
    public void DebugTestCardSelection()
    {
        Debug.Log($"CardInspector: Testing card selection on {gameObject.name}");
        Debug.Log($"  - showInspectorOnClick: {showInspectorOnClick}");
        Debug.Log($"  - sharedInspectorPanel: {(sharedInspectorPanel != null ? sharedInspectorPanel.name : "NULL")}");
        Debug.Log($"  - currentlySelectedCard: {(currentlySelectedCard != null ? currentlySelectedCard.gameObject.name : "NULL")}");
        Debug.Log($"  - CardData available: {(GetCurrentCard() != null ? GetCurrentCard().Title : "NULL")}");
        
        SelectCard();
    }
#endif
}