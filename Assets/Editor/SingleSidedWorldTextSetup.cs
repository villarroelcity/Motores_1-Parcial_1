using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EnElCamino.Editor
{
    [InitializeOnLoad]
    public static class SingleSidedWorldTextSetup
    {
        private const string MaterialPath = "Assets/Materials/WorldText_SingleSided.mat";

        static SingleSidedWorldTextSetup()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += ApplyToOpenScene;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.name == "EnElCamino") EditorApplication.delayCall += ApplyToOpenScene;
        }

        private static void ApplyToOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "EnElCamino") return;

            Shader shader = Shader.Find("EnElCamino/Single-Sided World Text");
            if (shader == null) return;

            TextMesh[] labels = Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Material sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            bool sceneWasAlreadyDirty = scene.isDirty;
            bool sceneChanged = false;

            foreach (TextMesh label in labels)
            {
                if (label == null || label.gameObject.scene != scene) continue;
                MeshRenderer renderer = label.GetComponent<MeshRenderer>();
                if (renderer == null) continue;

                Font font = label.font != null ? label.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
                Material fontMaterial = renderer.sharedMaterial;
                if (fontMaterial == null && font != null) fontMaterial = font.material;
                Texture fontAtlas = fontMaterial != null ? fontMaterial.mainTexture : null;
                if (fontAtlas == null && font != null && font.material != null)
                    fontAtlas = font.material.mainTexture;

                if (sharedMaterial == null)
                {
                    sharedMaterial = new Material(shader) { name = "WorldText_SingleSided" };
                    sharedMaterial.SetColor("_Color", Color.white);
                    if (fontAtlas != null) sharedMaterial.SetTexture("_MainTex", fontAtlas);
                    AssetDatabase.CreateAsset(sharedMaterial, MaterialPath);
                }
                else
                {
                    if (sharedMaterial.shader != shader) sharedMaterial.shader = shader;
                    if (fontAtlas != null && sharedMaterial.GetTexture("_MainTex") != fontAtlas)
                        sharedMaterial.SetTexture("_MainTex", fontAtlas);
                    EditorUtility.SetDirty(sharedMaterial);
                }

                if (renderer != null && renderer.sharedMaterial != sharedMaterial)
                {
                    renderer.sharedMaterial = sharedMaterial;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    EditorUtility.SetDirty(renderer);
                    sceneChanged = true;
                }
            }

            if (!sceneChanged) return;
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!sceneWasAlreadyDirty) EditorSceneManager.SaveScene(scene);
        }
    }
}
