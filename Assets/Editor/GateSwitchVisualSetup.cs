using EnElCamino;
using UnityEditor;
using UnityEngine;

namespace EnElCamino.Editor
{
    public static class GateSwitchVisualSetup
    {
        private const string ModelPath = "Assets/ThirdParty/ExternalModels/LightSwitch_JarlanPerez_CC_BY.glb";
        private const string VisualName = "TeclaPared_JarlanPerez_Texturada";

        public static Transform Build(Transform control, GarageDoorInteractable door, GeneratorInteractable generator)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (source == null)
            {
                Debug.LogError("No se importó el modelo 3D de la tecla: " + ModelPath);
                return null;
            }
            if (!AssetDatabase.IsValidFolder("Assets/Models")) AssetDatabase.CreateFolder("Assets", "Models");
            foreach (string name in new[] { "Gabinete", "Palanca", "Rotulo", VisualName })
            {
                Transform old = control.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            PlaceInsideWorkshop(control, door, generator);
            Transform visual = new GameObject(VisualName).transform;
            visual.SetParent(control, false);
            Transform rocker = new GameObject("Tecla").transform;
            rocker.SetParent(visual, false);
            Material plastic = PlasticMaterial("WallSwitch_IvoryPlastic", Color.white);
            Material darkPlastic = PlasticMaterial("WallSwitch_RockerPlastic", new Color(0.22f, 0.23f, 0.23f));
            int partIndex = 0;
            foreach (MeshFilter filter in source.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh original = filter.sharedMesh;
                if (original == null) continue;
                Renderer sourceRenderer = filter.GetComponent<Renderer>();
                for (int submesh = 0; submesh < original.subMeshCount; submesh++)
                {
                    Material imported = sourceRenderer != null && submesh < sourceRenderer.sharedMaterials.Length ? sourceRenderer.sharedMaterials[submesh] : null;
                    bool dark = imported != null && imported.name.StartsWith("mat22");
                    int[] triangles = original.GetTriangles(submesh);
                    if (dark)
                    {
                        CreateMeshPart(original, triangles, visual, darkPlastic, "MarcoInterior", partIndex++);
                        continue;
                    }
                    var plateTriangles = new System.Collections.Generic.List<int>();
                    var rockerTriangles = new System.Collections.Generic.List<int>();
                    Vector3[] vertices = original.vertices;
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        bool central = true;
                        for (int corner = 0; corner < 3; corner++)
                        {
                            Vector3 vertex = vertices[triangles[i + corner]];
                            central &= Mathf.Abs(vertex.x) < 0.105f && Mathf.Abs(vertex.y + 0.0043f) < 0.18f;
                        }
                        var selected = central ? rockerTriangles : plateTriangles;
                        selected.Add(triangles[i]);
                        selected.Add(triangles[i + 1]);
                        selected.Add(triangles[i + 2]);
                    }
                    CreateMeshPart(original, plateTriangles.ToArray(), visual, plastic, "Placa_Modelo3D", partIndex++);
                    CreateMeshPart(original, rockerTriangles.ToArray(), rocker, plastic, "Tecla_Modelo3D", partIndex++);
                }
            }
            rocker.localRotation = Quaternion.Euler(12f, 0f, 0f);
            Material metal = PlasticMaterial("WallSwitch_Screws", new Color(0.45f, 0.46f, 0.45f));
            if (metal.HasProperty("_Metallic")) metal.SetFloat("_Metallic", 0.65f);
            for (int i = 0; i < 2; i++)
            {
                GameObject screw = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                screw.name = "Tornillo_" + i;
                screw.transform.SetParent(visual, false);
                screw.transform.localPosition = new Vector3(0f, i == 0 ? 0.199f : -0.208f, -0.039f);
                screw.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                screw.transform.localScale = new Vector3(0.018f, 0.002f, 0.018f);
                screw.GetComponent<Renderer>().sharedMaterial = metal;
                Object.DestroyImmediate(screw.GetComponent<Collider>());
                GameObject slot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slot.name = "RanuraTornillo_" + i;
                slot.transform.SetParent(visual, false);
                slot.transform.localPosition = screw.transform.localPosition - Vector3.forward * 0.0025f;
                slot.transform.localScale = new Vector3(0.012f, 0.002f, 0.001f);
                slot.GetComponent<Renderer>().sharedMaterial = darkPlastic;
                Object.DestroyImmediate(slot.GetComponent<Collider>());
            }
            Label(visual, "Rotulo", "PORTÓN", new Vector3(0f, 0.27f, -0.045f), 0.0035f, Color.white, Quaternion.identity);
            Label(rocker, "Encendido", "I", new Vector3(0f, 0.086f, -0.041f), 0.0043f, Color.white, Quaternion.identity);
            Label(rocker, "Apagado", "O", new Vector3(0f, -0.084f, -0.041f), 0.0038f, Color.white, Quaternion.identity);
            BoxCollider hitbox = control.GetComponent<BoxCollider>();
            if (hitbox == null) hitbox = control.gameObject.AddComponent<BoxCollider>();
            hitbox.center = Vector3.zero;
            hitbox.size = new Vector3(0.36f, 0.54f, 0.16f);
            EditorUtility.SetDirty(hitbox);
            AssetDatabase.SaveAssets();
            return rocker;
        }

