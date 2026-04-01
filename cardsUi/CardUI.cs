using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

/// <summary>
/// UI component for displaying card data
/// Handles the visual representation of CardData on UI elements
/// </summary>
[RequireComponent(typeof(Image))]
public class CardUI : MonoBehaviour
{
    [Header("Card Data")]
    [SerializeField] private CardData cardData;
    
    [Header("UI References - Auto-detected")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image cardImage;
    [SerializeField] private Button cardButton;
    
    [Header("Display Settings")]
    [SerializeField] private bool autoDetectUIElements = true;
    [SerializeField] private bool showEffects = true;
    [SerializeField] private bool showRarity = true;
    
    [Header("Animation Settings")]
    [SerializeField] private bool enableHoverEffects = true;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float animationDuration = 0.2f;
    
    private Vector3 originalScale;
    private bool isHovered = false;

    void Awake()
    {
        if (autoDetectUIElements)
        {
            AutoDetectUIElements();
        }
        
        originalScale = transform.localScale;
        
        if (enableHoverEffects && cardButton != null)
        {
            SetupHoverEffects();
        }
    }

    /// <summary>
    /// Automatically finds UI elements in the card hierarchy
    /// </summary>
    private void AutoDetectUIElements()
    {
        // Get main components
        cardBackground = GetComponent<Image>();
        cardButton = GetComponent<Button>();
        
        // Find text components by name
        titleText = FindChildComponent<TextMeshProUGUI>("Title");
        descriptionText = FindChildComponent<TextMeshProUGUI>("Description");
        statsText = FindChildComponent<TextMeshProUGUI>("Stats");
        typeText = FindChildComponent<TextMeshProUGUI>("Type");
        
        // Find image component for card artwork
        cardImage = FindChildComponent<Image>("CardImage");
        if (cardImage == null)
            cardImage = FindChildComponent<Image>("Artwork");
    }

    /// <summary>
    /// Helper method to find child components by name
    /// </summary>
    private T FindChildComponent<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        return child?.GetComponent<T>();
    }

