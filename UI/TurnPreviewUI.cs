using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Displays current turn information to the player
/// Shows turn owner, phase, and turn number with animations
/// </summary>
public class TurnPreviewUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    
    [Header("Display UI Elements")]
    [SerializeField] private TextMeshProUGUI turnOwnerText;
    [SerializeField] private TextMeshProUGUI turnPhaseText;
    [SerializeField] private TextMeshProUGUI turnNumberText;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private Image highlightImage; // Background that changes color per player
    
    [Header("Display Settings")]
    [SerializeField] private Color player1Color = new Color(0.2f, 0.6f, 1f, 1f); // Blue
    [SerializeField] private Color player2Color = new Color(1f, 0.3f, 0.3f, 1f); // Red
    
    [Header("Animation Settings")]
    [SerializeField] private bool animateOnTurnChange = true;
    [SerializeField] private float scaleDuration = 0.3f;
    [SerializeField] private float scaleMultiplier = 1.2f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private Coroutine scaleRoutine;
    private TurnManager.TurnOwner lastOwner;
    private TurnManager.TurnPhase lastPhase;
    private int lastTurnNumber = -1;
    private bool hasCachedState = false;
    private bool warnedMissingReferences = false;

    void OnEnable()
    {
        TryWireTurnManager();
        AutoFindMissingUIReferences();
    }

    void OnDisable()
    {
        if (turnManager != null)
        {
            turnManager.OnTurnPhaseChanged -= HandleTurnPhaseChanged;
        }

        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
            scaleRoutine = null;
        }
    }

    void Start()
    {
        // Initialize display with current state
        UpdateTurnDisplay();
        CacheCurrentTurnState();
    }

    void Update()
    {
        EnsureAuthoritativeTurnManager();

        // Runtime fallback: recover missing manager or missed events.
        if (turnManager == null)
        {
            TryWireTurnManager();
            if (turnManager == null)
            {
                return;
            }
        }

        if (!hasCachedState)
        {
            CacheCurrentTurnState();
            return;
        }

        if (turnManager.CurrentOwner != lastOwner || turnManager.CurrentPhase != lastPhase || turnManager.TurnNumber != lastTurnNumber)
        {
            HandleTurnPhaseChanged(turnManager.CurrentOwner, turnManager.CurrentPhase, turnManager.TurnNumber);
            CacheCurrentTurnState();
        }
    }

    /// <summary>
    /// Called when turn phase changes
    /// </summary>
    private void HandleTurnPhaseChanged(TurnManager.TurnOwner owner, TurnManager.TurnPhase phase, int turnNumber)
    {
        UpdateTurnDisplay();

        if (animateOnTurnChange)
        {
            PlayTurnChangeAnimation();
        }

        if (showDebugInfo)
        {
            Debug.Log($"[TurnPreviewUI] Turn changed - {owner} | {phase} | Turn #{turnNumber}");
        }
    }

    /// <summary>
    /// Updates all display elements with current turn information
    /// </summary>
    void UpdateTurnDisplay()
    {
        if (turnManager == null) return;

        AutoFindMissingUIReferences();

        // Update turn owner
        if (turnOwnerText != null)
        {
            string ownerText = turnManager.CurrentOwner == TurnManager.TurnOwner.Player1 ? "PLAYER 1 TURN" : "PLAYER 2 TURN";
            turnOwnerText.text = ownerText;
        }

        // Update phase
        if (turnPhaseText != null)
        {
            turnPhaseText.text = $"Phase: {turnManager.CurrentPhase}";
        }

        // Update turn number
        if (turnNumberText != null)
        {
            turnNumberText.text = $"Turn #{turnManager.TurnNumber}";
        }

        // Update highlight color based on player
        if (highlightImage != null)
        {
            Color playerColor = turnManager.CurrentOwner == TurnManager.TurnOwner.Player1 ? player1Color : player2Color;
            highlightImage.color = playerColor;
        }

        if (showDebugInfo)
        {
            Debug.Log($"[TurnPreviewUI] Display updated - Owner: {turnManager.CurrentOwner}, Phase: {turnManager.CurrentPhase}, Turn: {turnManager.TurnNumber}");
        }

        if (!warnedMissingReferences && (turnOwnerText == null || turnPhaseText == null || turnNumberText == null))
        {
            warnedMissingReferences = true;
            Debug.LogWarning("[TurnPreviewUI] Some text references are missing. Assign TurnOwnerText, TurnPhaseText, and TurnNumberText in Inspector.", this);
        }
    }

    private void TryWireTurnManager()
    {
        TurnManager preferred = GetPreferredTurnManager();

        if (preferred != null && preferred != turnManager)
        {
            if (turnManager != null)
            {
                turnManager.OnTurnPhaseChanged -= HandleTurnPhaseChanged;
            }

            turnManager = preferred;
            hasCachedState = false;

            if (showDebugInfo)
            {
                Debug.Log($"[TurnPreviewUI] Bound to TurnManager: {turnManager.name} (scene: {turnManager.gameObject.scene.name})", this);
            }
        }

        if (turnManager != null)
        {
            turnManager.OnTurnPhaseChanged -= HandleTurnPhaseChanged;
            turnManager.OnTurnPhaseChanged += HandleTurnPhaseChanged;
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning("[TurnPreviewUI] No TurnManager found in scene!", this);
        }
    }

    private void EnsureAuthoritativeTurnManager()
    {
        TurnManager preferred = GetPreferredTurnManager();
        if (preferred != null && preferred != turnManager)
        {
            TryWireTurnManager();
            UpdateTurnDisplay();
            CacheCurrentTurnState();
        }
    }

    private TurnManager GetPreferredTurnManager()
    {
        if (GameManager.Instance != null && GameManager.Instance.turnManager != null)
        {
            return GameManager.Instance.turnManager;
        }

        if (turnManager != null)
        {
            return turnManager;
        }

        return FindObjectOfType<TurnManager>();
    }

    private void CacheCurrentTurnState()
    {
        if (turnManager == null)
        {
            return;
        }

        lastOwner = turnManager.CurrentOwner;
        lastPhase = turnManager.CurrentPhase;
        lastTurnNumber = turnManager.TurnNumber;
        hasCachedState = true;
    }

    private void AutoFindMissingUIReferences()
    {
        if (turnOwnerText != null && turnPhaseText != null && turnNumberText != null && highlightImage != null && panelCanvasGroup != null)
        {
            return;
        }

        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allChildren.Length; i++)
        {
            Transform child = allChildren[i];
            string childName = child.name.ToLowerInvariant();

            if (turnOwnerText == null && childName.Contains("owner"))
            {
                turnOwnerText = child.GetComponent<TextMeshProUGUI>();
            }
            else if (turnPhaseText == null && childName.Contains("phase"))
            {
                turnPhaseText = child.GetComponent<TextMeshProUGUI>();
            }
            else if (turnNumberText == null && childName.Contains("number"))
            {
                turnNumberText = child.GetComponent<TextMeshProUGUI>();
            }
            else if (highlightImage == null && childName.Contains("highlight"))
            {
                highlightImage = child.GetComponent<Image>();
            }
        }

        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = GetComponent<CanvasGroup>();
        }
    }

    /// <summary>
    /// Plays animation when turn changes
    /// </summary>
    void PlayTurnChangeAnimation()
    {
        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
        }

        scaleRoutine = StartCoroutine(ScaleAnimationRoutine());
    }

    /// <summary>
    /// Coroutine for scale animation
    /// </summary>
    IEnumerator ScaleAnimationRoutine()
    {
        float elapsed = 0f;
        float halfDuration = scaleDuration / 2f;

        // Scale up
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            t = scaleCurve.Evaluate(t);
            
            if (panelCanvasGroup != null)
            {
                transform.localScale = Vector3.one * Mathf.Lerp(1f, scaleMultiplier, t);
            }

            yield return null;
        }

        elapsed = 0f;

        // Scale back down
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            t = scaleCurve.Evaluate(t);
            
            if (panelCanvasGroup != null)
            {
                transform.localScale = Vector3.one * Mathf.Lerp(scaleMultiplier, 1f, t);
            }

            yield return null;
        }

        transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Manually update display (for testing)
    /// </summary>
    [ContextMenu("Refresh Display")]
    public void RefreshDisplay()
    {
        EnsureAuthoritativeTurnManager();
        UpdateTurnDisplay();
    }

    /// <summary>
    /// Test animation
    /// </summary>
    [ContextMenu("Test Animation")]
    public void TestAnimation()
    {
        PlayTurnChangeAnimation();
    }

    #if UNITY_EDITOR
    [ContextMenu("Auto Find UI Elements")]
    public void AutoFindUIElements()
    {
        // Find TurnManager
        if (turnManager == null)
        {
            turnManager = FindObjectOfType<TurnManager>();
        }

        // Find UI elements by common naming patterns
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren)
        {
            string childName = child.name.ToLower();

            if (childName.Contains("owner") && turnOwnerText == null)
            {
                turnOwnerText = child.GetComponent<TextMeshProUGUI>();
            }
            else if (childName.Contains("phase") && turnPhaseText == null)
            {
                turnPhaseText = child.GetComponent<TextMeshProUGUI>();
            }
            else if (childName.Contains("number") && turnNumberText == null)
            {
                turnNumberText = child.GetComponent<TextMeshProUGUI>();
            }
            else if (childName.Contains("highlight") && highlightImage == null)
            {
                highlightImage = child.GetComponent<Image>();
            }
        }

        // Find CanvasGroup on this panel
        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = GetComponent<CanvasGroup>();
        }

        Debug.Log("[TurnPreviewUI] Auto-find complete!");
    }
    #endif
}
