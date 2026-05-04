using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles a single deck builder card slot (available or selected).
/// </summary>
public class DeckBuilderCardSlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private CardFetcher cardFetcher;
    [SerializeField] private Outline selectionOutline;
    [SerializeField] private Color selectedOutlineColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color unselectedOutlineColor = new Color(0f, 0f, 0f, 0.2f);

    private CardData cardData;
    private DeckManager deckManager;

    public CardData CardData => cardData;

    private void Awake()
    {
        if (cardFetcher == null)
        {
            cardFetcher = GetComponent<CardFetcher>();
        }

        if (selectionOutline == null)
        {
            selectionOutline = GetComponent<Outline>();
        }

        SetSelected(false);
    }

    public void Setup(CardData card, DeckManager manager)
    {
        deckManager = manager;
        cardData = card;

        if (cardFetcher != null)
        {
            cardFetcher.SetAutoFetchOnStart(false);
            cardFetcher.SetApplyTypeColors(false);
            cardFetcher.DisplaySpecificCard(card);
        }

        if (deckManager != null)
        {
            SetSelected(deckManager.IsCardSelected(card));
        }
        else
        {
            SetSelected(false);
        }
    }

    public void Clear()
    {
        cardData = null;
        if (cardFetcher != null)
        {
            cardFetcher.ClearDisplayedCard();
        }
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectionOutline == null)
        {
            return;
        }

        selectionOutline.effectColor = selected ? selectedOutlineColor : unselectedOutlineColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (deckManager == null || cardData == null)
        {
            return;
        }

        deckManager.ToggleCard(cardData);
    }
}
