using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Auto-creates a complete Yu-Gi-Oh style card UI system with canvas, containers, and layout
/// Designed for bottom-center positioning from camera view
/// </summary>
public class CardUIAutoCreate : MonoBehaviour
{
    [Header("UI Configuration")]
    public Canvas cardCanvas;
    public GameObject handContainer;
    public GameObject fieldContainer;
    public GameObject deckContainer;
    public GameObject graveyardContainer;
    
    [Header("Yu-Gi-Oh Style Settings")]
    [SerializeField] private Vector2 handCardSize = new Vector2(120, 180);
    [SerializeField] private Vector2 fieldCardSize = new Vector2(140, 210);
    [SerializeField] private float cardSpacing = 15f;
    [SerializeField] private Color handAreaColor = new Color(0.1f, 0.1f, 0.4f, 0.95f);
    [SerializeField] private Color fieldAreaColor = new Color(0.2f, 0.4f, 0.1f, 0.95f);
    
    [Header("Layout Settings")]
    [SerializeField] private float handYPosition = 120f;  // Distance from bottom
    [SerializeField] private float fieldYPosition = 320f; // Distance from bottom
    [SerializeField] private int maxHandCards = 7;
    [SerializeField] private int maxFieldCards = 5;

    private static CardUIAutoCreate instance;
    public static CardUIAutoCreate Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CardUIAutoCreate>();
                if (instance == null)
                {
                    GameObject go = new GameObject("CardUIAutoCreate");
                    instance = go.AddComponent<CardUIAutoCreate>();
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Creates the complete Yu-Gi-Oh style UI system
    /// </summary>
    [ContextMenu("Create Yu-Gi-Oh UI System")]
    public void CreateCompleteUISystem()
    {
        Debug.Log("Creating Yu-Gi-Oh style card UI system...");
        
        CreateCanvas();
        CreateHandArea();
        CreateFieldArea();
        CreateDeckAndGraveyardAreas();
        SetupLayoutComponents();
        
        // Auto-create 3 sample cards for immediate visibility (only if hand is empty)
        if (handContainer.transform.childCount <= 1) // Only background image exists
        {
            for (int i = 0; i < 3; i++)
            {
                CreateSampleCard();
            }
            Debug.Log("Complete Yu-Gi-Oh UI system created successfully with 3 sample cards!");
        }
        else
        {
            Debug.Log("Complete Yu-Gi-Oh UI system created successfully! (Sample cards already exist)");
        }
    }

    /// <summary>
    /// Creates the main canvas for the card UI
    /// </summary>
    private void CreateCanvas()
    {
        if (cardCanvas != null)
        {
            Debug.Log("Canvas already exists, skipping creation.");
            return;
        }

        GameObject canvasGO = new GameObject("Card UI Canvas");
        canvasGO.layer = LayerMask.NameToLayer("UI");
        
        cardCanvas = canvasGO.AddComponent<Canvas>();
        cardCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cardCanvas.sortingOrder = 100;
        
        // Add Canvas Scaler for responsive design
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        // Add GraphicRaycaster for UI interactions
        canvasGO.AddComponent<GraphicRaycaster>();
        
        Debug.Log("Canvas created with screen space overlay mode.");
    }

    /// <summary>
    /// Creates the hand area at the bottom center of the screen
    /// </summary>
    private void CreateHandArea()
    {
        if (handContainer != null)
        {
            Debug.Log("Hand container already exists, skipping creation.");
            return;
        }

        handContainer = new GameObject("Hand Container");
        handContainer.transform.SetParent(cardCanvas.transform, false);
        
        // Setup RectTransform for bottom center positioning
        RectTransform handRect = handContainer.AddComponent<RectTransform>();
        handRect.anchorMin = new Vector2(0.5f, 0f);
        handRect.anchorMax = new Vector2(0.5f, 0f);
        handRect.pivot = new Vector2(0.5f, 0f);
        handRect.anchoredPosition = new Vector2(0, handYPosition);
        handRect.sizeDelta = new Vector2(800f, handCardSize.y + 40f);

        // Add background image
        Image handBG = handContainer.AddComponent<Image>();
        handBG.color = handAreaColor;
        handBG.raycastTarget = true;

        // Add horizontal layout group for automatic card arrangement
        HorizontalLayoutGroup handLayout = handContainer.AddComponent<HorizontalLayoutGroup>();
        handLayout.childAlignment = TextAnchor.MiddleCenter;
        handLayout.spacing = cardSpacing;
        handLayout.childForceExpandWidth = false;
        handLayout.childForceExpandHeight = false;
        handLayout.childControlWidth = true;
        handLayout.childControlHeight = true;

        // Add content size fitter for horizontal only (let cards arrange horizontally)
        ContentSizeFitter handFitter = handContainer.AddComponent<ContentSizeFitter>();
        handFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        handFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        
        // Add minimum layout element to ensure visibility even when empty
        LayoutElement handLayoutElement = handContainer.AddComponent<LayoutElement>();
        handLayoutElement.minWidth = 300f;
        handLayoutElement.minHeight = handCardSize.y + 40f;

        Debug.Log("Hand area created at bottom center position.");
    }

    /// <summary>
    /// Creates the field area above the hand
    /// </summary>
    private void CreateFieldArea()
    {
        if (fieldContainer != null)
        {
            Debug.Log("Field container already exists, skipping creation.");
            return;
        }

        fieldContainer = new GameObject("Field Container");
        fieldContainer.transform.SetParent(cardCanvas.transform, false);
        
        // Setup RectTransform for positioning above hand (still anchored to bottom)
        RectTransform fieldRect = fieldContainer.AddComponent<RectTransform>();
        fieldRect.anchorMin = new Vector2(0.5f, 0f);
        fieldRect.anchorMax = new Vector2(0.5f, 0f);
        fieldRect.pivot = new Vector2(0.5f, 0f);
        fieldRect.anchoredPosition = new Vector2(0, fieldYPosition);
        fieldRect.sizeDelta = new Vector2(900f, fieldCardSize.y + 40f);

        // Add background image
        Image fieldBG = fieldContainer.AddComponent<Image>();
        fieldBG.color = fieldAreaColor;
        fieldBG.raycastTarget = true;

        // Add horizontal layout group
        HorizontalLayoutGroup fieldLayout = fieldContainer.AddComponent<HorizontalLayoutGroup>();
        fieldLayout.childAlignment = TextAnchor.MiddleCenter;
        fieldLayout.spacing = cardSpacing;
        fieldLayout.childForceExpandWidth = false;
        fieldLayout.childForceExpandHeight = false;
        fieldLayout.childControlWidth = true;
        fieldLayout.childControlHeight = true;

        // Add content size fitter for horizontal only
        ContentSizeFitter fieldFitter = fieldContainer.AddComponent<ContentSizeFitter>();
        fieldFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fieldFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        Debug.Log("Field area created above hand area.");
    }

    /// <summary>
    /// Creates deck and graveyard areas on the sides
    /// </summary>
    private void CreateDeckAndGraveyardAreas()
    {
        CreateDeckArea();
        CreateGraveyardArea();
    }

    private void CreateDeckArea()
    {
        if (deckContainer != null)
        {
            Debug.Log("Deck container already exists, skipping creation.");
            return;
        }

        deckContainer = new GameObject("Deck Container");
        deckContainer.transform.SetParent(cardCanvas.transform, false);
        
        // Setup RectTransform for right side positioning
        RectTransform deckRect = deckContainer.AddComponent<RectTransform>();
        deckRect.anchorMin = new Vector2(1f, 0f);
        deckRect.anchorMax = new Vector2(1f, 0f);
        deckRect.pivot = new Vector2(1f, 0f);
        deckRect.anchoredPosition = new Vector2(-20, handYPosition);
        deckRect.sizeDelta = new Vector2(handCardSize.x, handCardSize.y);

        // Add background image
        Image deckBG = deckContainer.AddComponent<Image>();
        deckBG.color = new Color(0.4f, 0.1f, 0.1f, 0.95f);
        deckBG.raycastTarget = true;

        // Add deck label
        CreateLabel(deckContainer, "DECK", new Vector2(0, -10));

        Debug.Log("Deck area created on right side.");
    }

    private void CreateGraveyardArea()
    {
        if (graveyardContainer != null)
        {
            Debug.Log("Graveyard container already exists, skipping creation.");
            return;
        }

        graveyardContainer = new GameObject("Graveyard Container");
        graveyardContainer.transform.SetParent(cardCanvas.transform, false);
        
        // Setup RectTransform for left side positioning
        RectTransform graveyardRect = graveyardContainer.AddComponent<RectTransform>();
        graveyardRect.anchorMin = new Vector2(0f, 0f);
        graveyardRect.anchorMax = new Vector2(0f, 0f);
        graveyardRect.pivot = new Vector2(0f, 0f);
        graveyardRect.anchoredPosition = new Vector2(20, handYPosition);
        graveyardRect.sizeDelta = new Vector2(handCardSize.x, handCardSize.y);

        // Add background image
        Image graveyardBG = graveyardContainer.AddComponent<Image>();
        graveyardBG.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        graveyardBG.raycastTarget = true;

        // Add graveyard label
        CreateLabel(graveyardContainer, "GRAVEYARD", new Vector2(0, -10));

        Debug.Log("Graveyard area created on left side.");
    }

    /// <summary>
    /// Creates a text label for UI areas
    /// </summary>
    private void CreateLabel(GameObject parent, string text, Vector2 position)
    {
        GameObject labelGO = new GameObject($"{text} Label");
        labelGO.transform.SetParent(parent.transform, false);
        
        RectTransform labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0f);
        labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = position;
        labelRect.sizeDelta = new Vector2(80, 20);
        
        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 12;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
    }

