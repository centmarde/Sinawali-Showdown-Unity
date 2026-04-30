using UnityEngine;

/// <summary>
/// Optional helper for projects where Player1/Player2 are spawned from prefabs at runtime.
/// Attach this to any always-present scene object (e.g., GameManager) and it will
/// automatically add DamageOutput to Player1/Player2 when they become available.
///
/// If you already added DamageOutput to the player prefabs, you do NOT need this.
/// </summary>
public class DamageOutputAutoBinder : MonoBehaviour
{
    [Header("Options")]
    [SerializeField] private bool addToPlayer1 = true;
    [SerializeField] private bool addToPlayer2 = true;

    [Tooltip("Delay for Player2 damage popups (seconds).")]
    [SerializeField] private float player2PopupDelaySeconds = 5f;

    [Tooltip("How often to re-check for spawned players.")]
    [SerializeField] private float pollIntervalSeconds = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    private float nextPollTime;

    private void Update()
    {
        if (Time.unscaledTime < nextPollTime)
        {
            return;
        }

        nextPollTime = Time.unscaledTime + Mathf.Max(0.1f, pollIntervalSeconds);

        TryAttachOutputs();
    }

    private void TryAttachOutputs()
    {
        GameObject p1Obj = null;
        GameObject p2Obj = null;

        // Prefer GameManager resolution.
        if (GameManager.Instance != null && GameManager.Instance.TryGetPlayerCharacters(out Character p1, out Character p2))
        {
            if (p1 != null) p1Obj = p1.gameObject;
            if (p2 != null) p2Obj = p2.gameObject;
        }
        else
        {
            // Fallback to binder if GM isn't available.
            HPTrackerBinder binder = FindObjectOfType<HPTrackerBinder>();
            if (binder != null)
            {
                p1Obj = binder.player1Object;
                p2Obj = binder.player2Object;
            }
        }

        if (addToPlayer1 && p1Obj != null)
        {
            EnsureDamageOutput(p1Obj, "Player1", 0f);
        }

        if (addToPlayer2 && p2Obj != null)
        {
            EnsureDamageOutput(p2Obj, "Player2", player2PopupDelaySeconds);
        }
    }

    private void EnsureDamageOutput(GameObject target, string label, float popupDelaySeconds)
    {
        if (target == null) return;

        DamageOutput output = target.GetComponent<DamageOutput>();
        if (output == null)
        {
            output = target.AddComponent<DamageOutput>();
        }

        if (output != null)
        {
            output.SetPopupDelaySeconds(popupDelaySeconds);
        }

        if (logDebug)
        {
            Debug.Log($"[DamageOutputAutoBinder] Added DamageOutput to {label}: '{target.name}'.", target);
        }
    }
}
