using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EnElCamino.Editor
{
    public static class VehicleEnviroSetup
    {
        private const string VehiclePrefabPath = "Assets/Vehicle Physics Pro/Vehicles/Sport Coupe/VPP Sport Coupe.prefab";
        private const string EnviroPrefabPath = "Assets/Enviro 3 - Sky and Weather/Enviro 3.prefab";
        private const string MaterialPath = "Assets/Materials/GeneratorBody_Textured.mat";
        private const string TexturePath = "Assets/Materials/GeneratorBody_Textured.asset";

        [MenuItem("Parcial 2/Integrar Vehicle Physics Pro y Enviro 3")]
        public static void Apply()
        {
            SceneSetup();
            IntegrateVehicle();
            IntegrateEnviro();
            ConfigureGeneratorTexture();
            RemoveVoicePlayback();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Vehicle Physics Pro, Enviro 3 y el flujo de llegada fueron integrados.");
        }

        private static void SceneSetup()
        {
            EnsurePoint("AutoEntradaRuta", new Vector3(0f, 0.55f, -90f), Quaternion.identity);
            EnsurePoint("AutoParadaRuta", new Vector3(0f, 0.55f, -32f), Quaternion.identity);
            EnsurePoint("PuntoBajadaAuto", new Vector3(2.2f, 0.1f, -32f), Quaternion.identity);
        }

        private static void IntegrateVehicle()
        {
            GameObject station = GameObject.Find("EstacionDeServicio");
            GameObject oldCar = GameObject.Find("VehiculoConRuedas");
            if (oldCar == null) return;

            GameObject existingVpp = GameObject.Find("VehiculoConRuedas");
            if (existingVpp != null && existingVpp.GetComponent("VPVehicleController") != null)
            {
                ConfigureVehicleComponents(existingVpp);
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VehiclePrefabPath);
            if (prefab == null)
            {
                Debug.LogError("No se encontró el prefab de Vehicle Physics Pro: " + VehiclePrefabPath);
                return;
            }

            Transform parent = oldCar.transform.parent;
            Vector3 previousPosition = oldCar.transform.position;
            Quaternion previousRotation = oldCar.transform.rotation;
            oldCar.name = "Legacy_VehiculoConRuedas";
            oldCar.SetActive(false);

            GameObject car = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (car == null) return;
            car.name = "VehiculoConRuedas";
            car.transform.SetPositionAndRotation(previousPosition, previousRotation);

            ConfigureVehicleComponents(car);

            SetReference(GameObject.Find("GameFlow")?.GetComponent<VerticalSliceDirector>(), "vehicle", car.transform);
            SetReference(GameObject.Find("Main Camera")?.GetComponent<CinemachineFollowSetup>(), "vehicle", car.transform);
            SetReference(GameObject.Find("Jugador")?.GetComponent<RaycastInteractor>(), "car", car.GetComponent<CarInteractable>());
        }

        private static void ConfigureVehicleComponents(GameObject car)
        {
            GameObject flowObject = GameObject.Find("GameFlow");
            GameObject player = GameObject.Find("Jugador");
            GameObject camera = GameObject.Find("Main Camera");

            CarInteractable interactable = car.GetComponent<CarInteractable>();
            if (interactable == null) interactable = car.AddComponent<CarInteractable>();
            CarDriver driver = car.GetComponent<CarDriver>();
            if (driver == null) driver = car.AddComponent<CarDriver>();
            CarArrivalSequence arrival = car.GetComponent<CarArrivalSequence>();
            if (arrival == null) arrival = car.AddComponent<CarArrivalSequence>();

            GameFlow flow = flowObject != null ? flowObject.GetComponent<GameFlow>() : null;
            FuelCan fuelCan = Object.FindFirstObjectByType<FuelCan>();
            ThirdPersonCamera thirdPersonCamera = camera != null ? camera.GetComponent<ThirdPersonCamera>() : null;

            SetReference(interactable, "gameFlow", flow);
            SetReference(interactable, "fuelCan", fuelCan);
            SetReference(interactable, "carDriver", driver);
            SetReference(interactable, "startSound", LoadClip("SFX/motor_arranque.mp3"));

            SetReference(driver, "gameFlow", flow);
            SetReference(driver, "player", player);
            SetReference(driver, "cameraFollow", thirdPersonCamera);
            SetReference(driver, "accelerationSound", LoadClip("SFX/motor_acelerando.mp3"));
            SetReference(driver, "engineLoopSound", LoadClip("SFX/motor_acelerando.mp3"));

            SetReference(arrival, "gameFlow", flow);
            SetReference(arrival, "player", player);
            SetReference(arrival, "arrivalStart", GameObject.Find("AutoEntradaRuta")?.transform);
            SetReference(arrival, "stopPoint", GameObject.Find("AutoParadaRuta")?.transform);
            SetReference(arrival, "exitPoint", GameObject.Find("PuntoBajadaAuto")?.transform);
            SetValue(arrival, "duration", 7f);

            SetReference(GameObject.Find("GameFlow")?.GetComponent<VerticalSliceDirector>(), "vehicle", car.transform);
            SetReference(GameObject.Find("Main Camera")?.GetComponent<CinemachineFollowSetup>(), "vehicle", car.transform);
            SetReference(GameObject.Find("Jugador")?.GetComponent<RaycastInteractor>(), "car", interactable);
        }

        private static void IntegrateEnviro()
        {
            GameObject enviro = GameObject.Find("Enviro 3");
            if (enviro == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnviroPrefabPath);
                if (prefab == null)
                {
                    Debug.LogError("No se encontró el prefab de Enviro 3: " + EnviroPrefabPath);
                    return;
                }
                enviro = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                enviro.name = "Enviro 3";
            }

            Component manager = enviro.GetComponent("EnviroManager");
            Camera mainCamera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
            Transform player = GameObject.Find("Jugador")?.transform;
            SetReference(manager, "Camera", mainCamera);
            SetReference(manager, "optionalFollowTransform", player);
            SetValue(manager, "updateSkyAndLighting", true);
            SetValue(manager, "updateSkyAndLightingHDRP", false);

            DayTimer dayTimer = GameObject.Find("RelojDelDia")?.GetComponent<DayTimer>();
            SetValue(dayTimer, "controlLegacyLighting", false);
        }

        private static void ConfigureGeneratorTexture()
        {
            GameObject generator = GameObject.Find("GeneradorAuxiliar");
            if (generator == null) return;

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (texture == null)
            {
                texture = new Texture2D(64, 64, TextureFormat.RGBA32, false) { name = "GeneratorBody_Textured" };
                for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                {
                    bool stripe = ((x / 8) + (y / 8)) % 2 == 0;
                    Color color = stripe ? new Color(0.34f, 0.055f, 0.018f) : new Color(0.62f, 0.12f, 0.025f);
                    texture.SetPixel(x, y, color);
                }
                texture.Apply();
                AssetDatabase.CreateAsset(texture, TexturePath);
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "GeneratorBody_Textured" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.25f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.45f);
            EditorUtility.SetDirty(material);

            foreach (MeshRenderer renderer in generator.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.gameObject.name.Contains("Generated_PortableGenerator"))
                    renderer.sharedMaterial = material;
            }
        }

        private static void RemoveVoicePlayback()
        {
            GameObject voice = GameObject.Find("VoiceNarration");
            if (voice != null) Object.DestroyImmediate(voice);
        }

        private static Transform EnsurePoint(string name, Vector3 position, Quaternion rotation)
        {
            GameObject point = GameObject.Find(name);
            if (point == null) point = new GameObject(name);
            point.transform.SetPositionAndRotation(position, rotation);
            return point.transform;
        }

        private static AudioClip LoadClip(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/" + path);

        private static void SetReference(Object target, string field, Object value)
        {
            if (target == null) return;
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null) return;
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetValue(Object target, string field, object value)
        {
            if (target == null) return;
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null) return;
            if (value is bool boolean) property.boolValue = boolean;
            else if (value is float number) property.floatValue = number;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
