#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Editor tool to auto-create a Graveyard dialog UI.
/// </summary>
public static class GraveyardDialogAutoCreateUI
{
    [MenuItem("Tools/Card System/Setup Graveyard Dialog UI", priority = 615)]
    public static void SetupGraveyardDialogUI()
    {
        GraveyardDialog existing = Object.FindObjectOfType<GraveyardDialog>();
        if (existing != null)
        {
            RepairExistingUI(existing);
            Selection.activeObject = existing.gameObject;
            EditorUtility.DisplayDialog("Graveyard Dialog UI", "GraveyardDialog already exists. Attempted to auto-wire missing references.", "OK");
            return;
        }

        GameObject system = new GameObject("GraveyardDialogSystem");
        Undo.RegisterCreatedObjectUndo(system, "Create GraveyardDialogSystem");

        Canvas canvas = CreateCanvas(system.transform);
        GameObject dialogRoot = CreateDialogRoot(canvas.transform);

        GraveyardDialog dialog = dialogRoot.AddComponent<GraveyardDialog>();
        CanvasGroup canvasGroup = dialogRoot.GetComponent<CanvasGroup>();

        TextMeshProUGUI countText = CreateHeaderText(dialogRoot.transform);
        Button closeButton = CreateCloseButton(dialogRoot.transform);
        ScrollRect scrollRect = CreateScrollView(dialogRoot.transform, out RectTransform content);
        GraveyardDialogEntry entryTemplate = CreateEntryTemplate(content);
        entryTemplate.gameObject.SetActive(false);

        Button openButton = CreateOpenButton(canvas.transform);
        if (openButton != null)
        {
            UnityEventTools.AddPersistentListener(openButton.onClick, dialog.Toggle);
        }

        SerializedObject so = new SerializedObject(dialog);
        so.FindProperty("dialogCanvasGroup").objectReferenceValue = canvasGroup;
        so.FindProperty("contentParent").objectReferenceValue = content;
        so.FindProperty("entryPrefab").objectReferenceValue = entryTemplate;
        so.FindProperty("countText").objectReferenceValue = countText;
        so.FindProperty("closeButton").objectReferenceValue = closeButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        SetHiddenByDefault(canvasGroup);
        EnsureEventSystem();

        Selection.activeObject = system;
        EditorUtility.DisplayDialog("Graveyard Dialog UI", "Created Graveyard dialog UI and toggle button.", "OK");
    }

    private static void RepairExistingUI(GraveyardDialog dialog)
    {
        if (dialog == null)
        {
            return;
        }

        CanvasGroup canvasGroup = dialog.GetComponent<CanvasGroup>();
        RectTransform content = FindByName<RectTransform>("Content");
        GraveyardDialogEntry entryTemplate = FindByName<GraveyardDialogEntry>("GraveyardEntryTemplate");
        TextMeshProUGUI countText = FindByName<TextMeshProUGUI>("GraveyardCountText");
        Button closeButton = FindByName<Button>("CloseButton");

        SerializedObject so = new SerializedObject(dialog);
        if (canvasGroup != null)
        {
            so.FindProperty("dialogCanvasGroup").objectReferenceValue = canvasGroup;
        }
        if (content != null)
        {
            so.FindProperty("contentParent").objectReferenceValue = content;
        }
        if (entryTemplate != null)
        {
            so.FindProperty("entryPrefab").objectReferenceValue = entryTemplate;
        }
        if (countText != null)
        {
            so.FindProperty("countText").objectReferenceValue = countText;
        }
        if (closeButton != null)
        {
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        EnsureEventSystem();
    }

    private static Canvas CreateCanvas(Transform parent)
    {
        GameObject canvasObj = new GameObject("GraveyardDialogCanvas");
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create GraveyardDialogCanvas");
        canvasObj.transform.SetParent(parent, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static GameObject CreateDialogRoot(Transform parent)
    {
        GameObject root = new GameObject("GraveyardDialog");
        Undo.RegisterCreatedObjectUndo(root, "Create GraveyardDialog");
        root.transform.SetParent(parent, false);

        RectTransform rect = root.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, 0.1f);
        rect.anchorMax = new Vector2(0.9f, 0.9f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = root.AddComponent<Image>();
        image.color = new Color(0.06f, 0.06f, 0.08f, 0.95f);

        CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        return root;
    }

    private static TextMeshProUGUI CreateHeaderText(Transform parent)
    {
        GameObject textObj = new GameObject("GraveyardCountText");
        Undo.RegisterCreatedObjectUndo(textObj, "Create GraveyardCountText");
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Graveyard: 0";
        tmp.fontSize = 28f;
        tmp.alignment = TextAlignmentOptions.TopLeft;

        RectTransform rect = tmp.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(-40f, 40f);
        rect.anchoredPosition = new Vector2(0f, -20f);
        rect.offsetMin = new Vector2(20f, 0f);
        rect.offsetMax = new Vector2(-120f, 0f);

        return tmp;
    }

    private static Button CreateCloseButton(Transform parent)
    {
        GameObject buttonObj = new GameObject("CloseButton");
        Undo.RegisterCreatedObjectUndo(buttonObj, "Create CloseButton");
        buttonObj.transform.SetParent(parent, false);

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.75f, 0.2f, 0.2f, 0.95f);

        Button button = buttonObj.AddComponent<Button>();

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(120f, 40f);
        rect.anchoredPosition = new Vector2(-20f, -20f);

        TextMeshProUGUI tmp = CreateButtonLabel(buttonObj.transform, "Close");
        tmp.alignment = TextAlignmentOptions.Center;

        return button;
    }

    private static ScrollRect CreateScrollView(Transform parent, out RectTransform content)
    {
        GameObject scrollObj = new GameObject("GraveyardScroll");
        Undo.RegisterCreatedObjectUndo(scrollObj, "Create GraveyardScroll");
        scrollObj.transform.SetParent(parent, false);

        RectTransform rect = scrollObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(20f, 20f);
        rect.offsetMax = new Vector2(-20f, -70f);

        Image image = scrollObj.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.25f);

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.05f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewport.transform, false);
        content = contentObj.AddComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = new Vector2(10f, 0f);
        content.offsetMax = new Vector2(-10f, 0f);

        VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = 10f;
        layout.padding = new RectOffset(6, 6, 6, 6);

        ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRect;
        scrollRect.content = content;

        return scrollRect;
    }

