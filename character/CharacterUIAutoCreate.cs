using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CharacterUIAutoCreate : MonoBehaviour
{
    public static CharacterUIAutoCreate Instance { get; private set; }
    
    [Header("UI References")]
    public Canvas characterCanvas;
    public GameObject characterPanel;
    public TextMeshProUGUI nameText;
    public Slider hpSlider;
    public TextMeshProUGUI hpText;
    public Slider manaSlider;
    public TextMeshProUGUI manaText;
    public TextMeshProUGUI goldText;
    public Transform buffsParent;
    public Transform debuffsParent;
    public Image characterPortrait;
    
    [Header("UI Settings")]
    public Color hpBarColor = Color.red;
    public Color manaBarColor = Color.blue;
    public Color buffColor = Color.green;
    public Color debuffColor = Color.red;
    
    private Character trackedCharacter;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    [ContextMenu("Auto Create Character UI")]
    public void AutoCreateUI()
    {
        CreateCharacterUI();
    }
    
    public static void CreateCharacterUI()
    {
        // Check if UI already exists
        if (FindObjectOfType<CharacterUIAutoCreate>() != null)
        {
            Debug.Log("Character UI already exists!");
            return;
        }
        
        // Create main UI object
        GameObject uiObject = new GameObject("Character UI System");
        CharacterUIAutoCreate uiSystem = uiObject.AddComponent<CharacterUIAutoCreate>();
        
        // Create Canvas
        uiSystem.characterCanvas = CreateCanvas(uiObject);
        
        // Create main character panel
        uiSystem.characterPanel = CreateCharacterPanel(uiSystem.characterCanvas.transform);
        
        // Create UI elements
        CreateCharacterUIElements(uiSystem);
        
        Debug.Log("Character UI System created successfully in top-left corner!");
    }
    
    private static Canvas CreateCanvas(GameObject parent)
    {
        GameObject canvasObj = new GameObject("Character Canvas");
        canvasObj.transform.SetParent(parent.transform);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // High priority
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Ensure EventSystem exists
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        
        return canvas;
    }
    
    private static GameObject CreateCharacterPanel(Transform canvasParent)
    {
        GameObject panel = new GameObject("Character Panel");
        panel.transform.SetParent(canvasParent);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        
        // Position in top-left corner with better sizing
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(15, -15); // Smaller margin
        rect.sizeDelta = new Vector2(320, 180); // Better proportioned size
        
        // Add modern background with gradient effect
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.05f, 0.1f, 0.9f); // Darker, more solid
        
        // Add subtle shadow effect
        Shadow shadow = panel.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(3, -3);
        
        // Add border outline
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.5f, 0.8f, 0.8f); // Subtle blue outline
        outline.effectDistance = new Vector2(1, 1);
        
        // Improved layout with better spacing
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4;
        layout.padding = new RectOffset(12, 12, 8, 8); // Better padding
        layout.childForceExpandWidth = true;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        
        return panel;
    }
    
    private static void CreateCharacterUIElements(CharacterUIAutoCreate uiSystem)
    {
        Transform panel = uiSystem.characterPanel.transform;
        
        // Character Name Header (no portrait for cleaner look)
        GameObject nameRow = CreateCustomRow(panel, "Name Row", 25f);
        uiSystem.nameText = CreateText(nameRow.transform, "Character Name", "Name Text", 15, 
            TextAlignmentOptions.Center, new Vector2(280, 25)); // Explicit width to prevent wrapping
        uiSystem.nameText.fontStyle = FontStyles.Bold;
        uiSystem.nameText.color = new Color(0.9f, 0.9f, 1f, 1f); // Light blue tint
        uiSystem.nameText.enableWordWrapping = false; // Force no wrapping
        uiSystem.nameText.overflowMode = TextOverflowModes.Overflow;
        
        // HP Bar (improved spacing)
        uiSystem.hpSlider = CreateImprovedSliderWithText(panel, "HP", uiSystem.hpBarColor, out uiSystem.hpText);
        
        // Mana Bar
        uiSystem.manaSlider = CreateImprovedSliderWithText(panel, "MP", uiSystem.manaBarColor, out uiSystem.manaText);
        
        // Gold (more compact)
        GameObject goldRow = CreateCustomRow(panel, "Gold Row", 22f);
        CreateText(goldRow.transform, "💰", "Gold Icon", 14, TextAlignmentOptions.MidlineLeft, new Vector2(25, 22));
        uiSystem.goldText = CreateText(goldRow.transform, "0 Gold", "Gold Value", 12, 
            TextAlignmentOptions.MidlineLeft, new Vector2(200, 22));
        uiSystem.goldText.color = new Color(1f, 0.8f, 0f, 1f); // Golden color
        
        // Effects Section (combined and more compact)
        GameObject effectsSection = CreateCompactEffectsSection(panel);
        uiSystem.buffsParent = effectsSection.transform.Find("Buffs Container");
        uiSystem.debuffsParent = effectsSection.transform.Find("Debuffs Container");
    }
    
    private static GameObject CreateCustomRow(Transform parent, string name, float height = 30f)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent);
        
        RectTransform rect = row.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, height); // Ensure row has proper width
        
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = false; // Don't force expand width
        layout.childAlignment = TextAnchor.MiddleLeft;
        
        return row;
    }
    
    private static Slider CreateImprovedSliderWithText(Transform parent, string labelText, Color barColor, out TextMeshProUGUI valueText)
    {
        GameObject sliderRow = CreateCustomRow(parent, $"{labelText} Row", 25f);
        
        // Compact label
        CreateText(sliderRow.transform, $"{labelText}:", "Label", 11, TextAlignmentOptions.MidlineLeft, new Vector2(28, 25));
        
        // Slider with improved design
        GameObject sliderObj = new GameObject($"{labelText} Slider");
        sliderObj.transform.SetParent(sliderRow.transform);
        
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(160, 16); // Slightly taller, longer bar
        
        Slider slider = sliderObj.AddComponent<Slider>();
        
        // Modern background with border
        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderObj.transform);
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.2f, 0.8f); // Darker background
        
        // Add border to background
        Outline bgOutline = background.AddComponent<Outline>();
        bgOutline.effectColor = new Color(0.4f, 0.4f, 0.5f, 0.6f);
        bgOutline.effectDistance = new Vector2(1, 1);
        
        slider.targetGraphic = bgImage;
        
        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform);
        RectTransform fillRect = fillArea.AddComponent<RectTransform>();
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        
        // Fill with gradient effect
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform);
        RectTransform fillBarRect = fill.AddComponent<RectTransform>();
        fillBarRect.sizeDelta = Vector2.zero;
        fillBarRect.anchorMin = Vector2.zero;
        fillBarRect.anchorMax = Vector2.one;
        fillBarRect.offsetMin = Vector2.zero;
        fillBarRect.offsetMax = Vector2.zero;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = barColor;
        
        slider.fillRect = fillBarRect;
        slider.value = 1f;
        
        // Compact value text
        valueText = CreateText(sliderRow.transform, "100/100", "Value Text", 10, 
            TextAlignmentOptions.MidlineRight, new Vector2(65, 25));
        valueText.color = new Color(0.9f, 0.9f, 0.9f, 1f); // Lighter text
        
        return slider;
    }
    
    private static GameObject CreateCompactEffectsSection(Transform parent)
    {
        GameObject effectsRow = CreateCustomRow(parent, "Effects Row", 50f);
        
        // Buffs side
        GameObject buffsColumn = new GameObject("Buffs Column");
        buffsColumn.transform.SetParent(effectsRow.transform);
        RectTransform buffsRect = buffsColumn.AddComponent<RectTransform>();
        buffsRect.sizeDelta = new Vector2(145, 50);
        
        VerticalLayoutGroup buffsLayout = buffsColumn.AddComponent<VerticalLayoutGroup>();
        buffsLayout.spacing = 2;
        buffsLayout.childControlHeight = false;
        buffsLayout.childForceExpandHeight = false;
        
        // Buffs header
        GameObject buffsHeader = new GameObject("Buffs Header");
        buffsHeader.transform.SetParent(buffsColumn.transform);
        RectTransform buffsHeaderRect = buffsHeader.AddComponent<RectTransform>();
        buffsHeaderRect.sizeDelta = new Vector2(0, 16);
        
        TextMeshProUGUI buffsHeaderText = CreateText(buffsHeader.transform, "✓ Buffs", "Buffs Header Text", 10, 
            TextAlignmentOptions.Center, new Vector2(0, 16));
        buffsHeaderText.color = new Color(0.5f, 1f, 0.5f, 1f); // Light green
        buffsHeaderText.fontStyle = FontStyles.Bold;
        
        // Buffs container
        GameObject buffsContainer = new GameObject("Buffs Container");
        buffsContainer.transform.SetParent(buffsColumn.transform);
        RectTransform buffsContainerRect = buffsContainer.AddComponent<RectTransform>();
        buffsContainerRect.sizeDelta = new Vector2(0, 30);
        
        GridLayoutGroup buffsGrid = buffsContainer.AddComponent<GridLayoutGroup>();
        buffsGrid.cellSize = new Vector2(12, 12);
        buffsGrid.spacing = new Vector2(1, 1);
        buffsGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        buffsGrid.childAlignment = TextAnchor.UpperLeft;
        
        // Debuffs side
        GameObject debuffsColumn = new GameObject("Debuffs Column");
        debuffsColumn.transform.SetParent(effectsRow.transform);
        RectTransform debuffsRect = debuffsColumn.AddComponent<RectTransform>();
        debuffsRect.sizeDelta = new Vector2(145, 50);
        
        VerticalLayoutGroup debuffsLayout = debuffsColumn.AddComponent<VerticalLayoutGroup>();
        debuffsLayout.spacing = 2;
        debuffsLayout.childControlHeight = false;
        debuffsLayout.childForceExpandHeight = false;
        
        // Debuffs header
        GameObject debuffsHeader = new GameObject("Debuffs Header");
        debuffsHeader.transform.SetParent(debuffsColumn.transform);
        RectTransform debuffsHeaderRect = debuffsHeader.AddComponent<RectTransform>();
        debuffsHeaderRect.sizeDelta = new Vector2(0, 16);
        
        TextMeshProUGUI debuffsHeaderText = CreateText(debuffsHeader.transform, "✗ Debuffs", "Debuffs Header Text", 10, 
            TextAlignmentOptions.Center, new Vector2(0, 16));
        debuffsHeaderText.color = new Color(1f, 0.5f, 0.5f, 1f); // Light red
        debuffsHeaderText.fontStyle = FontStyles.Bold;
        
        // Debuffs container
        GameObject debuffsContainer = new GameObject("Debuffs Container");
        debuffsContainer.transform.SetParent(debuffsColumn.transform);
        RectTransform debuffsContainerRect = debuffsContainer.AddComponent<RectTransform>();
        debuffsContainerRect.sizeDelta = new Vector2(0, 30);
        
        GridLayoutGroup debuffsGrid = debuffsContainer.AddComponent<GridLayoutGroup>();
        debuffsGrid.cellSize = new Vector2(12, 12);
        debuffsGrid.spacing = new Vector2(1, 1);
        debuffsGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        debuffsGrid.childAlignment = TextAnchor.UpperLeft;
        
        return effectsRow;
    }
    
    private static TextMeshProUGUI CreateText(Transform parent, string content, string name, int fontSize = 14, TextAlignmentOptions alignment = TextAlignmentOptions.Center, Vector2? size = null)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.sizeDelta = size ?? new Vector2(280, 25); // Give text more width by default
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.fontStyle = FontStyles.Normal;
        text.enableWordWrapping = false; // Prevent unwanted word wrapping
        text.overflowMode = TextOverflowModes.Overflow; // Allow text to overflow rather than wrap
        
        return text;
    }
    
    // Character connection methods
    public void SetTrackedCharacter(Character character)
    {
        if (trackedCharacter != null)
        {
            UnsubscribeFromCharacter(trackedCharacter);
        }
        
        trackedCharacter = character;
        
        if (trackedCharacter != null)
        {
            SubscribeToCharacter(trackedCharacter);
            UpdateUI();
        }
    }
    
    private void SubscribeToCharacter(Character character)
    {
        character.OnHPChanged += UpdateHP;
        character.OnManaChanged += UpdateMana;
        character.OnGoldChanged += UpdateGold;
        character.OnBuffsChanged += UpdateBuffs;
        character.OnDebuffsChanged += UpdateDebuffs;
    }
    
    private void UnsubscribeFromCharacter(Character character)
    {
        character.OnHPChanged -= UpdateHP;
        character.OnManaChanged -= UpdateMana;
        character.OnGoldChanged -= UpdateGold;
        character.OnBuffsChanged -= UpdateBuffs;
        character.OnDebuffsChanged -= UpdateDebuffs;
    }
    
    private void UpdateUI()
    {
        if (trackedCharacter == null || trackedCharacter.characterData == null) return;
        
        nameText.text = trackedCharacter.GetCharacterName();
        
        if (trackedCharacter.characterData.characterPortrait != null)
        {
            characterPortrait.sprite = trackedCharacter.characterData.characterPortrait;
            characterPortrait.color = Color.white;
        }
        
        UpdateHP(trackedCharacter.GetCurrentHP(), trackedCharacter.GetMaxHP());
        UpdateMana(trackedCharacter.GetCurrentMana(), trackedCharacter.GetMaxMana());
        UpdateGold(trackedCharacter.GetCurrentGold());
        UpdateBuffs(trackedCharacter.GetActiveBuffs());
        UpdateDebuffs(trackedCharacter.GetActiveDebuffs());
    }
    
    private void UpdateHP(int current, int max)
    {
        if (hpSlider != null && hpText != null)
        {
            hpSlider.value = max > 0 ? (float)current / max : 0;
            hpText.text = $"{current}/{max}";
        }
    }
    
    private void UpdateMana(int current, int max)
    {
        if (manaSlider != null && manaText != null)
        {
            manaSlider.value = max > 0 ? (float)current / max : 0;
            manaText.text = $"{current}/{max}";
        }
    }
    
    private void UpdateGold(int gold)
    {
        if (goldText != null)
        {
            goldText.text = $"{gold:N0} Gold";
        }
    }
    
    private void UpdateBuffs(List<StatusEffect> buffs)
    {
        UpdateEffectsDisplay(buffsParent, buffs, buffColor);
    }
    
    private void UpdateDebuffs(List<StatusEffect> debuffs)
    {
        UpdateEffectsDisplay(debuffsParent, debuffs, debuffColor);
    }
    
    private void UpdateEffectsDisplay(Transform parent, List<StatusEffect> effects, Color color)
    {
        if (parent == null) return;
        
        // Clear existing effects
        foreach (Transform child in parent)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        
        // Add current effects (compact grid style)
        foreach (var effect in effects)
        {
            GameObject effectObj = new GameObject(effect.effectName);
            effectObj.transform.SetParent(parent);
            
            // Small indicator dot
            Image effectImage = effectObj.AddComponent<Image>();
            effectImage.color = color;
            
            // Add effect icon if available, otherwise use colored dot
            if (effect.effectIcon != null)
            {
                effectImage.sprite = effect.effectIcon;
                effectImage.color = Color.white;
            }
            
            // Add simple tooltip component for hover
            TooltipTrigger tooltip = effectObj.AddComponent<TooltipTrigger>();
            tooltip.tooltipText = $"{effect.effectName}\nDuration: {effect.duration}\n{effect.description}";
        }
    }
    
    // Simple tooltip component for status effects
    public class TooltipTrigger : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        public string tooltipText;
        private GameObject tooltipObject;
        
        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            ShowTooltip();
        }
        
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            HideTooltip();
        }
        
        private void ShowTooltip()
        {
            if (string.IsNullOrEmpty(tooltipText)) return;
            
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            
            tooltipObject = new GameObject("Tooltip");
            tooltipObject.transform.SetParent(canvas.transform);
            
            RectTransform tooltipRect = tooltipObject.AddComponent<RectTransform>();
            tooltipRect.sizeDelta = new Vector2(150, 60);
            tooltipRect.position = Input.mousePosition;
            
            Image tooltipBg = tooltipObject.AddComponent<Image>();
            tooltipBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            TextMeshProUGUI tooltipTextComp = tooltipObject.AddComponent<TextMeshProUGUI>();
            tooltipTextComp.text = tooltipText;
            tooltipTextComp.fontSize = 10;
            tooltipTextComp.color = Color.white;
            tooltipTextComp.alignment = TextAlignmentOptions.Center;
        }
        
        private void HideTooltip()
        {
            if (tooltipObject != null)
            {
                if (Application.isPlaying)
                    Destroy(tooltipObject);
                else
                    DestroyImmediate(tooltipObject);
            }
        }
    }
    
    [ContextMenu("Find and Connect Character")]
    public void FindAndConnectCharacter()
    {
        Character character = FindObjectOfType<Character>();
        if (character != null)
        {
            SetTrackedCharacter(character);
            Debug.Log($"Connected to character: {character.GetCharacterName()}");
        }
        else
        {
            Debug.LogWarning("No Character component found in scene!");
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CharacterUIAutoCreate))]
public class CharacterUIAutoCreateEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        CharacterUIAutoCreate uiSystem = (CharacterUIAutoCreate)target;
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Auto Create Character UI"))
        {
            CharacterUIAutoCreate.CreateCharacterUI();
        }
        
        if (GUILayout.Button("Find and Connect Character"))
        {
            uiSystem.FindAndConnectCharacter();
        }
    }
}
#endif