using UnityEditor;
using UnityEditor.AI;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EnElCamino.Editor
{
    [InitializeOnLoad]
    public static class DaylightStandbySetup
    {
        static DaylightStandbySetup()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += RestoreStandbyDaylight;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += RestoreStandbyDaylight;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.name == "EnElCamino")
                EditorApplication.delayCall += RestoreStandbyDaylight;
        }

        private static void RestoreStandbyDaylight()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "EnElCamino") return;

            typeof(NavMeshVisualizationSettings).GetProperty("showNavMesh",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(null, false);
            Juego game = Object.FindFirstObjectByType<Juego>();
            if (game != null) game.ApplyDaylight(0f);
        }
    }
}
