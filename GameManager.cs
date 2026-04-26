using System.Collections.Generic;
using UnityEngine;
using System;
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

    [Header("Optional References")]
    [Tooltip("Optional: assign a HPTrackerBinder in the scene. If empty, GameManager will try to find one.")]
    public HPTrackerBinder hpTrackerBinder;

    [Header("Turn System")]
    [Tooltip("Optional: assign a TurnManager in the scene. If empty, GameManager will try to find one or add one to itself.")]
    public TurnManager turnManager;

    [Tooltip("Who starts first when turn state is initialized/reset.")]
    [SerializeField] private TurnManager.TurnOwner startingTurnOwner = TurnManager.TurnOwner.Player1;

    [Header("Graveyard")]
    [Tooltip("Cards that have been played/confirmed. Stored as ScriptableObject references.")]
    [SerializeField] private List<CardData> graveyardCards = new List<CardData>();

    public event Action<IReadOnlyList<CardData>> OnGraveyardChanged;

    [Header("Two Player (Optional)")]
    [Tooltip("If enabled, Player2 will be set from player2CharacterDataOverride (used for Alon/Kidlat swapping).")]
    public bool overridePlayer2FromSelection = false;

    [Tooltip("If overridePlayer2FromSelection is enabled, this CharacterData will be applied to Player2.")]
    public CharacterData player2CharacterDataOverride;
    
    private void Awake()
    {
        // Singleton pattern with persistence across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("GameManager initialized and persisting across scenes.");

            // Optional turn system hookup
            if (turnManager == null)
            {
                turnManager = GetComponent<TurnManager>();
                if (turnManager == null)
                {
                    turnManager = FindObjectOfType<TurnManager>();
                }
                if (turnManager == null)
                {
                    turnManager = gameObject.AddComponent<TurnManager>();
                }
            }
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
        RefreshTurnManagerReference();
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
        RefreshTurnManagerReference(scene);
        
        // Clean up any previous character setup
        CleanupPreviousCharacter();
        
        // Auto-setup character in main scene
        if (isCharacterSelected && selectedCharacterData != null && IsMainGameScene(scene.name))
        {
            SetupCharacterInScene();
        }
    }

    private void RefreshTurnManagerReference(UnityEngine.SceneManagement.Scene targetScene = default)
    {
        TurnManager preferredTurnManager = null;

        UnityEngine.SceneManagement.Scene sceneToCheck = targetScene.IsValid() ? targetScene : UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (sceneToCheck.IsValid() && sceneToCheck.isLoaded)
        {
            GameObject[] roots = sceneToCheck.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                TurnManager candidate = roots[i].GetComponentInChildren<TurnManager>(true);
                if (candidate != null)
                {
                    preferredTurnManager = candidate;
                    break;
                }
            }
        }

        if (preferredTurnManager == null)
        {
            preferredTurnManager = FindObjectOfType<TurnManager>();
        }

        if (preferredTurnManager != null)
        {
            if (turnManager != preferredTurnManager)
            {
                turnManager = preferredTurnManager;
                Debug.Log($"GameManager: TurnManager reference updated to '{turnManager.name}' in scene '{turnManager.gameObject.scene.name}'.");
            }
        }
        else
        {
            Debug.LogWarning("GameManager: No TurnManager found while refreshing reference.");
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

        // If a TwoPlayerSpawner exists, let it control Player1/Player2 spawning + placement.
        TwoPlayerSpawner spawner = FindObjectOfType<TwoPlayerSpawner>();
        if (spawner != null)
        {
            spawner.SpawnFromGameState();
            activeCharacterObject = spawner.SpawnedPlayer1;
            NotifyHPTrackerBinder();
            return;
        }

        // Prefer explicit Player1 assignment via HPTrackerBinder for 2-player setups.
        HPTrackerBinder binder = GetHPTrackerBinder();
        Character player1Character = GetPlayer1CharacterTarget(binder);

        if (player1Character != null)
        {
            player1Character.SetCharacterData(selectedCharacterData);
            activeCharacterObject = player1Character.gameObject;
            Debug.Log($"Applied selected character to Player1: {selectedCharacterData.characterName}");
        }
        else
        {
            // Fallback for single-character scenes
            Character existingCharacter = FindObjectOfType<Character>();
            if (existingCharacter != null)
            {
                existingCharacter.SetCharacterData(selectedCharacterData);
                activeCharacterObject = existingCharacter.gameObject;
                Debug.Log($"Updated existing Character component with {selectedCharacterData.characterName}");
            }
            else
            {
                // Create new character object
                CreateCharacterInScene();
            }
        }

        // Player2 is expected to be configured manually in the inspector on its Character component.
        ApplyOrValidatePlayer2Setup(binder);
        
        // Notify HP/UI tracker systems (optional)
        NotifyHPTrackerBinder();
    }
    
    private void CreateCharacterInScene()
    {
        GameObject characterObj = new GameObject($"Character - {selectedCharacterData.characterName}");
        Character characterComponent = characterObj.AddComponent<Character>();
        characterComponent.SetCharacterData(selectedCharacterData);
        
        activeCharacterObject = characterObj;
        
        Debug.Log($"Created new character object: {selectedCharacterData.characterName}");
    }

    private HPTrackerBinder GetHPTrackerBinder()
    {
        if (hpTrackerBinder != null) return hpTrackerBinder;
        hpTrackerBinder = FindObjectOfType<HPTrackerBinder>();
        return hpTrackerBinder;
    }

    /// <summary>
    /// Attempts to resolve Player1/Player2 Character components for gameplay logic (cards, turns, etc.).
    /// Prefers HPTrackerBinder assignments when present.
    /// </summary>
    public bool TryGetPlayerCharacters(out Character player1, out Character player2)
    {
        player1 = null;
        player2 = null;

        HPTrackerBinder binder = GetHPTrackerBinder();
        if (binder != null)
        {
            if (binder.player1Object != null)
            {
                player1 = binder.player1Object.GetComponent<Character>();
            }
            if (binder.player2Object != null)
            {
                player2 = binder.player2Object.GetComponent<Character>();
            }
        }

        if (player1 == null && activeCharacterObject != null)
        {
            player1 = activeCharacterObject.GetComponent<Character>();
        }

        // Fallbacks for single-character scenes / misconfigured binder
        if (player1 == null)
        {
            Character[] all = FindObjectsOfType<Character>();
            if (all != null && all.Length > 0)
            {
                player1 = all[0];
            }
        }

        if (player2 == null)
        {
            Character[] all = FindObjectsOfType<Character>();
            if (all != null)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i] != player1)
                    {
                        player2 = all[i];
                        break;
                    }
                }
            }
        }

        return player1 != null;
    }

    public void AddCardToGraveyard(CardData card)
    {
        if (card == null)
        {
            Debug.LogWarning("GameManager: Tried to add null card to graveyard.");
            return;
        }

        graveyardCards.Add(card);
        Debug.Log($"GameManager: Added '{card.Title}' to graveyard. Total: {graveyardCards.Count}");
        OnGraveyardChanged?.Invoke(graveyardCards);
    }

    public IReadOnlyList<CardData> GetGraveyardCards()
    {
        return graveyardCards;
    }

    public void ClearGraveyard()
    {
        graveyardCards.Clear();
        Debug.Log("GameManager: Graveyard cleared.");
        OnGraveyardChanged?.Invoke(graveyardCards);
    }

    private static Character GetPlayer1CharacterTarget(HPTrackerBinder binder)
    {
        if (binder == null) return null;
        if (binder.player1Object == null) return null;

        Character c = binder.player1Object.GetComponent<Character>();
        if (c == null)
        {
            Debug.LogWarning("HPTrackerBinder.player1Object has no Character component. Player1 cannot be auto-assigned.");
        }
        return c;
    }

    private static void ValidatePlayer2Setup(HPTrackerBinder binder)
    {
        if (binder == null) return;
        if (binder.player2Object == null) return;

        Character p2 = binder.player2Object.GetComponent<Character>();
        if (p2 == null)
        {
            Debug.LogWarning("Player2 object is assigned on HPTrackerBinder but has no Character component.");
            return;
        }

        if (p2.characterData == null)
        {
            Debug.LogWarning("Player2 Character has no CharacterData assigned. Assign it in the Character component inspector (Player2 keeps its inspector CharacterData).");
        }
    }

    private void ApplyOrValidatePlayer2Setup(HPTrackerBinder binder)
    {
        if (binder == null || binder.player2Object == null)
        {
            return;
        }

        Character p2 = binder.player2Object.GetComponent<Character>();
        if (p2 == null)
        {
            Debug.LogWarning("Player2 object is assigned on HPTrackerBinder but has no Character component.");
            return;
        }

        if (overridePlayer2FromSelection && player2CharacterDataOverride != null)
        {
            p2.SetCharacterData(player2CharacterDataOverride);
            Debug.Log($"Applied override character to Player2: {player2CharacterDataOverride.characterName}");
        }
        else
        {
            // Keep manual inspector configuration.
            if (p2.characterData == null)
            {
                Debug.LogWarning("Player2 Character has no CharacterData assigned. Assign it in the Character component inspector.");
            }
        }
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
    
    private void NotifyHPTrackerBinder()
    {
        // Keep HP/UI tracking logic outside GameManager.
        // If a HPTrackerBinder exists in the scene and its Player1 slot is empty,
        // we bind the active character object to it.
        if (!IsMainGameScene(currentSceneName))
        {
            return;
        }

        if (activeCharacterObject == null)
        {
            return;
        }

        HPTrackerBinder binder = GetHPTrackerBinder();
        if (binder == null)
        {
            return;
        }

        if (binder.player1Object == null)
        {
            binder.SetPlayer1(activeCharacterObject);
        }
        else
        {
            // Respect manual inspector assignments; just re-bind in case UI/player refs changed.
            binder.RefreshBindings();
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

    /// <summary>
    /// Resets persistent game state when starting a new run from the Main Menu.
    /// This intentionally does NOT destroy the GameManager singleton.
    /// </summary>
    public void ResetGameState()
    {
        // Reset selection + spawned character reference
        ResetSelectedCharacter();

        // Reset graveyard
        ClearGraveyard();

        // Reset turn system
        if (turnManager != null)
        {
            turnManager.ResetTurn(GetStartingTurnOwner());
        }

        // Reset optional overrides to a safe default
        overridePlayer2FromSelection = false;
        player2CharacterDataOverride = null;

        // Reset any existing runtime characters (in case you returned to main menu
        // with persistent objects still alive).
        ResetRuntimeCharacterStates();

        // Clear cached binder reference; next scene can provide a new binder
        hpTrackerBinder = null;

        Debug.Log("GameManager: Game state reset.");
    }

    public TurnManager.TurnOwner GetStartingTurnOwner()
    {
        return startingTurnOwner;
    }

    public void SetStartingTurnOwner(TurnManager.TurnOwner owner, bool applyImmediately = false)
    {
        startingTurnOwner = owner;

        if (applyImmediately && turnManager != null)
        {
            turnManager.ResetTurn(startingTurnOwner);
        }
    }

    private static void ResetRuntimeCharacterStates()
    {
        Character[] characters = FindObjectsOfType<Character>(true);
        if (characters == null || characters.Length == 0) return;

        for (int i = 0; i < characters.Length; i++)
        {
            Character c = characters[i];
            if (c == null) continue;
            if (c.characterData == null) continue;
            c.ResetStats();
        }
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