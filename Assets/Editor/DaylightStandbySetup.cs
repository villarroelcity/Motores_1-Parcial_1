using Enviro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EnElCamino.Editor
{
    [InitializeOnLoad]
    public static class DaylightStandbySetup
    {
        private const float StandbyHour = 14f;

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

            EnviroManager manager = Object.FindFirstObjectByType<EnviroManager>();
            if (manager == null || manager.configuration == null) return;

            EnviroTimeModule time = manager.configuration.timeModule;
            if (time == null || time.Settings == null) return;

            time.Settings.simulate = false;
            time.SetTimeOfDay(StandbyHour);
            manager.Time = time;
            EditorUtility.SetDirty(time);
            AssetDatabase.SaveAssetIfDirty(time);
        }
    }
}
