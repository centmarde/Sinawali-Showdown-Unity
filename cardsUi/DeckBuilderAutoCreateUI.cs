#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Editor menu tool to auto-create a Deck Builder UI scene layout.
/// </summary>
public static class DeckBuilderAutoCreateUI
{
    [MenuItem("Tools/Card System/Setup Deck Builder UI", priority = 610)]
    public static void SetupDeckBuilderUI()
    {
        DeckBuilderUI existing = Object.FindObjectOfType<DeckBuilderUI>();
        if (existing != null)
        {
            RepairExistingUI(existing);
            Selection.activeObject = existing.gameObject;
            EditorUtility.DisplayDialog("Deck Builder UI", "DeckBuilderUI already exists. Attempted to auto-wire missing references and repair the UI.", "OK");
            return;
        }

        GameObject system = new GameObject("DeckBuilderSystem");
        Undo.RegisterCreatedObjectUndo(system, "Create DeckBuilderSystem");

        DeckManager deckManager = FindOrCreateDeckManager();
        DeckBuilderUI deckBuilderUI = system.AddComponent<DeckBuilderUI>();

        Canvas canvas = CreateCanvas(system.transform);
        GameObject root = CreateRootPanel(canvas.transform);

        // Left panel (available cards)
        GameObject availablePanel = CreateVerticalPanel(root.transform, "AvailableCardsPanel", 2f);
        TextMeshProUGUI availableTitle = CreateTitle(availablePanel.transform, "Available Cards");
        CreateAvailableGrid(availablePanel.transform, out RectTransform availableContent);

        // Right panel (selected deck)
        GameObject selectedPanel = CreateVerticalPanel(root.transform, "SelectedDeckPanel", 1f);
        TextMeshProUGUI selectedTitle = CreateTitle(selectedPanel.transform, "Selected Deck");
        TextMeshProUGUI deckCountText = CreateSubtitle(selectedPanel.transform, "Selected: 0/6");
        GameObject selectedGrid = CreateSelectedGrid(selectedPanel.transform, out RectTransform selectedContent);

        GameObject buttonBar = CreateButtonBar(selectedPanel.transform);
        Button confirmButton = CreateButton(buttonBar.transform, "ConfirmButton", "Confirm");
        Button clearButton = CreateButton(buttonBar.transform, "ClearButton", "Clear");

        GameObject dialogRoot = CreateCardDialog(canvas.transform, out CardFetcher dialogCardFetcher, out Button dialogConfirmButton, out Button dialogCancelButton);
        dialogRoot.SetActive(false);

        // Card slot template (hidden)
        GameObject cardSlotTemplate = CreateCardSlotTemplate(availableContent);
        cardSlotTemplate.SetActive(false);

        SerializedObject so = new SerializedObject(deckBuilderUI);
        so.FindProperty("availableCardsContent").objectReferenceValue = availableContent;
        so.FindProperty("selectedCardsContent").objectReferenceValue = selectedContent;
        so.FindProperty("cardSlotTemplate").objectReferenceValue = cardSlotTemplate;
        so.FindProperty("deckCountText").objectReferenceValue = deckCountText;
        so.FindProperty("confirmButton").objectReferenceValue = confirmButton;
        so.FindProperty("clearButton").objectReferenceValue = clearButton;
        so.FindProperty("cardDialogRoot").objectReferenceValue = dialogRoot;
        so.FindProperty("dialogCardFetcher").objectReferenceValue = dialogCardFetcher;
        so.FindProperty("dialogConfirmButton").objectReferenceValue = dialogConfirmButton;
        so.FindProperty("dialogCancelButton").objectReferenceValue = dialogCancelButton;
        so.FindProperty("loadSceneOnConfirm").boolValue = true;
        so.FindProperty("nextSceneName").stringValue = "MainScene";
        so.ApplyModifiedPropertiesWithoutUndo();

        EnsureEventSystem();

        Selection.activeObject = system;
        EditorUtility.DisplayDialog("Deck Builder UI", "Created Deck Builder UI with DeckManager and DeckBuilderUI.", "OK");
    }

