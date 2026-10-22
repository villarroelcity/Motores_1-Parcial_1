using UnityEngine;

namespace EnElCamino
{
    /// <summary>
    /// Genera vehiculos visuales de Vehicle Physics Pro en ambos sentidos.
    /// No usa sus controles ni sus fisicas: solo los mueve recto a velocidad constante.
    /// </summary>
    public class TrafficManager : MonoBehaviour
    {
        [SerializeField] private GameObject trafficSourceVehicle;
        [SerializeField] private Transform road;
        [SerializeField] private Transform spawnReference;
        [SerializeField] private int vehiclesPerDirection = 4;
        [SerializeField] private float speedKmh = 60f;
        [SerializeField] private float laneOffset = 1.5f;
        [SerializeField] private float spacing = 120f;
        [SerializeField] private float firstSpawnDistance = 160f;
        [SerializeField] private float routePadding = 40f;
        [SerializeField] private float vehicleHeight = 0.55f;

        private const float MetersPerKilometer = 1000f;
        private const float SecondsPerHour = 3600f;

        private void Start()
        {
            if (road == null)
            {
                GameObject roadObject = GameObject.Find("Ruta");
                if (roadObject != null) road = roadObject.transform;
            }

            if (spawnReference == null)
            {
                GameObject referenceObject = GameObject.Find("AutoEntradaRuta");
                if (referenceObject != null) spawnReference = referenceObject.transform;
            }

            if (trafficSourceVehicle == null)
            {
                trafficSourceVehicle = GameObject.Find("VehiculoConRuedas");
                if (trafficSourceVehicle == null) trafficSourceVehicle = GameObject.Find("VehiculoJugador");
                if (trafficSourceVehicle == null) trafficSourceVehicle = GameObject.Find("PatrulleroIA");
            }

            if (road == null || trafficSourceVehicle == null)
            {
                Debug.LogWarning("TrafficManager necesita la ruta y un vehiculo VPP en la escena.", this);
                return;
            }

            if (spawnReference == null) spawnReference = road;

            Bounds roadBounds = GetRoadBounds();
            float minZ = roadBounds.min.z + routePadding;
            float maxZ = roadBounds.max.z - routePadding;
            if (minZ >= maxZ)
            {
                minZ = road.position.z - 5000f;
                maxZ = road.position.z + 5000f;
            }

            float speedMetersPerSecond = speedKmh * MetersPerKilometer / SecondsPerHour;
            float centerX = road.position.x;
            float startZ = Mathf.Clamp(spawnReference.position.z + firstSpawnDistance, minZ, maxZ);
            float height = road.position.y + vehicleHeight;

            for (int i = 0; i < vehiclesPerDirection; i++)
            {
                SpawnVehicle(i, centerX + laneOffset, startZ + i * spacing, Vector3.forward,
                    speedMetersPerSecond, minZ, maxZ, height);
                SpawnVehicle(i + vehiclesPerDirection, centerX - laneOffset, startZ + i * spacing, -Vector3.forward,
                    speedMetersPerSecond, minZ, maxZ, height);
            }
        }

        private Bounds GetRoadBounds()
        {
            Renderer roadRenderer = road.GetComponent<Renderer>();
            if (roadRenderer != null) return roadRenderer.bounds;
            return new Bounds(road.position, new Vector3(10f, 1f, 10000f));
        }

        private void SpawnVehicle(int index, float x, float z, Vector3 direction,
            float speedMetersPerSecond, float minZ, float maxZ, float height)
        {
            Vector3 position = new Vector3(x, height, WrapZ(z, minZ, maxZ));
            GameObject vehicle = CreateVisualVehicle(position,
                Quaternion.LookRotation(direction, Vector3.up), index);

            if (vehicle == null)
            {
                Debug.LogError("El vehiculo VPP no tiene mallas visibles para el trafico.", this);
                return;
            }

            foreach (Behaviour behaviour in vehicle.GetComponentsInChildren<Behaviour>(true))
            {
                behaviour.enabled = false;
            }

            TrafficVehicle trafficVehicle = vehicle.AddComponent<TrafficVehicle>();
            trafficVehicle.Configure(direction, speedMetersPerSecond, minZ, maxZ, height, FindWheels(vehicle));
        }

