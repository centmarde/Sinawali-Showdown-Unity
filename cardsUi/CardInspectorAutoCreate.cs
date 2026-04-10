using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Auto-creates a complete card inspection and selection system
/// Creates professional overlay UI for card details with confirmation buttons
/// Adds CardInspector components to all cards in HandManager
/// Sets up event integration and visual highlighting
/// </summary>
public class CardInspectorAutoCreate : MonoBehaviour
{
    [Header("Inspector UI Settings")]
    [SerializeField] private bool createInspectorUI = true;
    [SerializeField] private bool addInspectorToCards = true;
    [SerializeField] private bool setupEventIntegration = true;
    
    [Header("UI Customization")]
    [SerializeField] private Color panelBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    [SerializeField] private Color panelBorderColor = Color.white;
    [SerializeField] private Color confirmButtonColor = Color.green;
    [SerializeField] private Color cancelButtonColor = Color.red;
    [SerializeField] private Vector2 panelSize = new Vector2(400, 500);
    
    [Header("Card Highlighting")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float highlightIntensity = 2f;
    [SerializeField] private float animationDuration = 0.3f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool persistAcrossScenes = true;
    
    // Static instance for singleton pattern
    private static CardInspectorAutoCreate instance;
    
    // References to created UI elements
    private GameObject inspectorPanel;
    private Canvas mainCanvas;
    private HandManager handManager;
    private List<CardInspector> cardInspectors = new List<CardInspector>();
    private Coroutine hideInspectorCoroutine; // Track the current hide delay coroutine
    
    void Awake()
    {
        // Singleton pattern with optional persistence
        if (instance == null)
        {
            instance = this;
            
            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (instance != this)
        {
            if (showDebugInfo)
            {
                Debug.Log("CardInspectorAutoCreate: Another instance exists, destroying this one");
            }
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        SetupCompleteCardInspectionSystem();
    }
    
    /// <summary>
    /// Creates the complete card inspection system
    /// </summary>
    [ContextMenu("Setup Complete Card Inspection System")]
    public void SetupCompleteCardInspectionSystem()
    {
        if (showDebugInfo)
        {
            Debug.Log("CardInspectorAutoCreate: Setting up complete card inspection system...");
        }
        
        // Find or create main canvas
        FindOrCreateMainCanvas();
        
        // Find HandManager
        FindHandManager();
        
        // Create inspector UI
        if (createInspectorUI)
        {
            CreateInspectorUI();
        }
        
        // Add inspector components to cards
        if (addInspectorToCards)
        {
            AddInspectorComponentsToCards();
        }
        
        // Setup event integration
        if (setupEventIntegration)
        {
            SetupEventIntegration();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"CardInspectorAutoCreate: Setup complete! Created {cardInspectors.Count} card inspectors");
        }
    }
    
    /// <summary>
    /// Find or create the main canvas for UI
    /// </summary>
    void FindOrCreateMainCanvas()
    {
        // Try to find existing canvas - prioritize by name first, then by render mode
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        
        if (showDebugInfo)
        {
            Debug.Log($"CardInspectorAutoCreate: Found {canvases.Length} canvases in scene");
            foreach (Canvas c in canvases)
            {
                Debug.Log($"  - Canvas: '{c.gameObject.name}' (RenderMode: {c.renderMode}, Active: {c.gameObject.activeInHierarchy})");
            }
        }
        
        // First try to find canvas by common names
        foreach (Canvas canvas in canvases)
        {
            if (canvas.gameObject.name.ToLower().Contains("card") || canvas.gameObject.name.ToLower().Contains("ui"))
            {
                mainCanvas = canvas;
                if (showDebugInfo)
                {
                    Debug.Log($"CardInspectorAutoCreate: Using existing canvas '{canvas.gameObject.name}' (by name match)");
                }
                break;
            }
        }
        
        // If no named match, use any ScreenSpaceOverlay canvas
        if (mainCanvas == null)
        {
            foreach (Canvas canvas in canvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    mainCanvas = canvas;
                    if (showDebugInfo)
                    {
                        Debug.Log($"CardInspectorAutoCreate: Using existing canvas '{canvas.gameObject.name}' (by render mode)");
                    }
                    break;
                }
            }
        }
        
        // If still no match, use the first active canvas
        if (mainCanvas == null && canvases.Length > 0)
        {
            foreach (Canvas canvas in canvases)
            {
                if (canvas.gameObject.activeInHierarchy)
                {
                    mainCanvas = canvas;
                    if (showDebugInfo)
                    {
                        Debug.Log($"CardInspectorAutoCreate: Using existing canvas '{canvas.gameObject.name}' (first active canvas)");
                    }
                    break;
                }
            }
        }
        
        // Create canvas if not found
        if (mainCanvas == null)
        {
            GameObject canvasGO = new GameObject("Main Canvas");
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 1000; // High sorting order for overlay
            
            // Add CanvasScaler for responsive design
            CanvasScaler canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            // Add GraphicRaycaster for UI interactions
            canvasGO.AddComponent<GraphicRaycaster>();
            
            if (showDebugInfo)
            {
                Debug.Log("CardInspectorAutoCreate: Created main canvas");
            }
        }
        
        // Ensure EventSystem exists
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            
            if (showDebugInfo)
            {
                Debug.Log("CardInspectorAutoCreate: Created EventSystem");
            }
        }
    }
    
