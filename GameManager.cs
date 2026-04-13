using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Selected Character")]
    public CharacterData selectedCharacterData;
    public GameObject activeCharacterObject;
    
    [Header("Game State")]
    public bool isCharacterSelected = false;
    public string currentSceneName;
    
    private void Awake()
    {
        // Singleton pattern with persistence across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("GameManager initialized and persisting across scenes.");
        }
        else
        {
            Debug.Log("GameManager instance already exists, destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }
    
    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        Debug.Log($"Scene loaded: {currentSceneName}");
        
        // Clean up any previous character setup
        CleanupPreviousCharacter();
        
        // Auto-setup character in main scene
        if (isCharacterSelected && selectedCharacterData != null && IsMainGameScene(scene.name))
        {
            SetupCharacterInScene();
        }
    }
    
    public void SetSelectedCharacter(CharacterData characterData)
    {
        if (characterData == null)
        {
            Debug.LogWarning("Attempted to set null character data!");
            return;
        }
        
        selectedCharacterData = characterData;
        isCharacterSelected = true;
        
        Debug.Log($"Selected character set: {characterData.characterName}");
        Debug.Log($"Character stats - HP: {characterData.maxHP}, MP: {characterData.maxMana}, Gold: {characterData.gold}");
    }
    
    public CharacterData GetSelectedCharacter()
    {
        return selectedCharacterData;
    }
    
    public bool HasSelectedCharacter()
    {
        return isCharacterSelected && selectedCharacterData != null;
    }
    
    public void SetupCharacterInScene()
    {
        if (!HasSelectedCharacter())
        {
            Debug.LogWarning("No character selected! Cannot setup character in scene.");
            return;
        }
        
        Debug.Log($"Setting up character {selectedCharacterData.characterName} in scene {currentSceneName}");
        
        // Find existing Character component in scene
        Character existingCharacter = FindObjectOfType<Character>();
        
        if (existingCharacter != null)
        {
            // Use existing Character component
            existingCharacter.SetCharacterData(selectedCharacterData);
            activeCharacterObject = existingCharacter.gameObject;
            Debug.Log($"Updated existing Character component with {selectedCharacterData.characterName}");
        }
        else
        {
            // Create new character object
            CreateCharacterInScene();
        }
        
        // Setup Character UI if it exists
        SetupCharacterUI();
    }
    
    private void CreateCharacterInScene()
    {
        GameObject characterObj = new GameObject($"Character - {selectedCharacterData.characterName}");
        Character characterComponent = characterObj.AddComponent<Character>();
        characterComponent.SetCharacterData(selectedCharacterData);
        
        activeCharacterObject = characterObj;
        
        Debug.Log($"Created new character object: {selectedCharacterData.characterName}");
    }
    
    private void CleanupPreviousCharacter()
    {
        // Cleanup character objects and UI when moving to menu scenes
        if (IsMenuScene(currentSceneName))
        {
            if (activeCharacterObject != null)
            {
                Debug.Log($"Cleaning up character object for menu scene: {currentSceneName}");
                activeCharacterObject = null; // Lose reference for menu scenes
            }
            
            // Also clean up any character UI that shouldn't be in menu scenes
            CharacterUIAutoCreate[] characterUIs = FindObjectsOfType<CharacterUIAutoCreate>();
            foreach (CharacterUIAutoCreate ui in characterUIs)
            {
                Debug.Log($"Removing character UI from menu scene: {currentSceneName}");
                if (Application.isPlaying)
                    Destroy(ui.gameObject);
                else
                    DestroyImmediate(ui.gameObject);
            }
        }
    }
    
    private void SetupCharacterUI()
    {
        // Only create UI in main game scenes
        if (!IsMainGameScene(currentSceneName))
        {
            Debug.Log($"Skipping Character UI setup - {currentSceneName} is not a main game scene");
            return;
        }
        
        // Find and connect to Character UI if it exists
        CharacterUIAutoCreate characterUI = FindObjectOfType<CharacterUIAutoCreate>();
        
        if (characterUI != null && activeCharacterObject != null)
        {
            Character characterComponent = activeCharacterObject.GetComponent<Character>();
            if (characterComponent != null)
            {
                characterUI.SetTrackedCharacter(characterComponent);
                Debug.Log($"Connected Character UI to {selectedCharacterData.characterName}");
            }
        }
        else if (characterUI == null && activeCharacterObject != null)
        {
            // Character UI must now be manually created via editor tools
            Debug.LogWarning($"Character UI not found for {selectedCharacterData.characterName}. " +
                           "Create Character UI manually via Tools → Character System → Create Character UI");
        }
    }
    
    private bool IsMainGameScene(string sceneName)
    {
        // Define which scenes are considered "main game scenes" where character should be setup
        string[] mainScenes = { "MainScene", "GameScene", "PlayScene", "Main", "Game" };
        
        foreach (string mainScene in mainScenes)
        {
            if (sceneName.Equals(mainScene, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        
        return false;
    }
    
    private bool IsMenuScene(string sceneName)
    {
        // Define which scenes are menu scenes that shouldn't have game UI
        string[] menuScenes = { "MainMenu", "Menu", "CharacterCreation", "CharacterSelection", "StartMenu" };
        
        foreach (string menuScene in menuScenes)
        {
            if (sceneName.Equals(menuScene, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        
        return false;
    }
    
    public void ResetSelectedCharacter()
    {
        selectedCharacterData = null;
        isCharacterSelected = false;
        
        if (activeCharacterObject != null)
        {
            Destroy(activeCharacterObject);
            activeCharacterObject = null;
        }
        
        Debug.Log("Selected character reset.");
    }
    
    [ContextMenu("Debug Character Info")]
    public void DebugCharacterInfo()
    {
        if (HasSelectedCharacter())
        {
            Debug.Log($"Current Character: {selectedCharacterData.characterName}");
            Debug.Log($"HP: {selectedCharacterData.maxHP} | MP: {selectedCharacterData.maxMana} | Gold: {selectedCharacterData.gold}");
            Debug.Log($"Buffs: {selectedCharacterData.buffs.Count} | Debuffs: {selectedCharacterData.debuffs.Count}");
        }
        else
        {
            Debug.Log("No character currently selected.");
        }
    }
    
    [ContextMenu("Force Setup Character")]
    public void ForceSetupCharacter()
    {
        if (HasSelectedCharacter())
        {
            SetupCharacterInScene();
        }
        else
        {
            Debug.LogWarning("No character selected to setup!");
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GameManager gameManager = (GameManager)target;
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Character Status:", EditorStyles.boldLabel);
        
        if (gameManager.HasSelectedCharacter())
        {
            EditorGUILayout.LabelField($"Selected: {gameManager.selectedCharacterData.characterName}");
            EditorGUILayout.LabelField($"HP: {gameManager.selectedCharacterData.maxHP}");
            EditorGUILayout.LabelField($"MP: {gameManager.selectedCharacterData.maxMana}");
            EditorGUILayout.LabelField($"Gold: {gameManager.selectedCharacterData.gold}");
        }
        else
        {
            EditorGUILayout.LabelField("No character selected", EditorStyles.helpBox);
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Debug Character Info"))
        {
            gameManager.DebugCharacterInfo();
        }
        
        if (GUILayout.Button("Force Setup Character"))
        {
            gameManager.ForceSetupCharacter();
        }
        
        if (GUILayout.Button("Reset Selected Character"))
        {
            gameManager.ResetSelectedCharacter();
        }
    }
}
#endif