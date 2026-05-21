#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MenuSceneCreator
{
    private const string MenuScenePath = "Assets/Scenes/Menu.unity";
    private const string GameplayScenePath = "Assets/Scenes/GamePlay.unity";

    public static void CreateMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Menu";

        CreateCamera();
        Canvas canvas = CreateCanvas();
        MapMenuController menuController = CreateMenuController(canvas.transform);

        CreateTitle(canvas.transform);
        CreateMapButton(canvas.transform, menuController, "Level 1", new Vector2(-240f, 0f));
        CreateMapButton(canvas.transform, menuController, "Level 2", Vector2.zero);
        CreateMapButton(canvas.transform, menuController, "Level 3", new Vector2(240f, 0f));
        CreateEventSystem();

        EditorSceneManager.SaveScene(scene, MenuScenePath);
        AddScenesToBuildSettings();
        AssetDatabase.Refresh();

        Debug.Log($"Created menu scene at {MenuScenePath}.");
    }

    private static void CreateCamera()
    {
        GameObject cameraGo = new GameObject("Main Camera");
        Camera camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.07f, 0.08f, 0.11f);
        cameraGo.tag = "MainCamera";
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasGo = new GameObject("Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static MapMenuController CreateMenuController(Transform parent)
    {
        GameObject menuGo = new GameObject("Map Menu Controller");
        menuGo.transform.SetParent(parent, false);
        MapMenuController menuController = menuGo.AddComponent<MapMenuController>();
        menuController.FallbackGameplaySceneName = "GamePlay";
        return menuController;
    }

    private static void CreateTitle(Transform parent)
    {
        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(parent, false);

        TextMeshProUGUI title = titleGo.AddComponent<TextMeshProUGUI>();
        title.text = "Map";
        title.alignment = TextAlignmentOptions.Center;
        title.fontSize = 64f;
        title.color = Color.white;

        RectTransform rectTransform = title.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = new Vector2(0f, -80f);
        rectTransform.sizeDelta = new Vector2(500f, 100f);
    }

    private static void CreateMapButton(Transform parent, MapMenuController menuController, string label, Vector2 anchoredPosition)
    {
        GameObject buttonGo = new GameObject(label);
        buttonGo.transform.SetParent(parent, false);

        Image image = buttonGo.AddComponent<Image>();
        image.color = new Color(0.2f, 0.55f, 0.9f);

        Button button = buttonGo.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.3f, 0.7f, 1f);
        colors.pressedColor = new Color(0.1f, 0.35f, 0.65f);
        button.colors = colors;

        RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(160f, 160f);

        MapLevelButton mapLevelButton = buttonGo.AddComponent<MapLevelButton>();
        mapLevelButton.MenuController = menuController;

        GameObject textGo = new GameObject("Label");
        textGo.transform.SetParent(buttonGo.transform, false);
        TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 28f;
        text.color = Color.white;
        mapLevelButton.NameText = text;

        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();
        eventSystemGo.AddComponent<StandaloneInputModule>();
    }

    private static void AddScenesToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        AddSceneIfMissing(scenes, GameplayScenePath);
        AddSceneIfMissing(scenes, MenuScenePath);
        MoveSceneToFirst(scenes, MenuScenePath);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void AddSceneIfMissing(List<EditorBuildSettingsScene> scenes, string scenePath)
    {
        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath))
            return;

        foreach (EditorBuildSettingsScene scene in scenes)
        {
            if (scene.path == scenePath)
            {
                scene.enabled = true;
                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
    }

    private static void MoveSceneToFirst(List<EditorBuildSettingsScene> scenes, string scenePath)
    {
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path != scenePath)
                continue;

            EditorBuildSettingsScene scene = scenes[i];
            scenes.RemoveAt(i);
            scenes.Insert(0, scene);
            return;
        }
    }
}
#endif
