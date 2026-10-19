#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EnElCamino.Editor
{
    public static class Parcial2VisualAssetSetup
    {
        private const string ModelRoot = "Assets/ThirdParty/ExternalModels/";

        [MenuItem("Parcial 2/Aplicar modelos y escalera animada")]
        public static void Apply()
        {
            SetupGeneratorVisual();
            SetupFuelCanVisual();
            SetupGhostThreat();
            SetupLadderFlow();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Parcial2VisualAssetSetup: modelos 3D, fantasma y escalera animada configurados.");
        }

        private static void SetupGeneratorVisual()
        {
            GameObject generator = GameObject.Find("GeneradorAuxiliar");
            if (generator == null) return;

            MeshRenderer legacyRenderer = generator.GetComponent<MeshRenderer>();
            if (legacyRenderer != null) legacyRenderer.enabled = false;

            ReplaceVisual(generator.transform, "Generated_PortableGenerator_CC0", ModelRoot + "PortableGenerator_CC0.fbx", new Vector3(1.30f, 1.00f, 1.00f));
        }

        private static void SetupFuelCanVisual()
        {
            GameObject fuelCan = GameObject.Find("BidonCombustible");
            if (fuelCan == null) return;

            MeshRenderer legacyRenderer = fuelCan.GetComponent<MeshRenderer>();
            if (legacyRenderer != null) legacyRenderer.enabled = false;

            ReplaceVisual(fuelCan.transform, "Generated_FuelCanister_CC0", ModelRoot + "FuelCanister_CC0.glb", new Vector3(0.55f, 0.85f, 0.40f));
        }

        private static void SetupGhostThreat()
        {
            VerticalSliceDirector director = Object.FindFirstObjectByType<VerticalSliceDirector>();
            if (director == null) return;
            GameObject ghost = AssetDatabase.LoadAssetAtPath<GameObject>(ModelRoot + "GhostGltf/lowpolyghost/lowpolyghost.glb");
            SetReference(director, "threatPrefab", ghost);
        }

        private static void SetupLadderFlow()
        {
            GameFlow flow = Object.FindFirstObjectByType<GameFlow>();
            GameObject playerObject = GameObject.Find("Jugador");
            GameObject exteriorObject = GameObject.Find("EscaleraExterior");
            GameObject interiorObject = GameObject.Find("EscaleraInterior");
            GameObject holeObject = GameObject.Find("ProteccionHueco");
            if (flow == null || playerObject == null || exteriorObject == null || holeObject == null) return;

            Transform roofPoint = EnsurePoint("PuntoEscaleraTecho", new Vector3(16.55f, 6.85f, 8.05f), new Vector3(0f, 0f, 344.4f), Vector3.one);
            Transform placementPoint = EnsurePoint("PuntoColocacionEscalera", new Vector3(19f, 3.65f, 8f), new Vector3(23.1f, 0f, 0f), new Vector3(1f, 1.32f, 1f));
            Transform topDestination = GameObject.Find("PuntoArribaTaller")?.transform;
            Transform lowerDestination = GameObject.Find("PuntoPisoTaller")?.transform;

            LadderInteractable exterior = exteriorObject.GetComponent<LadderInteractable>();
            SetReference(exterior, "gameFlow", flow);
            SetReference(exterior, "player", playerObject.transform);
            SetReference(exterior, "destination", topDestination);
            SetReference(exterior, "roofLadderPoint", roofPoint);
            SetReference(exterior, "lowerDestination", lowerDestination);
            SetBool(exterior, "portableExterior", true);
            SetString(exterior, "promptText", "[E] Subir por la escalera exterior");
            SetFloat(exterior, "climbDuration", 1.8f);
            SetFloat(exterior, "descendDuration", 1.25f);

            LadderPlacementInteractable placement = holeObject.GetComponent<LadderPlacementInteractable>();
            if (placement == null) placement = holeObject.AddComponent<LadderPlacementInteractable>();
            SetReference(placement, "gameFlow", flow);
            SetReference(placement, "exteriorLadder", exterior);

            BoxCollider holeBlocker = holeObject.GetComponent<BoxCollider>();
            if (holeBlocker != null) holeBlocker.isTrigger = true;

            GameObject fallObject = EnsureChild("HoyoTechoCaida", holeObject.transform.parent);
            fallObject.transform.position = new Vector3(19f, 4.05f, 8f);
            fallObject.transform.rotation = Quaternion.identity;
            fallObject.transform.localScale = Vector3.one;
            BoxCollider fallCollider = fallObject.GetComponent<BoxCollider>();
            if (fallCollider == null) fallCollider = fallObject.AddComponent<BoxCollider>();
            fallCollider.isTrigger = true;
            fallCollider.size = new Vector3(1.35f, 3.2f, 1.35f);
            RoofHoleFallTrigger fallTrigger = fallObject.GetComponent<RoofHoleFallTrigger>();
            if (fallTrigger == null) fallTrigger = fallObject.AddComponent<RoofHoleFallTrigger>();
            SetReference(fallTrigger, "gameFlow", flow);

            if (interiorObject != null) interiorObject.SetActive(false);

            RaycastInteractor interactor = playerObject.GetComponent<RaycastInteractor>();
            SetReference(interactor, "ladderPlacement", placement);
        }

        private static GameObject ReplaceVisual(Transform parent, string visualName, string assetPath, Vector3 desiredWorldSize)
        {
            Transform old = parent.Find(visualName);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) return null;
            GameObject visual = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            visual.name = visualName;
            visual.transform.SetParent(parent, false);

            Bounds sourceBounds = GetBounds(visual);
            Vector3 parentScale = parent.lossyScale;
            visual.transform.localScale = new Vector3(
                desiredWorldSize.x / Mathf.Max(0.01f, sourceBounds.size.x * parentScale.x),
                desiredWorldSize.y / Mathf.Max(0.01f, sourceBounds.size.y * parentScale.y),
                desiredWorldSize.z / Mathf.Max(0.01f, sourceBounds.size.z * parentScale.z));
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            Bounds worldBounds = GetBounds(visual);
            Collider parentCollider = parent.GetComponent<Collider>();
            float groundY = parentCollider != null ? parentCollider.bounds.min.y : parent.position.y;
            visual.transform.position += Vector3.up * (groundY - worldBounds.min.y);
            return visual;
        }

        private static Bounds GetBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static Transform EnsurePoint(string name, Vector3 position, Vector3 euler, Vector3 scale)
        {
            GameObject point = GameObject.Find(name);
            if (point == null)
            {
                point = new GameObject(name);
                SceneManager.MoveGameObjectToScene(point, SceneManager.GetActiveScene());
            }
            point.transform.position = position;
            point.transform.eulerAngles = euler;
            point.transform.localScale = scale;
            return point.transform;
        }

        private static GameObject EnsureChild(string name, Transform parent)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.gameObject;
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void SetReference(Object target, string propertyName, Object value)
        {
            if (target == null) return;
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) return;
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string propertyName, bool value) => SetValue(target, propertyName, p => p.boolValue = value);
        private static void SetFloat(Object target, string propertyName, float value) => SetValue(target, propertyName, p => p.floatValue = value);
        private static void SetString(Object target, string propertyName, string value) => SetValue(target, propertyName, p => p.stringValue = value);

        private static void SetValue(Object target, string propertyName, System.Action<SerializedProperty> apply)
        {
            if (target == null) return;
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) return;
            apply(property);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
