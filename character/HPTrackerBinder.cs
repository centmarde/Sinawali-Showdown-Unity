using UnityEngine;

/// <summary>
/// Binds one or two player objects (with a Character component) to one or two UI trackers
/// (CharacterUIAutoCreate). This lets you manually assign Player1/Player2 in the inspector
/// instead of keeping that logic inside GameManager.
/// 
/// - Player objects: assign GameObjects in the inspector (player1Object/player2Object)
/// - UI objects: optionally assign CharacterUIAutoCreate references (player1UI/player2UI)
/// - If a UI reference is missing and autoFindUIs is enabled, the script will try to find
///   CharacterUIAutoCreate instances in the scene and use the first for Player1, second for Player2.
/// </summary>
public class HPTrackerBinder : MonoBehaviour
{
    [Header("Players (Manual Assign)")]
    [Tooltip("Drag the Player1 GameObject here (must have a Character component).")]
    public GameObject player1Object;

    [Tooltip("Drag the Player2 GameObject here (optional; must have a Character component).")]
    public GameObject player2Object;

    [Header("UI Trackers (Optional)")]
    [Tooltip("UI tracker for Player1 (CharacterUIAutoCreate). If empty, auto-find can be used.")]
    public CharacterUIAutoCreate player1UI;

    [Tooltip("UI tracker for Player2 (CharacterUIAutoCreate). If empty, auto-find can be used.")]
    public CharacterUIAutoCreate player2UI;

    [Header("Options")]
    [Tooltip("If true and UI fields are empty, tries to find CharacterUIAutoCreate instances in the scene.")]
    public bool autoFindUIs = true;

    [Header("Winner")]
    [SerializeField] private bool declareWinnerOnZeroHP = true;
    [SerializeField] private string winnerSceneName = "WinnerScene";

    [SerializeField, Tooltip("Resolved Character component for Player1 (read-only).")]
    private Character player1Character;

    [SerializeField, Tooltip("Resolved Character component for Player2 (read-only).")]
    private Character player2Character;

    private bool winnerTriggered;

    public Character GetPlayer1Character()
    {
        return player1Character;
    }

    public Character GetPlayer2Character()
    {
        return player2Character;
    }

    public void ConfigureWinnerScene(string sceneName, bool enableWinner)
    {
        declareWinnerOnZeroHP = enableWinner;
        if (!string.IsNullOrEmpty(sceneName))
        {
            winnerSceneName = sceneName;
        }
    }

    private void OnEnable()
    {
        RefreshBindings();
    }

    private void OnDisable()
    {
        UnbindWinnerEvents();
        // Cleanly detach to avoid dangling event subscriptions inside CharacterUIAutoCreate
        if (player1UI != null) player1UI.SetTrackedCharacter(null);
        if (player2UI != null) player2UI.SetTrackedCharacter(null);
    }

    private void OnValidate()
    {
        // Keep things up-to-date in editor when assignments change.
        if (!Application.isPlaying)
        {
            ResolveCharacters();
        }
    }

    [ContextMenu("Refresh Bindings")]
    public void RefreshBindings()
    {
        ResolveCharacters();
        ResolveUIsIfNeeded();
        BindUIs();
        BindWinnerEvents();
    }

    public void SetPlayer1(GameObject playerObject)
    {
        player1Object = playerObject;
        RefreshBindings();
    }

    public void SetPlayer2(GameObject playerObject)
    {
        player2Object = playerObject;
        RefreshBindings();
    }

    private void ResolveCharacters()
    {
        UnbindWinnerEvents();
        player1Character = ResolveCharacterFromObject(player1Object);
        player2Character = ResolveCharacterFromObject(player2Object);
    }

    private static Character ResolveCharacterFromObject(GameObject obj)
    {
        if (obj == null) return null;
        return obj.GetComponent<Character>();
    }

