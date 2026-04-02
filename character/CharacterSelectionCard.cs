using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CharacterSelectionCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI Elements")]
    public Image cardBackground;
    public Image characterPortrait;
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI characterStatsText;
    public Image selectionBorder;
    
    [Header("Card Data")]
    public CharacterData characterData;
    
    private CharacterSelectionManager selectionManager;
    private Color normalColor;
    private Color hoverColor;
    private Color selectedColor;
    private bool isSelected = false;
    private bool isHovered = false;
    
    public void Setup(CharacterData character, CharacterSelectionManager manager, Color normal, Color hover, Color selected)
    {
        characterData = character;
        selectionManager = manager;
        normalColor = normal;
        hoverColor = hover;
        selectedColor = selected;
        
        CreateCardUI();
        UpdateCardDisplay();
    }
    
    private void CreateCardUI()
    {
        // Card background
        cardBackground = gameObject.AddComponent<Image>();
        cardBackground.color = normalColor;
        
        // Add shadow effect
        Shadow cardShadow = gameObject.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0, 0, 0, 0.3f);
        cardShadow.effectDistance = new Vector2(2, -2);
        
        // Card content container
        GameObject contentContainer = new GameObject("Content Container");
        contentContainer.transform.SetParent(transform);
        
        RectTransform contentRect = contentContainer.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(10, 10);
        contentRect.offsetMax = new Vector2(-10, -10);
        
        VerticalLayoutGroup contentLayout = contentContainer.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 8;
        contentLayout.padding = new RectOffset(8, 8, 8, 8);
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        
        // Character portrait
        GameObject portraitContainer = new GameObject("Portrait Container");
        portraitContainer.transform.SetParent(contentContainer.transform);
        
        RectTransform portraitRect = portraitContainer.AddComponent<RectTransform>();
        portraitRect.sizeDelta = new Vector2(0, 120);
        
        characterPortrait = portraitContainer.AddComponent<Image>();
        characterPortrait.color = Color.white;
        
        // Add portrait border
        Outline portraitBorder = portraitContainer.AddComponent<Outline>();
        portraitBorder.effectColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        portraitBorder.effectDistance = new Vector2(1, 1);
        
        // Character name
        GameObject nameContainer = new GameObject("Name Container");
        nameContainer.transform.SetParent(contentContainer.transform);
        
        RectTransform nameRect = nameContainer.AddComponent<RectTransform>();
        nameRect.sizeDelta = new Vector2(0, 30);
        
        characterNameText = nameContainer.AddComponent<TextMeshProUGUI>();
        characterNameText.fontSize = 16;
        characterNameText.fontStyle = FontStyles.Bold;
        characterNameText.color = Color.white;
        characterNameText.alignment = TextAlignmentOptions.Center;
        characterNameText.enableWordWrapping = false;
        characterNameText.overflowMode = TextOverflowModes.Overflow;
        
        // Character stats
        GameObject statsContainer = new GameObject("Stats Container");
        statsContainer.transform.SetParent(contentContainer.transform);
        
        RectTransform statsRect = statsContainer.AddComponent<RectTransform>();
        statsRect.sizeDelta = new Vector2(0, 80);
        
        characterStatsText = statsContainer.AddComponent<TextMeshProUGUI>();
        characterStatsText.fontSize = 12;
        characterStatsText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        characterStatsText.alignment = TextAlignmentOptions.Center;
        characterStatsText.enableWordWrapping = true;
        
        // Selection border (initially invisible)
        GameObject borderContainer = new GameObject("Selection Border");
        borderContainer.transform.SetParent(transform);
        
        RectTransform borderRect = borderContainer.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;
        
        selectionBorder = borderContainer.AddComponent<Image>();
        selectionBorder.color = Color.clear;
        
        Outline selectionOutline = borderContainer.AddComponent<Outline>();
        selectionOutline.effectColor = selectedColor;
        selectionOutline.effectDistance = new Vector2(3, 3);
        
        // Make sure border is behind content
        borderContainer.transform.SetSiblingIndex(0);
    }
    
    private void UpdateCardDisplay()
    {
        if (characterData == null) return;
        
        // Update character name
        if (characterNameText != null)
        {
            characterNameText.text = characterData.characterName;
        }
        
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
        
        // Update character stats
        if (characterStatsText != null)
        {
            characterStatsText.text = $"HP: {characterData.maxHP}\n" +
                                    $"MP: {characterData.maxMana}\n" +
                                    $"Gold: {characterData.gold}\n" +
                                    $"Effects: {characterData.buffs.Count + characterData.debuffs.Count}";
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