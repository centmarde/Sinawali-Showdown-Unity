using UnityEngine;
using TMPro;

/// <summary>
/// Displays the current card's mana cost in the top-left corner of a card UI.
/// Attach this to the same GameObject as CardFetcher (recommended), or assign a CardFetcher reference.
///
/// It updates automatically whenever CardFetcher changes its current card.
/// If no TextMeshProUGUI is assigned, it can auto-create one as a child.
/// </summary>
public class CardManaView : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private CardFetcher cardFetcher;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI manaText;
    [SerializeField] private bool autoCreateManaTextIfMissing = true;

    [Header("Layout (Top-Left)")]
    [SerializeField] private Vector2 padding = new Vector2(12f, 12f);
    [SerializeField] private int fontSize = 22;

    [Header("Text Format")]
    [SerializeField] private string prefix = "";
    [SerializeField] private string suffix = "";

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private CardData lastCard;

    void Reset()
    {
        cardFetcher = GetComponent<CardFetcher>();
        manaText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    void Awake()
    {
        if (cardFetcher == null)
        {
            cardFetcher = GetComponent<CardFetcher>();
        }

        if (manaText == null && autoCreateManaTextIfMissing)
        {
            manaText = CreateManaLabel();
        }

        ApplyTopLeftLayout();
    }

    void OnEnable()
    {
        Refresh();
    }

    void LateUpdate()
    {
        // Update when CardFetcher switches cards.
        if (cardFetcher == null) return;

        CardData current = cardFetcher.GetCurrentCard();
        if (current != lastCard)
        {
            Refresh();
        }
    }

    [ContextMenu("Refresh Mana View")]
    public void Refresh()
    {
        if (cardFetcher == null)
        {
            if (showDebugInfo) Debug.LogWarning($"CardManaView: No CardFetcher on '{gameObject.name}'");
            return;
        }

        if (manaText == null)
        {
            if (autoCreateManaTextIfMissing)
            {
                manaText = CreateManaLabel();
                ApplyTopLeftLayout();
            }
            else
            {
                if (showDebugInfo) Debug.LogWarning($"CardManaView: No manaText assigned on '{gameObject.name}'");
                return;
            }
        }

        CardData currentCard = cardFetcher.GetCurrentCard();
        lastCard = currentCard;

        if (currentCard == null)
        {
            manaText.text = string.Empty;
            return;
        }

        manaText.text = $"{prefix}{currentCard.ManaDeduction}{suffix}";
    }

    TextMeshProUGUI CreateManaLabel()
    {
        GameObject go = new GameObject("ManaText");
        go.transform.SetParent(transform, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = string.Empty;
        tmp.raycastTarget = false;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.TopLeft;

        // Keep it legible on top of art.
        tmp.enableWordWrapping = false;

        return tmp;
    }

    void ApplyTopLeftLayout()
    {
        if (manaText == null) return;

        manaText.fontSize = fontSize;
        manaText.alignment = TextAlignmentOptions.TopLeft;

        RectTransform rect = manaText.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(padding.x, -padding.y);
        rect.sizeDelta = new Vector2(120f, 40f);
    }
}
