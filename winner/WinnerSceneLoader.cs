using UnityEngine;
using UnityEngine.SceneManagement;

public class WinnerSceneLoader : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string winnerSceneName = "WinnerScene";

    [Header("Players (Optional)")]
    [SerializeField] private Character player1Character;
    [SerializeField] private Character player2Character;

    [Header("Auto Resolve")]
    [SerializeField] private bool autoResolvePlayers = true;
    [SerializeField] private bool persistAcrossScenes = true;
    [SerializeField] private bool preferHPTrackerBinder = true;

    [Header("HP Tracker")]
    [SerializeField] private HPTrackerBinder hpTrackerBinder;

    private bool hasTriggered;
    private bool useBinderForWinner;

    private void OnEnable()
    {
        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (autoResolvePlayers)
        {
            ResolvePlayers();
        }

        if (!useBinderForWinner)
        {
            BindCharacterEvents(player1Character, true);
            BindCharacterEvents(player2Character, false);
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (!useBinderForWinner)
        {
            UnbindCharacterEvents(player1Character, true);
            UnbindCharacterEvents(player2Character, false);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!autoResolvePlayers || hasTriggered)
        {
            return;
        }

        ResolvePlayers();

        if (!useBinderForWinner)
        {
            UnbindCharacterEvents(player1Character, true);
            UnbindCharacterEvents(player2Character, false);
            BindCharacterEvents(player1Character, true);
            BindCharacterEvents(player2Character, false);
        }
    }

    private void ResolvePlayers()
    {
        if (player1Character != null && player2Character != null)
        {
            return;
        }

        if (preferHPTrackerBinder)
        {
            ResolveFromHPTrackerBinder();
            if (player1Character != null && player2Character != null)
            {
                useBinderForWinner = true;
                return;
            }
        }

        // Prefer GameManager runtime references (supports prefab-based players).
        if (GameManager.Instance != null)
        {
            Character p1;
            Character p2;
            if (GameManager.Instance.TryGetPlayerCharacters(out p1, out p2))
            {
                if (player1Character == null)
                {
                    player1Character = p1;
                }

                if (player2Character == null)
                {
                    player2Character = p2;
                }
            }
        }

        if (player1Character != null && player2Character != null)
        {
            return;
        }

        // Fallback: HPTrackerBinder assignments.
        ResolveFromHPTrackerBinder();

        if (player1Character == null || player2Character == null)
        {
            Character[] allCharacters = FindObjectsOfType<Character>();
            if (allCharacters != null && allCharacters.Length > 0)
            {
                if (player1Character == null)
                {
                    player1Character = allCharacters[0];
                }

                if (player2Character == null && allCharacters.Length > 1)
                {
                    player2Character = allCharacters[1];
                }
            }
        }

        if (player1Character == null || player2Character == null)
        {
            Debug.LogWarning("WinnerSceneLoader: Could not resolve both players. Check GameManager, HPTrackerBinder, or scene Character objects.", this);
        }
    }

    private void ResolveFromHPTrackerBinder()
    {
        if (hpTrackerBinder == null)
        {
            if (GameManager.Instance != null && GameManager.Instance.hpTrackerBinder != null)
            {
                hpTrackerBinder = GameManager.Instance.hpTrackerBinder;
            }
            else
            {
                hpTrackerBinder = FindObjectOfType<HPTrackerBinder>();
            }
        }

        if (hpTrackerBinder == null)
        {
            return;
        }

        hpTrackerBinder.ConfigureWinnerScene(winnerSceneName, true);
        hpTrackerBinder.RefreshBindings();

        if (player1Character == null)
        {
            player1Character = hpTrackerBinder.GetPlayer1Character();
        }

        if (player2Character == null)
        {
            player2Character = hpTrackerBinder.GetPlayer2Character();
        }
    }

    private void BindCharacterEvents(Character character, bool isPlayer1)
    {
        if (character == null)
        {
            return;
        }

        character.OnCharacterDied -= isPlayer1 ? OnPlayer1Died : OnPlayer2Died;
        character.OnCharacterDied += isPlayer1 ? OnPlayer1Died : OnPlayer2Died;

        character.OnHPChanged -= isPlayer1 ? OnPlayer1HPChanged : OnPlayer2HPChanged;
        character.OnHPChanged += isPlayer1 ? OnPlayer1HPChanged : OnPlayer2HPChanged;
    }

    private void UnbindCharacterEvents(Character character, bool isPlayer1)
    {
        if (character == null)
        {
            return;
        }

        character.OnCharacterDied -= isPlayer1 ? OnPlayer1Died : OnPlayer2Died;
        character.OnHPChanged -= isPlayer1 ? OnPlayer1HPChanged : OnPlayer2HPChanged;
    }

    private void OnPlayer1Died()
    {
        TriggerWinner(player2Character, "Player 2");
    }

    private void OnPlayer2Died()
    {
        TriggerWinner(player1Character, "Player 1");
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

    private void TriggerWinner(Character winner, string fallbackName)
    {
        if (hasTriggered)
        {
            return;
        }

        hasTriggered = true;

        string winnerName = fallbackName;
        if (winner != null)
        {
            winnerName = winner.GetCharacterName();
        }

        WinnerState.SetWinner(winnerName);
        Time.timeScale = 1f;

        if (string.IsNullOrEmpty(winnerSceneName))
        {
            Debug.LogWarning("WinnerSceneLoader: Winner scene name is empty.");
            return;
        }

        SceneManager.LoadScene(winnerSceneName);
    }
}
