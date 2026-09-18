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
            Material tire = CreateMaterial("Mat_Tire", new Color(0.025f, 0.025f, 0.025f));

            GameObject fuelCanPrefab = CreateFuelCanPrefab(yellow);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientLight = new Color(0.48f, 0.48f, 0.48f);

            GameObject flowObject = new GameObject("GameFlow");
            GameFlow flow = flowObject.AddComponent<GameFlow>();

            CreateLight();
            CreateWorld(ground, road, wall, roof, metal, yellow, red, green, blue, tire, fuelCanPrefab, flow);
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

        private static void CreateWorld(Material ground, Material road, Material wall, Material roof, Material metal, Material yellow, Material red, Material green, Material blue, Material tire, GameObject fuelCanPrefab, GameFlow flow)
        {
            GameObject world = new GameObject("EstacionDeServicio");
            CreateBlock("Terreno", new Vector3(0f, -0.25f, 0f), new Vector3(60f, 0.5f, 50f), ground, world.transform);
            CreateBlock("Camino", new Vector3(0f, 0.01f, -4f), new Vector3(7f, 0.1f, 44f), road, world.transform);
            CreateBlock("PlayaEstacionamiento", new Vector3(0f, 0.03f, -1.5f), new Vector3(24f, 0.1f, 13f), metal, world.transform);

            CreateBlock("ParedFondo", new Vector3(0f, 2.8f, 15.5f), new Vector3(16f, 5.6f, 0.4f), wall, world.transform);
            CreateBlock("ParedIzquierda", new Vector3(-8f, 2.8f, 7.25f), new Vector3(0.4f, 5.6f, 16f), wall, world.transform);
            CreateBlock("ParedDerecha", new Vector3(8f, 2.8f, 7.25f), new Vector3(0.4f, 5.6f, 16f), wall, world.transform);
            CreateBlock("FachadaIzquierda", new Vector3(-5.75f, 2.8f, -0.75f), new Vector3(4.5f, 5.6f, 0.4f), wall, world.transform);
            CreateBlock("FachadaDerecha", new Vector3(5.75f, 2.8f, -0.75f), new Vector3(4.5f, 5.6f, 0.4f), wall, world.transform);
            CreateBlock("TechoEstacion", new Vector3(0f, 5.7f, 7.25f), new Vector3(16.8f, 0.45f, 17.6f), roof, world.transform);

            CreateWorkshop(world.transform, wall, roof, metal, yellow);

            GameObject generator = CreateBlock("GeneradorAuxiliar", new Vector3(-5.5f, 0.9f, 10.2f), new Vector3(1.6f, 1.8f, 1.2f), metal, world.transform);
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

            GameObject pump = CreateBlock("Surtidor", new Vector3(4.8f, 1.2f, 8.6f), new Vector3(1.2f, 2.4f, 1.2f), blue, world.transform);
            FuelPumpInteractable fuelPump = pump.AddComponent<FuelPumpInteractable>();
            CreateBlock("CabezalSurtidor", new Vector3(4.8f, 2.55f, 8.6f), new Vector3(1.5f, 0.35f, 1.5f), yellow, pump.transform);
            GameObject spawnPointObject = new GameObject("PuntoCombustible");
            spawnPointObject.transform.SetParent(pump.transform);
            spawnPointObject.transform.localPosition = new Vector3(0f, 1.2f, 0.9f);
            fuelPump.Configure(fuelCanPrefab, spawnPointObject.transform);

            CreateCar(world.transform, red, blue, tire);

            CreatePhysicsCrate(new Vector3(5.8f, 0.6f, 12f), world.transform, yellow);
            CreatePhysicsCrate(new Vector3(6.9f, 0.6f, 12f), world.transform, yellow);
            CreatePhysicsCrate(new Vector3(5.8f, 1.7f, 12f), world.transform, yellow);

            CreateBlock("CartelRuta", new Vector3(-12f, 2.3f, -3f), new Vector3(0.35f, 4.6f, 0.35f), metal, world.transform);
            CreateBlock("CartelRutaPanel", new Vector3(-12f, 4.1f, -3f), new Vector3(4.5f, 1.1f, 0.25f), yellow, world.transform);
            CreateBlock("PosteLuzIzquierdo", new Vector3(-12f, 3f, 8f), new Vector3(0.3f, 6f, 0.3f), metal, world.transform);
            CreateBlock("PosteLuzDerecho", new Vector3(12f, 3f, 8f), new Vector3(0.3f, 6f, 0.3f), metal, world.transform);

            GameObject exit = new GameObject("SalidaAlCamino");
            exit.transform.position = new Vector3(0f, 1f, -21f);
            BoxCollider exitCollider = exit.AddComponent<BoxCollider>();
            exitCollider.isTrigger = true;
            exitCollider.size = new Vector3(7f, 2f, 1f);
            exit.AddComponent<ExitTrigger>();

            GameObject generatorMarker = CreateObjectiveMarker("MarcadorGenerador", generator.transform.position, yellow, world.transform);
            GameObject fuelMarker = CreateObjectiveMarker("MarcadorSurtidor", pump.transform.position, blue, world.transform);
            GameObject exitMarker = CreateObjectiveMarker("MarcadorSalida", exit.transform.position, green, world.transform);
            flow.ConfigureObjectiveMarkers(generatorMarker, fuelMarker, exitMarker);
        }

        private static void CreateWorkshop(Transform parent, Material wall, Material roof, Material metal, Material yellow)
        {
            Transform workshop = new GameObject("TallerCerrado").transform;
            workshop.SetParent(parent);
            CreateBlock("TallerParedFondo", new Vector3(19f, 2.8f, 14f), new Vector3(10f, 5.6f, 0.4f), wall, workshop);
            CreateBlock("TallerParedIzquierda", new Vector3(14f, 2.8f, 8f), new Vector3(0.4f, 5.6f, 12f), wall, workshop);
            CreateBlock("TallerParedDerecha", new Vector3(24f, 2.8f, 8f), new Vector3(0.4f, 5.6f, 12f), wall, workshop);
            CreateBlock("TallerFachadaIzquierda", new Vector3(16.5f, 2.8f, 2f), new Vector3(4.5f, 5.6f, 0.4f), wall, workshop);
            CreateBlock("TallerFachadaDerecha", new Vector3(21.5f, 2.8f, 2f), new Vector3(4.5f, 5.6f, 0.4f), wall, workshop);
            CreateBlock("TallerTecho", new Vector3(19f, 5.7f, 8f), new Vector3(10.8f, 0.45f, 12.8f), roof, workshop);
            CreateBlock("BancoTaller", new Vector3(19f, 1f, 10.5f), new Vector3(5f, 0.7f, 1.2f), metal, workshop);
            CreateBlock("CartelTaller", new Vector3(19f, 4.3f, 1.75f), new Vector3(3.8f, 0.8f, 0.25f), yellow, workshop);
        }

        private static void CreateCar(Transform parent, Material body, Material glass, Material tire)
        {
            Transform car = new GameObject("VehiculoConRuedas").transform;
            car.SetParent(parent);
            car.position = new Vector3(5.2f, 0f, -7.2f);
            CreateBlock("Carroceria", car.position + new Vector3(0f, 0.65f, 0f), new Vector3(3f, 0.9f, 5.2f), body, car);
            CreateBlock("Capot", car.position + new Vector3(0f, 1.15f, -1.7f), new Vector3(2.7f, 0.25f, 1.5f), body, car);
            CreateBlock("TechoAuto", car.position + new Vector3(0f, 1.35f, 0.55f), new Vector3(2.45f, 0.25f, 2.1f), body, car);
            CreateBlock("Parabrisas", car.position + new Vector3(0f, 1.45f, -0.55f), new Vector3(2.25f, 0.6f, 0.12f), glass, car);
            CreateBlock("Luneta", car.position + new Vector3(0f, 1.45f, 1.65f), new Vector3(2.25f, 0.6f, 0.12f), glass, car);
            CreateWheel("RuedaDelanteraIzquierda", car, new Vector3(-1.5f, 0.45f, -1.55f), tire);
            CreateWheel("RuedaDelanteraDerecha", car, new Vector3(1.5f, 0.45f, -1.55f), tire);
            CreateWheel("RuedaTraseraIzquierda", car, new Vector3(-1.5f, 0.45f, 1.55f), tire);
            CreateWheel("RuedaTraseraDerecha", car, new Vector3(1.5f, 0.45f, 1.55f), tire);
        }

        private static void CreateWheel(string name, Transform parent, Vector3 localPosition, Material tire)
        {
            GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = name;
            wheel.transform.SetParent(parent);
            wheel.transform.localPosition = localPosition;
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheel.transform.localScale = new Vector3(0.65f, 0.22f, 0.65f);
            wheel.GetComponent<Renderer>().sharedMaterial = tire;
        }

        private static GameObject CreatePhysicsCrate(Vector3 position, Transform parent, Material material)
        {
            GameObject crate = CreateBlock("CajaFisica", position, Vector3.one * 1.1f, material, parent);
            crate.AddComponent<Rigidbody>();
            crate.AddComponent<PhysicsCrate>();
            return crate;
        }

        private static GameObject CreateObjectiveMarker(string name, Vector3 position, Material material, Transform parent)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent);
            marker.transform.position = position;

            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "HazDeGuia";
            beam.transform.SetParent(marker.transform);
            beam.transform.localPosition = new Vector3(0f, 5.2f, 0f);
            beam.transform.localScale = new Vector3(0.24f, 4.5f, 0.24f);
            beam.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(beam.GetComponent<Collider>());

            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = "BalizaDeGuia";
            beacon.transform.SetParent(marker.transform);
            beacon.transform.localPosition = new Vector3(0f, 9.8f, 0f);
            beacon.transform.localScale = Vector3.one * 0.75f;
            beacon.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(beacon.GetComponent<Collider>());

            GameObject lightObject = new GameObject("LuzDeGuia");
            lightObject.transform.SetParent(marker.transform);
            lightObject.transform.localPosition = new Vector3(0f, 9.5f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = material.color;
            light.range = 8f;
            light.intensity = 2.5f;
            return marker;
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
            GameObject objectivePanel = CreatePanel("PanelObjetivo", canvasObject.transform, new Color(0.03f, 0.05f, 0.07f, 0.88f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-270f, -132f), new Vector2(520f, 150f));
            CreatePanelText("EtiquetaObjetivo", objectivePanel.transform, "MISIÓN", 18, new Color(1f, 0.78f, 0.2f), new Vector2(20f, -14f), new Vector2(470f, 28f));
            Text objective = CreatePanelText("Objetivo", objectivePanel.transform, "Preparando objetivo...", 20, Color.white, new Vector2(20f, -45f), new Vector2(480f, 100f));
            Text prompt = CreateText("Prompt", canvasObject.transform, "", 24, TextAnchor.MiddleCenter, Color.yellow, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(700f, 55f));
            Text message = CreateText("Mensaje", canvasObject.transform, "", 22, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(900f, 55f));
            CreateText("PuntoDeMira", canvasObject.transform, "+", 30, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
            CreateText("Controles", canvasObject.transform, "WASD: mover  |  Mouse: girar cámara  |  E: usar", 15, TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0.75f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-260f, 24f), new Vector2(500f, 35f));

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

        private static GameObject CreatePanel(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image image = panel.AddComponent<Image>();
            image.color = color;
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return panel;
        }

        private static Text CreatePanelText(string name, Transform parent, string text, int size, Color color, Vector2 position, Vector2 dimensions)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            Text label = textObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAnchor.UpperLeft;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            return label;
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
            player.transform.position = new Vector3(0f, 0.1f, -14f);
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
            cameraObject.transform.position = new Vector3(0f, 2.7f, -20.5f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 65f;
            ThirdPersonCamera follow = cameraObject.AddComponent<ThirdPersonCamera>();
            follow.SetTarget(player.transform);
        }
    }
}
