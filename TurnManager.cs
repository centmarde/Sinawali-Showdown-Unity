using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Minimal turn/phase state machine for a future turn-based game.
/// Kept separate from GameManager so you can evolve rules without bloating GameManager.
/// </summary>
public class TurnManager : MonoBehaviour
{
    public enum TurnOwner
    {
        Player1,
        Player2
    }

    public enum TurnPhase
    {
        Start,
        Main,
        End
    }

    [Header("Turn State")]
    [SerializeField] private TurnOwner currentOwner = TurnOwner.Player1;
    [SerializeField] private TurnPhase currentPhase = TurnPhase.Start;
    [SerializeField] private int turnNumber = 1;

    [Header("System References")]
    [SerializeField] private HandManager player1HandManager;
    [SerializeField] private CardFetcherEnemy player2CardFetcher;
    [SerializeField] private Player2AttackTeleportOnAITurn player2AttackTeleport;
    [SerializeField] private bool autoSetupReferences = true;

    [Header("AI Turn Timing")]
    [SerializeField] private float player2SelectionDelaySeconds = 5f;

    [Header("Player1 Attack (QTE)")]
    [Tooltip("If enabled and an ArrowKeyAttackQTE is active in the scene, Player1 attack cards will not immediately advance the turn. The QTE system is expected to call AdvanceTurnAfterExternalResolution().")]
    [SerializeField] private bool deferPlayer1AttackTurnAdvanceWhenQTEPresent = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    public TurnOwner CurrentOwner => currentOwner;
    public TurnPhase CurrentPhase => currentPhase;
    public int TurnNumber => turnNumber;

    public event Action<TurnOwner, TurnPhase, int> OnTurnPhaseChanged;
    public event Action<TurnOwner> OnPlayerTurnStarted;
    public event Action<TurnOwner> OnPlayerTurnEnded;

    private Coroutine player2SelectionRoutine;

    private void OnEnable()
    {
        SetupReferences();
        InitializeFromGameManagerChoice();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();

        if (player2SelectionRoutine != null)
        {
            StopCoroutine(player2SelectionRoutine);
            player2SelectionRoutine = null;
        }
    }

    private void SetupReferences()
    {
        if (!autoSetupReferences) return;

        if (player1HandManager == null)
        {
            player1HandManager = FindObjectOfType<HandManager>();
        }

        if (player2CardFetcher == null)
        {
            player2CardFetcher = FindObjectOfType<CardFetcherEnemy>();
        }

        if (player2AttackTeleport == null)
        {
            player2AttackTeleport = FindObjectOfType<Player2AttackTeleportOnAITurn>();
        }

        SubscribeToEvents();

        if (showDebugInfo)
        {
            Debug.Log($"[TurnManager] Setup complete - Player1: {player1HandManager != null}, Player2: {player2CardFetcher != null}, Teleport: {player2AttackTeleport != null}");
        }
    }

    private void InitializeFromGameManagerChoice()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        TurnOwner startingOwner = GameManager.Instance.GetStartingTurnOwner();
        turnNumber = 1;
        SetPhase(startingOwner, TurnPhase.Start);

