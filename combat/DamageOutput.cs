using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Attach to Player1/Player2 to output damage/heal events.
///
/// Listens to Character.OnHPChanged and computes delta from the previous HP.
/// Can log to console and/or spawn a simple floating TextMeshPro popup.
/// </summary>
[DisallowMultipleComponent]
public class DamageOutput : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Optional. If empty, uses Character on the same GameObject.")]
    [SerializeField] private Character character;

    [Header("Output")]
    [SerializeField] private bool logToConsole = true;
    [SerializeField] private bool spawnFloatingPopup = true;
    [SerializeField] private bool showHealingPopups = false;

    [Header("Popup Settings")]
    [SerializeField] private Vector3 popupWorldOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private float popupLifetimeSeconds = 0.9f;
    [SerializeField] private float popupRiseDistance = 0.75f;
    [SerializeField] private float popupFontSize = 10f;

    [Tooltip("Delay before showing the popup (seconds). Useful for syncing with animations.")]
    [SerializeField] private float popupDelaySeconds = 0f;

    [SerializeField] private Color damageColor = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] private Color healColor = new Color(0.25f, 1f, 0.25f, 1f);

    [Tooltip("If empty, uses Camera.main.")]
    [SerializeField] private Camera targetCamera;

    private int lastHP;
    private bool hasBaseline;
    private bool isSubscribed;
    private bool ignoreFirstHPEvent;

    public void SetPopupDelaySeconds(float seconds)
    {
        popupDelaySeconds = Mathf.Max(0f, seconds);
    }

    private void OnEnable()
    {
        TryHook();
    }

    private void Update()
    {
        // For prefab-spawned players or late-added Character components.
        if (!isSubscribed)
        {
            TryHook();
        }
    }

    private void OnDisable()
    {
        if (character != null)
        {
            character.OnHPChanged -= HandleHPChanged;
        }

        hasBaseline = false;
        isSubscribed = false;
        ignoreFirstHPEvent = false;
    }

    private void TryHook()
    {
        if (character == null)
        {
            character = GetComponent<Character>();
        }

        if (character == null)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        character.OnHPChanged -= HandleHPChanged;
        character.OnHPChanged += HandleHPChanged;
        isSubscribed = true;

        // Character commonly fires an HP update during Start()/InitializeCharacter().
        // If we baseline from currentHP before that happens (often 0), we get a fake "heal" popup.
        ignoreFirstHPEvent = true;
    }

    private void HandleHPChanged(int currentHP, int maxHP)
    {
        if (ignoreFirstHPEvent || !hasBaseline)
        {
            lastHP = currentHP;
            hasBaseline = true;
            ignoreFirstHPEvent = false;
            return;
        }

        int delta = currentHP - lastHP;
        if (delta == 0)
        {
            lastHP = currentHP;
            return;
        }

        if (delta < 0)
        {
            int dmg = -delta;
            if (logToConsole)
            {
                Debug.Log($"[DamageOutput] {GetNameSafe()} took {dmg} damage ({currentHP}/{maxHP}).", this);
            }

            if (spawnFloatingPopup)
            {
                SpawnPopup($"-{dmg}", damageColor);
            }
        }
        else
        {
            int heal = delta;
            if (logToConsole)
            {
                Debug.Log($"[DamageOutput] {GetNameSafe()} healed {heal} ({currentHP}/{maxHP}).", this);
            }

            if (spawnFloatingPopup && showHealingPopups)
            {
                SpawnPopup($"+{heal}", healColor);
            }
        }

        lastHP = currentHP;
    }

    private string GetNameSafe()
    {
        if (character != null)
        {
            return character.GetCharacterName();
        }
        return gameObject.name;
    }

    private void SpawnPopup(string text, Color color)
    {
        float delay = Mathf.Max(0f, popupDelaySeconds);
        if (delay <= 0f)
        {
            SpawnPopupImmediate(text, color);
        }
        else
        {
            StartCoroutine(DelayedSpawn(text, color, delay));
        }
    }

    private IEnumerator DelayedSpawn(string text, Color color, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        SpawnPopupImmediate(text, color);
    }

    private void SpawnPopupImmediate(string text, Color color)
    {
        Vector3 spawnPos = transform.position + popupWorldOffset;

        GameObject popup = new GameObject("DamagePopup");
        popup.transform.position = spawnPos;

        TextMeshPro tmp = popup.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = popupFontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.enableWordWrapping = false;

        StartCoroutine(PopupRoutine(popup.transform, tmp, color));
    }

    private IEnumerator PopupRoutine(Transform popupTransform, TextMeshPro tmp, Color baseColor)
    {
        float lifetime = Mathf.Max(0.1f, popupLifetimeSeconds);
        float elapsed = 0f;

        Vector3 startPos = popupTransform.position;
        Vector3 endPos = startPos + Vector3.up * Mathf.Max(0f, popupRiseDistance);

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);

            popupTransform.position = Vector3.Lerp(startPos, endPos, t);

            if (targetCamera != null)
            {
                Vector3 dir = popupTransform.position - targetCamera.transform.position;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    popupTransform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                }
            }

            Color c = baseColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            if (tmp != null)
            {
                tmp.color = c;
            }

            yield return null;
        }

        if (popupTransform != null)
        {
            Destroy(popupTransform.gameObject);
        }
    }
}