    /// <summary>
    /// Find the HandManager in the scene
    /// </summary>
    void FindHandManager()
    {
        if (handManager == null)
        {
            // Try multiple methods to find HandManager
            HandManager[] allHandManagers = FindObjectsOfType<HandManager>(true); // Include inactive objects
            
            if (showDebugInfo)
            {
                Debug.Log($"CardInspectorAutoCreate: Found {allHandManagers.Length} HandManager components in scene");
                foreach (HandManager hm in allHandManagers)
                {
                    Debug.Log($"  - HandManager: '{hm.gameObject.name}' (Active: {hm.gameObject.activeInHierarchy}, Enabled: {hm.enabled})");
                }
            }
            
            // Use the first active HandManager
            foreach (HandManager hm in allHandManagers)
            {
                if (hm.gameObject.activeInHierarchy && hm.enabled)
                {
                    handManager = hm;
                    break;
                }
            }
            
            // If no active found, use the first one regardless of state
            if (handManager == null && allHandManagers.Length > 0)
            {
                handManager = allHandManagers[0];
                if (showDebugInfo)
                {
                    Debug.Log($"CardInspectorAutoCreate: Using inactive HandManager '{handManager.gameObject.name}' - you may need to activate it");
                }
            }
            
            if (handManager == null)
            {
                Debug.LogWarning("CardInspectorAutoCreate: No HandManager found in scene! Please ensure you have a HandManager component.");
            }
            else if (showDebugInfo)
            {
                Debug.Log($"CardInspectorAutoCreate: Found HandManager on '{handManager.gameObject.name}'");
            }
        }
    }
    
    /// <summary>
    /// Create the inspector UI overlay
    /// </summary>
    void CreateInspectorUI()
    {
        if (mainCanvas == null)
        {
            Debug.LogError("CardInspectorAutoCreate: Cannot create inspector UI - no canvas available");
            return;
        }
        
        // Check if inspector panel already exists
        if (inspectorPanel != null)
        {
            if (showDebugInfo)
            {
                Debug.Log("CardInspectorAutoCreate: Inspector panel already exists, skipping creation");
            }
            return;
        }
        
        // Create main inspector panel
        inspectorPanel = new GameObject("CardInspectorPanel");
        inspectorPanel.transform.SetParent(mainCanvas.transform, false);
        
        // Setup panel background
        Image panelBackground = inspectorPanel.AddComponent<Image>();
        panelBackground.color = panelBackgroundColor;
        panelBackground.raycastTarget = true; // Enable for background clicks
        
        // Add outline for border effect
        Outline panelOutline = inspectorPanel.AddComponent<Outline>();
        panelOutline.effectColor = panelBorderColor;
        panelOutline.effectDistance = new Vector2(2, 2);
        
        // Setup RectTransform for center positioning
        RectTransform panelRect = inspectorPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = panelSize;
        
        // Create content panel
        GameObject contentPanel = CreateContentPanel(inspectorPanel);
        
        // Create UI elements within content panel
        CreateInspectorUIElements(contentPanel);
        
        // Create button panel
        CreateButtonPanel(contentPanel);
        
        // Hide panel by default
        inspectorPanel.SetActive(false);
        
        if (showDebugInfo)
        {
            Debug.Log("CardInspectorAutoCreate: Created inspector UI panel");
        }
    }
    

    
    /// <summary>
    /// Create content panel for card details
    /// </summary>
    GameObject CreateContentPanel(GameObject parent)
    {
        GameObject contentPanel = new GameObject("ContentPanel");
        contentPanel.transform.SetParent(parent.transform, false);
        
        // Setup RectTransform to fill parent with padding
        RectTransform contentRect = contentPanel.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.sizeDelta = Vector2.zero;
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.offsetMin = new Vector2(20, 60); // Padding (left, bottom)
        contentRect.offsetMax = new Vector2(-20, -20); // Padding (right, top)
        
        // Add vertical layout group
        VerticalLayoutGroup contentLayout = contentPanel.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 15f;
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        
        return contentPanel;
    }
    
