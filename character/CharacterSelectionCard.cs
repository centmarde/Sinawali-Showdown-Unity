using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CharacterSelectionCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    /// <summary>
    /// The larger portrait size used for character selection cards.
    /// Both the portrait and the card dimensions are based on this size.
    /// </summary>
    private static readonly Vector2 LargerPortraitSize = new Vector2(240f, 360f);
    
    [Header("UI Elements")]
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image characterPortrait;
    [SerializeField] private Image selectionBorder;
    
    [Header("Card Data")]
    [SerializeField] private CharacterData characterData;
    
    [Header("Card Settings")]
    [SerializeField] private float cardWidth = LargerPortraitSize.x;
    [SerializeField] private float cardHeight = LargerPortraitSize.y;
    
    private CharacterSelectionManager selectionManager;
    private Color normalColor;
    private Color hoverColor;
    private Color selectedColor;
    private bool isSelected = false;
    private bool isHovered = false;
    
    /// <summary>
    /// Creates and initializes the card UI. Prefers manual inspector attachment,
    /// but will auto-create missing UI elements as a fallback.
    /// </summary>
    /// <returns>True if UI was successfully created or initialized.</returns>
    [ContextMenu("Create Card UI")]
    public bool CreateCardUI()
    {
        bool usedAutoCreate = false;
        
        // Auto-create missing UI elements as fallback
        if (cardBackground == null)
        {
            cardBackground = CreateCardBackground();
            usedAutoCreate = true;
        }
        
        if (characterPortrait == null)
        {
            characterPortrait = CreateCharacterPortrait();
            usedAutoCreate = true;
        }
        
        if (selectionBorder == null)
        {
            selectionBorder = CreateSelectionBorder();
            usedAutoCreate = true;
        }
        
        // Initialize card UI
        InitializeCardUI();
        
        if (usedAutoCreate)
        {
            Debug.Log($"CreateCardUI: Auto-created missing UI elements on {gameObject.name}. " +
                     "For better control, consider manually assigning UI elements in the inspector.");
        }
        else
        {
            Debug.Log($"CreateCardUI: Card UI successfully initialized using inspector references on {gameObject.name}");
        }
        
        return true;
    }
    
    /// <summary>
    /// Auto-creates the card background Image component as a fallback.
    /// </summary>
    private Image CreateCardBackground()
    {
        GameObject backgroundObj = new GameObject("Card Background");
        backgroundObj.transform.SetParent(transform);
        
        RectTransform rect = backgroundObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        Image bgImage = backgroundObj.AddComponent<Image>();
        bgImage.color = new Color(normalColor.r, normalColor.g, normalColor.b, 1f); // Ensure full opacity
        bgImage.raycastTarget = true;
        
        // Add rounded corners effect using a simple border
        Outline bgOutline = backgroundObj.AddComponent<Outline>();
        bgOutline.effectColor = new Color(0.3f, 0.3f, 0.4f, 0.8f);
        bgOutline.effectDistance = new Vector2(1, 1);
        
        return bgImage;
    }
    
    /// <summary>
    /// Auto-creates the character portrait Image component as a fallback.
    /// </summary>
    private Image CreateCharacterPortrait()
    {
        GameObject portraitObj = new GameObject("Character Portrait");
        portraitObj.transform.SetParent(transform);
        
        RectTransform rect = portraitObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = LargerPortraitSize;
        rect.anchoredPosition = new Vector2(0, 15f);
        
        Image portraitImage = portraitObj.AddComponent<Image>();
        portraitImage.color = Color.white;
        
        // Add a subtle border
        Outline portraitOutline = portraitObj.AddComponent<Outline>();
        portraitOutline.effectColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        portraitOutline.effectDistance = new Vector2(2, 2);
        
        return portraitImage;
    }
    
    /// <summary>
    /// Auto-creates the selection border Image component as a fallback.
    /// The border size matches the LargerPortraitSize to ensure consistent dimensions.
    /// </summary>
    private Image CreateSelectionBorder()
    {
        GameObject borderObj = new GameObject("Selection Border");
        borderObj.transform.SetParent(transform);
        
        RectTransform rect = borderObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(260f, 400f); // Match LargerPortraitSize for consistent border size
        rect.anchoredPosition = new Vector2(0f, 10f);
        
        Image borderImage = borderObj.AddComponent<Image>();
        // Set initial color - will be updated by UpdateVisualState
        borderImage.color = new Color(selectedColor.r, selectedColor.g, selectedColor.b, 0f);
        borderImage.raycastTarget = false;
        
        return borderImage;
    }
    
    /// <summary>
    /// Initializes the card UI elements with proper colors and settings.
    /// Called by CreateCardUI() after validation passes.
    /// </summary>
    private void InitializeCardUI()
    {
        // Set up RectTransform for card sizing
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }
        rectTransform.sizeDelta = new Vector2(cardWidth, cardHeight);
        
        // Initialize card background color
        if (cardBackground != null)
        {
            cardBackground.color = normalColor;
        }
        
        // Initialize selection border (hidden by default)
        if (selectionBorder != null)
        {
            selectionBorder.color = Color.clear;
        }
        
        UpdateCardDisplay();
    }
    
    public void Setup(CharacterData character, CharacterSelectionManager manager, Color normal, Color hover, Color selected)
    {
        characterData = character;
        selectionManager = manager;
        normalColor = normal;
        hoverColor = hover;
        selectedColor = selected;
        
        // Use CreateCardUI which validates inspector references
        if (!CreateCardUI())
        {
            Debug.LogWarning($"CharacterSelectionCard.Setup: Card UI not properly configured on {gameObject.name}");
            return;
        }
    }
    
    private void UpdateCardDisplay()
    {
        if (characterData == null) return;
        
        // Update character portrait
        if (characterPortrait != null)
        {
            if (characterData.characterPortrait != null)
            {
                characterPortrait.sprite = characterData.characterPortrait;
                characterPortrait.color = Color.white;
            }
            else
            {
                // Default portrait (colored square)
                characterPortrait.sprite = null;
                characterPortrait.color = GetCharacterColor(characterData.characterName);
            }
        }
    }
    
    private Color GetCharacterColor(string characterName)
    {
        // Generate a consistent color based on character name
        int hash = characterName.GetHashCode();
        Random.State oldState = Random.state;
        Random.InitState(hash);
        
        Color color = new Color(
            Random.Range(0.3f, 0.9f),
            Random.Range(0.3f, 0.9f),
            Random.Range(0.3f, 0.9f),
            1f
        );
        
        Random.state = oldState;
        return color;
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisualState();
    }
    
    private void UpdateVisualState()
    {
        Color targetColor;
        
        if (isSelected)
        {
            targetColor = selectedColor;
        }
        else if (isHovered)
        {
            targetColor = hoverColor;
        }
        else
        {
            targetColor = normalColor;
        }
        
        if (cardBackground != null)
        {
            cardBackground.color = targetColor;
        }
        
        // Update selection border visibility
        if (selectionBorder != null)
        {
            selectionBorder.color = isSelected ? new Color(selectedColor.r, selectedColor.g, selectedColor.b, 0.3f) : Color.clear;
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateVisualState();
        
        // Scale effect on hover
        if (!isSelected)
        {
            transform.localScale = Vector3.one * 1.05f;
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateVisualState();
        
        // Reset scale
        if (!isSelected)
        {
            transform.localScale = Vector3.one;
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (selectionManager != null && characterData != null)
        {
            selectionManager.SelectCharacter(characterData, this);
            
            // Selection feedback
            transform.localScale = Vector3.one * 1.1f;
            
            // Return to normal scale after a short delay
            if (Application.isPlaying)
            {
                StartCoroutine(ReturnToNormalScale());
            }
        }
    }
    
    private System.Collections.IEnumerator ReturnToNormalScale()
    {
        yield return new WaitForSeconds(0.1f);
        
        float elapsed = 0f;
        float duration = 0.2f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = Vector3.one;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            // Smooth easing
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            transform.localScale = Vector3.Lerp(startScale, targetScale, easedProgress);
            yield return null;
        }
        
        transform.localScale = targetScale;
    }
}