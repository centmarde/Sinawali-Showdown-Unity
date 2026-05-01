#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class WinnerSceneAutoCreateUI
{
    [MenuItem("Tools/Winner/Create Winner Scene", priority = 620)]
    public static void CreateWinnerScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EnsureScenesFolder();

        GameObject system = new GameObject("WinnerSceneSystem");
        WinnerSceneUI ui = system.AddComponent<WinnerSceneUI>();

        Canvas canvas = CreateCanvas(system.transform);
        GameObject panel = CreateBackgroundPanel(canvas.transform);

        TextMeshProUGUI titleText = CreateTMP(panel.transform, "TitleText", new Vector2(0f, 200f), 48f, TextAlignmentOptions.Center);
        titleText.text = "Winner";

        TextMeshProUGUI winnerText = CreateTMP(panel.transform, "WinnerText", new Vector2(0f, 80f), 36f, TextAlignmentOptions.Center);
        winnerText.text = "Winner: Player 1";

        Button replayButton = CreateButton(panel.transform, "ReplayButton", "Play Again", new Vector2(0f, -40f));
        Button menuButton = CreateButton(panel.transform, "MainMenuButton", "Main Menu", new Vector2(0f, -120f));

        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("winnerText").objectReferenceValue = winnerText;
        so.FindProperty("replayButton").objectReferenceValue = replayButton;
        so.FindProperty("mainMenuButton").objectReferenceValue = menuButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        EnsureEventSystem();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/WinnerScene.unity");
        Selection.activeObject = system;
        EditorUtility.DisplayDialog("Winner Scene", "Created Assets/Scenes/WinnerScene.unity with Winner UI.", "OK");
    }

    [MenuItem("Tools/Winner/Add Winner Scene Loader", priority = 621)]
    public static void AddWinnerSceneLoader()
    {
        WinnerSceneLoader existing = Object.FindObjectOfType<WinnerSceneLoader>();
        if (existing != null)
        {
            Selection.activeObject = existing.gameObject;
            EditorUtility.DisplayDialog("Winner Scene Loader", "WinnerSceneLoader already exists in the scene. Selected the existing object.", "OK");
            return;
        }

        GameObject loader = new GameObject("WinnerSceneLoader");
        loader.AddComponent<WinnerSceneLoader>();

        Selection.activeObject = loader;
        EditorUtility.DisplayDialog("Winner Scene Loader", "Added WinnerSceneLoader to the current scene.", "OK");
    }

    private static void EnsureScenesFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
    }

    private static Canvas CreateCanvas(Transform parent)
    {
        GameObject canvasObj = new GameObject("WinnerCanvas");
        canvasObj.transform.SetParent(parent, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static GameObject CreateBackgroundPanel(Transform parent)
    {
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.05f, 0.05f, 0.1f, 0.96f);

        return panel;
    }

    private static TextMeshProUGUI CreateTMP(Transform parent, string name, Vector2 anchoredPos, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name);
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
        rect.sizeDelta = new Vector2(900f, 100f);
        rect.anchoredPosition = anchoredPos;

        return tmp;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.25f, 0.95f);

        Button button = buttonObj.AddComponent<Button>();

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(320f, 70f);
        rect.anchoredPosition = anchoredPos;

        TextMeshProUGUI tmp = CreateTMP(buttonObj.transform, name + "_Text", Vector2.zero, 28f, TextAlignmentOptions.Center);
        tmp.text = label;

        return button;
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
}
#endif