    /// <summary>
    /// Create all UI elements for card details
    /// </summary>
    void CreateInspectorUIElements(GameObject parent)
    {
        // Title
        CreateTextElement(parent, "TitleText", "Card Title", 32, FontStyles.Bold);
        
        // Description (larger area)
        GameObject descArea = CreateTextElement(parent, "DescriptionText", "Card description will appear here...", 18, FontStyles.Normal);
        LayoutElement descLayout = descArea.AddComponent<LayoutElement>();
        descLayout.preferredHeight = 100f;
        descLayout.flexibleHeight = 1f;
        
        // Stats container
        GameObject statsContainer = CreateStatsContainer(parent);
        
        // Effects
        CreateTextElement(statsContainer, "EffectsText", "Effects: None", 16, FontStyles.Normal);
    }
    
    /// <summary>
    /// Create stats container with grid layout
    /// </summary>
    GameObject CreateStatsContainer(GameObject parent)
    {
        GameObject statsContainer = new GameObject("StatsContainer");
        statsContainer.transform.SetParent(parent.transform, false);
        
        // Setup RectTransform
        RectTransform statsRect = statsContainer.AddComponent<RectTransform>();
        
        // Add grid layout group for stats
        GridLayoutGroup gridLayout = statsContainer.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(180, 30);
        gridLayout.spacing = new Vector2(10, 5);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 2;
        
        // Add layout element for proper sizing
        LayoutElement statsLayout = statsContainer.AddComponent<LayoutElement>();
        statsLayout.preferredHeight = 120f;
        
        // Create stat elements
        CreateTextElement(statsContainer, "DamageText", "Damage: 0", 16, FontStyles.Normal);
        CreateTextElement(statsContainer, "ManaText", "Mana: 0", 16, FontStyles.Normal);
        CreateTextElement(statsContainer, "CardTypeText", "Type: Attack", 16, FontStyles.Normal);
        CreateTextElement(statsContainer, "RarityText", "Rarity: Common", 16, FontStyles.Normal);
        CreateTextElement(statsContainer, "CharacterClassText", "Class: Any", 16, FontStyles.Normal);
        
        return statsContainer;
    }
    
