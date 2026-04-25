using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal UI helper to display the current graveyard (played/confirmed cards).
/// Attach this to a UI GameObject and assign the text fields in the inspector.
/// </summary>
public class GraveyardDisplay : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI graveyardListText;
    [SerializeField] private TextMeshProUGUI graveyardCountText;
    [SerializeField] private Image lastCardArtwork;

    [Header("Display")]
    [SerializeField] private bool newestFirst = true;
    [SerializeField] private bool showIndexNumbers = true;
    [SerializeField] private int maxEntriesToShow = 0; // 0 = show all

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private GameManager boundGameManager;

    private void OnEnable()
    {
        BindToGameManager();
        Refresh();
    }

    private void OnDisable()
    {
        Unbind();
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
                Debug.Log("GraveyardDisplay: Bound to GameManager.OnGraveyardChanged");
            }
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning("GraveyardDisplay: No GameManager.Instance found (will not auto-update).");
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
        Refresh(cards);
    }

    [ContextMenu("Refresh")]
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

        if (graveyardCountText != null)
        {
            graveyardCountText.text = $"Graveyard: {count}";
        }

        if (graveyardListText != null)
        {
            graveyardListText.text = BuildListText(cards);
        }

        if (lastCardArtwork != null)
        {
            Sprite sprite = null;
            if (cards != null && cards.Count > 0)
            {
                CardData last = cards[cards.Count - 1];
                if (last != null)
                {
                    sprite = last.CardSprite;
                }
            }

            lastCardArtwork.sprite = sprite;
            lastCardArtwork.enabled = sprite != null;
        }
    }

    private string BuildListText(IReadOnlyList<CardData> cards)
    {
        if (cards == null || cards.Count == 0)
        {
            return "(empty)";
        }

        int total = cards.Count;
        int entriesToShow = maxEntriesToShow <= 0 ? total : Mathf.Min(maxEntriesToShow, total);

        var sb = new StringBuilder(256);

        if (newestFirst)
        {
            for (int i = total - 1; i >= 0 && sb.Length < 50000; i--)
            {
                int shownIndex = (total - 1) - i;
                if (shownIndex >= entriesToShow) break;

                AppendLine(sb, cards[i], i);
            }
        }
        else
        {
            for (int i = 0; i < total && sb.Length < 50000; i++)
            {
                if (i >= entriesToShow) break;
                AppendLine(sb, cards[i], i);
            }
        }

        return sb.ToString();
    }

    private void AppendLine(StringBuilder sb, CardData card, int index)
    {
        string title = card != null ? card.Title : "<null>";

        if (showIndexNumbers)
        {
            sb.Append(index + 1);
            sb.Append(". ");
        }

        sb.Append(title);
        sb.Append('\n');
    }
}
