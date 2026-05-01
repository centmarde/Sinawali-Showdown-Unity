#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor menu tool to auto-create a ready-to-use ArrowKeyAttackQTE system in the current scene.
/// </summary>
public static class ArrowKeyAttackQTEAutoCreateUI
{
    [MenuItem("Tools/QTE/Setup Arrow Key Attack QTE", priority = 600)]
    public static void SetupArrowKeyAttackQTE()
    {
        ArrowKeyAttackQTE existing = Object.FindObjectOfType<ArrowKeyAttackQTE>();
        if (existing != null)
        {
            Selection.activeObject = existing.gameObject;
            EditorUtility.DisplayDialog("Arrow Key Attack QTE", "ArrowKeyAttackQTE already exists in the scene. Selected the existing object.", "OK");
            return;
        }

        GameObject system = new GameObject("ArrowKeyAttackQTE_System");
        Undo.RegisterCreatedObjectUndo(system, "Create ArrowKeyAttackQTE System");

        ArrowKeyAttackQTE qte = system.AddComponent<ArrowKeyAttackQTE>();

        // Create UI
        GameObject canvasObj = new GameObject("ArrowKeyAttackQTE_UI");
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create ArrowKeyAttackQTE UI");
        canvasObj.transform.SetParent(system.transform, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Background panel
        GameObject panelObj = new GameObject("Panel");
        Undo.RegisterCreatedObjectUndo(panelObj, "Create ArrowKeyAttackQTE Panel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.6f);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(700f, 260f);
        panelRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI title = CreateTMP(panelObj.transform, "Title", new Vector2(0f, 95f), 30f, TextAlignmentOptions.Center);
        TextMeshProUGUI sequence = CreateTMP(panelObj.transform, "Sequence", new Vector2(0f, 45f), 42f, TextAlignmentOptions.Center);
        TextMeshProUGUI hint = CreateTMP(panelObj.transform, "Hint", new Vector2(0f, -20f), 22f, TextAlignmentOptions.Center);

        // Timing bar
        RectTransform timingBar;
        RectTransform timingMarker;
        Image sweetSpot;
        CreateTimingBar(panelObj.transform, out timingBar, out timingMarker, out sweetSpot);

        TextMeshProUGUI timer = CreateTMP(panelObj.transform, "Timer", new Vector2(0f, -95f), 20f, TextAlignmentOptions.Center);
        TextMeshProUGUI result = CreateTMP(panelObj.transform, "Result", new Vector2(0f, -125f), 20f, TextAlignmentOptions.Center);

        qte.SetUIReferences(canvas, title, sequence, hint, timer, result);
        qte.SetPanelReference(panelImage);
        qte.SetTimingUIReferences(timingBar, timingMarker, sweetSpot);

        // Auto-wire optional integrations if available
        HandManager hm = Object.FindObjectOfType<HandManager>();
        AttackTeleportOnAttackCardConfirm teleport = Object.FindObjectOfType<AttackTeleportOnAttackCardConfirm>();
        TurnManager tm = Object.FindObjectOfType<TurnManager>();

        SerializedObject so = new SerializedObject(qte);
        so.FindProperty("handManager").objectReferenceValue = hm;
        so.FindProperty("teleportAttack").objectReferenceValue = teleport;
        so.FindProperty("turnManager").objectReferenceValue = tm;
        so.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeObject = system;
        EditorUtility.DisplayDialog("Arrow Key Attack QTE", "Created ArrowKeyAttackQTE system + UI. Select an Attack card and set its Difficulty to control sequence length/time.", "OK");
    }

    private static TextMeshProUGUI CreateTMP(Transform parent, string name, Vector2 anchoredPos, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(textObj, "Create ArrowKeyAttackQTE Text");
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = string.Empty;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = false;

        RectTransform rect = tmp.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(680f, 60f);
        rect.anchoredPosition = anchoredPos;

        return tmp;
    }

    private static void CreateTimingBar(Transform parent, out RectTransform timingBarRect, out RectTransform timingMarkerRect, out Image timingSweetSpotImage)
    {
        GameObject barObj = new GameObject("TimingBar");
        Undo.RegisterCreatedObjectUndo(barObj, "Create ArrowKeyAttackQTE Timing Bar");
        barObj.transform.SetParent(parent, false);

        Image barBg = barObj.AddComponent<Image>();
        barBg.color = new Color(1f, 1f, 1f, 0.15f);

        timingBarRect = barObj.GetComponent<RectTransform>();
        timingBarRect.anchorMin = new Vector2(0.5f, 0.5f);
        timingBarRect.anchorMax = new Vector2(0.5f, 0.5f);
        timingBarRect.pivot = new Vector2(0.5f, 0.5f);
        timingBarRect.sizeDelta = new Vector2(520f, 20f);
        timingBarRect.anchoredPosition = new Vector2(0f, -55f);

        GameObject sweetObj = new GameObject("SweetSpot");
        Undo.RegisterCreatedObjectUndo(sweetObj, "Create ArrowKeyAttackQTE Sweet Spot");
        sweetObj.transform.SetParent(barObj.transform, false);
        timingSweetSpotImage = sweetObj.AddComponent<Image>();
        timingSweetSpotImage.color = new Color(0.2f, 1f, 0.2f, 0.35f);

        RectTransform sweetRect = sweetObj.GetComponent<RectTransform>();
        sweetRect.anchorMin = new Vector2(0.5f, 0.5f);
        sweetRect.anchorMax = new Vector2(0.5f, 0.5f);
        sweetRect.pivot = new Vector2(0.5f, 0.5f);
        sweetRect.sizeDelta = new Vector2(100f, 20f);
        sweetRect.anchoredPosition = Vector2.zero;

        GameObject markerObj = new GameObject("Marker");
        Undo.RegisterCreatedObjectUndo(markerObj, "Create ArrowKeyAttackQTE Marker");
        markerObj.transform.SetParent(barObj.transform, false);
        Image markerImg = markerObj.AddComponent<Image>();
        markerImg.color = new Color(1f, 1f, 1f, 0.95f);

        timingMarkerRect = markerObj.GetComponent<RectTransform>();
        timingMarkerRect.anchorMin = new Vector2(0.5f, 0.5f);
        timingMarkerRect.anchorMax = new Vector2(0.5f, 0.5f);
        timingMarkerRect.pivot = new Vector2(0.5f, 0.5f);
        timingMarkerRect.sizeDelta = new Vector2(8f, 24f);
        timingMarkerRect.anchoredPosition = Vector2.zero;

        // Hidden by default; ArrowKeyAttackQTE shows it when needed.
        timingBarRect.gameObject.SetActive(false);
    }
}
#endif
