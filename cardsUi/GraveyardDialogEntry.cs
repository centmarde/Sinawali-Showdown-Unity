using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI entry prefab for a single graveyard card row.
/// </summary>
public class GraveyardDialogEntry : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Image artworkImage;
    [SerializeField] private bool showIndexNumber = true;

    public void Bind(CardData card, int index)
    {
        string title = card != null ? card.Title : "<null>";
        if (showIndexNumber)
        {
            title = $"{index + 1}. {title}";
        }

        if (titleText != null)
        {
            titleText.text = title;
        }

        if (descriptionText != null)
        {
            descriptionText.text = card != null ? card.Description : string.Empty;
        }

        if (statsText != null)
        {
            if (card != null)
            {
                statsText.text = $"{card.Type} | DMG {card.Damage} | Mana {card.ManaDeduction}";
            }
            else
            {
                statsText.text = string.Empty;
            }
        }

        if (artworkImage != null)
        {
            Sprite sprite = card != null ? card.CardSprite : null;
            artworkImage.sprite = sprite;
            artworkImage.enabled = sprite != null;
        }
    }
}