    private static GraveyardDialogEntry CreateEntryTemplate(Transform parent)
    {
        GameObject entry = new GameObject("GraveyardEntryTemplate");
        Undo.RegisterCreatedObjectUndo(entry, "Create GraveyardEntryTemplate");
        entry.transform.SetParent(parent, false);

        RectTransform rect = entry.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, 110f);

        Image bg = entry.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.16f, 0.75f);

        HorizontalLayoutGroup layout = entry.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.childAlignment = TextAnchor.MiddleLeft;

        LayoutElement layoutElement = entry.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 110f;

        GameObject artObj = new GameObject("Artwork");
        artObj.transform.SetParent(entry.transform, false);
        Image artImage = artObj.AddComponent<Image>();
        artImage.preserveAspect = true;
        LayoutElement artLayout = artObj.AddComponent<LayoutElement>();
        artLayout.preferredWidth = 96f;
        artLayout.preferredHeight = 96f;

        GameObject textColumn = new GameObject("TextColumn");
        textColumn.transform.SetParent(entry.transform, false);
        VerticalLayoutGroup textLayout = textColumn.AddComponent<VerticalLayoutGroup>();
        textLayout.spacing = 2f;
        textLayout.childAlignment = TextAnchor.UpperLeft;
        LayoutElement textLayoutElement = textColumn.AddComponent<LayoutElement>();
        textLayoutElement.flexibleWidth = 1f;

        TextMeshProUGUI titleText = CreateRowText(textColumn.transform, "Title", 22f, true);
        TextMeshProUGUI descriptionText = CreateRowText(textColumn.transform, "Description", 18f, false);
        TextMeshProUGUI statsText = CreateRowText(textColumn.transform, "Stats", 16f, false);

        GraveyardDialogEntry entryComponent = entry.AddComponent<GraveyardDialogEntry>();

        SerializedObject so = new SerializedObject(entryComponent);
        so.FindProperty("titleText").objectReferenceValue = titleText;
        so.FindProperty("descriptionText").objectReferenceValue = descriptionText;
        so.FindProperty("statsText").objectReferenceValue = statsText;
        so.FindProperty("artworkImage").objectReferenceValue = artImage;
        so.ApplyModifiedPropertiesWithoutUndo();

        return entryComponent;
    }

    private static TextMeshProUGUI CreateRowText(Transform parent, string name, float fontSize, bool bold)
    {
        GameObject textObj = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(textObj, "Create " + name + " Text");
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = name;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = true;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;

        RectTransform rect = tmp.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, fontSize + 6f);

        return tmp;
    }

    private static Button CreateOpenButton(Transform parent)
    {
        GameObject buttonObj = new GameObject("GraveyardButton");
        Undo.RegisterCreatedObjectUndo(buttonObj, "Create GraveyardButton");
        buttonObj.transform.SetParent(parent, false);

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

        Button button = buttonObj.AddComponent<Button>();

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(180f, 56f);
        rect.anchoredPosition = new Vector2(-20f, 20f);

        TextMeshProUGUI tmp = CreateButtonLabel(buttonObj.transform, "Graveyard");
        tmp.alignment = TextAlignmentOptions.Center;

        return button;
    }

    private static TextMeshProUGUI CreateButtonLabel(Transform parent, string label)
    {
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20f;
        tmp.alignment = TextAlignmentOptions.Center;

        RectTransform rect = tmp.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return tmp;
    }

    private static void SetHiddenByDefault(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
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

    private static T FindByName<T>(string objectName) where T : Object
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
}
#endif
