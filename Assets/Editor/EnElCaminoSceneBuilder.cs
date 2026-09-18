using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EnElCamino.Editor
{
    public static class EnElCaminoSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/EnElCamino.unity";
        private const string FuelCanPath = "Assets/Prefabs/FuelCan.prefab";

        [MenuItem("En el Camino/Construir prototipo")]
        public static void BuildPrototype()
        {
            EnsureFolders();
            Material ground = CreateMaterial("Mat_Ground", new Color(0.36f, 0.29f, 0.20f));
            Material road = CreateMaterial("Mat_Road", new Color(0.08f, 0.09f, 0.1f));
            Material wall = CreateMaterial("Mat_Wall", new Color(0.62f, 0.55f, 0.43f));
            Material roof = CreateMaterial("Mat_Roof", new Color(0.18f, 0.20f, 0.22f));
            Material metal = CreateMaterial("Mat_Metal", new Color(0.25f, 0.28f, 0.30f));
            Material yellow = CreateMaterial("Mat_Yellow", new Color(0.95f, 0.62f, 0.08f));
            Material red = CreateMaterial("Mat_Red", new Color(0.75f, 0.08f, 0.05f));
            Material green = CreateMaterial("Mat_Green", new Color(0.1f, 0.75f, 0.18f));
            Material blue = CreateMaterial("Mat_Blue", new Color(0.08f, 0.25f, 0.65f));

            GameObject fuelCanPrefab = CreateFuelCanPrefab(yellow);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientLight = new Color(0.48f, 0.48f, 0.48f);

            GameObject flowObject = new GameObject("GameFlow");
            GameFlow flow = flowObject.AddComponent<GameFlow>();

            CreateLight();
            CreateWorld(ground, road, wall, roof, metal, yellow, red, green, blue, fuelCanPrefab);
            GameUI ui = CreateUI();
            CreatePlayerAndCamera();

            flow.name = "GameFlow";
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("En el Camino: prototipo construido en " + ScenePath);
        }

        public static void ValidatePrototype()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            int errors = 0;
            errors += RequireComponent<GameFlow>("GameFlow");
            errors += RequireComponent<PlayerController>("Jugador");
            errors += RequireComponent<ThirdPersonCamera>("Main Camera");
            errors += RequireComponent<GeneratorInteractable>("GeneradorAuxiliar");
            errors += RequireComponent<FuelPumpInteractable>("Surtidor");
            errors += RequireComponent<ExitTrigger>("SalidaAlCamino");
            if (GameObject.Find("TechoEstacion") == null)
            {
                Debug.LogError("Falta el techo de la estación.");
                errors++;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(FuelCanPath) == null)
            {
                Debug.LogError("Falta el prefab de combustible.");
                errors++;
            }
            if (scene.path != ScenePath)
            {
                Debug.LogError("La escena activa no coincide con la escena principal.");
                errors++;
            }
            if (errors > 0) throw new System.Exception("La validación de En el Camino encontró " + errors + " problemas.");
            Debug.Log("Validación correcta: escena, techo, jugador, cámara, interacciones, trigger y prefab presentes.");
        }

        private static int RequireComponent<T>(string objectName) where T : Component
        {
            GameObject target = GameObject.Find(objectName);
            if (target == null || target.GetComponent<T>() == null)
            {
                Debug.LogError("Falta " + typeof(T).Name + " en " + objectName + ".");
                return 1;
            }
            return 0;
        }

        private static void EnsureFolders()
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
            AssetDatabase.CreateFolder("Assets", "Scripts");
            AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder("Assets", "Editor");
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        private static Material CreateMaterial(string name, Color color)
        {
            string path = "Assets/Materials/" + name + ".mat";
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null && existing.shader == shader) return existing;
            if (existing != null) AssetDatabase.DeleteAsset(path);
            Material material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject CreateFuelCanPrefab(Material material)
        {
            GameObject old = AssetDatabase.LoadAssetAtPath<GameObject>(FuelCanPath);
            if (old != null) AssetDatabase.DeleteAsset(FuelCanPath);
            GameObject can = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            can.name = "FuelCan";
            can.transform.localScale = new Vector3(0.45f, 0.55f, 0.45f);
            can.GetComponent<Renderer>().sharedMaterial = material;
            can.AddComponent<FuelCan>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(can, FuelCanPath);
            Object.DestroyImmediate(can);
            return prefab;
        }

        private static void CreateLight()
        {
            GameObject lightObject = new GameObject("Sun");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.92f, 0.78f);
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        }

        private static void CreateWorld(Material ground, Material road, Material wall, Material roof, Material metal, Material yellow, Material red, Material green, Material blue, GameObject fuelCanPrefab)
        {
            GameObject world = new GameObject("EstacionDeServicio");
            CreateBlock("Terreno", new Vector3(0f, -0.25f, 0f), new Vector3(30f, 0.5f, 30f), ground, world.transform);
            CreateBlock("Camino", new Vector3(0f, 0.01f, -6f), new Vector3(5f, 0.1f, 22f), road, world.transform);

            CreateBlock("ParedFondo", new Vector3(0f, 2.8f, 9.5f), new Vector3(10f, 5.6f, 0.4f), wall, world.transform);
            CreateBlock("ParedIzquierda", new Vector3(-5f, 2.8f, 5.75f), new Vector3(0.4f, 5.6f, 8f), wall, world.transform);
            CreateBlock("ParedDerecha", new Vector3(5f, 2.8f, 5.75f), new Vector3(0.4f, 5.6f, 8f), wall, world.transform);
            CreateBlock("FachadaIzquierda", new Vector3(-3.5f, 2.8f, 1.75f), new Vector3(3f, 5.6f, 0.4f), wall, world.transform);
            CreateBlock("FachadaDerecha", new Vector3(3.5f, 2.8f, 1.75f), new Vector3(3f, 5.6f, 0.4f), wall, world.transform);
            CreateBlock("TechoEstacion", new Vector3(0f, 5.7f, 5.75f), new Vector3(10.8f, 0.45f, 8.8f), roof, world.transform);

            GameObject generator = CreateBlock("GeneradorAuxiliar", new Vector3(-3.2f, 0.9f, 6.3f), new Vector3(1.4f, 1.8f, 1f), metal, world.transform);
            GeneratorInteractable generatorInteractable = generator.AddComponent<GeneratorInteractable>();
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "IndicadorGenerador";
            indicator.transform.SetParent(generator.transform);
            indicator.transform.localPosition = new Vector3(0f, 0.35f, -0.52f);
            indicator.transform.localScale = Vector3.one * 0.22f;
            indicator.GetComponent<Renderer>().sharedMaterial = red;
            Object.DestroyImmediate(indicator.GetComponent<Collider>());
            GameObject generatorLightObject = new GameObject("LuzGenerador");
            generatorLightObject.transform.SetParent(generator.transform);
            generatorLightObject.transform.localPosition = new Vector3(0f, 0.5f, -0.6f);
            Light generatorLight = generatorLightObject.AddComponent<Light>();
            generatorLight.type = LightType.Point;
            generatorLight.range = 3f;
            generatorLight.color = Color.green;
            generatorLight.intensity = 2f;
            generatorLight.enabled = false;
            generatorInteractable.Configure(indicator.GetComponent<Renderer>(), red, green, generatorLight);

            GameObject pump = CreateBlock("Surtidor", new Vector3(3f, 1.2f, 5.3f), new Vector3(1.1f, 2.4f, 1.1f), blue, world.transform);
            FuelPumpInteractable fuelPump = pump.AddComponent<FuelPumpInteractable>();
            GameObject pumpTop = CreateBlock("CabezalSurtidor", new Vector3(3f, 2.55f, 5.3f), new Vector3(1.4f, 0.35f, 1.4f), yellow, pump.transform);
            GameObject spawnPointObject = new GameObject("PuntoCombustible");
            spawnPointObject.transform.SetParent(pump.transform);
            spawnPointObject.transform.localPosition = new Vector3(0f, 1.2f, 0.8f);
            fuelPump.Configure(fuelCanPrefab, spawnPointObject.transform);

            CreateBlock("Auto", new Vector3(0f, 0.55f, -3.7f), new Vector3(2.3f, 0.8f, 4f), red, world.transform);
            CreateBlock("CapotAuto", new Vector3(0f, 1.05f, -5.0f), new Vector3(2.1f, 0.25f, 1.2f), red, world.transform);
            CreateBlock("ParabrisasAuto", new Vector3(0f, 1.25f, -3.5f), new Vector3(1.8f, 0.55f, 0.12f), blue, world.transform);

            CreatePhysicsCrate(new Vector3(1.3f, 0.6f, 7.8f), world.transform, yellow);
            CreatePhysicsCrate(new Vector3(2.4f, 0.6f, 7.8f), world.transform, yellow);

            GameObject exit = new GameObject("SalidaAlCamino");
            exit.transform.position = new Vector3(0f, 1f, -10f);
            BoxCollider exitCollider = exit.AddComponent<BoxCollider>();
            exitCollider.isTrigger = true;
            exitCollider.size = new Vector3(4f, 2f, 1f);
            exit.AddComponent<ExitTrigger>();
        }

        private static GameObject CreatePhysicsCrate(Vector3 position, Transform parent, Material material)
        {
            GameObject crate = CreateBlock("CajaFisica", position, Vector3.one * 1.1f, material, parent);
            crate.AddComponent<Rigidbody>();
            crate.AddComponent<PhysicsCrate>();
            return crate;
        }

        private static GameObject CreateBlock(string name, Vector3 position, Vector3 size, Material material, Transform parent)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.transform.localScale = size;
            block.GetComponent<Renderer>().sharedMaterial = material;
            return block;
        }

        private static GameUI CreateUI()
        {
            GameObject canvasObject = new GameObject("HUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            canvasObject.AddComponent<GraphicRaycaster>();
            GameUI ui = canvasObject.AddComponent<GameUI>();

            Text title = CreateText("Titulo", canvasObject.transform, "EN EL CAMINO", 34, TextAnchor.UpperCenter, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(600f, 60f));
            Text objective = CreateText("Objetivo", canvasObject.transform, "", 22, TextAnchor.UpperLeft, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(650f, 100f));
            Text prompt = CreateText("Prompt", canvasObject.transform, "", 24, TextAnchor.MiddleCenter, Color.yellow, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(700f, 55f));
            Text message = CreateText("Mensaje", canvasObject.transform, "", 22, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(900f, 55f));
            Text controls = CreateText("Controles", canvasObject.transform, "WASD / Flechas: mover   Mouse: cámara   E: interactuar", 16, TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0.75f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 20f), new Vector2(680f, 35f));

            GameObject completed = new GameObject("PanelCompletado");
            completed.transform.SetParent(canvasObject.transform, false);
            Image background = completed.AddComponent<Image>();
            background.color = new Color(0.04f, 0.06f, 0.05f, 0.92f);
            RectTransform backgroundRect = completed.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
            backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
            backgroundRect.anchoredPosition = Vector2.zero;
            backgroundRect.sizeDelta = new Vector2(650f, 220f);
            Text completedText = CreateText("TextoFinal", completed.transform, "VIAJE CONTINUADO\n\nEn el Camino - prototipo completado", 30, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            completedText.rectTransform.offsetMin = new Vector2(20f, 20f);
            completedText.rectTransform.offsetMax = new Vector2(-20f, -20f);
            completed.SetActive(false);

            ui.Configure(objective, prompt, message, completed);
            return ui;
        }

        private static Text CreateText(string name, Transform parent, string text, int size, TextAnchor alignment, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 dimensions)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            Text label = textObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            return label;
        }

        private static void CreatePlayerAndCamera()
        {
            GameObject player = new GameObject("Jugador");
            player.transform.position = new Vector3(0f, 0.1f, -5.5f);
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            player.AddComponent<PlayerController>();
            player.AddComponent<RaycastInteractor>();

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "VisualJugador";
            visual.transform.SetParent(player.transform);
            visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            visual.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            Material playerMaterial = CreateMaterial("Mat_Player", new Color(0.15f, 0.45f, 0.75f));
            visual.GetComponent<Renderer>().sharedMaterial = playerMaterial;

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 5.5f, -13f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            ThirdPersonCamera follow = cameraObject.AddComponent<ThirdPersonCamera>();
            follow.SetTarget(player.transform);
        }
    }
}
