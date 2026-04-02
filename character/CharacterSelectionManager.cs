using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CharacterSelectionManager : MonoBehaviour
{
    public static CharacterSelectionManager Instance { get; private set; }
    
    [Header("UI References")]
    public Canvas selectionCanvas;
    public GameObject selectionPanel;
    public GameObject characterGrid;
    public Button startGameButton;
    public Button backButton;
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI selectedCharacterInfo;
    
    [Header("Character Selection")]
    public CharacterData selectedCharacter;
    public Color selectedColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public Color unselectedColor = new Color(0.3f, 0.3f, 0.4f, 1f);
    public Color hoverColor = new Color(0.4f, 0.5f, 0.7f, 1f);
    
    [Header("Scene Settings")]
    public string mainSceneName = "MainScene";
    public string mainMenuSceneName = "MainMenu";
    
    private List<CharacterData> availableCharacters = new List<CharacterData>();
    private List<CharacterSelectionCard> characterCards = new List<CharacterSelectionCard>();
    private CharacterSelectionCard currentSelectedCard;
    
    private void Awake()
    {
        // CharacterSelectionManager should NOT persist across scenes
        // Only set as instance for this scene
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // Only run if we're actually in a character selection scene
        string currentScene = SceneManager.GetActiveScene().name;
        if (IsCharacterSelectionScene(currentScene))
        {
            LoadAvailableCharacters();
            SetupUI();
        }
        else
        {
            Debug.LogWarning($"CharacterSelectionManager found in wrong scene: {currentScene}");
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        // Clear instance when this object is destroyed
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    [ContextMenu("Auto Create Character Selection")]
    public void AutoCreateSelection()
    {
        CreateCharacterSelectionUI();
    }
    
    public static void CreateCharacterSelectionUI()
    {
        // Check if selection UI already exists
        if (FindObjectOfType<CharacterSelectionManager>() != null)
        {
            Debug.Log("Character Selection UI already exists!");
            return;
        }
        
        // Create main UI object
        GameObject selectionObject = new GameObject("Character Selection System");
        CharacterSelectionManager selectionManager = selectionObject.AddComponent<CharacterSelectionManager>();
        
        // Create Canvas
        selectionManager.selectionCanvas = CreateSelectionCanvas(selectionObject);
        
        // Create main selection panel
        selectionManager.selectionPanel = CreateSelectionPanel(selectionManager.selectionCanvas.transform);
        
        // Create UI elements
        CreateSelectionElements(selectionManager);
        
        Debug.Log("Character Selection System created successfully!");
    }
    
    private static Canvas CreateSelectionCanvas(GameObject parent)
    {
        GameObject canvasObj = new GameObject("Character Selection Canvas");
        canvasObj.transform.SetParent(parent.transform);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        
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
    
    private static GameObject CreateSelectionPanel(Transform canvasParent)
    {
        GameObject panel = new GameObject("Character Selection Panel");
        panel.transform.SetParent(canvasParent);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        // Background
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);
        
        return panel;
    }
    
    private static void CreateSelectionElements(CharacterSelectionManager manager)
    {
        Transform panel = manager.selectionPanel.transform;
        
        // Main container
        GameObject mainContainer = new GameObject("Main Container");
        mainContainer.transform.SetParent(panel);
        
        RectTransform mainRect = mainContainer.AddComponent<RectTransform>();
        mainRect.anchorMin = Vector2.zero;
        mainRect.anchorMax = Vector2.one;
        mainRect.offsetMin = new Vector2(50, 50);
        mainRect.offsetMax = new Vector2(-50, -50);
        
        VerticalLayoutGroup mainLayout = mainContainer.AddComponent<VerticalLayoutGroup>();
        mainLayout.spacing = 30;
        mainLayout.padding = new RectOffset(50, 50, 30, 30);
        mainLayout.childAlignment = TextAnchor.UpperCenter;
        mainLayout.childControlWidth = true;
        mainLayout.childControlHeight = false;
        mainLayout.childForceExpandWidth = true;
        
        // Header
        manager.headerText = CreateHeaderText(mainContainer.transform);
        
        // Character grid container
        GameObject gridContainer = new GameObject("Character Grid Container");
        gridContainer.transform.SetParent(mainContainer.transform);
        
        RectTransform gridRect = gridContainer.AddComponent<RectTransform>();
        gridRect.sizeDelta = new Vector2(0, 600);
        
        // Character grid
        manager.characterGrid = new GameObject("Character Grid");
        manager.characterGrid.transform.SetParent(gridContainer.transform);
        
        RectTransform characterGridRect = manager.characterGrid.AddComponent<RectTransform>();
        characterGridRect.anchorMin = Vector2.zero;
        characterGridRect.anchorMax = Vector2.one;
        characterGridRect.offsetMin = Vector2.zero;
        characterGridRect.offsetMax = Vector2.zero;
        
        GridLayoutGroup gridLayout = manager.characterGrid.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(200, 280);
        gridLayout.spacing = new Vector2(20, 20);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 4;
        
        // Selected character info
        manager.selectedCharacterInfo = CreateCharacterInfoText(mainContainer.transform);
        
        // Button container
        GameObject buttonContainer = new GameObject("Button Container");
        buttonContainer.transform.SetParent(mainContainer.transform);
        
        RectTransform buttonRect = buttonContainer.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(0, 60);
        
        HorizontalLayoutGroup buttonLayout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
        buttonLayout.spacing = 20;
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.childControlWidth = false;
        buttonLayout.childControlHeight = true;
        buttonLayout.childForceExpandHeight = true;
        
        // Back button
        manager.backButton = CreateMenuButton(buttonContainer.transform, "Back to Main Menu", new Color(0.6f, 0.3f, 0.3f, 1f));
        
        // Start game button
        manager.startGameButton = CreateMenuButton(buttonContainer.transform, "Start Game", new Color(0.2f, 0.8f, 0.2f, 1f));
        manager.startGameButton.interactable = false; // Disabled until character selected
    }
    
    private static TextMeshProUGUI CreateHeaderText(Transform parent)
    {
        GameObject headerObj = new GameObject("Header Text");
        headerObj.transform.SetParent(parent);
        
        RectTransform headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.sizeDelta = new Vector2(0, 80);
        
        TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.text = "Choose Your Character";
        headerText.fontSize = 48;
        headerText.fontStyle = FontStyles.Bold;
        headerText.color = Color.white;
        headerText.alignment = TextAlignmentOptions.Center;
        
        Outline headerOutline = headerObj.AddComponent<Outline>();
        headerOutline.effectColor = new Color(0.2f, 0.4f, 0.8f, 0.8f);
        headerOutline.effectDistance = new Vector2(2, 2);
        
        return headerText;
    }
    
    private static TextMeshProUGUI CreateCharacterInfoText(Transform parent)
    {
        GameObject infoObj = new GameObject("Character Info Text");
        infoObj.transform.SetParent(parent);
        
        RectTransform infoRect = infoObj.AddComponent<RectTransform>();
        infoRect.sizeDelta = new Vector2(0, 100);
        
        TextMeshProUGUI infoText = infoObj.AddComponent<TextMeshProUGUI>();
        infoText.text = "Select a character to see details...";
        infoText.fontSize = 18;
        infoText.color = new Color(0.9f, 0.9f, 0.9f, 0.8f);
        infoText.alignment = TextAlignmentOptions.Center;
        infoText.enableWordWrapping = true;
        
        return infoText;
    }
    
    private static Button CreateMenuButton(Transform parent, string buttonText, Color buttonColor)
    {
        GameObject buttonObj = new GameObject($"{buttonText} Button");
        buttonObj.transform.SetParent(parent);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(200, 60);
        
        Button button = buttonObj.AddComponent<Button>();
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = buttonColor;
        
        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonColor * 1.2f;
        colors.pressedColor = buttonColor * 0.8f;
        colors.disabledColor = buttonColor * 0.5f;
        button.colors = colors;
        
        button.targetGraphic = buttonImage;
        
        // Button text
        GameObject textObj = new GameObject("Button Text");
        textObj.transform.SetParent(buttonObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI buttonTextComp = textObj.AddComponent<TextMeshProUGUI>();
        buttonTextComp.text = buttonText;
        buttonTextComp.fontSize = 18;
        buttonTextComp.fontStyle = FontStyles.Bold;
        buttonTextComp.color = Color.white;
        buttonTextComp.alignment = TextAlignmentOptions.Center;
        
        return button;
    }
    
    public void LoadAvailableCharacters()
    {
        availableCharacters.Clear();
        
        // Load all CharacterData assets from the CharacterData folder
        CharacterData[] allCharacters = Resources.LoadAll<CharacterData>("CharacterData");
        
        if (allCharacters.Length == 0)
        {
            // Fallback: Try loading from Assets/CharacterData using AssetDatabase (Editor only)
            #if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets/CharacterData" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                if (character != null)
                {
                    availableCharacters.Add(character);
                }
            }
            #endif
        }
        else
        {
            availableCharacters.AddRange(allCharacters);
        }
        
        Debug.Log($"Loaded {availableCharacters.Count} characters for selection");
    }
    
    public void SetupUI()
    {
        if (characterGrid == null) return;
        
        // Clear existing character cards
        foreach (Transform child in characterGrid.transform)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        characterCards.Clear();
        
        // Create character selection cards
        foreach (CharacterData character in availableCharacters)
        {
            CreateCharacterCard(character);
        }
        
        // Setup button events
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(StartGame);
        }
        
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(BackToMainMenu);
        }
    }
    
    private void CreateCharacterCard(CharacterData character)
    {
        GameObject cardObj = new GameObject($"{character.characterName} Card");
        cardObj.transform.SetParent(characterGrid.transform);
        
        CharacterSelectionCard card = cardObj.AddComponent<CharacterSelectionCard>();
        card.Setup(character, this, unselectedColor, hoverColor, selectedColor);
        
        characterCards.Add(card);
    }
    
    public void SelectCharacter(CharacterData character, CharacterSelectionCard card)
    {
        // Deselect previous card
        if (currentSelectedCard != null)
        {
            currentSelectedCard.SetSelected(false);
        }
        
        // Select new character
        selectedCharacter = character;
        currentSelectedCard = card;
        card.SetSelected(true);
        
        // Update character info display
        UpdateCharacterInfo();
        
        // Enable start game button
        if (startGameButton != null)
        {
            startGameButton.interactable = true;
        }
        
        Debug.Log($"Selected character: {character.characterName}");
    }
    
    private void UpdateCharacterInfo()
    {
        if (selectedCharacter != null && selectedCharacterInfo != null)
        {
            selectedCharacterInfo.text = $"{selectedCharacter.characterName}\n" +
                                       $"HP: {selectedCharacter.maxHP} | MP: {selectedCharacter.maxMana} | Gold: {selectedCharacter.gold}\n" +
                                       $"Buffs: {selectedCharacter.buffs.Count} | Debuffs: {selectedCharacter.debuffs.Count}";
        }
    }
    
    private void EnsureGameManagerExists()
    {
        if (GameManager.Instance == null)
        {
            Debug.Log("GameManager not found, creating one...");
            GameObject gameManagerGO = new GameObject("GameManager");
            gameManagerGO.AddComponent<GameManager>();
        }
    }
    
    public void StartGame()
    {
        if (selectedCharacter == null)
        {
            Debug.LogWarning("No character selected!");
            return;
        }
        
        // Ensure GameManager exists before using it
        EnsureGameManagerExists();
        
        // Double-check that GameManager instance is now available
        if (GameManager.Instance == null)
        {
            Debug.LogError("Failed to create or find GameManager.Instance! Cannot start game.");
            return;
        }
        
        // Store selected character in persistent game manager
        GameManager.Instance.SetSelectedCharacter(selectedCharacter);
        
        Debug.Log($"Starting game with character: {selectedCharacter.characterName}");
        
        // Load main scene
        if (SceneExists(mainSceneName))
        {
            SceneManager.LoadScene(mainSceneName);
        }
        else
        {
            Debug.LogWarning($"Main scene '{mainSceneName}' not found! Loading next scene in build settings.");
            int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextSceneIndex);
            }
        }
    }
    
    public void BackToMainMenu()
    {
        Debug.Log("Returning to main menu...");
        
        if (SceneExists(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            // Load previous scene
            int prevSceneIndex = SceneManager.GetActiveScene().buildIndex - 1;
            if (prevSceneIndex >= 0)
            {
                SceneManager.LoadScene(prevSceneIndex);
            }
        }
    }
    
    private bool IsCharacterSelectionScene(string sceneName)
    {
        // Define which scenes should have character selection UI
        string[] selectionScenes = { "CharacterCreation", "CharacterSelection", "SelectCharacter" };
        
        foreach (string selectionScene in selectionScenes)
        {
            if (sceneName.Equals(selectionScene, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
    
    private bool SceneExists(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameFromPath == sceneName)
                return true;
        }
        return false;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CharacterSelectionManager))]
public class CharacterSelectionManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        CharacterSelectionManager manager = (CharacterSelectionManager)target;
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Auto Create Character Selection"))
        {
            CharacterSelectionManager.CreateCharacterSelectionUI();
        }
        
        if (GUILayout.Button("Reload Characters"))
        {
            if (Application.isPlaying)
            {
                manager.LoadAvailableCharacters();
                manager.SetupUI();
            }
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Make sure CharacterData assets are in a Resources/CharacterData folder or Assets/CharacterData for proper loading!", MessageType.Info);
    }
    
    [MenuItem("Tools/Character System/Create Character Selection")]
    public static void CreateCharacterSelectionFromMenu()
    {
        CharacterSelectionManager.CreateCharacterSelectionUI();
    }
}
#endif