        private GameObject CreateVisualVehicle(Vector3 position, Quaternion rotation, int index)
        {
            GameObject vehicle = new GameObject("TraficoVPP_" + (index + 1).ToString("00"));
            vehicle.transform.SetParent(transform, false);
            vehicle.transform.SetPositionAndRotation(position, rotation);

            Transform sourceRoot = trafficSourceVehicle.transform;
            Vector3 sourceRootScale = sourceRoot.lossyScale;
            Renderer[] sourceRenderers = trafficSourceVehicle.GetComponentsInChildren<Renderer>(true);
            if (sourceRenderers.Length == 0)
            {
                Destroy(vehicle);
                return null;
            }

            foreach (Renderer sourceRenderer in sourceRenderers)
            {
                GameObject part = CreateRendererCopy(sourceRenderer);
                if (part == null) continue;

                part.transform.SetParent(vehicle.transform, false);
                part.transform.localPosition = sourceRoot.InverseTransformPoint(sourceRenderer.transform.position);
                part.transform.localRotation = Quaternion.Inverse(sourceRoot.rotation) * sourceRenderer.transform.rotation;
                part.transform.localScale = DivideScale(sourceRenderer.transform.lossyScale, sourceRootScale);
            }

            ApplyTrafficColor(vehicle, index);
            return vehicle;
        }

        private GameObject CreateRendererCopy(Renderer sourceRenderer)
        {
            GameObject part = new GameObject(sourceRenderer.gameObject.name);
            part.SetActive(sourceRenderer.gameObject.activeSelf);

            MeshFilter sourceMeshFilter = sourceRenderer.GetComponent<MeshFilter>();
            MeshRenderer sourceMeshRenderer = sourceRenderer as MeshRenderer;
            if (sourceMeshRenderer != null && sourceMeshFilter != null)
            {
                MeshFilter meshFilter = part.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = sourceMeshFilter.sharedMesh;
                MeshRenderer meshRenderer = part.AddComponent<MeshRenderer>();
                CopyRendererSettings(sourceMeshRenderer, meshRenderer);
                meshRenderer.sharedMaterials = sourceMeshRenderer.sharedMaterials;
                return part;
            }

            SkinnedMeshRenderer sourceSkinnedRenderer = sourceRenderer as SkinnedMeshRenderer;
            if (sourceSkinnedRenderer != null)
            {
                SkinnedMeshRenderer skinnedRenderer = part.AddComponent<SkinnedMeshRenderer>();
                CopyRendererSettings(sourceSkinnedRenderer, skinnedRenderer);
                skinnedRenderer.sharedMesh = sourceSkinnedRenderer.sharedMesh;
                skinnedRenderer.sharedMaterials = sourceSkinnedRenderer.sharedMaterials;
                skinnedRenderer.localBounds = sourceSkinnedRenderer.localBounds;
                skinnedRenderer.updateWhenOffscreen = sourceSkinnedRenderer.updateWhenOffscreen;
                return part;
            }

            Destroy(part);
            return null;
        }

        private void CopyRendererSettings(Renderer source, Renderer target)
        {
            target.enabled = source.enabled;
            target.shadowCastingMode = source.shadowCastingMode;
            target.receiveShadows = source.receiveShadows;
            target.lightProbeUsage = source.lightProbeUsage;
            target.reflectionProbeUsage = source.reflectionProbeUsage;
            target.motionVectorGenerationMode = source.motionVectorGenerationMode;
        }

        private Vector3 DivideScale(Vector3 value, Vector3 divisor)
        {
            return new Vector3(
                divisor.x == 0f ? value.x : value.x / divisor.x,
                divisor.y == 0f ? value.y : value.y / divisor.y,
                divisor.z == 0f ? value.z : value.z / divisor.z);
        }

        private void ApplyTrafficColor(GameObject vehicle, int index)
        {
            Color[] colors =
            {
                new Color(0.78f, 0.04f, 0.03f),
                new Color(0.04f, 0.16f, 0.72f),
                new Color(0.82f, 0.82f, 0.78f),
                new Color(0.85f, 0.48f, 0.02f)
            };
            Color color = colors[index % colors.Length];

            foreach (Renderer renderer in vehicle.GetComponentsInChildren<Renderer>(true))
            {
                string rendererName = renderer.name.ToLowerInvariant();
                if (rendererName.Contains("wheel") || rendererName.Contains("tire") ||
                    rendererName.Contains("glass") || rendererName.Contains("window"))
                {
                    continue;
                }

                Material material = renderer.material;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                else if (material.HasProperty("_Color")) material.color = color;
            }
        }

        private Transform[] FindWheels(GameObject vehicle)
        {
            Transform[] found = new Transform[4];
            int count = 0;
            foreach (Transform child in vehicle.GetComponentsInChildren<Transform>(true))
            {
                string childName = child.name.ToLowerInvariant();
                if (!childName.Contains("wheel")) continue;
                if (count >= found.Length) break;
                found[count++] = child;
            }
            return found;
        }

        private float WrapZ(float value, float minZ, float maxZ)
        {
            float length = maxZ - minZ;
            if (length <= 0f) return minZ;
            while (value > maxZ) value -= length;
            while (value < minZ) value += length;
            return value;
        }
    }
}