        if (showDebugInfo)
        {
            Debug.Log($"[TurnManager] Initialized from GameManager choice. Starting owner: {startingOwner}");
        }
    }

    private void SubscribeToEvents()
    {
        // Player1 card confirmation
        if (player1HandManager != null)
        {
            player1HandManager.OnCardConfirmed -= HandlePlayer1CardConfirmed;
            player1HandManager.OnCardConfirmed += HandlePlayer1CardConfirmed;
        }

        // Player2 card selection
        if (player2CardFetcher != null)
        {
            player2CardFetcher.OnAICardSelected -= HandlePlayer2CardSelected;
            player2CardFetcher.OnAICardSelected += HandlePlayer2CardSelected;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (player1HandManager != null)
        {
            player1HandManager.OnCardConfirmed -= HandlePlayer1CardConfirmed;
        }

        if (player2CardFetcher != null)
        {
            player2CardFetcher.OnAICardSelected -= HandlePlayer2CardSelected;
        }
    }

    /// <summary>
    /// Called when Player1 confirms a card attack
    /// </summary>
    private void HandlePlayer1CardConfirmed(CardInspector inspector, CardData card)
    {
        if (card == null) return;
        if (card.Type != CardType.Attack) return;

        if (deferPlayer1AttackTurnAdvanceWhenQTEPresent && FindObjectOfType<ArrowKeyAttackQTE>() != null)
        {
            if (showDebugInfo)
            {
                Debug.Log("[TurnManager] Player1 attack confirmed, but turn advance deferred to ArrowKeyAttackQTE.");
            }
            return;
        }

        if (showDebugInfo)
        {
            Debug.Log($"[TurnManager] Player1 confirmed attack card: {card.Title}");
        }

        // Advance to Player2's turn
        AdvanceTurn();
    }

    /// <summary>
    /// Allows external systems (e.g., ArrowKeyAttackQTE) to advance the turn after they finish resolving Player1's attack.
    /// Safe to call only when it is currently Player1's turn.
    /// </summary>
    public void AdvanceTurnAfterExternalResolution()
    {
        if (currentOwner != TurnOwner.Player1)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"[TurnManager] AdvanceTurnAfterExternalResolution called while currentOwner is {currentOwner}; ignoring.");
            }
            return;
        }

        AdvanceTurn();
    }

    /// <summary>
    /// Called when Player2 AI selects a card
    /// </summary>
    private void HandlePlayer2CardSelected(CardData card)
    {
        if (card == null) return;

        if (showDebugInfo)
        {
            Debug.Log($"[TurnManager] Player2 selected card: {card.Title}");
        }

        // Trigger Player2's attack animation
        TriggerPlayer2Attack(card);
    }

    /// <summary>
    /// Triggers Player2's attack animation
    /// </summary>
    private void TriggerPlayer2Attack(CardData card)
    {
        if (showDebugInfo)
        {
            Debug.Log($"[TurnManager] Triggering Player2 attack animation for: {card.Title}");
        }

        // Try to find Player1/Player2 and apply AI attack flow.
        if (GameManager.Instance != null && GameManager.Instance.TryGetPlayerCharacters(out Character player1, out Character player2))
        {
            if (player2 != null)
            {
                Animator player2Animator = player2.GetComponent<Animator>();

                // Prefer teleport attack sequence when available.
                if (player2AttackTeleport != null)
                {
                    player2AttackTeleport.TriggerAttack(player2.transform, player2Animator);

                    if (showDebugInfo)
                    {
                        Debug.Log("[TurnManager] Player2 attack teleport sequence triggered");
                    }
                }
                else if (player2Animator != null)
                {
                    player2Animator.SetTrigger("Attack");

                    if (showDebugInfo)
                    {
                        Debug.Log("[TurnManager] Player2 attack animation triggered");
                    }
                }
                else if (showDebugInfo)
                {
                    Debug.LogWarning("[TurnManager] Player2 has no Animator and no teleport script assigned.");
                }

                // Reflect damage on Player1 during Player2 turn.
                if (player1 != null && card.Damage > 0)
                {
                    player1.TakeDamage(card.Damage);

                    if (showDebugInfo)
                    {
                        Debug.Log($"[TurnManager] Player2 dealt {card.Damage} damage to Player1 using '{card.Title}'");
                    }
                }
                else if (showDebugInfo && player1 == null)
                {
                    Debug.LogWarning("[TurnManager] Player1 not found; could not apply Player2 attack damage.");
                }

                // End AI turn and return control to Player1.
                AdvanceTurn();
            }
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning("[TurnManager] Could not resolve player characters from GameManager for Player2 attack.");
        }
    }

    [ContextMenu("Next Phase")]
    public void NextPhase()
    {
        switch (currentPhase)
        {
            case TurnPhase.Start:
                SetPhase(currentOwner, TurnPhase.Main);
                break;
            case TurnPhase.Main:
                SetPhase(currentOwner, TurnPhase.End);
                break;
            case TurnPhase.End:
                AdvanceTurn();
                break;
            default:
                SetPhase(currentOwner, TurnPhase.Main);
                break;
        }
    }

    [ContextMenu("Reset Turn")]
    public void ResetTurn(TurnOwner startingOwner = TurnOwner.Player1)
    {
        turnNumber = 1;
        SetPhase(startingOwner, TurnPhase.Start);
    }

    public void SetPhase(TurnOwner owner, TurnPhase phase)
    {
        currentOwner = owner;
        currentPhase = phase;
        UpdatePlayer1HandVisibility();

        // Enter-turn hook: when Player2 reaches Start phase, force AI card selection.
        if (currentOwner == TurnOwner.Player2 && currentPhase == TurnPhase.Start)
        {
            SchedulePlayer2CardSelection();
        }
        else if (player2SelectionRoutine != null)
        {
            StopCoroutine(player2SelectionRoutine);
            player2SelectionRoutine = null;
        }

        OnTurnPhaseChanged?.Invoke(currentOwner, currentPhase, turnNumber);
    }

    private void TriggerPlayer2CardSelection()
    {
        if (player2CardFetcher == null)
        {
            if (autoSetupReferences)
            {
                player2CardFetcher = FindObjectOfType<CardFetcherEnemy>();
            }

            if (player2CardFetcher == null)
            {
                Debug.LogWarning("[TurnManager] Player2 turn started, but no CardFetcherEnemy is assigned/found.");
                return;
            }
        }

        if (showDebugInfo)
        {
            Debug.Log("[TurnManager] Player2 Start phase reached. Triggering AI card selection...");
        }

        if (player2CardFetcher.GetAIHand().Count == 0)
        {
            if (showDebugInfo)
            {
                Debug.Log("[TurnManager] AI hand is empty. Running SetupAIHand() before selection.");
            }

            player2CardFetcher.SetupAIHand();
        }

        player2CardFetcher.SelectRandomCard();
    }

    private void SchedulePlayer2CardSelection()
    {
        if (player2SelectionRoutine != null)
        {
            StopCoroutine(player2SelectionRoutine);
        }

        player2SelectionRoutine = StartCoroutine(Player2SelectionDelayRoutine());
    }

    private IEnumerator Player2SelectionDelayRoutine()
    {
        float delay = Mathf.Max(0f, player2SelectionDelaySeconds);

        if (showDebugInfo)
        {
            Debug.Log($"[TurnManager] Player2 AI delay started ({delay:0.##}s) before selecting a card.");
        }

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (currentOwner != TurnOwner.Player2 || currentPhase != TurnPhase.Start)
        {
            if (showDebugInfo)
            {
                Debug.Log("[TurnManager] Player2 AI delay finished, but turn/phase changed. Selection canceled.");
            }

            player2SelectionRoutine = null;
            yield break;
        }

        TriggerPlayer2CardSelection();
        player2SelectionRoutine = null;
    }

    private void UpdatePlayer1HandVisibility()
    {
        if (player1HandManager == null)
        {
            return;
        }

        bool shouldShowHand = currentOwner == TurnOwner.Player1;
        GameObject handObject = player1HandManager.gameObject;

        if (handObject.activeSelf != shouldShowHand)
        {
            handObject.SetActive(shouldShowHand);

            if (showDebugInfo)
            {
                Debug.Log($"[TurnManager] Player1 hand UI {(shouldShowHand ? "shown" : "hidden")} for {currentOwner} turn");
            }
        }
    }

    private void AdvanceTurn()
    {
        currentOwner = currentOwner == TurnOwner.Player1 ? TurnOwner.Player2 : TurnOwner.Player1;

        if (currentOwner == TurnOwner.Player1)
        {
            turnNumber++;
        }

        SetPhase(currentOwner, TurnPhase.Start);
    }
}
