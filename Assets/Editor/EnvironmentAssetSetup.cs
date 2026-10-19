#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EnElCamino.Editor
{
    public static class EnvironmentAssetSetup
    {
        private const string EnvironmentRootName = "Art_Environment";
        private const string GeneratedPrefix = "Generated_";

        [MenuItem("Tools/Environment/Apply Kenney Environment")]
        public static void Apply()
        {
            GameObject station = GameObject.Find("EstacionDeServicio");
            if (station == null)
            {
                Debug.LogError("No se encontró EstacionDeServicio en la escena activa.");
                return;
            }

            Transform environment = GetOrCreateChild(station.transform, EnvironmentRootName);
            ClearGeneratedChildren(environment);

            Transform nature = GetOrCreateChild(environment, "Nature");
            Transform streetLights = GetOrCreateChild(environment, "StreetLights");
            Transform gasStation = GetOrCreateChild(environment, "GasStation");

            AddNature(nature);
            AddStreetLights(streetLights);
            AddVehicleVisual(station.transform);
            AddFuelPumpVisual(gasStation);
            ConfigureSkybox();

            EditorSceneManager.MarkSceneDirty(station.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("EnvironmentAssetSetup: escena actualizada con auto, árboles, postes, surtidor y cielo nocturno.");
        }

        private static void AddNature(Transform parent)
        {
            string oak = "Assets/ThirdParty/Kenney/NatureKit/Models/FBX format/tree_oak.fbx";
            string pine = "Assets/ThirdParty/Kenney/NatureKit/Models/FBX format/tree_pineTallA.fbx";

            CreateModel("Generated_Tree_Oak_Left", oak, parent, new Vector3(-15f, 0f, 9f), 5.2f);
            CreateModel("Generated_Tree_Oak_Right", oak, parent, new Vector3(15f, 0f, 9f), 5.2f);
            CreateModel("Generated_Tree_Pine_Left", pine, parent, new Vector3(-14f, 0f, -5f), 4.8f);
            CreateModel("Generated_Tree_Pine_Right", pine, parent, new Vector3(14f, 0f, -5f), 4.8f);
            CreateModel("Generated_Tree_Oak_BackLeft", oak, parent, new Vector3(-10f, 0f, 15f), 4.2f);
            CreateModel("Generated_Tree_Pine_BackRight", pine, parent, new Vector3(10f, 0f, 15f), 4.2f);
        }

        private static void AddStreetLights(Transform parent)
        {
            string modelPath = "Assets/ThirdParty/Kenney/CityKitRoads/Models/FBX format/light-curved.fbx";
            Vector3[] positions =
            {
                new Vector3(-12f, 0f, 8f),
                new Vector3(12f, 0f, 8f),
                new Vector3(-12f, 0f, -5f),
                new Vector3(12f, 0f, -5f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject post = CreateModel("Generated_StreetLight_" + (i + 1), modelPath, parent, positions[i], 8f);
                if (post == null) continue;

                post.transform.rotation = Quaternion.Euler(0f, i % 2 == 0 ? 90f : -90f, 0f);
                Light lamp = new GameObject("PosteLuz_Light").AddComponent<Light>();
                lamp.transform.SetParent(post.transform, false);
                lamp.transform.localPosition = new Vector3(0f, 0.61f * 8f, 0f);
                lamp.type = LightType.Point;
                lamp.color = new Color(1f, 0.72f, 0.36f);
                lamp.intensity = 1.1f;
                lamp.range = 8f;
                lamp.shadows = LightShadows.Soft;
            }

            HideExistingMesh("PosteLuzIzquierdo");
            HideExistingMesh("PosteLuzDerecho");
        }

        private static void AddVehicleVisual(Transform station)
        {
            GameObject vehicle = GameObject.Find("VehiculoConRuedas");
            if (vehicle == null) return;

            HideMeshRenderers(vehicle);
            string sedanPath = "Assets/ThirdParty/Kenney/CarKit/Models/FBX format/sedan.fbx";
            GameObject car = CreateModel("Generated_Car_Kenney_Sedan", sedanPath, vehicle.transform, Vector3.zero, 1.55f);
            if (car == null) return;

            car.transform.localRotation = Quaternion.identity;
            AlignModelToGround(car, 0.04f);
        }

        private static void AddFuelPumpVisual(Transform parent)
        {
            GameObject oldPump = GameObject.Find("Surtidor");
            Vector3 position = oldPump != null ? oldPump.transform.position : new Vector3(4.8f, 1.2f, 8.6f);
            position.y = 0f;
            if (oldPump != null) HideMeshRenderers(oldPump);

            GameObject root = new GameObject("Generated_FuelPump_Visual");
            root.transform.SetParent(parent, false);
            root.transform.position = position;

            Material metal = LoadMaterial("Assets/Materials/Mat_Metal.mat");
            Material yellow = LoadMaterial("Assets/Materials/Mat_Yellow.mat");
            Material red = LoadMaterial("Assets/Materials/Mat_Red.mat");
            Material dark = LoadMaterial("Assets/Materials/Mat_Eyes.mat");

            CreatePrimitivePart("PumpBody", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.05f, 0f), new Vector3(1.35f, 2.1f, 0.78f), metal);
            CreatePrimitivePart("PumpTop", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 2.22f, 0f), new Vector3(0.72f, 0.22f, 0.72f), yellow);
            CreatePrimitivePart("PumpFace", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.55f, -0.405f), new Vector3(0.88f, 0.62f, 0.035f), dark);
            CreatePrimitivePart("PumpStripe", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.05f, -0.405f), new Vector3(1.05f, 0.13f, 0.04f), red);
            CreatePrimitivePart("PumpBase", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.1f, 0f), new Vector3(1.55f, 0.2f, 0.92f), yellow);

            GameObject hose = new GameObject("PumpHose");
            hose.transform.SetParent(root.transform, false);
            LineRenderer line = hose.AddComponent<LineRenderer>();
            line.positionCount = 4;
            line.useWorldSpace = false;
            line.startWidth = 0.045f;
            line.endWidth = 0.055f;
            line.material = new Material(Shader.Find("Sprites/Default")) { color = Color.black };
            line.SetPositions(new[]
            {
                new Vector3(0.48f, 1.7f, -0.33f),
                new Vector3(0.62f, 1.25f, -0.34f),
                new Vector3(0.5f, 0.75f, -0.35f),
                new Vector3(0.34f, 0.9f, -0.38f)
            });

            CreatePrimitivePart("Nozzle", PrimitiveType.Capsule, root.transform, new Vector3(0.34f, 0.92f, -0.38f), new Vector3(0.09f, 0.22f, 0.09f), yellow);
        }

        private static void ConfigureSkybox()
        {
            const string path = "Assets/Materials/Skybox_Procedural_Night.mat";
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (sky == null)
            {
                Shader shader = Shader.Find("Skybox/Procedural");
                if (shader == null)
                {
                    Debug.LogWarning("No se encontró Skybox/Procedural.");
                    return;
                }

                sky = new Material(shader) { name = "Skybox_Procedural_Night" };
                AssetDatabase.CreateAsset(sky, path);
            }

            sky.SetColor("_SkyTint", new Color(0.08f, 0.1f, 0.22f));
            sky.SetColor("_GroundColor", new Color(0.015f, 0.02f, 0.04f));
            sky.SetFloat("_AtmosphereThickness", 0.65f);
            sky.SetFloat("_Exposure", 0.65f);
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            EditorUtility.SetDirty(sky);
        }

        private static GameObject CreateModel(string name, string assetPath, Transform parent, Vector3 worldPosition, float scale)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (source == null)
            {
                Debug.LogWarning("No se pudo cargar el modelo: " + assetPath);
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = name;
            instance.transform.SetParent(parent, true);
            instance.transform.position = worldPosition;
            instance.transform.localScale = Vector3.one * scale;
            return instance;
        }

        private static GameObject CreatePrimitivePart(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            return part;
        }

        private static Material LoadMaterial(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        private static Transform GetOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;
            GameObject created = new GameObject(name);
            created.transform.SetParent(parent, false);
            return created.transform;
        }

        private static void ClearGeneratedChildren(Transform parent)
        {
            List<GameObject> toDelete = new List<GameObject>();
            foreach (Transform child in parent)
            {
                if (child.name.StartsWith(GeneratedPrefix) || child.name == "Nature" || child.name == "StreetLights" || child.name == "GasStation")
                    toDelete.Add(child.gameObject);
            }

            foreach (GameObject child in toDelete)
                Object.DestroyImmediate(child);
        }

        private static void HideExistingMesh(string name)
        {
            GameObject target = GameObject.Find(name);
            if (target != null) HideMeshRenderers(target);
        }

        private static void HideMeshRenderers(GameObject root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
        }

        private static void AlignModelToGround(GameObject model, float groundY)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            model.transform.position += Vector3.up * (groundY - bounds.min.y);
        }
    }
}
#endif
