using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pokiwar.EditorTools
{
    [InitializeOnLoad]
    internal static class PokiwarStartScene
    {
        private const string Checked = "Pokiwar.StartSceneChecked";

        static PokiwarStartScene()
        {
            if (Application.isBatchMode || SessionState.GetBool(Checked, false)) return;
            EditorApplication.delayCall += OpenIfEmpty;
        }

        private static void OpenIfEmpty()
        {
            SessionState.SetBool(Checked, true);
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var active = SceneManager.GetActiveScene();
            if (SceneManager.sceneCount > 1 || !string.IsNullOrEmpty(active.path) || active.isDirty) return;
            if (!File.Exists(PokiwarSceneBuilder.ScenePath)) return;
            EditorSceneManager.OpenScene(PokiwarSceneBuilder.ScenePath);
            Debug.Log("[Pokiwar] opened " + PokiwarSceneBuilder.ScenePath);
        }
    }
}
