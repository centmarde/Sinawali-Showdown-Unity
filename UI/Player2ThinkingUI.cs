using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows a Player2 thinking countdown UI during Player2 Start phase.
/// Hides automatically when countdown ends or when turn changes away from Player2 Start.
/// </summary>
public class Player2ThinkingUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private GameObject thinkingPanel;
    [SerializeField] private CanvasGroup thinkingCanvasGroup;
    [SerializeField] private TextMeshProUGUI thinkingText;

    [Header("Display")]
    [SerializeField] private string thinkingPrefix = "PLAYER 2 THINKING";
    [SerializeField] private float countdownSeconds = 5f;
    [SerializeField] private bool useCanvasGroupWhenSelfHosted = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private Coroutine countdownRoutine;
    private TurnManager.TurnOwner lastOwner;
    private TurnManager.TurnPhase lastPhase;
    private int lastTurnNumber = -1;
    private bool hasCachedTurnState;

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
        RefreshFromCurrentTurnState();
        CacheTurnState();
    }

    private void OnDisable()
    {
        Unsubscribe();
        StopCountdown();
        HideThinkingUI();
    }

    private void ResolveReferences()
    {
        EnsureAuthoritativeTurnManager();

        if (thinkingPanel == null)
        {
            thinkingPanel = gameObject;
        }

        if (thinkingText == null)
        {
            thinkingText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (thinkingCanvasGroup == null)
        {
            GameObject canvasTarget = thinkingPanel != null ? thinkingPanel : gameObject;
            thinkingCanvasGroup = canvasTarget.GetComponent<CanvasGroup>();

            if (thinkingCanvasGroup == null && canvasTarget == gameObject && useCanvasGroupWhenSelfHosted)
            {
                thinkingCanvasGroup = canvasTarget.AddComponent<CanvasGroup>();

                if (showDebugInfo)
                {
                    Debug.Log("[Player2ThinkingUI] Added CanvasGroup to self-hosted panel for safe hide/show.", this);
                }
            }
        }
    }

    private void Subscribe()
    {
        if (turnManager == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("[Player2ThinkingUI] No TurnManager found.", this);
            }
            return;
        }

        turnManager.OnTurnPhaseChanged -= HandleTurnPhaseChanged;
        turnManager.OnTurnPhaseChanged += HandleTurnPhaseChanged;
    }

    private void Update()
    {
        EnsureAuthoritativeTurnManager();

        if (turnManager == null)
        {
            return;
        }

        if (!hasCachedTurnState)
        {
            RefreshFromCurrentTurnState();
            CacheTurnState();
            return;
        }

        if (turnManager.CurrentOwner != lastOwner || turnManager.CurrentPhase != lastPhase || turnManager.TurnNumber != lastTurnNumber)
        {
            HandleTurnPhaseChanged(turnManager.CurrentOwner, turnManager.CurrentPhase, turnManager.TurnNumber);
            CacheTurnState();
        }
    }

    private void Unsubscribe()
    {
        if (turnManager != null)
        {
            turnManager.OnTurnPhaseChanged -= HandleTurnPhaseChanged;
        }
    }

    private void RefreshFromCurrentTurnState()
    {
        if (turnManager == null)
        {
            HideThinkingUI();
            return;
        }

        bool shouldShow = turnManager.CurrentOwner == TurnManager.TurnOwner.Player2
                          && turnManager.CurrentPhase == TurnManager.TurnPhase.Start;

        if (shouldShow)
        {
            StartThinkingCountdown();
        }
        else
        {
            StopCountdown();
            HideThinkingUI();
        }
    }

    private void HandleTurnPhaseChanged(TurnManager.TurnOwner owner, TurnManager.TurnPhase phase, int turnNumber)
    {
        bool shouldShow = owner == TurnManager.TurnOwner.Player2 && phase == TurnManager.TurnPhase.Start;

        if (shouldShow)
        {
            StartThinkingCountdown();
        }
        else
        {
            StopCountdown();
            HideThinkingUI();
        }

        if (showDebugInfo)
        {
            Debug.Log($"[Player2ThinkingUI] Turn changed: {owner} | {phase} | Turn #{turnNumber} | ShowThinking={shouldShow}", this);
        }
    }

    private void StartThinkingCountdown()
    {
        StopCountdown();
        ShowThinkingUI();
        countdownRoutine = StartCoroutine(CountdownRoutine());
    }

    private void StopCountdown()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }
    }

    private IEnumerator CountdownRoutine()
    {
        float duration = Mathf.Max(0f, countdownSeconds);
        float endTime = Time.time + duration;

        while (Time.time < endTime)
        {
            float remaining = Mathf.Max(0f, endTime - Time.time);
            int secondsLeft = Mathf.CeilToInt(remaining);
            SetThinkingText(secondsLeft);
            yield return null;
        }

        SetThinkingText(0);
        HideThinkingUI();
        countdownRoutine = null;

        if (showDebugInfo)
        {
            Debug.Log("[Player2ThinkingUI] Countdown finished, UI hidden.", this);
        }
    }

    private void CacheTurnState()
    {
        if (turnManager == null)
        {
            hasCachedTurnState = false;
            return;
        }

        lastOwner = turnManager.CurrentOwner;
        lastPhase = turnManager.CurrentPhase;
        lastTurnNumber = turnManager.TurnNumber;
        hasCachedTurnState = true;
    }

    private void EnsureAuthoritativeTurnManager()
    {
        TurnManager preferred = null;

        if (GameManager.Instance != null && GameManager.Instance.turnManager != null)
        {
            preferred = GameManager.Instance.turnManager;
        }

        if (preferred == null)
        {
            preferred = FindObjectOfType<TurnManager>();
        }

        if (preferred == turnManager)
        {
            return;
        }

        if (turnManager != null)
        {
            turnManager.OnTurnPhaseChanged -= HandleTurnPhaseChanged;
        }

        turnManager = preferred;
        hasCachedTurnState = false;

        if (turnManager != null)
        {
            turnManager.OnTurnPhaseChanged -= HandleTurnPhaseChanged;
            turnManager.OnTurnPhaseChanged += HandleTurnPhaseChanged;

            if (showDebugInfo)
            {
                Debug.Log($"[Player2ThinkingUI] Bound to TurnManager: {turnManager.name} (scene: {turnManager.gameObject.scene.name})", this);
            }
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning("[Player2ThinkingUI] Could not bind to any TurnManager.", this);
        }
    }

    private void SetThinkingText(int secondsLeft)
    {
        if (thinkingText != null)
        {
            thinkingText.text = $"{thinkingPrefix}... {secondsLeft}";
        }
    }

    private void ShowThinkingUI()
    {
        if (thinkingPanel == gameObject && useCanvasGroupWhenSelfHosted)
        {
            if (thinkingCanvasGroup != null)
            {
                SetCanvasVisible(true);
            }
            else if (thinkingText != null)
            {
                thinkingText.gameObject.SetActive(true);
            }
            return;
        }

        if (thinkingPanel != null && !thinkingPanel.activeSelf)
        {
            thinkingPanel.SetActive(true);
        }

        SetCanvasVisible(true);
    }

    private void HideThinkingUI()
    {
        if (thinkingPanel == gameObject && useCanvasGroupWhenSelfHosted)
        {
            if (thinkingCanvasGroup != null)
            {
                SetCanvasVisible(false);
            }
            else if (thinkingText != null)
            {
                thinkingText.gameObject.SetActive(false);
            }
            return;
        }

        if (thinkingPanel != null && thinkingPanel.activeSelf)
        {
            thinkingPanel.SetActive(false);
        }

        SetCanvasVisible(false);
    }

    private void SetCanvasVisible(bool visible)
    {
        if (thinkingCanvasGroup == null)
        {
            return;
        }

        thinkingCanvasGroup.alpha = visible ? 1f : 0f;
        thinkingCanvasGroup.interactable = visible;
        thinkingCanvasGroup.blocksRaycasts = visible;
    }

    [ContextMenu("Debug: Start Thinking Countdown")]
    public void DebugStartCountdown()
    {
        StartThinkingCountdown();
    }

    [ContextMenu("Debug: Hide Thinking UI")]
    public void DebugHideThinkingUI()
    {
        StopCountdown();
        HideThinkingUI();
    }
}
