using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dialog that shows all cards currently in the graveyard.
/// Attach this to a dialog root and wire the UI references in the inspector.
/// </summary>
public class GraveyardDialog : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup dialogCanvasGroup;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GraveyardDialogEntry entryPrefab;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Button closeButton;

    [Header("Behavior")]
    [SerializeField] private bool newestFirst = true;
    [SerializeField] private bool autoRefreshWhenVisible = true;
    [SerializeField] private bool closeOnEscape = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private readonly List<GraveyardDialogEntry> liveEntries = new List<GraveyardDialogEntry>();
    private GameManager boundGameManager;

    public bool IsVisible => dialogCanvasGroup != null ? dialogCanvasGroup.alpha > 0.001f : gameObject.activeSelf;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }

        if (entryPrefab != null && entryPrefab.gameObject.activeSelf)
        {
            entryPrefab.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        BindToGameManager();
        Refresh();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Update()
    {
        if (closeOnEscape && IsVisible && Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
        }
    }

    public void Show()
    {
        SetVisible(true);
        Refresh();
    }

    public void Hide()
    {
        SetVisible(false);
    }

    public void Toggle()
    {
        SetVisible(!IsVisible);
        if (IsVisible)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        if (GameManager.Instance == null)
        {
            Refresh(null);
            return;
        }

        Refresh(GameManager.Instance.GetGraveyardCards());
    }

    public void Refresh(IReadOnlyList<CardData> cards)
    {
        int count = cards != null ? cards.Count : 0;

        if (countText != null)
        {
            countText.text = $"Graveyard: {count}";
        }

        if (contentParent == null || entryPrefab == null)
        {
            return;
        }

        RebuildList(cards);
    }

    private void RebuildList(IReadOnlyList<CardData> cards)
    {
        ClearEntries();

        if (cards == null || cards.Count == 0)
        {
            return;
        }

        if (newestFirst)
        {
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                CreateEntry(cards[i], i);
            }
        }
        else
        {
            for (int i = 0; i < cards.Count; i++)
            {
                CreateEntry(cards[i], i);
            }
        }
    }

    private void CreateEntry(CardData card, int index)
    {
        GraveyardDialogEntry entry = Instantiate(entryPrefab, contentParent);
        entry.gameObject.SetActive(true);
        entry.Bind(card, index);
        liveEntries.Add(entry);
    }

    private void ClearEntries()
    {
        for (int i = 0; i < liveEntries.Count; i++)
        {
            if (liveEntries[i] != null)
            {
                Destroy(liveEntries[i].gameObject);
            }
        }

        liveEntries.Clear();

        if (entryPrefab != null && entryPrefab.gameObject.activeSelf)
        {
            entryPrefab.gameObject.SetActive(false);
        }
    }

    private void SetVisible(bool isVisible)
    {
        if (dialogCanvasGroup != null)
        {
            dialogCanvasGroup.alpha = isVisible ? 1f : 0f;
            dialogCanvasGroup.interactable = isVisible;
            dialogCanvasGroup.blocksRaycasts = isVisible;
        }
        else
        {
            gameObject.SetActive(isVisible);
        }
    }

    private void BindToGameManager()
    {
        if (boundGameManager == GameManager.Instance)
        {
            return;
        }

        Unbind();

        boundGameManager = GameManager.Instance;
        if (boundGameManager != null)
        {
            boundGameManager.OnGraveyardChanged += HandleGraveyardChanged;
            if (showDebugInfo)
            {
                Debug.Log("GraveyardDialog: Bound to GameManager.OnGraveyardChanged");
            }
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning("GraveyardDialog: No GameManager.Instance found (will not auto-update).");
        }
    }

    private void Unbind()
    {
        if (boundGameManager != null)
        {
            boundGameManager.OnGraveyardChanged -= HandleGraveyardChanged;
        }

        boundGameManager = null;
    }

    private void HandleGraveyardChanged(IReadOnlyList<CardData> cards)
    {
        if (!autoRefreshWhenVisible || IsVisible)
        {
            Refresh(cards);
        }
    }
}
