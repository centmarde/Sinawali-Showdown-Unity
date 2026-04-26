using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Displays the AI's selected card choice on screen
/// Attach this to a UI panel that shows the AI's card decision
/// </summary>
public class AICardDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CardFetcherEnemy aiCardFetcher;
    
    [Header("Display UI Elements")]
    [SerializeField] private Image selectedCardBackground;
    [SerializeField] private Image selectedCardBorder;
    [SerializeField] private Image selectedCardArtwork;
    [SerializeField] private TextMeshProUGUI selectedCardNameText;
    [SerializeField] private TextMeshProUGUI selectedCardTypeText;
    [SerializeField] private TextMeshProUGUI selectedCardDescriptionText;
    [SerializeField] private TextMeshProUGUI selectedDamageText;
    [SerializeField] private TextMeshProUGUI selectedManaText;
    [SerializeField] private CanvasGroup panelCanvasGroup; // For fade in/out
    
    [Header("Animation Settings")]
    [SerializeField] private bool autoFadeIn = true;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Display Settings")]
    [SerializeField] private Color attackCardColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color buffCardColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color healCardColor = new Color(0.8f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color specialCardColor = new Color(0.6f, 0.2f, 0.8f, 1f);
    
    [Header("Image Loading Settings")]
    [SerializeField] private bool prioritizeCardData = true;
    [SerializeField] private Sprite defaultCardSprite = null;
    [SerializeField] private bool loadFromUrlIfSpriteNull = true;
    [SerializeField] private float imageLoadTimeout = 5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private Coroutine fadeRoutine;
    
    void OnEnable()
    {
        if (aiCardFetcher == null)
        {
            aiCardFetcher = FindObjectOfType<CardFetcherEnemy>();
        }
        
        if (aiCardFetcher != null)
        {
            aiCardFetcher.OnAICardSelected -= DisplaySelectedCard;
            aiCardFetcher.OnAICardSelected += DisplaySelectedCard;
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning("[AICardDisplay] No CardFetcherEnemy found in scene!", this);
        }
        
        // Initialize panel as hidden
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
        }
    }
    
    void OnDisable()
    {
        if (aiCardFetcher != null)
        {
            aiCardFetcher.OnAICardSelected -= DisplaySelectedCard;
        }
        
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
    }
    
    /// <summary>
    /// Called when AI selects a card
    /// </summary>
    private void DisplaySelectedCard(CardData card)
    {
        if (card == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("[AICardDisplay] Received null card!");
            }
            return;
        }
        
        UpdateCardDisplay(card);
        
        if (autoFadeIn)
        {
            FadeInPanel();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[AICardDisplay] Displaying AI selected card: {card.Title}");
        }
    }
    
    /// <summary>
    /// Updates all UI elements with the card data
    /// </summary>
    void UpdateCardDisplay(CardData card)
    {
        if (card == null) return;
        
        // Update text fields
        if (selectedCardNameText != null)
            selectedCardNameText.text = card.Title;
        
        if (selectedCardTypeText != null)
            selectedCardTypeText.text = card.Type.ToString();
        
        if (selectedCardDescriptionText != null)
            selectedCardDescriptionText.text = card.Description;
        
        if (selectedDamageText != null)
            selectedDamageText.text = card.Damage.ToString();
        
        if (selectedManaText != null)
            selectedManaText.text = card.ManaDeduction.ToString();
        
        // Update background color based on card type
        if (selectedCardBackground != null)
            selectedCardBackground.color = GetCardTypeColor(card.Type);
        
        // Update card artwork
        if (selectedCardArtwork != null)
        {
            UpdateCardArtwork(card);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[AICardDisplay] Updated display for: {card.Title} ({card.Type})");
        }
    }
    
    /// <summary>
    /// Updates card artwork with priority system: CardSprite -> ImageUrl -> Default
    /// </summary>
    void UpdateCardArtwork(CardData card)
    {
        if (selectedCardArtwork == null) return;
        
        if (!prioritizeCardData)
        {
            if (card.CardSprite != null)
            {
                selectedCardArtwork.sprite = card.CardSprite;
                return;
            }
        }
        
        // Priority 1: CardData sprite
        if (card.CardSprite != null)
        {
            selectedCardArtwork.sprite = card.CardSprite;
            if (showDebugInfo)
            {
                Debug.Log($"[AICardDisplay] Loaded sprite from CardData for {card.Title}");
            }
            return;
        }
        
        // Priority 2: ImageUrl
        if (!string.IsNullOrEmpty(card.ImageUrl) && loadFromUrlIfSpriteNull)
        {
            StartCoroutine(LoadImageFromUrl(card.ImageUrl, card.Title));
            if (showDebugInfo)
            {
                Debug.Log($"[AICardDisplay] Loading image from URL for {card.Title}");
            }
            return;
        }
        
        // Priority 3: Default sprite
        if (defaultCardSprite != null)
        {
            selectedCardArtwork.sprite = defaultCardSprite;
            if (showDebugInfo)
            {
                Debug.Log($"[AICardDisplay] Using default sprite for {card.Title}");
            }
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"[AICardDisplay] No artwork available for {card.Title}");
            }
        }
    }
    
    /// <summary>
    /// Loads an image from URL and applies it to the card artwork
    /// </summary>
    IEnumerator LoadImageFromUrl(string url, string cardTitle)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            request.timeout = (int)imageLoadTimeout;
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success && selectedCardArtwork != null)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                if (texture != null)
                {
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                    selectedCardArtwork.sprite = sprite;
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"[AICardDisplay] Successfully loaded image from URL for {cardTitle}");
                    }
                }
            }
            else
            {
                HandleImageLoadFailure(cardTitle, request.error);
            }
        }
    }
    
    /// <summary>
    /// Handles image loading failures with fallback behavior
    /// </summary>
    void HandleImageLoadFailure(string cardTitle, string error)
    {
        if (showDebugInfo)
        {
            Debug.LogWarning($"[AICardDisplay] Failed to load image for {cardTitle}: {error}");
        }
        
        if (selectedCardArtwork != null && defaultCardSprite != null)
        {
            selectedCardArtwork.sprite = defaultCardSprite;
            if (showDebugInfo)
            {
                Debug.Log($"[AICardDisplay] Using default sprite fallback for {cardTitle}");
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
    /// Fades in the display panel
    /// </summary>
    [ContextMenu("Fade In")]
    public void FadeInPanel()
    {
        if (panelCanvasGroup == null) return;
        
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }
        
        fadeRoutine = StartCoroutine(FadeRoutine(0f, 1f, fadeDuration));
    }
    
    /// <summary>
    /// Fades out the display panel
    /// </summary>
    [ContextMenu("Fade Out")]
    public void FadeOutPanel()
    {
        if (panelCanvasGroup == null) return;
        
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }
        
        fadeRoutine = StartCoroutine(FadeRoutine(1f, 0f, fadeDuration));
    }
    
    /// <summary>
    /// Coroutine for fading the panel in/out
    /// </summary>
    IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = fadeCurve.Evaluate(t);
            
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            }
            
            yield return null;
        }
        
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = endAlpha;
        }
    }
    
    /// <summary>
    /// Hides the AI card display
    /// </summary>
    public void HideDisplay()
    {
        FadeOutPanel();
    }
    
    /// <summary>
    /// Shows the AI card display with a delay
    /// </summary>
    public void ShowDisplayWithDelay(float delaySeconds)
    {
        StartCoroutine(ShowDisplayDelayed(delaySeconds));
    }
    
    IEnumerator ShowDisplayDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        FadeInPanel();
    }
    
    #if UNITY_EDITOR
    [ContextMenu("Test Display")]
    public void TestDisplay()
    {
        if (aiCardFetcher == null)
        {
            aiCardFetcher = FindObjectOfType<CardFetcherEnemy>();
        }
        
        if (aiCardFetcher != null)
        {
            CardData testCard = aiCardFetcher.GetSelectedCard();
            if (testCard == null)
            {
                Debug.LogWarning("[AICardDisplay] No card selected yet. Run 'Select Random Card' on CardFetcherEnemy first.");
                return;
            }
            
            UpdateCardDisplay(testCard);
            FadeInPanel();
        }
    }
    #endif
}