        private static void CreateMeshPart(Mesh original, int[] triangles, Transform parent, Material material, string name, int index)
        {
            if (triangles.Length == 0) return;
            string path = "Assets/Models/WallSwitch_Part_" + index + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool create = mesh == null;
            if (create) mesh = new Mesh();
            mesh.Clear();
            mesh.name = name;
            Vector3[] vertices = original.vertices;
            Vector2[] uv = new Vector2[vertices.Length];
            Bounds bounds = original.bounds;
            for (int i = 0; i < vertices.Length; i++)
                uv[i] = new Vector2((vertices[i].x - bounds.min.x) / Mathf.Max(0.001f, bounds.size.x),
                                    (vertices[i].y - bounds.min.y) / Mathf.Max(0.001f, bounds.size.y));
            mesh.vertices = vertices;
            mesh.normals = original.normals;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            if (create) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);
            GameObject part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Material PlasticMaterial(string name, Color tint)
        {
            string path = "Assets/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            Texture texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Materials/LightSwitch_AgedPlastic_Albedo.png");
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.12f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.12f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void PlaceInsideWorkshop(Transform control, GarageDoorInteractable door, GeneratorInteractable generator)
        {
            if (door == null) return;
            Renderer renderer = door.DoorObject.GetComponentInChildren<Renderer>(true);
            Collider collider = door.DoorObject.GetComponentInChildren<Collider>(true);
            Bounds bounds = renderer != null ? renderer.bounds : collider != null ? collider.bounds : new Bounds(door.transform.position, new Vector3(3f, 3f, 0.3f));
            Vector3 interior = generator != null ? generator.transform.position - bounds.center : Vector3.forward;
            bool normalIsZ = bounds.size.z < bounds.size.x;
            Vector3 normal = normalIsZ ? Vector3.forward * (interior.z >= 0f ? 1f : -1f) : Vector3.right * (interior.x >= 0f ? 1f : -1f);
            Vector3 position = bounds.center;
            position.y = bounds.min.y + 1.25f;
            if (normalIsZ) position.x = bounds.max.x + 0.23f;
            else position.z = bounds.max.z + 0.23f;
            position += normal * ((normalIsZ ? bounds.extents.z : bounds.extents.x) + 0.09f);
            control.SetPositionAndRotation(position, Quaternion.LookRotation(-normal, Vector3.up));
            control.localScale = Vector3.one;
            Debug.Log("[GateSetup] Tecla interior a altura " + position.y + "; portón=" + bounds + "; posición=" + position);
        }

        private static void Label(Transform parent, string name, string text, Vector3 position, float size, Color color, Quaternion rotation)
        {
            TextMesh label = new GameObject(name).AddComponent<TextMesh>();
            label.transform.SetParent(parent, false);
            label.transform.localPosition = position;
            label.transform.localRotation = rotation;
            label.text = text;
            label.fontSize = 80;
            label.characterSize = size;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = color;
        }
    }
}
