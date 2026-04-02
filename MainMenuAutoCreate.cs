using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuAutoCreate : MonoBehaviour
{
    public static MainMenuAutoCreate Instance { get; private set; }
    
    [Header("UI References")]
    public Canvas mainMenuCanvas;
    public GameObject mainMenuPanel;
    public Button startGameButton;
    public Button quitButton;
    public TextMeshProUGUI titleText;
    public Image backgroundImage;
    
    [Header("Menu Settings")]
    public string gameTitle = "My Awesome Game";
    public string characterCreationSceneName = "characterCreation";
    public Color titleColor = Color.white;
    public Color buttonNormalColor = new Color(0.2f, 0.3f, 0.8f, 1f);
    public Color buttonHighlightColor = new Color(0.3f, 0.4f, 0.9f, 1f);
    public Color buttonPressedColor = new Color(0.1f, 0.2f, 0.7f, 1f);
    public Color backgroundColor = new Color(0.05f, 0.05f, 0.1f, 0.95f);
    
    private void Awake()
    {
        // MainMenuAutoCreate should NOT persist across scenes
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
        // Only run if we're actually in a main menu scene
        string currentScene = SceneManager.GetActiveScene().name;
        if (IsMainMenuScene(currentScene))
        {
            // Auto-connect buttons if they exist but aren't connected
            ConnectButtonEvents();
        }
        else
        {
            Debug.LogWarning($"MainMenuAutoCreate found in wrong scene: {currentScene}");
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
    
    [ContextMenu("Auto Create Main Menu")]
    public void AutoCreateMainMenu()
    {
        CreateMainMenuUI();
    }
    
    public static void CreateMainMenuUI()
    {
        // Check if main menu already exists
        if (FindObjectOfType<MainMenuAutoCreate>() != null)
        {
            Debug.Log("Main Menu UI already exists!");
            return;
        }
        
        // Create main UI object
        GameObject menuObject = new GameObject("Main Menu System");
        MainMenuAutoCreate menuSystem = menuObject.AddComponent<MainMenuAutoCreate>();
        
        // Create Canvas
        menuSystem.mainMenuCanvas = CreateMainMenuCanvas(menuObject);
        
        // Create main menu panel
        menuSystem.mainMenuPanel = CreateMainMenuPanel(menuSystem.mainMenuCanvas.transform);
        
        // Create UI elements
        CreateMainMenuElements(menuSystem);
        
        // Connect button events
        menuSystem.ConnectButtonEvents();
        
        Debug.Log("Main Menu System created successfully!");
    }
    
    private static Canvas CreateMainMenuCanvas(GameObject parent)
    {
        GameObject canvasObj = new GameObject("Main Menu Canvas");
        canvasObj.transform.SetParent(parent.transform);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Highest priority for main menu
        
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
    
    private static GameObject CreateMainMenuPanel(Transform canvasParent)
    {
        GameObject panel = new GameObject("Main Menu Panel");
        panel.transform.SetParent(canvasParent);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        
        // Center the panel and make it fullscreen
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        // Add background with gradient effect
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.05f, 0.1f, 0.95f); // Dark semi-transparent background
        
        return panel;
    }
    
    private static void CreateMainMenuElements(MainMenuAutoCreate menuSystem)
    {
        Transform panel = menuSystem.mainMenuPanel.transform;
        
        // Create main container for centered content
        GameObject contentContainer = new GameObject("Content Container");
        contentContainer.transform.SetParent(panel);
        
        RectTransform containerRect = contentContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = new Vector2(400, 500);
        
        // Add vertical layout for menu elements
        VerticalLayoutGroup contentLayout = contentContainer.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 30;
        contentLayout.padding = new RectOffset(50, 50, 50, 50);
        contentLayout.childAlignment = TextAnchor.MiddleCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        
        // Game Title
        menuSystem.titleText = CreateGameTitle(contentContainer.transform, menuSystem.gameTitle);
        
        // Add some space before buttons
        CreateSpacer(contentContainer.transform, 50);
        
        // Start Game Button
        menuSystem.startGameButton = CreateMenuButton(contentContainer.transform, "Start Game", menuSystem.buttonNormalColor, menuSystem.buttonHighlightColor, menuSystem.buttonPressedColor);
        
        // Quit Button
        menuSystem.quitButton = CreateMenuButton(contentContainer.transform, "Quit Game", menuSystem.buttonNormalColor, menuSystem.buttonHighlightColor, menuSystem.buttonPressedColor);
        
        // Add footer space
        CreateSpacer(contentContainer.transform, 100);
        
        // Version/Credit text
        CreateFooterText(contentContainer.transform, "Made with Unity • Version 1.0");
    }
    
    private static TextMeshProUGUI CreateGameTitle(Transform parent, string title)
    {
        GameObject titleObj = new GameObject("Game Title");
        titleObj.transform.SetParent(parent);
        
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(0, 80);
        
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.fontSize = 48;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.enableWordWrapping = false;
        
        // Add glow effect
        titleText.fontMaterial = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Outline");
        if (titleText.fontMaterial == null)
        {
            // Add outline as fallback
            Outline titleOutline = titleObj.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0.2f, 0.4f, 0.8f, 0.8f);
            titleOutline.effectDistance = new Vector2(2, 2);
        }
        
        return titleText;
    }
    
    private static Button CreateMenuButton(Transform parent, string buttonText, Color normalColor, Color highlightColor, Color pressedColor)
    {
        GameObject buttonObj = new GameObject($"{buttonText} Button");
        buttonObj.transform.SetParent(parent);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(0, 60);
        
        // Button component
        Button button = buttonObj.AddComponent<Button>();
        
        // Button background image
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = normalColor;
        
        // Add rounded corners effect with border
        Shadow buttonShadow = buttonObj.AddComponent<Shadow>();
        buttonShadow.effectColor = new Color(0, 0, 0, 0.5f);
        buttonShadow.effectDistance = new Vector2(0, -3);
        
        Outline buttonOutline = buttonObj.AddComponent<Outline>();
        buttonOutline.effectColor = new Color(1f, 1f, 1f, 0.3f);
        buttonOutline.effectDistance = new Vector2(1, 1);
        
        // Setup button colors
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = highlightColor;
        colors.pressedColor = pressedColor;
        colors.selectedColor = highlightColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.2f;
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
        buttonTextComp.fontSize = 24;
        buttonTextComp.fontStyle = FontStyles.Bold;
        buttonTextComp.color = Color.white;
        buttonTextComp.alignment = TextAlignmentOptions.Center;
        
        return button;
    }
    
    private static void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(parent);
        
        RectTransform spacerRect = spacer.AddComponent<RectTransform>();
        spacerRect.sizeDelta = new Vector2(0, height);
    }
    
    private static TextMeshProUGUI CreateFooterText(Transform parent, string text)
    {
        GameObject footerObj = new GameObject("Footer Text");
        footerObj.transform.SetParent(parent);
        
        RectTransform footerRect = footerObj.AddComponent<RectTransform>();
        footerRect.sizeDelta = new Vector2(0, 25);
        
        TextMeshProUGUI footerText = footerObj.AddComponent<TextMeshProUGUI>();
        footerText.text = text;
        footerText.fontSize = 14;
        footerText.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
        footerText.alignment = TextAlignmentOptions.Center;
        
        return footerText;
    }
    
    private void ConnectButtonEvents()
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(StartGame);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }
    }
    
    public void StartGame()
    {
        Debug.Log($"Starting game - Loading scene: {characterCreationSceneName}");
        
        // Check if scene exists in build settings
        if (DoesSceneExist(characterCreationSceneName))
        {
            SceneManager.LoadScene(characterCreationSceneName);
        }
        else
        {
            Debug.LogWarning($"Scene '{characterCreationSceneName}' not found in build settings! Please add it to File > Build Settings > Scenes In Build");
            
            // Try loading by index (assuming it's the next scene after main menu)
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            int nextSceneIndex = currentSceneIndex + 1;
            
            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                Debug.Log($"Attempting to load scene at index {nextSceneIndex}");
                SceneManager.LoadScene(nextSceneIndex);
            }
            else
            {
                Debug.LogError("No scenes available to load! Please configure build settings.");
            }
        }
    }
    
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    private bool IsMainMenuScene(string sceneName)
    {
        // Define which scenes should have main menu UI
        string[] mainMenuScenes = { "MainMenu", "Menu", "StartMenu", "Title", "Home" };
        
        foreach (string menuScene in mainMenuScenes)
        {
            if (sceneName.Equals(menuScene, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
    
    private bool DoesSceneExist(string sceneName)
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
    
    [ContextMenu("Test Start Game")]
    public void TestStartGame()
    {
        StartGame();
    }
    
    [ContextMenu("Test Quit Game")]
    public void TestQuitGame()
    {
        QuitGame();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MainMenuAutoCreate))]
public class MainMenuAutoCreateEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        MainMenuAutoCreate mainMenu = (MainMenuAutoCreate)target;
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Auto Create Main Menu"))
        {
            MainMenuAutoCreate.CreateMainMenuUI();
        }
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Test Buttons:", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Test Start Game"))
        {
            mainMenu.TestStartGame();
        }
        
        if (GUILayout.Button("Test Quit Game"))
        {
            mainMenu.TestQuitGame();
        }
        
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox("Remember to add your scenes to File > Build Settings > Scenes In Build for proper scene loading!", MessageType.Info);
    }
    
    [MenuItem("Tools/Main Menu System/Create Main Menu UI")]
    public static void CreateMainMenuFromMenu()
    {
        MainMenuAutoCreate.CreateMainMenuUI();
    }
    
    [MenuItem("Tools/Main Menu System/Find Main Menu")]
    public static void FindMainMenu()
    {
        MainMenuAutoCreate existingMenu = FindObjectOfType<MainMenuAutoCreate>();
        if (existingMenu != null)
        {
            Selection.activeGameObject = existingMenu.gameObject;
            EditorGUIUtility.PingObject(existingMenu.gameObject);
            Debug.Log("Main Menu found and selected!");
        }
        else
        {
            Debug.Log("No Main Menu found in scene.");
        }
    }
}
#endif