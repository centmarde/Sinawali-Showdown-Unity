using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Character UI system for displaying character stats, health, mana, and effects.
/// USAGE: This is now a TOOL-ONLY creation system. UI must be manually created via:
/// - Unity Menu: Tools -> Character System -> Create Character UI
/// - Context Menu: Right-click component -> "Auto Create Character UI"
/// - Inspector Button: "Auto Create Character UI"
/// 
/// After creation, manually connect to character using "Find and Connect Character" or SetTrackedCharacter().
/// </summary>
public class CharacterUIAutoCreate : MonoBehaviour
{
    // Backwards-compatible reference (no longer enforces a global singleton)
    public static CharacterUIAutoCreate Instance { get; private set; }

    public enum PlayerSlot
    {
        Player1,
        Player2
    }

    [Header("Player Slot")]
    [Tooltip("Which player this UI panel represents. Allows Player1 and Player2 UIs to coexist.")]
    public PlayerSlot playerSlot = PlayerSlot.Player1;

    private static CharacterUIAutoCreate player1Instance;
    private static CharacterUIAutoCreate player2Instance;
    
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
        // Allow multiple UI instances, but only one per PlayerSlot.
        CharacterUIAutoCreate existingForSlot = GetInstanceForSlot(playerSlot);
        if (existingForSlot != null && existingForSlot != this)
        {
            Debug.LogWarning($"Multiple CharacterUI instances detected for {playerSlot}. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        SetInstanceForSlot(playerSlot, this);

        // Keep a convenient reference for older code paths (typically Player1).
        if (Instance == null || playerSlot == PlayerSlot.Player1)
        {
            Instance = this;
        }
    }
    
    private void OnDestroy()
    {
        if (GetInstanceForSlot(playerSlot) == this)
        {
            SetInstanceForSlot(playerSlot, null);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static CharacterUIAutoCreate GetInstanceForSlot(PlayerSlot slot)
    {
        return slot == PlayerSlot.Player1 ? player1Instance : player2Instance;
    }

    private static void SetInstanceForSlot(PlayerSlot slot, CharacterUIAutoCreate instance)
    {
        if (slot == PlayerSlot.Player1)
            player1Instance = instance;
        else
            player2Instance = instance;
    }
    
    private bool IsGameScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        // Helper method for scene detection (reference only - UI is now manually created)
        return sceneName == "MainScene" || sceneName == "GameScene" || sceneName == "BattleScene";
    }
    
    [ContextMenu("Auto Create Character UI")]
    public void AutoCreateUI()
    {
        CreateCharacterUI();
    }
    
    public static void CreateCharacterUI()
    {
        // Create Player1 first, then Player2 if Player1 already exists.
        PlayerSlot slotToCreate;
        if (GetInstanceForSlot(PlayerSlot.Player1) == null)
            slotToCreate = PlayerSlot.Player1;
        else if (GetInstanceForSlot(PlayerSlot.Player2) == null)
            slotToCreate = PlayerSlot.Player2;
        else
        {
            Debug.Log("Character UI for Player1 and Player2 already exists!");
            return;
        }

        // Create main UI object
        GameObject uiObject = new GameObject($"Character UI System - {slotToCreate}");
        CharacterUIAutoCreate uiSystem = uiObject.AddComponent<CharacterUIAutoCreate>();
        uiSystem.playerSlot = slotToCreate;
        
        // Create Canvas
        uiSystem.characterCanvas = CreateCanvas(uiObject);
        
        // Create main character panel
        uiSystem.characterPanel = CreateCharacterPanel(uiSystem.characterCanvas.transform);
        
        // Create UI elements
        CreateCharacterUIElements(uiSystem);
        
        Debug.Log($"Character UI System created successfully for {slotToCreate}!");
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
        
        // Position in top-left corner with horizontal sizing
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(15, -15); // Smaller margin
        rect.sizeDelta = new Vector2(800, 50); // Wide horizontal layout
        
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
        
        // Horizontal layout for row alignment
        HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12;
        layout.padding = new RectOffset(12, 12, 8, 8); // Better padding
        layout.childForceExpandWidth = false;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;
        layout.childAlignment = TextAnchor.MiddleCenter;
        
        return panel;
    }
    
    private static void CreateCharacterUIElements(CharacterUIAutoCreate uiSystem)
    {
        Transform panel = uiSystem.characterPanel.transform;
        
        // Character Name (compact for horizontal layout)
        GameObject nameSection = CreateHorizontalSection(panel, "Name Section", 100f);
        uiSystem.nameText = CreateText(nameSection.transform, "Character Name", "Name Text", 12, 
            TextAlignmentOptions.Center, new Vector2(100, 30));
        uiSystem.nameText.fontStyle = FontStyles.Bold;
        uiSystem.nameText.color = new Color(0.9f, 0.9f, 1f, 1f);
        uiSystem.nameText.enableWordWrapping = false;
        uiSystem.nameText.overflowMode = TextOverflowModes.Overflow;
        
        // HP Section
        GameObject hpSection = CreateHorizontalSection(panel, "HP Section", 150f);
        uiSystem.hpSlider = CreateHorizontalSliderWithText(hpSection.transform, "HP", uiSystem.hpBarColor, out uiSystem.hpText);
        
        // Mana Section  
        GameObject manaSection = CreateHorizontalSection(panel, "Mana Section", 150f);
        uiSystem.manaSlider = CreateHorizontalSliderWithText(manaSection.transform, "MP", uiSystem.manaBarColor, out uiSystem.manaText);
        
        // Gold Section (compact)
        GameObject goldSection = CreateHorizontalSection(panel, "Gold Section", 120f);
        CreateText(goldSection.transform, "💰", "Gold Icon", 14, TextAlignmentOptions.Center, new Vector2(20, 30));
        uiSystem.goldText = CreateText(goldSection.transform, "0 Gold", "Gold Value", 10, 
            TextAlignmentOptions.Center, new Vector2(100, 30));
        uiSystem.goldText.color = new Color(1f, 0.8f, 0f, 1f);
        
        // Effects Section (horizontal)
        GameObject effectsSection = CreateHorizontalEffectsSection(panel);
        uiSystem.buffsParent = effectsSection.transform.Find("Buffs Container");
        uiSystem.debuffsParent = effectsSection.transform.Find("Debuffs Container");
    }
    
    private static GameObject CreateHorizontalSection(Transform parent, string name, float width = 120f)
    {
        GameObject section = new GameObject(name);
        section.transform.SetParent(parent);
        
        RectTransform rect = section.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, 30); // Fixed width for horizontal sections
        
        VerticalLayoutGroup layout = section.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 2;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childAlignment = TextAnchor.MiddleCenter;
        
        return section;
    }
    
    private static Slider CreateHorizontalSliderWithText(Transform parent, string labelText, Color barColor, out TextMeshProUGUI valueText)
    {
        // Compact label for horizontal layout
        CreateText(parent, $"{labelText}:", "Label", 9, TextAlignmentOptions.Center, new Vector2(0, 12));
        
        // Slider with compact design
        GameObject sliderObj = new GameObject($"{labelText} Slider");
        sliderObj.transform.SetParent(parent);
        
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(120, 12); // Compact horizontal bar
        
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.wholeNumbers = true;
        
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
        slider.value = slider.maxValue;
        
        // Compact value text below slider
        valueText = CreateText(parent, "100/100", "Value Text", 8, 
            TextAlignmentOptions.Center, new Vector2(0, 10));
        valueText.color = new Color(0.9f, 0.9f, 0.9f, 1f); // Lighter text
        
        return slider;
    }
    
    private static GameObject CreateHorizontalEffectsSection(Transform parent)
    {
        GameObject effectsSection = CreateHorizontalSection(parent, "Effects Section", 180f);
        
        // Horizontal effects layout
        GameObject effectsContainer = new GameObject("Effects Container");
        effectsContainer.transform.SetParent(effectsSection.transform);
        RectTransform effectsRect = effectsContainer.AddComponent<RectTransform>();
        effectsRect.sizeDelta = new Vector2(0, 25);
        
        HorizontalLayoutGroup effectsLayout = effectsContainer.AddComponent<HorizontalLayoutGroup>();
        effectsLayout.spacing = 8;
        effectsLayout.childControlWidth = false;
        effectsLayout.childControlHeight = true;
        effectsLayout.childForceExpandHeight = true;
        
        // Buffs side
        GameObject buffsColumn = new GameObject("Buffs Column");
        buffsColumn.transform.SetParent(effectsContainer.transform);
        RectTransform buffsRect = buffsColumn.AddComponent<RectTransform>();
        buffsRect.sizeDelta = new Vector2(80, 25);
        
        VerticalLayoutGroup buffsLayout = buffsColumn.AddComponent<VerticalLayoutGroup>();
        buffsLayout.spacing = 1;
        buffsLayout.childControlHeight = false;
        buffsLayout.childForceExpandHeight = false;
        
        // Compact buffs header
        TextMeshProUGUI buffsHeaderText = CreateText(buffsColumn.transform, "✓ Buffs", "Buffs Header Text", 8, 
            TextAlignmentOptions.Center, new Vector2(0, 10));
        buffsHeaderText.color = new Color(0.5f, 1f, 0.5f, 1f); // Light green
        buffsHeaderText.fontStyle = FontStyles.Bold;
        
        // Buffs container
        GameObject buffsContainer = new GameObject("Buffs Container");
        buffsContainer.transform.SetParent(buffsColumn.transform);
        RectTransform buffsContainerRect = buffsContainer.AddComponent<RectTransform>();
        buffsContainerRect.sizeDelta = new Vector2(0, 15);
        
        HorizontalLayoutGroup buffsGrid = buffsContainer.AddComponent<HorizontalLayoutGroup>();
        buffsGrid.spacing = 2;
        buffsGrid.childControlWidth = false;
        buffsGrid.childControlHeight = true;
        buffsGrid.childForceExpandHeight = true;
        
        // Debuffs side
        GameObject debuffsColumn = new GameObject("Debuffs Column");
        debuffsColumn.transform.SetParent(effectsContainer.transform);
        RectTransform debuffsRect = debuffsColumn.AddComponent<RectTransform>();
        debuffsRect.sizeDelta = new Vector2(80, 25);
        
        VerticalLayoutGroup debuffsLayout = debuffsColumn.AddComponent<VerticalLayoutGroup>();
        debuffsLayout.spacing = 1;
        debuffsLayout.childControlHeight = false;
        debuffsLayout.childForceExpandHeight = false;
        
        // Compact debuffs header
        TextMeshProUGUI debuffsHeaderText = CreateText(debuffsColumn.transform, "✗ Debuffs", "Debuffs Header Text", 8, 
            TextAlignmentOptions.Center, new Vector2(0, 10));
        debuffsHeaderText.color = new Color(1f, 0.5f, 0.5f, 1f); // Light red
        debuffsHeaderText.fontStyle = FontStyles.Bold;
        
        // Debuffs container
        GameObject debuffsContainer = new GameObject("Debuffs Container");
        debuffsContainer.transform.SetParent(debuffsColumn.transform);
        RectTransform debuffsContainerRect = debuffsContainer.AddComponent<RectTransform>();
        debuffsContainerRect.sizeDelta = new Vector2(0, 15);
        
        HorizontalLayoutGroup debuffsGrid = debuffsContainer.AddComponent<HorizontalLayoutGroup>();
        debuffsGrid.spacing = 2;
        debuffsGrid.childControlWidth = false;
        debuffsGrid.childControlHeight = true;
        debuffsGrid.childForceExpandHeight = true;
        
        return effectsSection;
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
        if (hpSlider == null || hpText == null) return;

        int safeMax = Mathf.Max(1, max);
        int clampedCurrent = Mathf.Clamp(current, 0, safeMax);

        // Drive the slider using real values (not 0..1) so it works with Whole Numbers
        // and matches what you see in the inspector.
        hpSlider.minValue = 0f;
        hpSlider.maxValue = safeMax;
        hpSlider.wholeNumbers = true;
        hpSlider.value = clampedCurrent;

        hpText.text = $"{clampedCurrent}/{safeMax}";
    }
    
    private void UpdateMana(int current, int max)
    {
        if (manaSlider == null || manaText == null) return;

        int safeMax = Mathf.Max(1, max);
        int clampedCurrent = Mathf.Clamp(current, 0, safeMax);

        manaSlider.minValue = 0f;
        manaSlider.maxValue = safeMax;
        manaSlider.wholeNumbers = true;
        manaSlider.value = clampedCurrent;

        manaText.text = $"{clampedCurrent}/{safeMax}";
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
        
        // Add current effects (horizontal compact style)
        foreach (var effect in effects)
        {
            GameObject effectObj = new GameObject(effect.effectName);
            effectObj.transform.SetParent(parent);
            
            RectTransform effectRect = effectObj.GetComponent<RectTransform>();
            if (effectRect == null) effectRect = effectObj.AddComponent<RectTransform>();
            effectRect.sizeDelta = new Vector2(10, 10); // Small horizontal indicators
            
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