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
        private const string GrassTexturePath = "Assets/Enviro 3 - Sky and Weather/Sample/Textures/Grass004_1K_Color.png";
        private const string GrassNormalPath = "Assets/Enviro 3 - Sky and Weather/Sample/Textures/Grass004_1K_NormalDX.png";
        private const string NatureKitPath = "Assets/NatureStarterKit2/Nature/";
        private const string NatureGrassMaterialPath = "Assets/Materials/NatureStarterGrass.mat";
        private const string NatureGrassMeshPath = "Assets/Materials/NatureStarterGrassBillboard.asset";

        [MenuItem("Parcial 2/Integrar Vehicle Physics Pro y Enviro 3")]
        public static void Apply()
        {
            SceneSetup();
            IntegrateVehicle();
            IntegrateEnviro();
            ConfigureGeneratorTexture();
            ConfigureTelemetryAndLighting();
            FixNegativeTerrainScale();
            RemoveGeneratedGrass();
            ConfigureEnviroGroundTexture();
            AddEnviroVegetation();
            ConfigureEnviroAudio();
            ReplaceFuelCanVisual();
            RemoveVoicePlayback();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Vehicle Physics Pro, Enviro 3 y el flujo de llegada fueron integrados.");
        }

        private static void SceneSetup()
        {
            Transform sign = GameObject.Find("EstacionDeServicio/CartelRuta")?.transform;
            Vector3 signPosition = sign != null ? sign.position : new Vector3(-42.39f, 2.3f, -127.89f);
            Transform legacyCar = FindTransformIncludingInactive("Legacy_VehiculoConRuedas");
            float roadX = legacyCar != null ? legacyCar.position.x : signPosition.x;
            float roadStartZ = legacyCar != null ? legacyCar.position.z : signPosition.z - 74f;
            Vector3 stop = new Vector3(roadX, 0.55f, signPosition.z - 4f);
            EnsurePoint("AutoEntradaRuta", new Vector3(roadX, 0.55f, roadStartZ), Quaternion.identity);
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

            Light enviroLight = FindChildLight(enviro, "Sun/Moon Directional Light");
            if (enviroLight != null)
            {
                enviroLight.intensity = 0.22f;
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
            if (Shader.Find("Standard") != null) material.shader = Shader.Find("Standard");

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
                if (matte.HasProperty("_Glossiness")) matte.SetFloat("_Glossiness", 0f);
                if (matte.HasProperty("_SpecColor")) matte.SetColor("_SpecColor", Color.black);
                renderer.sharedMaterial = matte;
            }
        }

        private static void ConfigureEnviroGroundTexture()
        {
            GameObject terrain = GameObject.Find("EstacionDeServicio/Terreno");
            MeshRenderer renderer = terrain != null ? terrain.GetComponent<MeshRenderer>() : null;
            Texture2D grassTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassTexturePath);
            Texture2D grassNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassNormalPath);
            if (renderer == null || grassTexture == null) return;

            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(GrassMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "EnviroGrass_Ground" };
                AssetDatabase.CreateAsset(material, GrassMaterialPath);
            }
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", grassTexture);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", grassTexture);
            if (material.HasProperty("_BumpMap") && grassNormal != null) material.SetTexture("_BumpMap", grassNormal);
            if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(0.68f, 0.75f, 0.62f));
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(0.68f, 0.75f, 0.62f));
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0f);
            material.mainTextureScale = new Vector2(80f, 80f);
            EditorUtility.SetDirty(material);
            renderer.sharedMaterial = material;
        }

        private static void AddEnviroVegetation()
        {
            GameObject station = GameObject.Find("EstacionDeServicio");
            Transform nature = station != null ? station.transform.Find("Art_Environment/Nature") : null;
            if (station == null || nature == null) return;

            Transform vegetation = station.transform.Find("NatureStarterVegetation");
            if (vegetation == null)
            {
                GameObject root = new GameObject("NatureStarterVegetation");
                root.transform.SetParent(station.transform);
                vegetation = root.transform;
            }

            Transform trees = vegetation.Find("Trees");
            if (trees == null)
            {
                GameObject root = new GameObject("Trees");
                root.transform.SetParent(vegetation);
                trees = root.transform;
            }

            // Desactivamos los árboles placeholder, conservando sus posiciones para reemplazarlos.
            var oldTrees = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in nature.GetComponentsInChildren<Transform>(true))
                if (child.name.StartsWith("Generated_Tree_")) oldTrees.Add(child);

            string[] treePrefabs = { "tree01.prefab", "tree02.prefab", "tree03.prefab", "tree04.prefab" };
            for (int i = trees.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(trees.GetChild(i).gameObject);

            for (int i = 0; i < oldTrees.Count; i++)
            {
                Transform old = oldTrees[i];
                Vector3 position = old.position;
                Quaternion rotation = old.rotation;
                old.gameObject.SetActive(false);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NatureKitPath + treePrefabs[i % treePrefabs.Length]);
                if (prefab == null) continue;
                GameObject copy = PrefabUtility.InstantiatePrefab(prefab, trees) as GameObject;
                if (copy == null) continue;
                copy.name = "NatureStarterTree_" + i.ToString("00");
                copy.transform.SetPositionAndRotation(position, rotation);
                copy.transform.localScale = Vector3.one;
            }

            // Pasto 3D del paquete, colocado en los laterales de tierra y fuera del camino.
            Transform grass = vegetation.Find("Grass3D");
            if (grass == null)
            {
                GameObject root = new GameObject("Grass3D");
                root.transform.SetParent(vegetation);
                grass = root.transform;
            }
            for (int i = grass.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(grass.GetChild(i).gameObject);

            Mesh grassMesh = GetOrCreateNatureGrassMesh();
            Material grassMaterial = GetOrCreateNatureGrassMaterial();
            if (grassMesh == null || grassMaterial == null) return;

            int grassIndex = 0;
            for (int side = 0; side < 2; side++)
            for (int row = 0; row < 7; row++)
            for (int column = 0; column < 5; column++)
            {
                float x = side == 0 ? 8f + column * 2.4f : 49f + column * 2.4f;
                float z = -10f + row * 8.2f + ((column * 1.7f + row * 0.9f) % 2.2f);
                GameObject clump = new GameObject("NatureStarterGrass_" + grassIndex.ToString("000"));
                clump.transform.SetParent(grass);
                clump.transform.position = new Vector3(x, 0.04f, z);
                clump.transform.rotation = Quaternion.Euler(0f, (grassIndex * 37) % 360, 0f);
                clump.transform.localScale = Vector3.one * (0.85f + (grassIndex % 4) * 0.12f);
                MeshFilter filter = clump.AddComponent<MeshFilter>();
                filter.sharedMesh = grassMesh;
                MeshRenderer renderer = clump.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = grassMaterial;
                grassIndex++;
            }
        }

        private static Mesh GetOrCreateNatureGrassMesh()
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(NatureGrassMeshPath);
            if (mesh != null) return mesh;
            mesh = new Mesh { name = "NatureStarterGrassBillboard" };
            mesh.vertices = new[]
            {
                new Vector3(-0.42f, 0f, 0f), new Vector3(0.42f, 0f, 0f), new Vector3(0.34f, 1.15f, 0f), new Vector3(-0.34f, 1.15f, 0f),
                new Vector3(0f, 0f, -0.42f), new Vector3(0f, 0f, 0.42f), new Vector3(0f, 1.15f, 0.34f), new Vector3(0f, 1.15f, -0.34f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 1, 2, 2, 3, 0, 4, 6, 5, 6, 4, 7 };
            mesh.RecalculateNormals();
            AssetDatabase.CreateAsset(mesh, NatureGrassMeshPath);
            return mesh;
        }

        private static Material GetOrCreateNatureGrassMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(NatureGrassMaterialPath);
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            if (material == null)
            {
                material = new Material(shader) { name = "NatureStarterGrass" };
                AssetDatabase.CreateAsset(material, NatureGrassMaterialPath);
            }
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(NatureKitPath.Replace("Nature/", "Textures/") + "grass01.tga");
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Cutoff")) material.SetFloat("_Cutoff", 0.35f);
            if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 1f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = 2450;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Light FindChildLight(GameObject root, string name)
        {
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
                if (light.name == name || light.name.Contains("Sun/Moon")) return light;
            return null;
        }

        private static void ConfigureEnviroAudio()
        {
            GameObject enviro = GameObject.Find("Enviro 3");
            if (enviro == null) return;
            foreach (AudioSource source in enviro.GetComponentsInChildren<AudioSource>(true))
            {
                source.volume = Mathf.Min(source.volume, 0.12f);
                EditorUtility.SetDirty(source);
            }
        }

        private static void ReplaceFuelCanVisual()
        {
            Transform fuelCanTransform = FindTransformIncludingInactive("BidonCombustible");
            GameObject fuelCan = fuelCanTransform != null ? fuelCanTransform.gameObject : null;
            GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FuelCan.prefab");
            if (fuelCan == null || visualPrefab == null) return;

            Transform oldVisual = fuelCan.transform.Find("Generated_FuelCanister_CC0");
            if (oldVisual != null) oldVisual.gameObject.SetActive(false);
            Transform existing = fuelCan.transform.Find("PlasticFuelCan_Visual");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            GameObject visual = PrefabUtility.InstantiatePrefab(visualPrefab, fuelCan.transform) as GameObject;
            if (visual == null) return;
            visual.name = "PlasticFuelCan_Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(0.7f, 0.75f, 0.7f);
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

            return;

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

        private static void RemoveGeneratedGrass()
        {
            GameObject station = GameObject.Find("EstacionDeServicio");
            Transform existing = station != null ? station.transform.Find("EnviroVegetation/Generated_EnviroGrass") : null;
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            Transform vegetation = station != null ? station.transform.Find("EnviroVegetation") : null;
            if (vegetation != null && vegetation.childCount == 0) Object.DestroyImmediate(vegetation.gameObject);
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

        private static Transform FindTransformIncludingInactive(string objectName)
        {
            foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (transform.name == objectName && transform.gameObject.scene.IsValid()) return transform;
            }
            return null;
        }

        private static AudioClip LoadClip(string path)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/" + path);
            if (clip != null) return clip;
            return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + path);
        }

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