    /// <summary>
    /// Create button panel with confirm/cancel buttons
    /// </summary>
    void CreateButtonPanel(GameObject parent)
    {
        GameObject buttonPanel = new GameObject("ButtonPanel");
        buttonPanel.transform.SetParent(parent.transform, false);
        
        // Setup RectTransform
        RectTransform buttonRect = buttonPanel.AddComponent<RectTransform>();
        
        // Add horizontal layout group
        HorizontalLayoutGroup buttonLayout = buttonPanel.AddComponent<HorizontalLayoutGroup>();
        buttonLayout.spacing = 20f;
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.childControlWidth = true;
        buttonLayout.childControlHeight = true;
        buttonLayout.childForceExpandWidth = true;
        buttonLayout.childForceExpandHeight = false;
        
        // Add layout element for fixed height
        LayoutElement panelLayout = buttonPanel.AddComponent<LayoutElement>();
        panelLayout.preferredHeight = 50f;
        
        // Create confirm button
        GameObject confirmBtn = CreateButton(buttonPanel, "ConfirmButton", "Confirm", confirmButtonColor);
        
        // Create cancel button
        GameObject cancelBtn = CreateButton(buttonPanel, "CancelButton", "Cancel", cancelButtonColor);
        
        // Create close button (X) - make it more prominent
        GameObject closeBtn = CreateCloseButton(buttonPanel, "CloseButton");
        
        // Setup close button functionality with improved handling
        Button closeBtnComponent = closeBtn.GetComponent<Button>();
        if (closeBtnComponent != null)
        {
            closeBtnComponent.onClick.AddListener(() => {
                if (showDebugInfo)
                {
                    Debug.Log("CardInspectorAutoCreate: Close button clicked - closing inspector");
                }
                
                // Find the currently selected card and properly close the inspector
                foreach (CardInspector inspector in cardInspectors)
                {
                    if (inspector != null && inspector.IsSelected())
                    {
                        inspector.ForceCloseInspector(); // This now properly closes with hideInspector=true
                        break; // Only one card should be selected at a time
                    }
                }
                
                // Fallback: directly hide the inspector if no card was selected
                if (inspectorPanel != null && inspectorPanel.activeInHierarchy)
                {
                    inspectorPanel.SetActive(false);
                }
            });
        }
    }
    