    private static void RepairExistingUI(DeckBuilderUI deckBuilderUI)
    {
        if (deckBuilderUI == null)
        {
            return;
        }

        EnsureSeparateDeckManager(deckBuilderUI.gameObject);

        RectTransform availableContent = FindAvailableContent();
        RectTransform selectedContent = FindByName<RectTransform>("SelectedCardsContainer");
        GameObject cardSlotTemplate = FindByName<GameObject>("CardSlotTemplate");
        TextMeshProUGUI deckCountText = FindByName<TextMeshProUGUI>("Subtitle");
        Button confirmButton = FindByName<Button>("ConfirmButton");
        Button clearButton = FindByName<Button>("ClearButton");
        GameObject dialogRoot = FindByName<GameObject>("DeckBuilderDialog");
        CardFetcher dialogCardFetcher = FindByName<CardFetcher>("DialogCardDisplay");
        Button dialogConfirmButton = FindByName<Button>("DialogConfirmButton");
        Button dialogCancelButton = FindByName<Button>("DialogCancelButton");

        if (dialogRoot == null)
        {
            Canvas canvas = FindByName<Canvas>("DeckBuilderCanvas");
            if (canvas != null)
            {
                dialogRoot = CreateCardDialog(canvas.transform, out dialogCardFetcher, out dialogConfirmButton, out dialogCancelButton);
                dialogRoot.SetActive(false);
            }
        }
        else
        {
            ApplyDialogLayout(dialogRoot);
        }

        if (cardSlotTemplate == null && availableContent != null)
        {
            cardSlotTemplate = CreateCardSlotTemplate(availableContent);
            cardSlotTemplate.SetActive(false);
        }

        SerializedObject so = new SerializedObject(deckBuilderUI);
        if (availableContent != null)
        {
            so.FindProperty("availableCardsContent").objectReferenceValue = availableContent;
        }
        if (selectedContent != null)
        {
            so.FindProperty("selectedCardsContent").objectReferenceValue = selectedContent;
        }
        if (cardSlotTemplate != null)
        {
            so.FindProperty("cardSlotTemplate").objectReferenceValue = cardSlotTemplate;
        }
        if (deckCountText != null)
        {
            so.FindProperty("deckCountText").objectReferenceValue = deckCountText;
        }
        if (confirmButton != null)
        {
            so.FindProperty("confirmButton").objectReferenceValue = confirmButton;
        }
        if (clearButton != null)
        {
            so.FindProperty("clearButton").objectReferenceValue = clearButton;
        }
        if (dialogRoot != null)
        {
            so.FindProperty("cardDialogRoot").objectReferenceValue = dialogRoot;
        }
        if (dialogCardFetcher != null)
        {
            so.FindProperty("dialogCardFetcher").objectReferenceValue = dialogCardFetcher;
        }
        if (dialogConfirmButton != null)
        {
            so.FindProperty("dialogConfirmButton").objectReferenceValue = dialogConfirmButton;
        }
        if (dialogCancelButton != null)
        {
            so.FindProperty("dialogCancelButton").objectReferenceValue = dialogCancelButton;
        }
        so.FindProperty("loadSceneOnConfirm").boolValue = true;
        if (string.IsNullOrEmpty(so.FindProperty("nextSceneName").stringValue))
        {
            so.FindProperty("nextSceneName").stringValue = "MainScene";
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static DeckManager FindOrCreateDeckManager()
    {
        DeckManager existing = Object.FindObjectOfType<DeckManager>();
        if (existing != null)
        {
            if (existing.GetComponent<DeckBuilderUI>() != null)
            {
                return EnsureSeparateDeckManager(existing.gameObject);
            }

            return existing;
        }

        GameObject managerObj = new GameObject("DeckManager");
        Undo.RegisterCreatedObjectUndo(managerObj, "Create DeckManager");
        return managerObj.AddComponent<DeckManager>();
    }

    private static DeckManager EnsureSeparateDeckManager(GameObject uiObject)
    {
        if (uiObject == null)
        {
            return null;
        }

        DeckManager onUi = uiObject.GetComponent<DeckManager>();
        if (onUi == null)
        {
            return Object.FindObjectOfType<DeckManager>();
        }

        GameObject managerObj = new GameObject("DeckManager");
        Undo.RegisterCreatedObjectUndo(managerObj, "Create DeckManager");
        DeckManager newManager = managerObj.AddComponent<DeckManager>();
        newManager.InitializeFrom(onUi);

        Object.DestroyImmediate(onUi, true);
        return newManager;
    }

    private static RectTransform FindAvailableContent()
    {
        RectTransform availableGrid = FindByName<RectTransform>("AvailableCardsGrid");
        if (availableGrid != null)
        {
            return availableGrid;
        }

        ScrollRect scroll = FindByName<ScrollRect>("AvailableCardsScroll");
        if (scroll != null && scroll.content != null)
        {
            return scroll.content;
        }

        GameObject contentObj = FindByName<GameObject>("Content");
        if (contentObj != null && contentObj.transform.parent != null && contentObj.transform.parent.name == "Viewport")
        {
            return contentObj.GetComponent<RectTransform>();
        }

        return null;
    }

    private static T FindByName<T>(string objectName) where T : UnityEngine.Object
    {
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>(true);
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

    private static Canvas CreateCanvas(Transform parent)
    {
        GameObject canvasObj = new GameObject("DeckBuilderCanvas");
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create DeckBuilderCanvas");
        canvasObj.transform.SetParent(parent, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static GameObject CreateRootPanel(Transform parent)
    {
        GameObject root = new GameObject("DeckBuilderRoot");
        Undo.RegisterCreatedObjectUndo(root, "Create DeckBuilderRoot");
        root.transform.SetParent(parent, false);

        RectTransform rect = root.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.07f, 0.1f, 0.98f);

        HorizontalLayoutGroup layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16;
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = true;

        return root;
    }

    private static GameObject CreateVerticalPanel(Transform parent, string name, float flexibleWidth)
    {
        GameObject panel = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(panel, "Create " + name);
        panel.transform.SetParent(parent, false);

        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.16f, 0.9f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10;
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        LayoutElement le = panel.AddComponent<LayoutElement>();
        le.flexibleWidth = flexibleWidth;
        le.flexibleHeight = 1f;

        return panel;
    }

    private static GameObject CreateAvailableGrid(Transform parent, out RectTransform content)
    {
        GameObject container = CreateUIObject("AvailableCardsGrid", parent);
        if (container == null)
        {
            Debug.LogError("DeckBuilderAutoCreateUI: Failed to create AvailableCardsGrid.");
            content = null;
            return null;
        }

        Image img = container.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.14f, 0.9f);

        content = container.GetComponent<RectTransform>();
        content.sizeDelta = new Vector2(760, 660);

        GridLayoutGroup grid = container.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(170, 240);
        grid.spacing = new Vector2(10, 10);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.padding = new RectOffset(8, 8, 8, 8);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;

        return container;
    }

    private static TextMeshProUGUI CreateTitle(Transform parent, string text)
    {
        GameObject obj = new GameObject("Title");
        Undo.RegisterCreatedObjectUndo(obj, "Create Title");
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 30f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.95f, 0.95f, 1f, 1f);

        RectTransform rect = tmp.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 40);

        return tmp;
    }

    private static TextMeshProUGUI CreateSubtitle(Transform parent, string text)
    {
        GameObject obj = new GameObject("Subtitle");
        Undo.RegisterCreatedObjectUndo(obj, "Create Subtitle");
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 22f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.85f, 0.85f, 0.9f, 1f);

        RectTransform rect = tmp.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 30);

