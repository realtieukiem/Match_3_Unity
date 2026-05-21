#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Match3PlayMenu
{
    [MenuItem("Tools/Match 3/Play First Scene")]
    public static void PlayFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        string scenePath = GetFirstEnabledBuildScenePath();
        if (string.IsNullOrEmpty(scenePath))
        {
            Debug.LogError("Build Settings scene 0 is not configured.");
            return;
        }

        EditorSceneManager.OpenScene(scenePath);
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/Match 3/Play First Scene", true)]
    private static bool CanPlayFromMenu()
    {
        return !EditorApplication.isCompiling;
    }

    private static string GetFirstEnabledBuildScenePath()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes == null || scenes.Length == 0)
            return string.Empty;

        return scenes[0].enabled ? scenes[0].path : string.Empty;
    }
}
#endif