    /// <summary>
    /// Sets up additional layout components and configurations
    /// </summary>
    private void SetupLayoutComponents()
    {
        // Add event system if it doesn't exist
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("EventSystem created for UI interactions.");
        }

        // Configure canvas sorting and rendering
        if (cardCanvas != null)
        {
            cardCanvas.planeDistance = 1f;
            cardCanvas.sortingOrder = 100;
        }

        Debug.Log("Layout components setup completed.");
    }

    /// <summary>
    /// Creates enhanced sample cards with Yu-Gi-Oh style visuals for testing
    /// </summary>
    [ContextMenu("Create Sample Card")]
    public void CreateSampleCard()
    {
        if (handContainer == null)
        {
            CreateCompleteUISystem();
        }

        // Create different card types for variety
        string[] cardTypes = { "Monster", "Spell", "Trap" };
        string[] cardNames = { "Fire Dragon", "Lightning Strike", "Mirror Force" };
        Color[] cardColors = { 
            new Color(1f, 0.8f, 0.3f, 1f),    // Gold for Monster
            new Color(0.3f, 0.8f, 0.3f, 1f),  // Green for Spell
            new Color(0.8f, 0.3f, 0.8f, 1f)   // Purple for Trap
        };
        int[] attackValues = { 2400, 1200, 0 };
        int[] defenseValues = { 1800, 0, 0 };

        // Create a random card type
        int randomType = Random.Range(0, cardTypes.Length);
        
        GameObject cardGO = CreateCardVisuals(
            cardNames[randomType], 
            cardTypes[randomType], 
            cardColors[randomType],
            attackValues[randomType],
            defenseValues[randomType]
        );
        
        cardGO.transform.SetParent(handContainer.transform, false);
        
        Debug.Log($"Enhanced {cardTypes[randomType]} card '{cardNames[randomType]}' created!");
    }

    /// <summary>
    /// Creates a card with enhanced Yu-Gi-Oh style visuals
    /// </summary>
    private GameObject CreateCardVisuals(string cardName, string cardType, Color primaryColor, int attack, int defense)
    {
        GameObject cardGO = new GameObject($"{cardName} Card");
        
        // Setup card size
        RectTransform cardRect = cardGO.AddComponent<RectTransform>();
        cardRect.sizeDelta = handCardSize;
        
        // Ensure card has proper layout element for consistent sizing
        LayoutElement cardLayout = cardGO.AddComponent<LayoutElement>();
        cardLayout.preferredWidth = handCardSize.x;
        cardLayout.preferredHeight = handCardSize.y;
        cardLayout.flexibleWidth = 0;
        cardLayout.flexibleHeight = 0;
        
        // Main card background with gradient effect
        Image cardBG = cardGO.AddComponent<Image>();
        cardBG.color = primaryColor;
        
        // Add shadow effect
        GameObject shadowGO = new GameObject("Card Shadow");
        shadowGO.transform.SetParent(cardGO.transform, false);
        shadowGO.transform.SetAsFirstSibling(); // Behind main card
        
        RectTransform shadowRect = shadowGO.AddComponent<RectTransform>();
        shadowRect.anchorMin = Vector2.zero;
        shadowRect.anchorMax = Vector2.one;
        shadowRect.offsetMin = Vector2.zero;
        shadowRect.offsetMax = Vector2.zero;
        shadowRect.anchoredPosition = new Vector2(3, -3); // Offset for shadow effect
        
        Image shadowImage = shadowGO.AddComponent<Image>();
        shadowImage.color = new Color(0, 0, 0, 0.5f);
        
        // Card border with thicker frame
        GameObject borderGO = new GameObject("Card Border");
        borderGO.transform.SetParent(cardGO.transform, false);
        
        RectTransform borderRect = borderGO.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(2, 2);
        borderRect.offsetMax = new Vector2(-2, -2);
        
        Image border = borderGO.AddComponent<Image>();
        border.color = new Color(0.1f, 0.1f, 0.1f, 1f); // Dark border
        
        // Inner border for card type color coding
        GameObject innerBorderGO = new GameObject("Inner Border");
        innerBorderGO.transform.SetParent(cardGO.transform, false);
        
        RectTransform innerBorderRect = innerBorderGO.AddComponent<RectTransform>();
        innerBorderRect.anchorMin = Vector2.zero;
        innerBorderRect.anchorMax = Vector2.one;
        innerBorderRect.offsetMin = new Vector2(5, 5);
        innerBorderRect.offsetMax = new Vector2(-5, -5);
        
        Image innerBorder = innerBorderGO.AddComponent<Image>();
        innerBorder.color = primaryColor * 0.7f; // Darker shade of primary color
        
        // Artwork area (placeholder with gradient)
        GameObject artworkGO = new GameObject("Card Artwork");
        artworkGO.transform.SetParent(cardGO.transform, false);
        
        RectTransform artworkRect = artworkGO.AddComponent<RectTransform>();
        artworkRect.anchorMin = new Vector2(0.1f, 0.4f);
        artworkRect.anchorMax = new Vector2(0.9f, 0.85f);
        artworkRect.offsetMin = Vector2.zero;
        artworkRect.offsetMax = Vector2.zero;
        
        Image artwork = artworkGO.AddComponent<Image>();
        artwork.color = Color.Lerp(primaryColor, Color.white, 0.3f);
        
        // Card name with better styling
        GameObject nameGO = new GameObject("Card Name");
        nameGO.transform.SetParent(cardGO.transform, false);
        
        RectTransform nameRect = nameGO.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.05f, 0.87f);
        nameRect.anchorMax = new Vector2(0.95f, 0.97f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI nameText = nameGO.AddComponent<TextMeshProUGUI>();
        nameText.text = cardName;
        nameText.fontSize = 10f;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = Color.black;
        nameText.alignment = TextAlignmentOptions.Center;
        
        // Card type label
        GameObject typeGO = new GameObject("Card Type");
        typeGO.transform.SetParent(cardGO.transform, false);
        
        RectTransform typeRect = typeGO.AddComponent<RectTransform>();
        typeRect.anchorMin = new Vector2(0.05f, 0.32f);
        typeRect.anchorMax = new Vector2(0.95f, 0.38f);
        typeRect.offsetMin = Vector2.zero;
        typeRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI typeText = typeGO.AddComponent<TextMeshProUGUI>();
        typeText.text = $"[{cardType}]"; 
        typeText.fontSize = 8f;
        typeText.fontStyle = FontStyles.Italic;
        typeText.color = Color.black;
        typeText.alignment = TextAlignmentOptions.Center;
        
        // Stats display for Monster cards
        if (cardType == "Monster")
        {
            // Attack stat
            GameObject atkGO = new GameObject("ATK Display");
            atkGO.transform.SetParent(cardGO.transform, false);
            
            RectTransform atkRect = atkGO.AddComponent<RectTransform>();
            atkRect.anchorMin = new Vector2(0.05f, 0.05f);
            atkRect.anchorMax = new Vector2(0.45f, 0.15f);
            atkRect.offsetMin = Vector2.zero;
            atkRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI atkText = atkGO.AddComponent<TextMeshProUGUI>();
            atkText.text = $"ATK/{attack}";
            atkText.fontSize = 7f;
            atkText.fontStyle = FontStyles.Bold;
            atkText.color = new Color(0.8f, 0.1f, 0.1f, 1f); // Red for attack
            atkText.alignment = TextAlignmentOptions.Center;
            
            // Defense stat
            GameObject defGO = new GameObject("DEF Display");
            defGO.transform.SetParent(cardGO.transform, false);
            
            RectTransform defRect = defGO.AddComponent<RectTransform>();
            defRect.anchorMin = new Vector2(0.55f, 0.05f);
            defRect.anchorMax = new Vector2(0.95f, 0.15f);
            defRect.offsetMin = Vector2.zero;
            defRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI defText = defGO.AddComponent<TextMeshProUGUI>();
            defText.text = $"DEF/{defense}";
            defText.fontSize = 7f;
            defText.fontStyle = FontStyles.Bold;
            defText.color = new Color(0.1f, 0.1f, 0.8f, 1f); // Blue for defense
            defText.alignment = TextAlignmentOptions.Center;
            
            // Level stars (placeholder)
            GameObject starsGO = new GameObject("Level Stars");
            starsGO.transform.SetParent(cardGO.transform, false);
            
            RectTransform starsRect = starsGO.AddComponent<RectTransform>();
            starsRect.anchorMin = new Vector2(0.1f, 0.78f);
            starsRect.anchorMax = new Vector2(0.9f, 0.85f);
            starsRect.offsetMin = Vector2.zero;
            starsRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI starsText = starsGO.AddComponent<TextMeshProUGUI>();
            int level = Random.Range(1, 8);
            starsText.text = new string('★', level);
            starsText.fontSize = 8f;
            starsText.color = Color.yellow;
            starsText.alignment = TextAlignmentOptions.Center;
        }
        
        // Description area
        GameObject descGO = new GameObject("Card Description");
        descGO.transform.SetParent(cardGO.transform, false);
        
        RectTransform descRect = descGO.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.05f, 0.18f);
        descRect.anchorMax = new Vector2(0.95f, 0.30f);
        descRect.offsetMin = Vector2.zero;
        descRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI descText = descGO.AddComponent<TextMeshProUGUI>();
        descText.text = GetCardDescription(cardType);
        descText.fontSize = 6f;
        descText.color = Color.black;
        descText.alignment = TextAlignmentOptions.Center;
        descText.textWrappingMode = TextWrappingModes.Normal;
        
        // Add button component for interaction with enhanced feedback
        Button cardButton = cardGO.AddComponent<Button>();
        cardButton.onClick.AddListener(() => {
            Debug.Log($"Played {cardType} card: {cardName}!");
            // Add visual feedback with Unity's built-in animation
            StartCoroutine(AnimateCardClick(cardGO.transform));
        });
        
        return cardGO;
    }
    
    /// <summary>
    /// Animates card click feedback using Unity's built-in animation
    /// </summary>
    private System.Collections.IEnumerator AnimateCardClick(Transform cardTransform)
    {
        Vector3 originalScale = cardTransform.localScale;
        Vector3 enlargedScale = originalScale * 1.1f;
        
        // Scale up quickly
        float duration = 0.1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            cardTransform.localScale = Vector3.Lerp(originalScale, enlargedScale, progress);
            yield return null;
        }
        
        // Scale back down with bounce effect
        duration = 0.15f;
        elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            // Simple easeOutBounce approximation
            float bounceProgress = 1f - Mathf.Pow(1f - progress, 3f);
            cardTransform.localScale = Vector3.Lerp(enlargedScale, originalScale, bounceProgress);
            yield return null;
        }
        
        cardTransform.localScale = originalScale;
    }
    
    /// <summary>
    /// Gets appropriate description text for different card types
    /// </summary>
    private string GetCardDescription(string cardType)
    {
        switch (cardType)
        {
            case "Monster":
                string[] monsterDescs = {
                    "A fierce dragon that breathes scorching flames.",
                    "An ancient warrior with unmatched strength.",
                    "A mystical creature from distant lands."
                };
                return monsterDescs[Random.Range(0, monsterDescs.Length)];
                
            case "Spell":
                string[] spellDescs = {
                    "Unleash devastating magical energy.",
                    "Channel the power of the elements.",
                    "Ancient magic to aid your monsters."
                };
                return spellDescs[Random.Range(0, spellDescs.Length)];
                
            case "Trap":
                string[] trapDescs = {
                    "Activate when opponent attacks.",
                    "Counter your enemy's strategy.",
                    "A hidden surprise for the unwary."
                };
                return trapDescs[Random.Range(0, trapDescs.Length)];
                
            default:
                return "A mysterious card with unknown power.";
        }
    }

    /// <summary>
    /// Clears all cards from the hand area
    /// </summary>
    [ContextMenu("Clear All Cards")]
    public void ClearAllCards()
    {
        if (handContainer != null)
        {
            // Keep the background image, remove only the cards
            for (int i = handContainer.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = handContainer.transform.GetChild(i);
                if (child.name.Contains("Card"))
                {
                    DestroyImmediate(child.gameObject);
                }
            }
            Debug.Log("All cards cleared from hand area.");
        }
    }

    /// <summary>
    /// Clears all created UI elements
    /// </summary>
    [ContextMenu("Clear UI System")]
    public void ClearUISystem()
    {
        if (cardCanvas != null)
        {
            DestroyImmediate(cardCanvas.gameObject);
            cardCanvas = null;
        }
        
        handContainer = null;
        fieldContainer = null;
        deckContainer = null;
        graveyardContainer = null;
        
        Debug.Log("UI system cleared.");
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor menu item for quick access
    /// </summary>
    [MenuItem("Tools/Card Game/Create Yu-Gi-Oh UI System")]
    public static void CreateUISystemMenuItem()
    {
        CardUIAutoCreate autoCreator = Instance;
        autoCreator.CreateCompleteUISystem();
        
        // Select the created canvas in hierarchy
        if (autoCreator.cardCanvas != null)
        {
            Selection.activeGameObject = autoCreator.cardCanvas.gameObject;
        }
    }

    [MenuItem("Tools/Card Game/Create Sample Card")]
    public static void CreateSampleCardMenuItem()
    {
        Instance.CreateSampleCard();
    }
    
    [MenuItem("Tools/Card Game/Clear All Cards")]
    public static void ClearCardsMenuItem()
    {
        Instance.ClearAllCards();
    }

    [MenuItem("Tools/Card Game/Clear UI System")]
    public static void ClearUISystemMenuItem()
    {
        Instance.ClearUISystem();
    }
#endif
}
