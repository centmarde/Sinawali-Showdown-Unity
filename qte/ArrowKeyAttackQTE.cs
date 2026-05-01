using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Arrow-key QTE resolver for Attack cards.
///
/// - Generates a random sequence of Up/Down/Left/Right based on CardData.Difficulty.
/// - Player must input arrows left-to-right, then press Space before time runs out.
/// - Damage to the other player is only applied after the QTE completes.
/// - Damage deduction is based on how many arrows were missed.
/// </summary>
public class ArrowKeyAttackQTE : MonoBehaviour
{
    private enum ArrowDir
    {
        Up,
        Down,
        Left,
        Right
    }

    private enum QTEPhase
    {
        EnterArrows,
        ConfirmTiming
    }

    [Header("Event Source")]
    [Tooltip("Optional. If empty, the script will FindObjectOfType<HandManager>() on enable.")]
    [SerializeField] private HandManager handManager;

    [Header("Optional Integrations")]
    [Tooltip("Optional. If assigned, the script will trigger teleport attack after the QTE (only when final damage > 0).")]
    [SerializeField] private AttackTeleportOnAttackCardConfirm teleportAttack;

    [Tooltip("Optional. If assigned or found, the script will advance the turn after resolving Player1 attack.")]
    [SerializeField] private TurnManager turnManager;

    [Header("Sequence Length (by difficulty)")]
    [SerializeField] private int veryEasyKeys = 4;
    [SerializeField] private int easyKeys = 5;
    [SerializeField] private int mediumKeys = 6;
    [SerializeField] private int hardKeys = 7;
    [SerializeField] private int veryHardKeys = 8;

    [Header("Time Limit (by difficulty)")]
    [Tooltip("Maximum time to copy the arrow keys for VeryEasy cards. This is also used as a hard cap for all difficulties.")]
    [SerializeField] private float veryEasyTimeLimitSeconds = 7f;

    [SerializeField] private float easyTimeLimitSeconds = 6f;
    [SerializeField] private float mediumTimeLimitSeconds = 5f;
    [SerializeField] private float hardTimeLimitSeconds = 4f;
    [SerializeField] private float veryHardTimeLimitSeconds = 3.5f;

    [Header("Damage")]
    [Tooltip("If true, triggers the teleport attack sequence when damage is dealt.")]
    [SerializeField] private bool triggerTeleportAttackWhenDamageDealt = true;

    [Tooltip("If true, triggers the player's 'Attack' animation during the teleport attack sequence.")]
    [SerializeField] private bool fireAttackAnimationWhenTeleporting = true;

    [Tooltip("If true, when teleport attack is used, damage is applied after the teleport attack ends (so damage numbers appear after the attack animation).")]
    [SerializeField] private bool applyDamageAfterTeleportEnds = true;

    [Header("UI")]
    [SerializeField] private bool autoCreateUI = true;

    [Tooltip("Optional. If empty and Auto Create UI is enabled, a simple overlay canvas will be created at runtime.")]
    [SerializeField] private Canvas uiCanvas;

    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI sequenceText;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Image panelImage;
    [SerializeField] private Outline panelOutline;

    [Header("Space Confirm Timing")]
    [Tooltip("Sweet spot center along the bar (0..1).")]
    [Range(0f, 1f)]
    [SerializeField] private float sweetSpotCenter = 0.5f;