    private void ResolveUIsIfNeeded()
    {
        if (!autoFindUIs) return;

        if (player1UI != null && player2UI != null) return;

        // Include inactive objects so you can keep UI disabled by default.
        CharacterUIAutoCreate[] foundUIs = FindObjectsOfType<CharacterUIAutoCreate>(true);
        if (foundUIs == null || foundUIs.Length == 0) return;

        // Prefer slot-based assignment.
        if (player1UI == null)
        {
            foreach (var ui in foundUIs)
            {
                if (ui != null && ui.playerSlot == CharacterUIAutoCreate.PlayerSlot.Player1)
                {
                    player1UI = ui;
                    break;
                }
            }
        }

        if (player2UI == null)
        {
            foreach (var ui in foundUIs)
            {
                if (ui != null && ui.playerSlot == CharacterUIAutoCreate.PlayerSlot.Player2)
                {
                    player2UI = ui;
                    break;
                }
            }
        }

        // Fallback: name-based assignment.
        if (player1UI == null)
        {
            foreach (var ui in foundUIs)
            {
                if (ui == null) continue;
                string n = ui.gameObject.name.ToLowerInvariant();
                if (n.Contains("player1") || n.Contains("p1"))
                {
                    player1UI = ui;
                    break;
                }
            }
        }

        if (player2UI == null)
        {
            foreach (var ui in foundUIs)
            {
                if (ui == null) continue;
                string n = ui.gameObject.name.ToLowerInvariant();
                if (n.Contains("player2") || n.Contains("p2"))
                {
                    player2UI = ui;
                    break;
                }
            }
        }

        // Last resort: first/second found.
        if (player1UI == null) player1UI = foundUIs[0];
        if (player2UI == null && foundUIs.Length > 1) player2UI = foundUIs[1];
    }

    private void BindUIs()
    {
        if (player1UI != null)
        {
            if (player1Character != null)
            {
                player1UI.SetTrackedCharacter(player1Character);
            }
            else
            {
                player1UI.SetTrackedCharacter(null);
            }
        }

        if (player2UI != null)
        {
            if (player2Character != null)
            {
                player2UI.SetTrackedCharacter(player2Character);
            }
            else
            {
                player2UI.SetTrackedCharacter(null);
            }
        }
    }

    private void BindWinnerEvents()
    {
        if (!declareWinnerOnZeroHP)
        {
            return;
        }

        if (player1Character != null)
        {
            player1Character.OnHPChanged -= OnPlayer1HPChanged;
            player1Character.OnHPChanged += OnPlayer1HPChanged;
            player1Character.OnCharacterDied -= OnPlayer1Died;
            player1Character.OnCharacterDied += OnPlayer1Died;
        }

        if (player2Character != null)
        {
            player2Character.OnHPChanged -= OnPlayer2HPChanged;
            player2Character.OnHPChanged += OnPlayer2HPChanged;
            player2Character.OnCharacterDied -= OnPlayer2Died;
            player2Character.OnCharacterDied += OnPlayer2Died;
        }
    }

    private void UnbindWinnerEvents()
    {
        if (player1Character != null)
        {
            player1Character.OnHPChanged -= OnPlayer1HPChanged;
            player1Character.OnCharacterDied -= OnPlayer1Died;
        }

        if (player2Character != null)
        {
            player2Character.OnHPChanged -= OnPlayer2HPChanged;
            player2Character.OnCharacterDied -= OnPlayer2Died;
        }
    }

    private void OnPlayer1HPChanged(int currentHP, int maxHP)
    {
        if (currentHP <= 0)
        {
            TriggerWinner(player2Character, "Player 2");
        }
    }

    private void OnPlayer2HPChanged(int currentHP, int maxHP)
    {
        if (currentHP <= 0)
        {
            TriggerWinner(player1Character, "Player 1");
        }
    }

    private void OnPlayer1Died()
    {
        TriggerWinner(player2Character, "Player 2");
    }

    private void OnPlayer2Died()
    {
        TriggerWinner(player1Character, "Player 1");
    }

    private void TriggerWinner(Character winner, string fallbackName)
    {
        if (winnerTriggered)
        {
            return;
        }

        winnerTriggered = true;

        string winnerName = fallbackName;
        if (winner != null)
        {
            winnerName = winner.GetCharacterName();
        }

        WinnerState.SetWinner(winnerName);
        Time.timeScale = 1f;

        if (string.IsNullOrEmpty(winnerSceneName))
        {
            Debug.LogWarning("HPTrackerBinder: Winner scene name is empty.", this);
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(winnerSceneName);
    }
}
