using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Populates the deck building UI with all available cards and selected deck slots.
/// </summary>
public class DeckBuilderUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform availableCardsContent;
    [SerializeField] private RectTransform selectedCardsContent;
    [SerializeField] private GameObject cardSlotTemplate;
    [SerializeField] private TextMeshProUGUI deckCountText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button clearButton;

    [Header("Scene Flow")]
    [SerializeField] private bool loadSceneOnConfirm = true;
    [SerializeField] private string nextSceneName = "MainScene";

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool autoFindReferences = true;

    private DeckManager deckManager;
    private readonly List<DeckBuilderCardSlot> availableSlots = new List<DeckBuilderCardSlot>();
    private readonly List<DeckBuilderCardSlot> selectedSlots = new List<DeckBuilderCardSlot>();

    private void Awake()
    {
        if (cardSlotTemplate != null)
        {
            cardSlotTemplate.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (deckManager == null)
        {
            deckManager = FindObjectOfType<DeckManager>();
        }

        if (deckManager != null)
        {
            deckManager.OnDeckChanged += HandleDeckChanged;
        }
    }

    private void OnDisable()
    {
        if (deckManager != null)
        {
            deckManager.OnDeckChanged -= HandleDeckChanged;
        }
    }

    private void Start()
    {
        if (autoFindReferences)
        {
            AutoFindReferences();
        }

        if (deckManager == null)
        {
            deckManager = FindObjectOfType<DeckManager>();
        }

        if (deckManager == null)
        {
            Debug.LogWarning("DeckBuilderUI: No DeckManager found in scene.");
            return;
        }

        BuildAvailableCards();
        BuildSelectedSlots();
        UpdateSelectedDeckUI();

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            confirmButton.onClick.AddListener(HandleConfirmClicked);
        }

        if (clearButton != null)
        {
            clearButton.onClick.RemoveListener(HandleClearClicked);
            clearButton.onClick.AddListener(HandleClearClicked);
        }
    }

    private void BuildAvailableCards()
    {
        if (availableCardsContent == null || cardSlotTemplate == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("DeckBuilderUI: Missing references (availableCardsContent or cardSlotTemplate). Did the auto-create tool run?");
            }
            return;
        }

        availableSlots.Clear();

        for (int i = availableCardsContent.childCount - 1; i >= 0; i--)
        {
            Transform child = availableCardsContent.GetChild(i);
            if (child.gameObject != cardSlotTemplate)
            {
                Destroy(child.gameObject);
            }
        }

        IReadOnlyList<CardData> cards = deckManager.GetAvailableCards();

        foreach (CardData card in cards)
        {
            GameObject slotObj = Instantiate(cardSlotTemplate, availableCardsContent);
            slotObj.name = $"CardSlot_{card.Title}";
            slotObj.SetActive(true);

            CardFetcher fetcher = slotObj.GetComponent<CardFetcher>();
            if (fetcher != null)
            {
                fetcher.SetAutoFetchOnStart(false);
            }

            DeckBuilderCardSlot slot = slotObj.GetComponent<DeckBuilderCardSlot>();
            if (slot == null)
            {
                slot = slotObj.AddComponent<DeckBuilderCardSlot>();
            }

            slot.Setup(card, deckManager);
            availableSlots.Add(slot);
        }

        if (showDebugInfo)
        {
            Debug.Log($"DeckBuilderUI: Created {availableSlots.Count} available card slots.");
        }
    }

    private void BuildSelectedSlots()
    {
        if (selectedCardsContent == null || cardSlotTemplate == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("DeckBuilderUI: Missing references (selectedCardsContent or cardSlotTemplate). Did the auto-create tool run?");
            }
            return;
        }

        selectedSlots.Clear();

        for (int i = selectedCardsContent.childCount - 1; i >= 0; i--)
        {
            Transform child = selectedCardsContent.GetChild(i);
            if (child.gameObject != cardSlotTemplate)
            {
                Destroy(child.gameObject);
            }
        }

        int deckLimit = deckManager.GetDeckLimit();
        for (int i = 0; i < deckLimit; i++)
        {
            GameObject slotObj = Instantiate(cardSlotTemplate, selectedCardsContent);
            slotObj.name = $"SelectedSlot_{i + 1}";
            slotObj.SetActive(true);

            CardFetcher fetcher = slotObj.GetComponent<CardFetcher>();
            if (fetcher != null)
            {
                fetcher.SetAutoFetchOnStart(false);
            }

            DeckBuilderCardSlot slot = slotObj.GetComponent<DeckBuilderCardSlot>();
            if (slot == null)
            {
                slot = slotObj.AddComponent<DeckBuilderCardSlot>();
            }

            slot.Clear();
            selectedSlots.Add(slot);
        }
    }

    private void HandleDeckChanged(IReadOnlyList<CardData> cards)
    {
        UpdateSelectedDeckUI();
        UpdateAvailableHighlights();
    }

    private void UpdateSelectedDeckUI()
    {
        if (deckManager == null)
        {
            return;
        }

        IReadOnlyList<CardData> selected = deckManager.GetSelectedCards();

        if (deckCountText != null)
        {
            deckCountText.text = $"Selected: {selected.Count}/{deckManager.GetDeckLimit()}";
        }

        for (int i = 0; i < selectedSlots.Count; i++)
        {
            DeckBuilderCardSlot slot = selectedSlots[i];
            if (slot == null) continue;

            if (i < selected.Count)
            {
                slot.Setup(selected[i], deckManager);
            }
            else
            {
                slot.Clear();
            }
        }
    }

    private void UpdateAvailableHighlights()
    {
        if (deckManager == null)
        {
            return;
        }

        for (int i = 0; i < availableSlots.Count; i++)
        {
            DeckBuilderCardSlot slot = availableSlots[i];
            if (slot == null || slot.CardData == null) continue;
            slot.SetSelected(deckManager.IsCardSelected(slot.CardData));
        }
    }

    private void HandleConfirmClicked()
    {
        if (deckManager == null)
        {
            return;
        }

        int deckLimit = deckManager.GetDeckLimit();
        if (deckManager.GetSelectedCards().Count < deckLimit)
        {
            Debug.LogWarning($"DeckBuilderUI: Select {deckLimit} cards before confirming.");
            return;
        }

        if (loadSceneOnConfirm && !string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void HandleClearClicked()
    {
        if (deckManager != null)
        {
            deckManager.ClearDeck();
        }
    }

    private void AutoFindReferences()
    {
        if (availableCardsContent == null)
        {
            ScrollRect availableScroll = FindByName<ScrollRect>("AvailableCardsScroll");
            if (availableScroll != null)
            {
                availableCardsContent = availableScroll.content;
                if (availableCardsContent == null)
                {
                    Transform contentTransform = availableScroll.transform.Find("Viewport/Content");
                    if (contentTransform != null)
                    {
                        availableCardsContent = contentTransform.GetComponent<RectTransform>();
                    }
                }
            }
        }

        if (selectedCardsContent == null)
        {
            selectedCardsContent = FindByName<RectTransform>("SelectedCardsContainer");
        }

        if (cardSlotTemplate == null)
        {
            GameObject template = FindByName<GameObject>("CardSlotTemplate");
            if (template != null)
            {
                cardSlotTemplate = template;
            }
        }

        if (deckCountText == null)
        {
            deckCountText = FindByName<TextMeshProUGUI>("Subtitle");
        }

        if (confirmButton == null)
        {
            confirmButton = FindByName<Button>("ConfirmButton");
        }

        if (clearButton == null)
        {
            clearButton = FindByName<Button>("ClearButton");
        }

        if (showDebugInfo)
        {
            Debug.Log("DeckBuilderUI: AutoFind references completed.");
        }
    }

    private static T FindByName<T>(string objectName) where T : UnityEngine.Object
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject obj = allObjects[i];
            if (obj != null && obj.name == objectName)
            {
                if (typeof(T) == typeof(GameObject))
                {
                    return obj as T;
                }

                return obj.GetComponent<T>();
            }
        }

        return null;
    }
}