    /// <summary>
    /// Create a text element with specified properties
    /// </summary>
    GameObject CreateTextElement(GameObject parent, string name, string text, int fontSize, FontStyles fontStyle)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent.transform, false);
        
        TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.overflowMode = TextOverflowModes.Overflow;
        
        return textGO;
    }
    
    /// <summary>
    /// Create a button with specified properties
    /// </summary>
    GameObject CreateButton(GameObject parent, string name, string text, Color buttonColor)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent.transform, false);
        
        // Add Image component for button background
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = buttonColor;
        
        // Add Button component
        Button button = buttonGO.AddComponent<Button>();
        
        // Create button text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        
        TextMeshProUGUI buttonText = textGO.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 18;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        
        // Setup text RectTransform to fill button
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        // Add shadow/outline for better text visibility
        Shadow textShadow = textGO.AddComponent<Shadow>();
        textShadow.effectColor = Color.black;
        textShadow.effectDistance = new Vector2(1, -1);
        
        return buttonGO;
    }
    
    /// <summary>
    /// Create a special close button with enhanced styling
    /// </summary>
    GameObject CreateCloseButton(GameObject parent, string name)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent.transform, false);
        
        // Add Image component for button background - make it more prominent
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.8f, 0.2f, 0.2f, 0.9f); // Darker red with transparency
        
        // Add Button component
        Button button = buttonGO.AddComponent<Button>();
        
        // Add hover effect colors
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.8f, 0.2f, 0.2f, 0.9f);
        colors.highlightedColor = new Color(1f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.6f, 0.1f, 0.1f, 1f);
        button.colors = colors;
        
        // Create button text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        
        TextMeshProUGUI buttonText = textGO.AddComponent<TextMeshProUGUI>();
        buttonText.text = "✕";
        buttonText.fontSize = 24; // Larger font for close button
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        
        // Setup text RectTransform to fill button
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        // Add outline for better visibility
        Outline textOutline = textGO.AddComponent<Outline>();
        textOutline.effectColor = Color.black;
        textOutline.effectDistance = new Vector2(2, -2);
        
        // Add shadow for depth
        Shadow textShadow = textGO.AddComponent<Shadow>();
        textShadow.effectColor = new Color(0, 0, 0, 0.5f);
        textShadow.effectDistance = new Vector2(1, -1);
        
        return buttonGO;
    }
    
    /// <summary>
    /// Add CardInspector components to all cards in HandManager
    /// </summary>
    void AddInspectorComponentsToCards()
    {
        if (handManager == null)
        {
            Debug.LogWarning("CardInspectorAutoCreate: No HandManager found, cannot add inspectors to cards");
            return;
        }
        
        cardInspectors.Clear();
        
        // Get all cards from HandManager
        List<CardFetcher> allCards = handManager.GetAllCards();
        
        foreach (CardFetcher cardFetcher in allCards)
        {
            // Check if CardInspector already exists
            CardInspector existingInspector = cardFetcher.GetComponent<CardInspector>();
            
            if (existingInspector == null)
            {
                // Add CardInspector component
                CardInspector inspector = cardFetcher.gameObject.AddComponent<CardInspector>();
                
                // Configure inspector settings
                ConfigureCardInspector(inspector);
                
                cardInspectors.Add(inspector);
                
                if (showDebugInfo)
                {
                    Debug.Log($"CardInspectorAutoCreate: Added CardInspector to '{cardFetcher.gameObject.name}'");
                }
            }
            else
            {
                cardInspectors.Add(existingInspector);
                
                if (showDebugInfo)
                {
                    Debug.Log($"CardInspectorAutoCreate: CardInspector already exists on '{cardFetcher.gameObject.name}'");
                }
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"CardInspectorAutoCreate: Added/found {cardInspectors.Count} card inspectors");
        }
    }
    
    /// <summary>
    /// Configure CardInspector component with auto-creation settings
    /// </summary>
    void ConfigureCardInspector(CardInspector inspector)
    {
        // Use reflection to set private fields if needed, or public properties if available
        // For now, we'll set what we can through initialization
        inspector.Initialize();
        
        // The inspector will auto-find the inspector panel and other components
    }
    
    /// <summary>
    /// Setup event integration between HandManager and CardInspectors
    /// </summary>
    void SetupEventIntegration()
    {
        if (handManager == null)
        {
            Debug.LogWarning("CardInspectorAutoCreate: No HandManager found, cannot setup event integration");
            return;
        }
        
        // Subscribe to HandManager events for card selection
        handManager.OnCardSelected += OnCardSelected;
        handManager.OnCardDeselected += OnCardDeselected;
        handManager.OnCardConfirmed += OnCardConfirmed;
        
        if (showDebugInfo)
        {
            Debug.Log("CardInspectorAutoCreate: Event integration setup complete");
        }
    }
    
    /// <summary>
    /// Handle card selection event
    /// </summary>
    void OnCardSelected(CardInspector inspector)
    {
        // Cancel any pending hide operations since a card is now selected
        if (hideInspectorCoroutine != null)
        {
            StopCoroutine(hideInspectorCoroutine);
            hideInspectorCoroutine = null;
            
            if (showDebugInfo)
            {
                Debug.Log("CardInspectorAutoCreate: Cancelled hide delay - new card selected");
            }
        }
        
        // Let the CardInspector handle showing and populating the inspector
        // This ensures the card data is properly displayed
        if (inspector != null)
        {
            inspector.ShowInspector();
        }
        
        if (showDebugInfo)
        {
            CardData card = inspector.GetCurrentCard();
            string cardName = card != null ? card.Title : "Unknown";
            Debug.Log($"CardInspectorAutoCreate: Card selected and inspector updated - '{cardName}'");
        }
    }
    
    /// <summary>
    /// Handle card deselection event - auto-close like close button behavior
    /// </summary>
    void OnCardDeselected(CardInspector inspector)
    {
        if (showDebugInfo)
        {
            Debug.Log("CardInspectorAutoCreate: Card deselected - auto-closing inspector");
        }
        
        // Cancel any existing hide coroutines
        if (hideInspectorCoroutine != null)
        {
            StopCoroutine(hideInspectorCoroutine);
            hideInspectorCoroutine = null;
        }
        
        // Emulate close button behavior: hide panel and deselect all cards
        if (inspectorPanel != null)
        {
            inspectorPanel.SetActive(false);
            
            // Deselect all cards (same as close button)
            foreach (CardInspector cardInspector in cardInspectors)
            {
                if (cardInspector != null && cardInspector.IsSelected())
                {
                    cardInspector.DeselectCard();
                }
            }
            
            if (showDebugInfo)
            {
                Debug.Log("CardInspectorAutoCreate: Inspector auto-closed and all cards deselected");
            }
        }
    }
    
    /// <summary>
    /// Check if inspector should be hidden after a delay (no cards selected)
    /// </summary>
    System.Collections.IEnumerator CheckHideInspectorDelayed()
    {
        yield return new WaitForSeconds(0.2f); // Slightly longer delay to ensure card switching completes
        
        // Check if any card is currently selected
        bool anyCardSelected = false;
        foreach (CardInspector inspector in cardInspectors)
        {
            if (inspector != null && inspector.IsSelected())
            {
                anyCardSelected = true;
                break;
            }
        }
        
        // Hide panel only if no cards are selected and this coroutine wasn't cancelled
        if (!anyCardSelected && inspectorPanel != null && inspectorPanel.activeInHierarchy)
        {
            inspectorPanel.SetActive(false);
            if (showDebugInfo)
            {
                Debug.Log("CardInspectorAutoCreate: No cards selected - hiding inspector panel");
            }
        }
        
        // Clear the coroutine reference since it's completed
        hideInspectorCoroutine = null;
    }
    
    /// <summary>
    /// Handle card confirmation event
    /// </summary>
    void OnCardConfirmed(CardInspector inspector, CardData card)
    {
        if (showDebugInfo)
        {
            Debug.Log($"CardInspectorAutoCreate: Card confirmed - '{card.Title}'");
        }
        
        // You can add custom logic here for what happens when a card is confirmed
        // For example: play the card, remove it from hand, trigger effects, etc.
    }
    
    /// <summary>
    /// Get the created inspector panel
    /// </summary>
    public GameObject GetInspectorPanel()
    {
        return inspectorPanel;
    }
    
    /// <summary>
    /// Get all created card inspectors
    /// </summary>
    public List<CardInspector> GetCardInspectors()
    {
        return new List<CardInspector>(cardInspectors);
    }
    
    /// <summary>
    /// Refresh the card inspection system (useful after adding new cards)
    /// </summary>
    [ContextMenu("Refresh Card Inspection System")]
    public void RefreshCardInspectionSystem()
    {
        FindHandManager();
        AddInspectorComponentsToCards();
        
        if (showDebugInfo)
        {
            Debug.Log("CardInspectorAutoCreate: Card inspection system refreshed");
        }
    }
    
    /// <summary>
    /// Clear the inspection system completely
    /// </summary>
    [ContextMenu("Clear Card Inspection System")]
    public void ClearCardInspectionSystem()
    {
        // Remove inspector panel
        if (inspectorPanel != null)
        {
            DestroyImmediate(inspectorPanel);
            inspectorPanel = null;
        }
        
        // Clear inspector components from cards
        foreach (CardInspector inspector in cardInspectors)
        {
            if (inspector != null)
            {
                DestroyImmediate(inspector);
            }
        }
        
        cardInspectors.Clear();
        
        if (showDebugInfo)
        {
            Debug.Log("CardInspectorAutoCreate: Card inspection system cleared");
        }
    }

    #if UNITY_EDITOR
    [ContextMenu("Debug: List All Components")]
    public void DebugListAllComponents()
    {
        Debug.Log("=== CardInspectorAutoCreate Debug Info ===");
        Debug.Log($"Main Canvas: {(mainCanvas != null ? mainCanvas.name : "NULL")}");
        Debug.Log($"Hand Manager: {(handManager != null ? handManager.name : "NULL")}");
        Debug.Log($"Inspector Panel: {(inspectorPanel != null ? inspectorPanel.name : "NULL")}");
        Debug.Log($"Card Inspectors: {cardInspectors.Count}");
        
        if (handManager != null)
        {
            var cards = handManager.GetAllCards();
            Debug.Log($"Cards in HandManager: {cards.Count}");
            foreach (var card in cards)
            {
                var inspector = card.GetComponent<CardInspector>();
                Debug.Log($"  - {card.name}: {(inspector != null ? "HAS INSPECTOR" : "NO INSPECTOR")}");
            }
        }
    }
    
    [ContextMenu("Debug: Test Inspector Panel")]
    public void DebugTestInspectorPanel()
    {
        if (inspectorPanel != null)
        {
            inspectorPanel.SetActive(!inspectorPanel.activeSelf);
            Debug.Log($"CardInspectorAutoCreate: Toggled inspector panel to {inspectorPanel.activeSelf}");
        }
        else
        {
            Debug.LogWarning("CardInspectorAutoCreate: No inspector panel to test");
        }
    }
    
    [ContextMenu("Debug: Force Initialize All Inspectors")]
    public void DebugForceInitializeInspectors()
    {
        foreach (CardInspector inspector in cardInspectors)
        {
            if (inspector != null)
            {
                inspector.Initialize();
                Debug.Log($"CardInspectorAutoCreate: Force initialized {inspector.gameObject.name}");
            }
        }
    }
    
    /// <summary>
    /// Unity Editor Menu: Setup Card Selection System
    /// </summary>
    [UnityEditor.MenuItem("Tools/Card System/Setup Card Selection System", false, 1)]
    static void SetupCardSelectionSystemEditor()
    {
        // Find existing instance or create one
        CardInspectorAutoCreate existing = FindObjectOfType<CardInspectorAutoCreate>();
        
        if (existing != null)
        {
            // Use existing instance
            existing.SetupCompleteCardInspectionSystem();
            UnityEditor.Selection.activeGameObject = existing.gameObject;
        }
        else
        {
            // Create new instance
            GameObject autoCreator = new GameObject("CardInspectorAutoCreate");
            CardInspectorAutoCreate creator = autoCreator.AddComponent<CardInspectorAutoCreate>();
            creator.SetupCompleteCardInspectionSystem();
            
            UnityEditor.Selection.activeGameObject = autoCreator;
        }
        
        Debug.Log("CardInspectorAutoCreate: Setup initiated from Unity Editor menu");
    }
    
    /// <summary>
    /// Unity Editor Menu: System Status Check
    /// </summary>
    [UnityEditor.MenuItem("Tools/Card System/Check Selection System Status", false, 2)]
    static void CheckSystemStatusEditor()
    {
        Debug.Log("=== Card Selection System Status Check ===");
        
        // Check for CardInspectorAutoCreate
        CardInspectorAutoCreate creator = FindObjectOfType<CardInspectorAutoCreate>();
        Debug.Log($"CardInspectorAutoCreate: {(creator != null ? "FOUND" : "MISSING")}");
        
        // Check for HandManager
        HandManager handManager = FindObjectOfType<HandManager>();
        Debug.Log($"HandManager: {(handManager != null ? "FOUND" : "MISSING")}");
        
        // Check for Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        Debug.Log($"Canvas: {(canvas != null ? "FOUND" : "MISSING")}");
        
        // Check for EventSystem
        UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        Debug.Log($"EventSystem: {(eventSystem != null ? "FOUND" : "MISSING")}");
        
        // Check for Inspector Panel
        GameObject inspectorPanel = GameObject.Find("CardInspectorPanel");
        Debug.Log($"CardInspectorPanel: {(inspectorPanel != null ? "FOUND" : "MISSING")}");
        
        if (creator != null)
        {
            creator.DebugListAllComponents();
        }
        
        // Provide recommendations
        if (handManager == null)
        {
            Debug.LogWarning("RECOMMENDATION: Create a HandManager in your scene first");
        }
        if (creator == null)
        {
            Debug.LogWarning("RECOMMENDATION: Run 'Setup Card Selection System' from the menu");
        }
        if (inspectorPanel == null && creator != null)
        {
            Debug.LogWarning("RECOMMENDATION: Inspector panel missing, try running setup again");
        }
    }
    
    /// <summary>
    /// Unity Editor Menu: Clear System
    /// </summary>
    [UnityEditor.MenuItem("Tools/Card System/Clear Selection System", false, 3)]
    static void ClearSystemEditor()
    {
        CardInspectorAutoCreate existing = FindObjectOfType<CardInspectorAutoCreate>();
        if (existing != null)
        {
            existing.ClearCardInspectionSystem();
            DestroyImmediate(existing.gameObject);
            Debug.Log("CardInspectorAutoCreate: System cleared and component removed");
        }
        else
        {
            Debug.Log("CardInspectorAutoCreate: No system found to clear");
        }
    }
    #endif
}
 