    /// <summary>
    /// Sets up hover effects for the card
    /// </summary>
    private void SetupHoverEffects()
    {
        if (cardButton != null)
        {
            // Add event triggers for hover effects
            UnityEngine.EventSystems.EventTrigger trigger = cardButton.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null)
            {
                trigger = cardButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            }

            // Hover enter
            UnityEngine.EventSystems.EventTrigger.Entry hoverEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            hoverEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            hoverEnter.callback.AddListener((eventData) => OnCardHoverEnter());
            trigger.triggers.Add(hoverEnter);

            // Hover exit
            UnityEngine.EventSystems.EventTrigger.Entry hoverExit = new UnityEngine.EventSystems.EventTrigger.Entry();
            hoverExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            hoverExit.callback.AddListener((eventData) => OnCardHoverExit());
            trigger.triggers.Add(hoverExit);

            // Click
            cardButton.onClick.AddListener(OnCardClicked);
        }
    }

    /// <summary>
    /// Sets up the card UI with the provided card data
    /// </summary>
    public void SetupCard(CardData data)
    {
        cardData = data;
        
        if (cardData == null)
        {
            Debug.LogError("Card data is null!");
            return;
        }
        
        UpdateCardDisplay();
    }

    /// <summary>
    /// Updates all UI elements with current card data
    /// </summary>
    public void UpdateCardDisplay()
    {
        if (cardData == null) return;
        
        // Update title
        if (titleText != null)
        {
            titleText.text = cardData.Title;
            
            // Add rarity indicator if enabled
            if (showRarity && cardData.IsLegendary)
            {
                titleText.text += " ★";
            }
        }
        
        // Update description
        if (descriptionText != null)
        {
            string description = cardData.Description;
            
            // Add effects to description if enabled
            if (showEffects && cardData.Effects.Count > 0)
            {
                StringBuilder effectsText = new StringBuilder();
                effectsText.AppendLine(description);
                effectsText.AppendLine();
                effectsText.Append("Effects: ");
                
                for (int i = 0; i < cardData.Effects.Count; i++)
                {
                    effectsText.Append(cardData.Effects[i].ToString());
                    if (i < cardData.Effects.Count - 1)
                        effectsText.Append(", ");
                }
                
                description = effectsText.ToString();
            }
            
            descriptionText.text = description;
        }
        
        // Update stats
        if (statsText != null)
        {
            statsText.text = $"DMG: {cardData.Damage} | MANA: {cardData.ManaDeduction}";
        }
        
        // Update type
        if (typeText != null)
        {
            typeText.text = cardData.Type.ToString().ToUpper();
            
            // Color the type text based on rarity
            if (showRarity)
            {
                typeText.color = GetRarityColor(cardData.Rarity);
            }
        }
        
        // Update card image if sprite is provided
        if (cardImage != null && cardData.CardSprite != null)
        {
            cardImage.sprite = cardData.CardSprite;
            cardImage.enabled = true;
        }
        else if (cardImage != null)
        {
            cardImage.enabled = false;
        }
        
        // Set card name for easier debugging
        gameObject.name = $"Card - {cardData.Title}";
    }

    /// <summary>
    /// Returns color based on card rarity
    /// </summary>
    private Color GetRarityColor(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => Color.white,
            Rarity.Uncommon => Color.green,
            Rarity.Rare => Color.blue,
            Rarity.Epic => new Color(0.6f, 0f, 1f), // Purple
            Rarity.Legendary => new Color(1f, 0.5f, 0f), // Orange
            Rarity.Mythic => Color.red,
            _ => Color.white
        };
    }

    /// <summary>
    /// Called when card is hovered
    /// </summary>
    private void OnCardHoverEnter()
    {
        if (!enableHoverEffects || isHovered) return;
        
        isHovered = true;
        StartCoroutine(AnimateScale(originalScale * hoverScale, animationDuration));
        
        // Optional: Show card details in a tooltip
        Debug.Log($"Hovered over: {cardData?.Title}");
    }

    /// <summary>
    /// Called when card hover ends
    /// </summary>
    private void OnCardHoverExit()
    {
        if (!enableHoverEffects || !isHovered) return;
        
        isHovered = false;
        StartCoroutine(AnimateScale(originalScale, animationDuration));
    }

    /// <summary>
    /// Called when card is clicked
    /// </summary>
    private void OnCardClicked()
    {
        if (cardData == null) return;
        
        Debug.Log($"Clicked card: {cardData.Title} - {cardData.Description}");
        
        // Add click animation
        StartCoroutine(AnimateClickEffect());
        
        // You can add custom card click logic here
        // For example: Play the card, show detailed view, etc.
        OnCardPlayed();
    }

    /// <summary>
    /// Override this method to add custom card play logic
    /// </summary>
    protected virtual void OnCardPlayed()
    {
        Debug.Log($"Playing card: {cardData.Title}");
        Debug.Log($"Card effects: {string.Join(", ", cardData.Effects)}");
        Debug.Log($"Damage: {cardData.Damage}, Mana Cost: {cardData.ManaDeduction}");
        
        // Example: Remove card from hand after playing
        // Destroy(gameObject);
    }

    /// <summary>
    /// Validates the card UI setup
    /// </summary>
    public bool ValidateCardUI()
    {
        bool isValid = true;
        
        if (cardData == null)
        {
            Debug.LogWarning($"Card UI '{gameObject.name}' has no card data assigned.");
            isValid = false;
        }
        
        if (titleText == null)
        {
            Debug.LogWarning($"Card UI '{gameObject.name}' is missing title text component.");
            isValid = false;
        }
        
        return isValid;
    }

    /// <summary>
    /// Editor utility to refresh the card display
    /// </summary>
    [ContextMenu("Refresh Card Display")]
    public void RefreshCardDisplay()
    {
        if (autoDetectUIElements)
        {
            AutoDetectUIElements();
        }
        UpdateCardDisplay();
    }

    // Public properties for external access
    public CardData CardData => cardData;
    public bool IsHovered => isHovered;
    
    #if UNITY_EDITOR
    /// <summary>
    /// Editor validation
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        
        // Auto-refresh in editor when card data changes
        if (cardData != null && autoDetectUIElements)
        {
            // Delay the refresh to next frame to avoid editor issues
            UnityEditor.EditorApplication.delayCall += () => {
                if (this != null) RefreshCardDisplay();
            };
        }
    }
    #endif

    /// <summary>
    /// Animates scale change using Unity's built-in animation
    /// </summary>
    private System.Collections.IEnumerator AnimateScale(Vector3 targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Ease out back animation curve
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            
            transform.localScale = Vector3.Lerp(startScale, targetScale, easedProgress);
            yield return null;
        }
        
        transform.localScale = targetScale;
    }
    
    /// <summary>
    /// Animates click effect with scale bounce
    /// </summary>
    private System.Collections.IEnumerator AnimateClickEffect()
    {
        // Scale down quickly
        yield return StartCoroutine(AnimateScale(originalScale * 0.9f, 0.1f));
        
        // Scale back up
        yield return StartCoroutine(AnimateScale(originalScale, 0.1f));
    }
}