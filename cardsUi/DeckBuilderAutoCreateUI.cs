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
        ScrollRect availableScroll = CreateScrollView(availablePanel.transform, "AvailableCardsScroll", out RectTransform availableContent);

        // Right panel (selected deck)
        GameObject selectedPanel = CreateVerticalPanel(root.transform, "SelectedDeckPanel", 1f);
        TextMeshProUGUI selectedTitle = CreateTitle(selectedPanel.transform, "Selected Deck");
        TextMeshProUGUI deckCountText = CreateSubtitle(selectedPanel.transform, "Selected: 0/6");
        GameObject selectedGrid = CreateSelectedGrid(selectedPanel.transform, out RectTransform selectedContent);

        GameObject buttonBar = CreateButtonBar(selectedPanel.transform);
        Button confirmButton = CreateButton(buttonBar.transform, "ConfirmButton", "Confirm");
        Button clearButton = CreateButton(buttonBar.transform, "ClearButton", "Clear");

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

        GameObject border = CreateImageChild(slot.transform, "Border", new Vector2(0, 0), new Vector2(1, 1));
        border.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

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