        return tmp;
    }

    private static ScrollRect CreateScrollView(Transform parent, string name, out RectTransform content)
    {
        if (parent == null)
        {
            Debug.LogError("DeckBuilderAutoCreateUI: CreateScrollView called with null parent.");
            content = null;
            return null;
        }

        GameObject scrollObj = CreateUIObject(name, parent);
        if (scrollObj == null)
        {
            Debug.LogError("DeckBuilderAutoCreateUI: Failed to create scroll GameObject.");
            content = null;
            return null;
        }

        Image scrollImg = scrollObj.AddComponent<Image>();
        scrollImg.color = new Color(0.1f, 0.1f, 0.14f, 0.9f);

        RectTransform scrollRect = scrollObj.GetComponent<RectTransform>();
        scrollRect.sizeDelta = new Vector2(900, 900);

        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        if (scroll == null)
        {
            Debug.LogError("DeckBuilderAutoCreateUI: Failed to add ScrollRect.");
            content = null;
            return null;
        }
        scroll.horizontal = false;

        GameObject viewport = CreateUIObject("Viewport", scrollObj.transform);
        if (viewport == null)
        {
            Debug.LogError("DeckBuilderAutoCreateUI: Failed to create viewport.");
            content = null;
            return null;
        }
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Image viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = new Color(0, 0, 0, 0.2f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        GameObject contentObj = CreateUIObject("Content", viewport.transform);
        if (contentObj == null)
        {
            Debug.LogError("DeckBuilderAutoCreateUI: Failed to create content.");
            content = null;
            return scroll;
        }
        content = contentObj.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;

        GridLayoutGroup grid = contentObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(220, 320);
        grid.spacing = new Vector2(12, 12);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.padding = new RectOffset(8, 8, 8, 8);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (scroll != null)
        {
            scroll.viewport = viewportRect;
            scroll.content = content;
        }

        return scroll;
    }

    private static GameObject CreateSelectedGrid(Transform parent, out RectTransform content)
    {
        if (parent == null)
        {
            Debug.LogError("DeckBuilderAutoCreateUI: CreateSelectedGrid called with null parent.");
            content = null;
            return null;
        }

        GameObject container = CreateUIObject("SelectedCardsContainer", parent);
        if (container == null)
        {
            Debug.LogError("DeckBuilderAutoCreateUI: Failed to create SelectedCardsContainer.");
            content = null;
            return null;
        }

        Image img = container.AddComponent<Image>();
        img.color = new Color(0.08f, 0.08f, 0.12f, 0.8f);

        content = container.GetComponent<RectTransform>();
        content.sizeDelta = new Vector2(360, 720);

        GridLayoutGroup grid = container.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(170, 250);
        grid.spacing = new Vector2(8, 8);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;

        return container;
    }

    private static GameObject CreateButtonBar(Transform parent)
    {
        GameObject bar = CreateUIObject("ButtonBar", parent);
        if (bar == null)
        {
            return null;
        }

        HorizontalLayoutGroup layout = bar.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        RectTransform rect = bar.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(360, 60);

        return bar;
    }

    private static GameObject CreateCardDialog(Transform parent, out CardFetcher dialogFetcher, out Button confirmButton, out Button cancelButton)
    {
        GameObject dialogRoot = CreateUIObject("DeckBuilderDialog", parent);
        dialogFetcher = null;
        confirmButton = null;
        cancelButton = null;

        if (dialogRoot == null)
        {
            return null;
        }

        RectTransform rootRect = dialogRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image rootImage = dialogRoot.AddComponent<Image>();
        rootImage.color = new Color(0f, 0f, 0f, 0.6f);

        GameObject panel = CreateUIObject("DialogPanel", dialogRoot.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(640, 780);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.12f, 0.12f, 0.18f, 0.98f);

        VerticalLayoutGroup panelLayout = panel.AddComponent<VerticalLayoutGroup>();
        panelLayout.spacing = 14;
        panelLayout.padding = new RectOffset(20, 20, 20, 20);
        panelLayout.childAlignment = TextAnchor.UpperCenter;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childForceExpandWidth = true;

        CreateTitle(panel.transform, "Card Details");

        GameObject cardDisplay = CreateDialogCardDisplay(panel.transform);
        dialogFetcher = cardDisplay != null ? cardDisplay.GetComponent<CardFetcher>() : null;

        GameObject dialogButtons = CreateButtonBar(panel.transform);
        if (dialogButtons != null)
        {
            confirmButton = CreateButton(dialogButtons.transform, "DialogConfirmButton", "Confirm");
            cancelButton = CreateButton(dialogButtons.transform, "DialogCancelButton", "Cancel");
        }

        return dialogRoot;
    }

    private static Button CreateButton(Transform parent, string name, string label)
    {
        GameObject obj = CreateUIObject(name, parent);
        if (obj == null)
        {
            return null;
        }

        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f, 0.95f);

        Button button = obj.AddComponent<Button>();

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(150, 45);

        GameObject textObj = new GameObject(name + "_Text");
        textObj.transform.SetParent(obj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        RectTransform textRect = tmp.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private static GameObject CreateCardSlotTemplate(Transform parent)
    {
        GameObject slot = CreateUIObject("CardSlotTemplate", parent);
        if (slot == null)
        {
            return null;
        }

        Image bg = slot.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0f);

        Outline outline = slot.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.2f);
        outline.effectDistance = new Vector2(2, -2);

        LayoutElement le = slot.AddComponent<LayoutElement>();
        le.preferredHeight = 300;
        le.preferredWidth = 200;

        slot.AddComponent<CardFetcher>();
        slot.AddComponent<DeckBuilderCardSlot>();

        GameObject artwork = CreateImageChild(slot.transform, "Artwork", new Vector2(0, 0.5f), new Vector2(1, 1));
        Image artImage = artwork.GetComponent<Image>();
        artImage.color = Color.white;
        artImage.preserveAspect = true;
        RectTransform artRect = artwork.GetComponent<RectTransform>();
        artRect.sizeDelta = new Vector2(0, 140);

        GameObject textContainer = CreateUIObject("TextContainer", slot.transform);
        RectTransform textRect = textContainer.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 0.5f);
        textRect.offsetMin = new Vector2(8, 8);
        textRect.offsetMax = new Vector2(-8, -8);

        VerticalLayoutGroup textLayout = textContainer.AddComponent<VerticalLayoutGroup>();
        textLayout.spacing = 2;
        textLayout.childAlignment = TextAnchor.UpperCenter;
        textLayout.childForceExpandWidth = true;
        textLayout.childForceExpandHeight = false;

        CreateTMPChild(textContainer.transform, "CardNameText", "Card Name", 18f, TextAlignmentOptions.Center);
        CreateTMPChild(textContainer.transform, "CardTypeText", "Type", 14f, TextAlignmentOptions.Center);
        CreateTMPChild(textContainer.transform, "CardDescriptionText", "Description", 12f, TextAlignmentOptions.TopLeft);
        CreateTMPChild(textContainer.transform, "DamageText", "DMG", 12f, TextAlignmentOptions.Left);
        CreateTMPChild(textContainer.transform, "ManaText", "MP", 12f, TextAlignmentOptions.Left);

        return slot;
    }

    private static GameObject CreateDialogCardDisplay(Transform parent)
    {
        GameObject slot = CreateUIObject("DialogCardDisplay", parent);
        if (slot == null)
        {
            return null;
        }

        Image bg = slot.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.8f);

        LayoutElement le = slot.AddComponent<LayoutElement>();
        le.preferredHeight = 540;
        le.preferredWidth = 480;

        slot.AddComponent<CardFetcher>();

        GameObject artwork = CreateImageChild(slot.transform, "Artwork", new Vector2(0, 0.45f), new Vector2(1, 1));
        Image artImage = artwork.GetComponent<Image>();
        artImage.color = Color.white;
        artImage.preserveAspect = true;
        RectTransform artRect = artwork.GetComponent<RectTransform>();
        artRect.offsetMin = new Vector2(12, 12);
        artRect.offsetMax = new Vector2(-12, -12);

        GameObject textContainer = CreateUIObject("TextContainer", slot.transform);
        RectTransform textRect = textContainer.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 0.45f);
        textRect.offsetMin = new Vector2(12, 12);
        textRect.offsetMax = new Vector2(-12, -12);

        VerticalLayoutGroup textLayout = textContainer.AddComponent<VerticalLayoutGroup>();
        textLayout.spacing = 4;
        textLayout.childAlignment = TextAnchor.UpperCenter;
        textLayout.childControlWidth = true;
        textLayout.childControlHeight = true;
        textLayout.childForceExpandWidth = true;
        textLayout.childForceExpandHeight = false;

        TextMeshProUGUI name = CreateTMPChild(textContainer.transform, "CardNameText", "Card Name", 22f, TextAlignmentOptions.Center);
        TextMeshProUGUI type = CreateTMPChild(textContainer.transform, "CardTypeText", "Type", 18f, TextAlignmentOptions.Center);
        TextMeshProUGUI desc = CreateTMPChild(textContainer.transform, "CardDescriptionText", "Description", 16f, TextAlignmentOptions.TopLeft);
        TextMeshProUGUI dmg = CreateTMPChild(textContainer.transform, "DamageText", "DMG", 16f, TextAlignmentOptions.Left);
        TextMeshProUGUI mana = CreateTMPChild(textContainer.transform, "ManaText", "MP", 16f, TextAlignmentOptions.Left);

        SetDialogTextSizing(name, 30, 360, false, TextOverflowModes.Truncate);
        SetDialogTextSizing(type, 24, 360, false, TextOverflowModes.Truncate);
        SetDialogTextSizing(desc, 120, 360, true, TextOverflowModes.Ellipsis);
        SetDialogTextSizing(dmg, 24, 360, false, TextOverflowModes.Truncate);
        SetDialogTextSizing(mana, 24, 360, false, TextOverflowModes.Truncate);

        return slot;
    }

    private static GameObject CreateImageChild(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject obj = CreateUIObject(name, parent);
        if (obj == null)
        {
            return null;
        }

        Image img = obj.AddComponent<Image>();
        img.color = Color.white;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return obj;
    }

    private static TextMeshProUGUI CreateTMPChild(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject obj = CreateUIObject(name, parent);
        if (obj == null)
        {
            return null;
        }

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        bool isDescription = name.ToLower().Contains("description");
        tmp.enableWordWrapping = isDescription;
        tmp.color = Color.white;
        tmp.overflowMode = TextOverflowModes.Truncate;

        RectTransform rect = tmp.GetComponent<RectTransform>();
        rect.sizeDelta = isDescription ? new Vector2(0, 80) : new Vector2(0, 24);

        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.preferredWidth = 180;
        layout.preferredHeight = isDescription ? 80 : 24;

        return tmp;
    }

    private static void SetDialogTextSizing(TextMeshProUGUI tmp, float height, float preferredWidth, bool wrap, TextOverflowModes overflow)
    {
        if (tmp == null)
        {
            return;
        }

        RectTransform rect = tmp.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0, height);

        LayoutElement layout = tmp.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.minWidth = preferredWidth;
            layout.preferredWidth = preferredWidth;
            layout.minHeight = height;
            layout.preferredHeight = height;
            layout.flexibleWidth = 1f;
            layout.flexibleHeight = 0f;
        }

        tmp.enableWordWrapping = wrap;
        tmp.overflowMode = overflow;
    }

    private static void ApplyDialogLayout(GameObject dialogRoot)
    {
        if (dialogRoot == null)
        {
            return;
        }

        RectTransform panelRect = FindChildByName<RectTransform>(dialogRoot.transform, "DialogPanel");
        if (panelRect != null)
        {
            panelRect.sizeDelta = new Vector2(640, 780);
        }

        VerticalLayoutGroup panelLayout = FindChildByName<VerticalLayoutGroup>(dialogRoot.transform, "DialogPanel");
        if (panelLayout != null)
        {
            panelLayout.spacing = 14;
            panelLayout.padding = new RectOffset(20, 20, 20, 20);
        }

        LayoutElement displayLayout = FindChildByName<LayoutElement>(dialogRoot.transform, "DialogCardDisplay");
        if (displayLayout != null)
        {
            displayLayout.preferredHeight = 540;
            displayLayout.preferredWidth = 480;
        }

        TextMeshProUGUI name = FindChildByName<TextMeshProUGUI>(dialogRoot.transform, "CardNameText");
        TextMeshProUGUI type = FindChildByName<TextMeshProUGUI>(dialogRoot.transform, "CardTypeText");
        TextMeshProUGUI desc = FindChildByName<TextMeshProUGUI>(dialogRoot.transform, "CardDescriptionText");
        TextMeshProUGUI dmg = FindChildByName<TextMeshProUGUI>(dialogRoot.transform, "DamageText");
        TextMeshProUGUI mana = FindChildByName<TextMeshProUGUI>(dialogRoot.transform, "ManaText");

        SetDialogTextSizing(name, 30, 360, false, TextOverflowModes.Truncate);
        SetDialogTextSizing(type, 24, 360, false, TextOverflowModes.Truncate);
        SetDialogTextSizing(desc, 120, 360, true, TextOverflowModes.Ellipsis);
        SetDialogTextSizing(dmg, 24, 360, false, TextOverflowModes.Truncate);
        SetDialogTextSizing(mana, 24, 360, false, TextOverflowModes.Truncate);
    }

    private static T FindChildByName<T>(Transform root, string childName) where T : Component
    {
        if (root == null)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child.name == childName)
            {
                return child.GetComponent<T>();
            }
        }

        return null;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        if (parent == null)
        {
            Debug.LogError($"DeckBuilderAutoCreateUI: Cannot create '{name}' with null parent.");
            return null;
        }

        GameObject obj = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        obj.transform.SetParent(parent, false);
        return obj;
    }
}
#endif
