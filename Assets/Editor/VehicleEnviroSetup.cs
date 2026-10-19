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
        private const string GrassMaterialPath = "Assets/Materials/EnviroGrass_Generated.mat";

        [MenuItem("Parcial 2/Integrar Vehicle Physics Pro y Enviro 3")]
        public static void Apply()
        {
            SceneSetup();
            IntegrateVehicle();
            IntegrateEnviro();
            ConfigureGeneratorTexture();
            ConfigureTelemetryAndLighting();
            FixNegativeTerrainScale();
            GenerateEnviroGrass();
            RemoveVoicePlayback();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Vehicle Physics Pro, Enviro 3 y el flujo de llegada fueron integrados.");
        }

        private static void SceneSetup()
        {
            Transform sign = GameObject.Find("EstacionDeServicio/CartelRuta")?.transform;
            Vector3 signPosition = sign != null ? sign.position : new Vector3(-42.39f, 2.3f, -127.89f);
            Vector3 stop = new Vector3(signPosition.x, 0.55f, signPosition.z - 4f);
            EnsurePoint("AutoEntradaRuta", stop + Vector3.back * 70f, Quaternion.identity);
            EnsurePoint("AutoParadaRuta", stop, Quaternion.identity);
            EnsurePoint("PuntoBajadaAuto", stop + Vector3.right * 2.4f, Quaternion.identity);
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

            Component telemetry = car.GetComponent("VPTelemetry");
            if (telemetry is Behaviour telemetryBehaviour)
            {
                telemetryBehaviour.enabled = false;
                EditorUtility.SetDirty(telemetryBehaviour);
            }

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
            SetValue(dayTimer, "duration", 180f);

            GameObject legacySun = GameObject.Find("Sun");
            if (legacySun != null) legacySun.SetActive(false);

            Transform enviroSun = enviro.transform.Find("Sun/Moon Directional Light");
            Light enviroLight = enviroSun != null ? enviroSun.GetComponent<Light>() : null;
            if (enviroLight != null)
            {
                enviroLight.intensity = 0.75f;
                enviroLight.shadows = LightShadows.Soft;
                EditorUtility.SetDirty(enviroLight);
            }
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

            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
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
                renderer.sharedMaterial = material;
            }
        }

        private static void ConfigureTelemetryAndLighting()
        {
            GameObject car = GameObject.Find("EstacionDeServicio/VehiculoConRuedas");
            Component telemetry = car != null ? car.GetComponent("VPTelemetry") : null;
            if (telemetry is Behaviour behaviour)
            {
                behaviour.enabled = false;
                EditorUtility.SetDirty(behaviour);
            }

            GameObject station = GameObject.Find("EstacionDeServicio");
            if (station == null) return;
            foreach (MeshRenderer renderer in station.GetComponentsInChildren<MeshRenderer>(true))
            {
                string objectName = renderer.gameObject.name.ToLowerInvariant();
                if (!objectName.Contains("terreno") && !objectName.Contains("camino") && !objectName.Contains("ruta") && !objectName.Contains("playa")) continue;
                Material source = renderer.sharedMaterial;
                if (source == null) continue;
                Material matte = new Material(source) { name = source.name + "_Matte" };
                if (matte.HasProperty("_Metallic")) matte.SetFloat("_Metallic", 0f);
                if (matte.HasProperty("_Smoothness")) matte.SetFloat("_Smoothness", 0.18f);
                renderer.sharedMaterial = matte;
            }
        }

        private static void FixNegativeTerrainScale()
        {
            GameObject terrain = GameObject.Find("EstacionDeServicio/Terreno");
            if (terrain == null) return;
            Vector3 scale = terrain.transform.localScale;
            terrain.transform.localScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            BoxCollider box = terrain.GetComponent<BoxCollider>();
            if (box != null)
            {
                Vector3 size = box.size;
                box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
                EditorUtility.SetDirty(box);
            }
            EditorUtility.SetDirty(terrain.transform);
        }

        private static void GenerateEnviroGrass()
        {
            GameObject station = GameObject.Find("EstacionDeServicio");
            if (station == null) return;
            Transform existing = station.transform.Find("EnviroVegetation/Generated_EnviroGrass");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            GameObject parent = station.transform.Find("EnviroVegetation")?.gameObject;
            if (parent == null) { parent = new GameObject("EnviroVegetation"); parent.transform.SetParent(station.transform); }
            GameObject grass = new GameObject("Generated_EnviroGrass");
            grass.transform.SetParent(parent.transform);

            Mesh mesh = new Mesh { name = "EnviroGrassGeneratedMesh" };
            var vertices = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();
            int index = 0;
            for (int x = -35; x <= 35; x += 3)
            for (int z = -155; z <= -55; z += 4)
            {
                if ((x * 17 + z * 7) % 11 == 0) continue;
                float height = 0.35f + Mathf.Abs((x * 13 + z * 3) % 10) * 0.025f;
                Vector3 center = new Vector3(x + 0.35f * Mathf.Sin(z), 0.04f, z);
                AddGrassCross(vertices, triangles, ref index, center, height);
            }
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
            MeshFilter filter = grass.AddComponent<MeshFilter>(); filter.sharedMesh = mesh;
            MeshRenderer renderer = grass.AddComponent<MeshRenderer>(); renderer.sharedMaterial = GetGrassMaterial();
            EditorUtility.SetDirty(mesh);
        }

        private static void AddGrassCross(System.Collections.Generic.List<Vector3> vertices, System.Collections.Generic.List<int> triangles, ref int index, Vector3 c, float h)
        {
            float w = 0.22f;
            Vector3[] points = { c + new Vector3(-w, 0, 0), c + new Vector3(w, 0, 0), c + new Vector3(w * 0.5f, h, 0), c + new Vector3(-w * 0.5f, h, 0), c + new Vector3(0, 0, -w), c + new Vector3(0, 0, w), c + new Vector3(0, h, w * 0.5f), c + new Vector3(0, h, -w * 0.5f) };
            vertices.AddRange(points);
            for (int i = 0; i < 2; i++) { int o = index + i * 4; triangles.AddRange(new[] { o, o + 1, o + 2, o + 2, o + 3, o }); }
            index += 8;
        }

        private static Material GetGrassMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(GrassMaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader) { name = "EnviroGrass_Generated" };
                AssetDatabase.CreateAsset(material, GrassMaterialPath);
            }
            Shader standard = Shader.Find("Standard");
            if (standard != null && material.shader != standard) material.shader = standard;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(0.12f, 0.34f, 0.055f));
            if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(0.12f, 0.34f, 0.055f));
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.9f);
            return material;
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
