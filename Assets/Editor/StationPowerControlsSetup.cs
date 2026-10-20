using EnElCamino;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EnElCamino.Editor
{
    [InitializeOnLoad]
    public static class StationPowerControlsSetup
    {
        [InitializeOnLoadMethod]
        private static void ApplyOnEditorLoad()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += ApplyWhenSceneIsEditable;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += ApplyWhenSceneIsEditable;
        }

        private static void ApplyWhenSceneIsEditable()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "EnElCamino") return;
            GameObject gate = FindSceneObject(scene, "InterruptorPorton");
            GameObject existingBox = FindSceneObject(scene, "CajaElectricaEstacion");
            if (existingBox != null && existingBox.transform.Find("TableroFrontal") != null &&
                existingBox.GetComponent<StationPowerCableVisual>() != null && gate != null &&
                gate.transform.Find("TeclaPared_JarlanPerez_Texturada") != null && PowerBoxHasTexture()) return;
            Apply();
        }

        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene activeScene = EditorSceneManager.GetActiveScene();
            GameObject station = FindSceneObject(activeScene, "EstacionDeServicio");
            GameFlow flow = FindSceneComponent<GameFlow>(activeScene);
            GeneratorInteractable generator = FindSceneComponent<GeneratorInteractable>(activeScene);
            GarageDoorInteractable door = FindSceneComponent<GarageDoorInteractable>(activeScene);
            GameObject car = FindSceneObject(activeScene, "VehiculoConRuedas");
            if (station == null || flow == null || generator == null)
            {
                Debug.LogError($"No se pudo armar el tablero: escena={activeScene.name}, estación={station != null}, GameFlow={flow != null}, generador={generator != null}.");
                return;
            }

            // Replacing a visual must not deactivate the generator's interaction button.
            for (Transform current = generator.transform; current != null && current != station.transform; current = current.parent)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(true);
                    EditorUtility.SetDirty(current.gameObject);
                }
            }
            Debug.Log("[GateSetup] Botón generador=" + generator.name + "; activo=" + generator.gameObject.activeInHierarchy);

            Transform powerBox = station.transform.Find("CajaElectricaEstacion");
            if (powerBox == null) powerBox = BuildControl(station.transform, "CajaElectricaEstacion", generator.transform.position + Vector3.up * 1.25f + generator.transform.right * 1.5f, false);
            StationPowerCableVisual cableVisual = BuildPowerBoxVisuals(powerBox, generator.transform);
            StationPowerBoxInteractable boxInteractable = powerBox.GetComponent<StationPowerBoxInteractable>();
            if (boxInteractable == null) boxInteractable = powerBox.gameObject.AddComponent<StationPowerBoxInteractable>();
            Set(boxInteractable, "gameFlow", flow);
            Set(boxInteractable, "statusLight", powerBox.Find("LuzEstado")?.GetComponent<Renderer>());
            Set(boxInteractable, "cableVisual", cableVisual);

            Transform gateSwitch = station.transform.Find("InterruptorPorton");
            if (gateSwitch == null)
            {
                Vector3 position = door != null ? door.transform.position : generator.transform.position + Vector3.right * 2f;
                position += Vector3.right * 1.1f + Vector3.up * 1.25f;
                gateSwitch = BuildControl(station.transform, "InterruptorPorton", position, true);
            }
            GateSwitchInteractable switchInteractable = gateSwitch.GetComponent<GateSwitchInteractable>();
            if (switchInteractable == null) switchInteractable = gateSwitch.gameObject.AddComponent<GateSwitchInteractable>();
            Transform rocker = GateSwitchVisualSetup.Build(gateSwitch, door, generator);
            if (door != null && door.DoorObject != door.gameObject)
            {
                Renderer oldButtonRenderer = door.GetComponent<Renderer>();
                if (oldButtonRenderer != null) oldButtonRenderer.enabled = false;
                Collider oldButtonCollider = door.GetComponent<Collider>();
                if (oldButtonCollider != null) oldButtonCollider.enabled = false;
            }
            Set(switchInteractable, "gameFlow", flow);
            Set(switchInteractable, "garageDoor", door);
            Set(switchInteractable, "lever", rocker);
            Set(switchInteractable, "switchSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/switch_electricidad.mp3"));
            RaycastInteractor interactor = FindSceneComponent<RaycastInteractor>(activeScene);
            Set(interactor, "gateSwitch", switchInteractable);
            Set(interactor, "powerBox", boxInteractable);
            Set(interactor, "garageDoor", door);

            RestoreVppAudioAndDefaults(activeScene);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Caja eléctrica e interruptor físico del portón listos en la escena.");
        }

        private static void RestoreVppAudioAndDefaults(Scene scene)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid() || !scene.isLoaded) return;

            RefreshGroundMaterials(scene);

            GameObject car = FindSceneObject(scene, "VehiculoConRuedas");
            if (car != null)
            {
                foreach (AudioSource source in car.GetComponentsInChildren<AudioSource>(true))
                {
                    if (source == null || source.gameObject == car) continue;
                    source.enabled = true;
                    source.mute = false;
                    EditorUtility.SetDirty(source);
                }
                foreach (VehiclePhysics.VPAudio nativeAudio in car.GetComponentsInChildren<VehiclePhysics.VPAudio>(true))
                {
                    nativeAudio.enabled = true;
                    EditorUtility.SetDirty(nativeAudio);
                }

                ClearAudioClip(car.GetComponent<CarInteractable>(), "startSound");
                ClearAudioClip(car.GetComponent<CarDriver>(), "accelerationSound", "engineLoopSound", "engineStartSound");
            }

            GameFlow flow = FindSceneComponent<GameFlow>(scene);
            if (flow != null) SetValue(flow, "generatorFuelSeconds", 60f);

            DayTimer dayTimer = FindSceneComponent<DayTimer>(scene);
            if (dayTimer != null)
            {
                SetValue(dayTimer, "duration", 180f);
                SetValue(dayTimer, "enviroStartHour", 17.25f);
                SetValue(dayTimer, "enviroNightHour", 21.5f);
                SetValue(dayTimer, "controlLegacyLighting", false);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void RefreshGroundMaterials(Scene scene)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid() || !scene.isLoaded) return;
            GameObject station = FindSceneObject(scene, "EstacionDeServicio");
            Material dirtMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Ground.mat");
            Material grassMaterial = GetOrCreateGroundGrassMaterial();
            Material parkingMaterial = GetOrCreateParkingMaterial();
            if (station != null && dirtMaterial != null)
            {
                foreach (MeshRenderer renderer in station.GetComponentsInChildren<MeshRenderer>(true))
                {
                    string objectName = renderer.gameObject.name.ToLowerInvariant();
                    bool parking = objectName.Contains("playaestacionamiento");
                    bool dirtGround = objectName.Contains("terreno") || objectName.Contains("tierra") || objectName.Contains("suelo");
                    bool road = objectName.Contains("camino") || objectName.Contains("ruta");
                    Material desired = parking ? parkingMaterial : dirtGround && objectName != "terreno" && !road ? dirtMaterial : null;
                    if (desired == null || renderer.sharedMaterial == desired) continue;
                    renderer.sharedMaterial = desired;
                    EditorUtility.SetDirty(renderer);
                }
            }

            if (station != null && grassMaterial != null)
            {
                foreach (MeshRenderer renderer in station.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (renderer.gameObject.name.ToLowerInvariant() != "terreno" || renderer.sharedMaterial == grassMaterial) continue;
                    renderer.sharedMaterial = grassMaterial;
                    EditorUtility.SetDirty(renderer);
                }
            }
        }

        private static Material GetOrCreateGroundGrassMaterial()
        {
            const string materialPath = "Assets/Materials/EnviroGrass_Generated.mat";
            const string texturePath = "Assets/Enviro 3 - Sky and Weather/Sample/Textures/Grass004_1K_Color.png";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) return null;
                material = new Material(shader) { name = "EnviroGrass_Generated" };
                AssetDatabase.CreateAsset(material, materialPath);
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture != null)
            {
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                material.mainTextureScale = new Vector2(80f, 80f);
            }
            Color naturalTint = new Color(0.68f, 0.75f, 0.62f, 1f);
            if (material.HasProperty("_Color")) material.SetColor("_Color", naturalTint);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", naturalTint);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetOrCreateParkingMaterial()
        {
            const string materialPath = "Assets/Materials/Mat_PlayaEstacionamiento_GrisClaro.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) return null;
                material = new Material(shader) { name = "Mat_PlayaEstacionamiento_GrisClaro" };
                AssetDatabase.CreateAsset(material, materialPath);
            }

            Color lightGray = new Color(0.68f, 0.68f, 0.68f, 1f);
            if (material.HasProperty("_Color")) material.SetColor("_Color", lightGray);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", lightGray);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ClearAudioClip(Object target, params string[] fields)
        {
            if (target == null) return;
            SerializedObject serialized = new SerializedObject(target);
            foreach (string field in fields)
            {
                SerializedProperty property = serialized.FindProperty(field);
                if (property != null) property.objectReferenceValue = null;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static GameObject FindSceneObject(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform candidate in transforms)
                    if (candidate.name == objectName) return candidate.gameObject;
            }
            return null;
        }

        private static T FindSceneComponent<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            return null;
        }

        private static Transform BuildControl(Transform parent, string name, Vector3 position, bool toggle)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, true);
            root.transform.position = position;

            GameObject housing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            housing.name = "Gabinete";
            housing.transform.SetParent(root.transform, false);
            housing.transform.localScale = toggle ? new Vector3(0.34f, 0.48f, 0.12f) : new Vector3(0.58f, 0.72f, 0.24f);
            ApplyMaterial(housing.GetComponent<Renderer>(), new Color(0.12f, 0.15f, 0.17f), 0.3f);

            if (toggle)
            {
                GameObject lever = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                lever.name = "Palanca";
                lever.transform.SetParent(root.transform, false);
                lever.transform.localPosition = new Vector3(0f, 0f, -0.11f);
                lever.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
                lever.transform.localScale = new Vector3(0.09f, 0.22f, 0.09f);
                Object.DestroyImmediate(lever.GetComponent<Collider>());
                ApplyMaterial(lever.GetComponent<Renderer>(), new Color(0.78f, 0.22f, 0.08f), 0.15f);
            }
            else
            {
                GameObject status = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                status.name = "LuzEstado";
                status.transform.SetParent(root.transform, false);
                status.transform.localPosition = new Vector3(0.18f, 0.23f, -0.14f);
                status.transform.localScale = Vector3.one * 0.09f;
                Object.DestroyImmediate(status.GetComponent<Collider>());
                ApplyMaterial(status.GetComponent<Renderer>(), new Color(0.82f, 0.16f, 0.04f), 0.25f);
            }

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = toggle ? new Vector3(0.48f, 0.6f, 0.45f) : new Vector3(0.8f, 0.9f, 0.45f);
            TextMesh label = new GameObject("Rotulo").AddComponent<TextMesh>();
            label.transform.SetParent(root.transform, false);
            label.transform.localPosition = new Vector3(0f, toggle ? 0.34f : 0.47f, -0.14f);
            label.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            label.characterSize = 0.07f;
            label.fontSize = 80;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.white;
            label.text = toggle ? "PORTON" : "ENERGIA";
            return root.transform;
        }

        private static StationPowerCableVisual BuildPowerBoxVisuals(Transform root, Transform generator)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);

            BoxCollider collider = root.GetComponent<BoxCollider>();
            if (collider == null) collider = root.gameObject.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = new Vector3(1.15f, 1.45f, 0.65f);
            collider.enabled = true;

            Material cabinet = CreateMaterial("Assets/Materials/PowerBox_Cabinet.mat", new Color(0.19f, 0.23f, 0.25f), 0.55f);
            Material door = CreateMaterial("Assets/Materials/PowerBox_Door.mat", new Color(0.68f, 0.71f, 0.69f), 0.35f);
            Material dark = CreateMaterial("Assets/Materials/PowerBox_Details.mat", new Color(0.075f, 0.09f, 0.095f), 0.2f);
            Material danger = CreateMaterial("Assets/Materials/PowerBox_Warning.mat", new Color(0.9f, 0.16f, 0.045f), 0.15f);
            Material cableMaterial = CreateMaterial("Assets/Materials/PowerCable_Black.mat", new Color(0.025f, 0.03f, 0.033f), 0f);

            CreatePart("Gabinete", PrimitiveType.Cube, root, Vector3.zero, new Vector3(0.92f, 1.24f, 0.32f), cabinet);
            CreatePart("TableroFrontal", PrimitiveType.Cube, root, new Vector3(0f, 0f, -0.19f), new Vector3(0.78f, 1.08f, 0.075f), door);
            CreatePart("MarcoSuperior", PrimitiveType.Cube, root, new Vector3(0f, 0.49f, -0.238f), new Vector3(0.77f, 0.11f, 0.025f), dark);
            CreatePart("IndicadorTension", PrimitiveType.Cube, root, new Vector3(0f, 0.49f, -0.255f), new Vector3(0.24f, 0.035f, 0.015f), danger);
            CreatePart("InterruptorGeneral", PrimitiveType.Cube, root, new Vector3(-0.18f, 0.13f, -0.25f), new Vector3(0.16f, 0.25f, 0.075f), dark);
            CreatePart("LlaveTermica", PrimitiveType.Capsule, root, new Vector3(-0.18f, 0.14f, -0.305f), new Vector3(0.075f, 0.16f, 0.055f), danger);
            CreatePart("TomaIndustrial", PrimitiveType.Cylinder, root, new Vector3(0.17f, -0.24f, -0.255f), new Vector3(0.25f, 0.07f, 0.25f), dark, Quaternion.Euler(90f, 0f, 0f));
            CreatePart("LuzEstado", PrimitiveType.Sphere, root, new Vector3(0.24f, 0.42f, -0.285f), Vector3.one * 0.085f, danger);
            CreateText("RótuloCaja", root, "TABLERO\nELECTRICO", new Vector3(0f, 0.28f, -0.265f), 0.055f, Color.white);
            CreateText("RótuloToma", root, "220 V", new Vector3(0.17f, -0.39f, -0.28f), 0.045f, Color.white);

            Transform socket = new GameObject("EntradaCable").transform;
            socket.SetParent(root, false);
            socket.localPosition = new Vector3(0.17f, -0.24f, -0.37f);
            socket.localRotation = Quaternion.Euler(0f, 180f, 0f);

            Transform outlet = generator.Find("SalidaCableGenerador");
            if (outlet == null)
            {
                outlet = new GameObject("SalidaCableGenerador").transform;
                outlet.SetParent(generator, false);
                outlet.localPosition = Vector3.up * 0.65f;
            }
            GameObject cableObject = new GameObject("CableGeneradorEstacion");
            cableObject.transform.SetParent(root, false);
            LineRenderer cable = cableObject.AddComponent<LineRenderer>();
            cable.useWorldSpace = true;
            cable.positionCount = 4;
            cable.startWidth = cable.endWidth = 0.045f;
            cable.numCapVertices = 4;
            cable.material = cableMaterial;
            cable.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            Transform plug = CreatePart("ConectorCable", PrimitiveType.Cylinder, root, socket.localPosition + Vector3.forward * 0.8f, new Vector3(0.11f, 0.19f, 0.11f), danger, Quaternion.Euler(90f, 0f, 0f)).transform;
            Light lamp = new GameObject("LuzTablero").AddComponent<Light>();
            lamp.transform.SetParent(root, false);
            lamp.transform.localPosition = new Vector3(0.24f, 0.42f, -0.34f);
            lamp.type = LightType.Point;
            lamp.color = Color.green;
            lamp.intensity = 0.65f;
            lamp.range = 1.5f;
            lamp.enabled = false;

            StationPowerCableVisual visual = root.GetComponent<StationPowerCableVisual>();
            if (visual == null) visual = root.gameObject.AddComponent<StationPowerCableVisual>();
            Set(visual, "generatorOutlet", outlet);
            Set(visual, "boxSocket", socket);
            Set(visual, "cable", cable);
            Set(visual, "plugIndicator", plug.GetComponent<Renderer>());
            Set(visual, "statusLamp", lamp);
            EditorUtility.SetDirty(root.gameObject);
            return visual;
        }

        private static GameObject CreatePart(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, Quaternion? rotation = null)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            if (rotation.HasValue) part.transform.localRotation = rotation.Value;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            return part;
        }

        private static void CreateText(string name, Transform parent, string text, Vector3 position, float size, Color color)
        {
            TextMesh label = new GameObject(name).AddComponent<TextMesh>();
            label.transform.SetParent(parent, false);
            label.transform.localPosition = position;
            label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            label.characterSize = size;
            label.fontSize = 72;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = color;
            label.text = text;
        }

        private static Material CreateMaterial(string path, Color color, float metallic)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.35f);
            if (path.EndsWith("PowerBox_Door.mat") || path.EndsWith("PowerBox_Cabinet.mat"))
            {
                Texture2D enamel = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Materials/PowerBox_Enamel_Albedo.png");
                if (enamel != null)
                {
                    if (material.HasProperty("_BaseMap"))
                    {
                        material.SetTexture("_BaseMap", enamel);
                        material.SetTextureScale("_BaseMap", Vector2.one * 2f);
                    }
                    if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", enamel);
                }
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static bool PowerBoxHasTexture()
        {
            Material door = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PowerBox_Door.mat");
            return door != null && ((door.HasProperty("_BaseMap") && door.GetTexture("_BaseMap") != null) ||
                                    (door.HasProperty("_MainTex") && door.GetTexture("_MainTex") != null));
        }

        private static void ApplyMaterial(Renderer renderer, Color color, float metallic)
        {
            if (renderer == null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.32f);
            renderer.sharedMaterial = material;
        }

        private static void Set(Object target, string fieldName, Object value)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field == null) return;
            field.SetValue(target, value);
            EditorUtility.SetDirty(target);
        }

        private static void SetValue(Object target, string fieldName, object value)
        {
            if (target == null) return;
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null) return;
            if (value is float number) property.floatValue = number;
            else if (value is bool boolean) property.boolValue = boolean;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