    [Tooltip("VeryEasy: wider sweet spot, slower bar.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float veryEasySweetSpotWidth = 0.35f;
    [Range(0.25f, 5f)]
    [SerializeField] private float veryEasyTimingBarCyclesPerSecond = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float veryEasyTimingMissDamageMultiplier = 0.35f;

    [Tooltip("Easy: slightly narrower/faster than VeryEasy.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float easySweetSpotWidth = 0.28f;
    [Range(0.25f, 5f)]
    [SerializeField] private float easyTimingBarCyclesPerSecond = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] private float easyTimingMissDamageMultiplier = 0.25f;

    [Tooltip("Medium: moderate sweet spot and speed.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float mediumSweetSpotWidth = 0.22f;
    [Range(0.25f, 5f)]
    [SerializeField] private float mediumTimingBarCyclesPerSecond = 1.2f;
    [Range(0f, 1f)]
    [SerializeField] private float mediumTimingMissDamageMultiplier = 0.20f;

    [Tooltip("Hard: narrower and faster.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float hardSweetSpotWidth = 0.18f;
    [Range(0.25f, 5f)]
    [SerializeField] private float hardTimingBarCyclesPerSecond = 1.5f;
    [Range(0f, 1f)]
    [SerializeField] private float hardTimingMissDamageMultiplier = 0.15f;

    [Tooltip("VeryHard: narrowest and fastest.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float veryHardSweetSpotWidth = 0.14f;
    [Range(0.25f, 5f)]
    [SerializeField] private float veryHardTimingBarCyclesPerSecond = 1.8f;
    [Range(0f, 1f)]
    [SerializeField] private float veryHardTimingMissDamageMultiplier = 0.10f;

    [SerializeField] private RectTransform timingBarRect;
    [SerializeField] private RectTransform timingMarkerRect;
    [SerializeField] private Image timingSweetSpotImage;

    [Header("Per-Phase Indicators")]
    [SerializeField] private bool showKeyIndicator = true;
    [SerializeField] private bool showTimingIndicator = true;
    [SerializeField] private Color keySuccessColor = new Color(0.2f, 1f, 0.2f, 1f);
    [SerializeField] private Color keyFailColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color timingSuccessColor = new Color(0.2f, 1f, 0.2f, 0.5f);
    [SerializeField] private Color timingFailColor = new Color(1f, 0.2f, 0.2f, 0.5f);

    [Header("Card Result Effect")]
    [SerializeField] private bool flashCardResult = true;
    [SerializeField] private float resultFlashSeconds = 0.6f;
    [SerializeField] private Color resultSuccessColor = new Color(0.2f, 1f, 0.2f, 1f);
    [SerializeField] private Color resultPartialColor = new Color(1f, 0.9f, 0.2f, 1f);
    [SerializeField] private Color resultFailColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("Panel Result Glow")]
    [SerializeField] private bool flashPanelResult = true;
    [SerializeField] private float panelGlowSeconds = 0.6f;
    [SerializeField] private float panelGlowIntensity = 1.6f;
    [SerializeField] private Vector2 panelGlowOutlineDistance = new Vector2(6f, 6f);

    [Header("Result Display")]
    [SerializeField] private float resultDisplaySeconds = 1.0f;

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    private Coroutine qteRoutine;
    private bool isRunning;
    private Coroutine damageResolveRoutine;

    private List<ArrowDir> sequence;
    private int index;
    private int missed;
    private float timeRemaining;
    private CardData currentCard;
    private QTEPhase phase;
    private float timingElapsed;
    private float timingMarkerT;
    private float activeSweetSpotWidth;
    private float activeTimingBarCyclesPerSecond;
    private float activeTimingMissDamageMultiplier;
    private CardInspector currentInspector;
    private Coroutine panelGlowRoutine;
    private Color originalSequenceColor = Color.white;
    private Color originalSweetSpotColor = Color.white;

    /// <summary>
    /// Allows editor tools (or custom setup scripts) to wire UI references into this component.
    /// </summary>
    public void SetUIReferences(Canvas canvas, TextMeshProUGUI title, TextMeshProUGUI sequence, TextMeshProUGUI hint, TextMeshProUGUI timer, TextMeshProUGUI result)
    {
        uiCanvas = canvas;
        titleText = title;
        sequenceText = sequence;
        hintText = hint;
        timerText = timer;
        resultText = result;
    }

    public void SetPanelReference(Image panel)
    {
        panelImage = panel;
        if (panelImage != null)
        {
            panelOutline = panelImage.GetComponent<Outline>();
        }
    }

    /// <summary>
    /// Allows editor tools (or custom setup scripts) to wire timing meter references.
    /// </summary>
    public void SetTimingUIReferences(RectTransform barRect, RectTransform markerRect, Image sweetSpot)
    {
        timingBarRect = barRect;
        timingMarkerRect = markerRect;
        timingSweetSpotImage = sweetSpot;
        RefreshSweetSpotVisual();
    }

    private void OnEnable()
    {
        if (handManager == null)
        {
            handManager = FindObjectOfType<HandManager>();
        }

        if (teleportAttack == null)
        {
            teleportAttack = FindObjectOfType<AttackTeleportOnAttackCardConfirm>();
        }

        if (turnManager == null)
        {
            turnManager = FindObjectOfType<TurnManager>();
        }

        EnsureUI();
        SetUIVisible(false);

        if (handManager != null)
        {
            handManager.OnCardConfirmed -= HandleCardConfirmed;
            handManager.OnCardConfirmed += HandleCardConfirmed;
        }
        else if (logDebug)
        {
            Debug.LogWarning("ArrowKeyAttackQTE: No HandManager found; won't listen for card confirms.", this);
        }
    }

    private void OnDisable()
    {
        if (handManager != null)
        {
            handManager.OnCardConfirmed -= HandleCardConfirmed;
        }

        StopQTE();
        SetUIVisible(false);
    }

    private void HandleCardConfirmed(CardInspector inspector, CardData card)
    {
        if (card == null) return;
        if (card.Type != CardType.Attack) return;

        if (isRunning)
        {
            return;
        }

        currentInspector = inspector;
        StartQTE(card);
    }

    private void StartQTE(CardData card)
    {
        StopQTE();

        currentCard = card;
        isRunning = true;

        int keyCount = Mathf.Max(4, GetKeyCountForDifficulty(card.Difficulty));
        float timeLimit = Mathf.Clamp(GetTimeLimitForDifficulty(card.Difficulty), 0.5f, veryEasyTimeLimitSeconds);

        sequence = GenerateSequence(keyCount);
        index = 0;
        missed = 0;
        timeRemaining = timeLimit;
        phase = QTEPhase.EnterArrows;
        timingElapsed = 0f;
        timingMarkerT = 0f;

        ApplyDifficultyTimingSettings(card.Difficulty);

        if (titleText != null)
        {
            titleText.text = $"Attack QTE ({card.Difficulty})";
        }

        if (hintText != null)
        {
            hintText.text = "Copy the arrows left-to-right.";
        }

        if (resultText != null)
        {
            resultText.text = string.Empty;
        }

        CacheOriginalIndicatorColors();
        ResetIndicators();

        UpdateSequenceUI();
        UpdateTimerUI();
        RefreshSweetSpotVisual();
        SetTimingMeterVisible(false);
        SetUIVisible(true);

        qteRoutine = StartCoroutine(QTERoutine());

        if (logDebug)
        {
            Debug.Log($"ArrowKeyAttackQTE: Started QTE for '{card.Title}' with {keyCount} keys and {timeLimit:0.##}s time limit.");
        }
    }

    private void StopQTE()
    {
        if (qteRoutine != null)
        {
            StopCoroutine(qteRoutine);
            qteRoutine = null;
        }

        if (damageResolveRoutine != null)
        {
            StopCoroutine(damageResolveRoutine);
            damageResolveRoutine = null;
        }

        isRunning = false;
        sequence = null;
        index = 0;
        missed = 0;
        timeRemaining = 0f;
        currentCard = null;
        currentInspector = null;
        phase = QTEPhase.EnterArrows;
        timingElapsed = 0f;
        timingMarkerT = 0f;
        activeSweetSpotWidth = 0.2f;
        activeTimingBarCyclesPerSecond = 1.2f;
        activeTimingMissDamageMultiplier = 0.2f;
        ResetIndicators();
    }

    private IEnumerator QTERoutine()
    {
        bool confirmed = false;
        bool timingFailed = false;
        bool spacePressed = false;

        while (timeRemaining > 0f)
        {
            timeRemaining -= Time.unscaledDeltaTime;

            if (phase == QTEPhase.EnterArrows)
            {
                if (index < sequence.Count && TryGetArrowDown(out ArrowDir pressed))
                {
                    if (pressed == sequence[index])
                    {
                        index++;
                    }
                    else
                    {
                        missed++;
                        index++;
                    }

                    UpdateSequenceUI();
                }

                if (index >= sequence.Count)
                {
                    phase = QTEPhase.ConfirmTiming;
                    timingElapsed = 0f;

                    if (hintText != null)
                    {
                        hintText.text = "Press SPACE inside the sweet spot.";
                    }

                    ApplyKeyIndicator(missed == 0);
                    SetTimingMeterVisible(true);
                    RefreshSweetSpotVisual();
                }
            }
            else // ConfirmTiming
            {
                UpdateTimingMeterUI();
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    bool inSweetSpot = IsMarkerInSweetSpot();
                    confirmed = inSweetSpot;
                    timingFailed = !inSweetSpot;
                    spacePressed = true;
                    ApplyTimingIndicator(inSweetSpot);
                    break;
                }
            }

            UpdateTimerUI();
            yield return null;
        }

        // Time ran out.
        if (!confirmed)
        {
            int remaining = Mathf.Max(0, sequence.Count - index);
            missed += remaining;
        }

        if (index < (sequence != null ? sequence.Count : 0))
        {
            ApplyKeyIndicator(false);
        }

        if (phase == QTEPhase.ConfirmTiming && !spacePressed)
        {
            ApplyTimingIndicator(false);
        }

        // If the player missed the Space timing window, treat the whole sequence as failed.
        int total = sequence != null ? sequence.Count : 0;
        int baseDamage = currentCard != null ? currentCard.Damage : 0;
        int missedClamped = Mathf.Clamp(missed, 0, total);
        int finalDamage = CalculateDamageAfterDeduction(baseDamage, missedClamped, total);

        bool arrowsPerfect = missedClamped == 0 && index >= total && total > 0;
        bool timingSuccess = confirmed && !timingFailed;

        // If the player missed the timing bar, still apply damage but with a massive deduction.
        if (timingFailed)
        {
            finalDamage = Mathf.RoundToInt(finalDamage * Mathf.Clamp01(activeTimingMissDamageMultiplier));
        }

        // Resolve the attack: optionally play teleport/attack animation first, then apply damage.
        damageResolveRoutine = StartCoroutine(ResolveAttackThenAdvanceTurn(finalDamage));

        // Wait for resolution to finish before stopping QTE state.
        yield return damageResolveRoutine;
        damageResolveRoutine = null;

        if (resultText != null)
        {
            if (total <= 0)
            {
                resultText.text = "No sequence.";
            }
            else
            {
                if (timingFailed)
                {
                    resultText.text = $"Timing missed. Damage {finalDamage}/{baseDamage}.";
                }
                else
                {
                    resultText.text = $"Missed {missedClamped}/{total}. Damage {finalDamage}/{baseDamage}.";
                }
            }
        }

        if (logDebug)
        {
            Debug.Log($"ArrowKeyAttackQTE: Complete. Missed {missedClamped}/{total}. Damage {finalDamage}/{baseDamage}.");
        }

        Color resultColor = GetResultColor(arrowsPerfect, timingSuccess);
        ApplyResultCardEffect(resultColor);

        if (spacePressed)
        {
            float displaySeconds = Mathf.Max(resultDisplaySeconds, Mathf.Max(resultFlashSeconds, panelGlowSeconds));
            if (displaySeconds > 0f)
            {
                // Briefly show result, then hide.
                yield return new WaitForSecondsRealtime(displaySeconds);
            }
        }
        SetUIVisible(false);

        StopQTE();
    }

    private Color GetResultColor(bool arrowsPerfect, bool timingSuccess)
    {
        if (arrowsPerfect && timingSuccess)
        {
            return resultSuccessColor;
        }
        if (!arrowsPerfect && !timingSuccess)
        {
            return resultFailColor;
        }
        return resultPartialColor;
    }

    private void ApplyResultCardEffect(Color color)
    {
        if (!flashCardResult || currentInspector == null)
        {
            return;
        }

        currentInspector.FlashResultColor(color, resultFlashSeconds);
    }

    private void ApplyResultPanelEffect(Color color)
    {
        if (!flashPanelResult)
        {
            return;
        }

        ResolvePanelReferences();
        if (panelImage == null)
        {
            return;
        }

        if (panelGlowRoutine != null)
        {
            StopCoroutine(panelGlowRoutine);
        }

        panelGlowRoutine = StartCoroutine(PanelGlowRoutine(color));
    }

    private IEnumerator PanelGlowRoutine(Color color)
    {
        Color originalBg = panelImage != null ? panelImage.color : Color.white;
        bool originalOutlineEnabled = panelOutline != null && panelOutline.enabled;
        Color originalOutlineColor = panelOutline != null ? panelOutline.effectColor : Color.white;
        Vector2 originalOutlineDistance = panelOutline != null ? panelOutline.effectDistance : Vector2.zero;

        if (panelImage != null)
        {
            Color flashed = color * panelGlowIntensity;
            flashed.a = originalBg.a;
            panelImage.color = flashed;
        }

        if (panelOutline == null && panelImage != null)
        {
            panelOutline = panelImage.gameObject.AddComponent<Outline>();
        }

        if (panelOutline != null)
        {
            panelOutline.effectColor = color;
            panelOutline.effectDistance = panelGlowOutlineDistance;
            panelOutline.enabled = true;
        }

        float duration = panelGlowSeconds > 0f ? panelGlowSeconds : 0.6f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (panelImage != null)
        {
            panelImage.color = originalBg;
        }

        if (panelOutline != null)
        {
            panelOutline.effectColor = originalOutlineColor;
            panelOutline.effectDistance = originalOutlineDistance;
            panelOutline.enabled = originalOutlineEnabled;
        }

        panelGlowRoutine = null;
    }

    private void CacheOriginalIndicatorColors()
    {
        if (sequenceText != null)
        {
            originalSequenceColor = sequenceText.color;
        }

        if (timingSweetSpotImage != null)
        {
            originalSweetSpotColor = timingSweetSpotImage.color;
        }
    }

    private void ResetIndicators()
    {
        if (sequenceText != null)
        {
            sequenceText.color = originalSequenceColor;
        }

        if (timingSweetSpotImage != null)
        {
            timingSweetSpotImage.color = originalSweetSpotColor;
        }
    }

    private void ApplyKeyIndicator(bool success)
    {
        if (!showKeyIndicator || sequenceText == null)
        {
            return;
        }

        sequenceText.color = success ? keySuccessColor : keyFailColor;
    }

    private void ApplyTimingIndicator(bool success)
    {
        if (!showTimingIndicator || timingSweetSpotImage == null)
        {
            return;
        }

        timingSweetSpotImage.color = success ? timingSuccessColor : timingFailColor;
    }

    private IEnumerator ResolveAttackThenAdvanceTurn(int finalDamage)
    {
        bool willTeleport = triggerTeleportAttackWhenDamageDealt && finalDamage > 0 && teleportAttack != null;

        if (willTeleport && applyDamageAfterTeleportEnds)
        {
            bool ended = false;
            void OnEnded(Transform _) => ended = true;

            teleportAttack.OnTeleportAttackEnded -= OnEnded;
            teleportAttack.OnTeleportAttackEnded += OnEnded;

            teleportAttack.TriggerAttackTeleport(fireAttackAnimationWhenTeleporting);

            const float timeoutSeconds = 6f;
            float elapsed = 0f;
            while (!ended && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            teleportAttack.OnTeleportAttackEnded -= OnEnded;

            // Apply damage after the animation sequence ends (or timeout).
            ApplyDamageToPlayer2(finalDamage);
        }
        else
        {
            // Apply immediately.
            ApplyDamageToPlayer2(finalDamage);

            if (willTeleport)
            {
                teleportAttack.TriggerAttackTeleport(fireAttackAnimationWhenTeleporting);
            }
        }

        if (turnManager != null)
        {
            turnManager.AdvanceTurnAfterExternalResolution();
        }
    }

    private int GetKeyCountForDifficulty(CardDifficulty difficulty)
    {
        switch (difficulty)
        {
            case CardDifficulty.VeryEasy: return veryEasyKeys;
            case CardDifficulty.Easy: return easyKeys;
            case CardDifficulty.Medium: return mediumKeys;
            case CardDifficulty.Hard: return hardKeys;
            case CardDifficulty.VeryHard: return veryHardKeys;
            default: return veryEasyKeys;
        }
    }

    private float GetTimeLimitForDifficulty(CardDifficulty difficulty)
    {
        switch (difficulty)
        {
            case CardDifficulty.VeryEasy: return veryEasyTimeLimitSeconds;
            case CardDifficulty.Easy: return easyTimeLimitSeconds;
            case CardDifficulty.Medium: return mediumTimeLimitSeconds;
            case CardDifficulty.Hard: return hardTimeLimitSeconds;
            case CardDifficulty.VeryHard: return veryHardTimeLimitSeconds;
            default: return veryEasyTimeLimitSeconds;
        }
    }

    private static List<ArrowDir> GenerateSequence(int count)
    {
        List<ArrowDir> s = new List<ArrowDir>(count);
        for (int i = 0; i < count; i++)
        {
            s.Add((ArrowDir)Random.Range(0, 4));
        }
        return s;
    }

    private static int CalculateDamageAfterDeduction(int baseDamage, int missedCount, int totalCount)
    {
        if (baseDamage <= 0) return 0;
        if (totalCount <= 0) return baseDamage;

        float missRatio = Mathf.Clamp01(missedCount / (float)totalCount);
        int deduction = Mathf.RoundToInt(baseDamage * missRatio);
        return Mathf.Max(0, baseDamage - deduction);
    }

    private void ApplyDamageToPlayer2(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        // Resolve player characters via GameManager (preferred).
        Character player1 = null;
        Character player2 = null;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TryGetPlayerCharacters(out player1, out player2);
        }
        else
        {
            // Fallback if GameManager isn't present.
            HPTrackerBinder binder = FindObjectOfType<HPTrackerBinder>();
            if (binder != null)
            {
                if (binder.player1Object != null) player1 = binder.player1Object.GetComponent<Character>();
                if (binder.player2Object != null) player2 = binder.player2Object.GetComponent<Character>();
            }
        }

        if (player2 == null)
        {
            if (logDebug)
            {
                Debug.LogWarning("ArrowKeyAttackQTE: Could not resolve Player2 Character (damage not applied).", this);
            }
            return;
        }

        player2.TakeDamage(damage);

        if (logDebug)
        {
            string p1Name = player1 != null ? player1.GetCharacterName() : "<none>";
            string p2Name = player2 != null ? player2.GetCharacterName() : "<none>";
            Debug.Log($"ArrowKeyAttackQTE: Applied {damage} damage from {p1Name} to {p2Name}.");
        }
    }

    private static bool TryGetArrowDown(out ArrowDir dir)
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            dir = ArrowDir.Up;
            return true;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            dir = ArrowDir.Down;
            return true;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            dir = ArrowDir.Left;
            return true;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            dir = ArrowDir.Right;
            return true;
        }

        dir = default;
        return false;
    }

    private static string ArrowToGlyph(ArrowDir d)
    {
        switch (d)
        {
            case ArrowDir.Up: return "↑";
            case ArrowDir.Down: return "↓";
            case ArrowDir.Left: return "←";
            case ArrowDir.Right: return "→";
            default: return "?";
        }
    }

    private void UpdateSequenceUI()
    {
        if (sequenceText == null || sequence == null)
        {
            return;
        }

        // Minimal display: highlight current position with brackets.
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < sequence.Count; i++)
        {
            string glyph = ArrowToGlyph(sequence[i]);
            if (i == index)
            {
                sb.Append('[').Append(glyph).Append(']');
            }
            else
            {
                sb.Append(glyph);
            }

            if (i < sequence.Count - 1)
            {
                sb.Append(' ');
            }
        }

        if (index >= sequence.Count)
        {
            sb.Append("  (SPACE)");
        }

        sequenceText.text = sb.ToString();
    }

    private void UpdateTimerUI()
    {
        if (timerText == null)
        {
            return;
        }

        timerText.text = $"Time: {Mathf.Max(0f, timeRemaining):0.0}s";
    }

    private void EnsureUI()
    {
        if (!autoCreateUI)
        {
            return;
        }

        if (uiCanvas != null)
        {
            return;
        }

        GameObject canvasObj = new GameObject("ArrowKeyAttackQTE_UI");
        uiCanvas = canvasObj.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Background panel
        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelImageComponent = panelObj.AddComponent<Image>();
        panelImageComponent.color = new Color(0f, 0f, 0f, 0.6f);
        panelImage = panelImageComponent;

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(700f, 260f);
        panelRect.anchoredPosition = Vector2.zero;

        titleText = CreateTMP(panelObj.transform, "Title", new Vector2(0f, 95f), 30f, TextAlignmentOptions.Center);
        sequenceText = CreateTMP(panelObj.transform, "Sequence", new Vector2(0f, 45f), 42f, TextAlignmentOptions.Center);
        hintText = CreateTMP(panelObj.transform, "Hint", new Vector2(0f, -20f), 22f, TextAlignmentOptions.Center);

        // Timing bar container
        CreateTimingBar(panelObj.transform);

        timerText = CreateTMP(panelObj.transform, "Timer", new Vector2(0f, -95f), 20f, TextAlignmentOptions.Center);
        resultText = CreateTMP(panelObj.transform, "Result", new Vector2(0f, -125f), 20f, TextAlignmentOptions.Center);

        ResolvePanelReferences();

        if (logDebug)
        {
            Debug.Log("ArrowKeyAttackQTE: Auto-created UI overlay.");
        }
    }

    private static TextMeshProUGUI CreateTMP(Transform parent, string name, Vector2 anchoredPos, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = string.Empty;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = false;

        RectTransform rect = tmp.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(680f, 60f);
        rect.anchoredPosition = anchoredPos;

        return tmp;
    }

    private void SetUIVisible(bool visible)
    {
        if (uiCanvas != null)
        {
            uiCanvas.enabled = visible;
        }
    }

    private void ResolvePanelReferences()
    {
        if (panelImage != null)
        {
            if (panelOutline == null)
            {
                panelOutline = panelImage.GetComponent<Outline>();
            }
            return;
        }

        if (uiCanvas == null)
        {
            return;
        }

        Image[] images = uiCanvas.GetComponentsInChildren<Image>(true);
        Image found = null;
        for (int i = 0; i < images.Length; i++)
        {
            string nameLower = images[i].name.ToLower();
            if (nameLower == "panel" || nameLower.Contains("panel"))
            {
                found = images[i];
                break;
            }
        }

        panelImage = found;
        if (panelImage != null && panelOutline == null)
        {
            panelOutline = panelImage.GetComponent<Outline>();
        }
    }

    private void CreateTimingBar(Transform parent)
    {
        GameObject barObj = new GameObject("TimingBar");
        barObj.transform.SetParent(parent, false);

        Image barBg = barObj.AddComponent<Image>();
        barBg.color = new Color(1f, 1f, 1f, 0.15f);

        timingBarRect = barObj.GetComponent<RectTransform>();
        timingBarRect.anchorMin = new Vector2(0.5f, 0.5f);
        timingBarRect.anchorMax = new Vector2(0.5f, 0.5f);
        timingBarRect.pivot = new Vector2(0.5f, 0.5f);
        timingBarRect.sizeDelta = new Vector2(520f, 20f);
        timingBarRect.anchoredPosition = new Vector2(0f, -55f);

        // Sweet spot
        GameObject sweetObj = new GameObject("SweetSpot");
        sweetObj.transform.SetParent(barObj.transform, false);
        timingSweetSpotImage = sweetObj.AddComponent<Image>();
        timingSweetSpotImage.color = new Color(0.2f, 1f, 0.2f, 0.35f);

        RectTransform sweetRect = sweetObj.GetComponent<RectTransform>();
        sweetRect.anchorMin = new Vector2(0.5f, 0.5f);
        sweetRect.anchorMax = new Vector2(0.5f, 0.5f);
        sweetRect.pivot = new Vector2(0.5f, 0.5f);
        sweetRect.sizeDelta = new Vector2(100f, 20f);
        sweetRect.anchoredPosition = Vector2.zero;

        // Marker
        GameObject markerObj = new GameObject("Marker");
        markerObj.transform.SetParent(barObj.transform, false);
        Image markerImg = markerObj.AddComponent<Image>();
        markerImg.color = new Color(1f, 1f, 1f, 0.95f);

        timingMarkerRect = markerObj.GetComponent<RectTransform>();
        timingMarkerRect.anchorMin = new Vector2(0.5f, 0.5f);
        timingMarkerRect.anchorMax = new Vector2(0.5f, 0.5f);
        timingMarkerRect.pivot = new Vector2(0.5f, 0.5f);
        timingMarkerRect.sizeDelta = new Vector2(8f, 24f);
        timingMarkerRect.anchoredPosition = Vector2.zero;

        RefreshSweetSpotVisual();
        SetTimingMeterVisible(false);
    }

    private void SetTimingMeterVisible(bool visible)
    {
        if (timingBarRect != null)
        {
            timingBarRect.gameObject.SetActive(visible);
        }
    }

    private void RefreshSweetSpotVisual()
    {
        if (timingBarRect == null || timingSweetSpotImage == null)
        {
            return;
        }

        RectTransform sweetRect = timingSweetSpotImage.GetComponent<RectTransform>();
        if (sweetRect == null)
        {
            return;
        }

        float width = Mathf.Max(1f, timingBarRect.rect.width);
        float clampedWidth01 = Mathf.Clamp01(activeSweetSpotWidth);
        float halfWidthPx = (width * clampedWidth01) * 0.5f;

        float center01 = Mathf.Clamp01(sweetSpotCenter);
        float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, center01);

        sweetRect.sizeDelta = new Vector2(halfWidthPx * 2f, sweetRect.sizeDelta.y);
        sweetRect.anchoredPosition = new Vector2(x, sweetRect.anchoredPosition.y);
    }

    private void UpdateTimingMeterUI()
    {
        if (timingBarRect == null || timingMarkerRect == null)
        {
            return;
        }

        timingElapsed += Time.unscaledDeltaTime;

        float width = Mathf.Max(1f, timingBarRect.rect.width);
        float cycles = Mathf.Max(0.01f, activeTimingBarCyclesPerSecond);
        timingMarkerT = Mathf.PingPong(timingElapsed * cycles, 1f);

        float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, timingMarkerT);
        timingMarkerRect.anchoredPosition = new Vector2(x, timingMarkerRect.anchoredPosition.y);
    }

    private bool IsMarkerInSweetSpot()
    {
        float half = Mathf.Clamp01(activeSweetSpotWidth) * 0.5f;
        float min = Mathf.Clamp01(sweetSpotCenter - half);
        float max = Mathf.Clamp01(sweetSpotCenter + half);
        return timingMarkerT >= min && timingMarkerT <= max;
    }

    private void ApplyDifficultyTimingSettings(CardDifficulty difficulty)
    {
        switch (difficulty)
        {
            case CardDifficulty.VeryEasy:
                activeSweetSpotWidth = veryEasySweetSpotWidth;
                activeTimingBarCyclesPerSecond = veryEasyTimingBarCyclesPerSecond;
                activeTimingMissDamageMultiplier = veryEasyTimingMissDamageMultiplier;
                break;
            case CardDifficulty.Easy:
                activeSweetSpotWidth = easySweetSpotWidth;
                activeTimingBarCyclesPerSecond = easyTimingBarCyclesPerSecond;
                activeTimingMissDamageMultiplier = easyTimingMissDamageMultiplier;
                break;
            case CardDifficulty.Medium:
                activeSweetSpotWidth = mediumSweetSpotWidth;
                activeTimingBarCyclesPerSecond = mediumTimingBarCyclesPerSecond;
                activeTimingMissDamageMultiplier = mediumTimingMissDamageMultiplier;
                break;
            case CardDifficulty.Hard:
                activeSweetSpotWidth = hardSweetSpotWidth;
                activeTimingBarCyclesPerSecond = hardTimingBarCyclesPerSecond;
                activeTimingMissDamageMultiplier = hardTimingMissDamageMultiplier;
                break;
            case CardDifficulty.VeryHard:
                activeSweetSpotWidth = veryHardSweetSpotWidth;
                activeTimingBarCyclesPerSecond = veryHardTimingBarCyclesPerSecond;
                activeTimingMissDamageMultiplier = veryHardTimingMissDamageMultiplier;
                break;
            default:
                activeSweetSpotWidth = veryEasySweetSpotWidth;
                activeTimingBarCyclesPerSecond = veryEasyTimingBarCyclesPerSecond;
                activeTimingMissDamageMultiplier = veryEasyTimingMissDamageMultiplier;
                break;
        }
    }
}
