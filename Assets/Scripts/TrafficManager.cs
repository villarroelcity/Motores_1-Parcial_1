using UnityEngine;
using System.Collections.Generic;

namespace EnElCamino
{
    /// <summary>
    /// Genera vehiculos visuales de Vehicle Physics Pro en ambos sentidos.
    /// No usa sus controles ni sus fisicas: solo los mueve recto a velocidad constante.
    /// </summary>
    public class TrafficManager : MonoBehaviour
    {
        [SerializeField] private GameObject trafficSourceVehicle;
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private Transform road;
        [SerializeField] private Transform spawnReference;
        [SerializeField] private int trafficVehiclesPerDirection = 8;
        [SerializeField] private float speedKmh = 60f;
        [SerializeField] private float laneOffset = 1.5f;
        [SerializeField] private float trafficSpacing = 90f;
        [SerializeField] private float firstSpawnDistance = 160f;
        [SerializeField] private float routePadding = 40f;

        private const float MetersPerKilometer = 1000f;
        private const float SecondsPerHour = 3600f;
        private const int MinimumTrafficVehiclesPerDirection = 8;
        private float minZ;
        private float maxZ;
        private float height;
        private float centerX;
        private float startZ;
        private float speedMetersPerSecond;
        private int nextVehicleIndex;
        private bool trafficSpawned;
        private CarDriver playerCar;
        private readonly List<TrafficVehicle> vehicles = new List<TrafficVehicle>();

        private void Start()
        {
            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();

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
            minZ = roadBounds.min.z + routePadding;
            maxZ = roadBounds.max.z - routePadding;
            if (minZ >= maxZ)
            {
                minZ = road.position.z - 5000f;
                maxZ = road.position.z + 5000f;
            }

            speedMetersPerSecond = speedKmh * MetersPerKilometer / SecondsPerHour;
            centerX = roadBounds.center.x;
            startZ = Mathf.Clamp(spawnReference.position.z + firstSpawnDistance, minZ, maxZ);
            height = roadBounds.max.y;
            playerCar = FindFirstObjectByType<CarDriver>();

            if (gameFlow != null)
            {
                gameFlow.DrivingStarted += SpawnDrivingTraffic;
                if (gameFlow.driving) SpawnDrivingTraffic();
            }
        }

        private void OnDestroy()
        {
            if (gameFlow != null) gameFlow.DrivingStarted -= SpawnDrivingTraffic;
        }

        private void SpawnDrivingTraffic()
        {
            if (trafficSpawned) return;
            trafficSpawned = true;
            if (playerCar != null)
                startZ = Mathf.Clamp(playerCar.transform.position.z + firstSpawnDistance, minZ, maxZ);

            int vehicles = Mathf.Max(trafficVehiclesPerDirection, MinimumTrafficVehiclesPerDirection);
            SpawnTraffic(vehicles, trafficSpacing, 0f);
        }

        private void Update()
        {
            if (!trafficSpawned || playerCar == null || gameFlow == null || gameFlow.gameOver || gameFlow.finished) return;
            float playerZ = playerCar.transform.position.z;
            for (int i = 0; i < vehicles.Count; i++)
            {
                TrafficVehicle vehicle = vehicles[i];
                if (vehicle == null || !vehicle.CanRecycle) continue;
                float gap = vehicle.transform.position.z - playerZ;
                // Reciclar fuera de la vista mantiene trafico de frente durante toda la ruta.
                if (gap < -180f || gap > 1100f)
                    vehicle.Recycle(WrapZ(playerZ + 700f + i * 32f, minZ, maxZ));
            }
        }

        private void SpawnTraffic(int vehiclesPerDirectionToSpawn, float vehicleSpacing, float startOffset)
        {
            for (int i = 0; i < vehiclesPerDirectionToSpawn; i++)
            {
                float z = startZ + startOffset + i * vehicleSpacing;
                SpawnVehicle(nextVehicleIndex++, centerX + laneOffset, z, Vector3.forward,
                    speedMetersPerSecond, minZ, maxZ, height);
                SpawnVehicle(nextVehicleIndex++, centerX - laneOffset, z, -Vector3.forward,
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

            AddTrafficCollision(vehicle);
            // Las mallas de VPP tienen pivotes distintos: apoyar las ruedas, no el pivote.
            Bounds bounds = GetLocalBounds(vehicle);
            Vector3 groundedPosition = vehicle.transform.position;
            groundedPosition.y = height - bounds.min.y + 0.015f;
            vehicle.transform.position = groundedPosition;
            TrafficVehicle trafficVehicle = vehicle.AddComponent<TrafficVehicle>();
            trafficVehicle.Configure(direction, speedMetersPerSecond, minZ, maxZ, groundedPosition.y, FindWheels(vehicle));
            vehicles.Add(trafficVehicle);
        }

        private void AddTrafficCollision(GameObject vehicle)
        {
            BoxCollider boxCollider = vehicle.AddComponent<BoxCollider>();
            Bounds bounds = GetLocalBounds(vehicle);
            boxCollider.center = bounds.center;
            boxCollider.size = bounds.size;
            PhysicsMaterial rollingSurface = new PhysicsMaterial("RodaduraTrafico");
            rollingSurface.dynamicFriction = 0.02f;
            rollingSurface.staticFriction = 0.02f;
            rollingSurface.frictionCombine = PhysicsMaterialCombine.Minimum;
            boxCollider.sharedMaterial = rollingSurface;

            Rigidbody rigidbody = vehicle.AddComponent<Rigidbody>();
            rigidbody.mass = 1400f;
            rigidbody.useGravity = true;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private Bounds GetLocalBounds(GameObject vehicle)
        {
            Renderer[] renderers = vehicle.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);

            bool hasBounds = false;
            Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                Bounds worldBounds = renderer.bounds;
                Vector3[] corners =
                {
                    new Vector3(worldBounds.min.x, worldBounds.min.y, worldBounds.min.z),
                    new Vector3(worldBounds.min.x, worldBounds.min.y, worldBounds.max.z),
                    new Vector3(worldBounds.min.x, worldBounds.max.y, worldBounds.min.z),
                    new Vector3(worldBounds.min.x, worldBounds.max.y, worldBounds.max.z),
                    new Vector3(worldBounds.max.x, worldBounds.min.y, worldBounds.min.z),
                    new Vector3(worldBounds.max.x, worldBounds.min.y, worldBounds.max.z),
                    new Vector3(worldBounds.max.x, worldBounds.max.y, worldBounds.min.z),
                    new Vector3(worldBounds.max.x, worldBounds.max.y, worldBounds.max.z)
                };

                foreach (Vector3 corner in corners)
                {
                    Vector3 localCorner = vehicle.transform.InverseTransformPoint(corner);
                    if (!hasBounds)
                    {
                        localBounds = new Bounds(localCorner, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(localCorner);
                    }
                }
            }

            return hasBounds ? localBounds : new Bounds(Vector3.zero, Vector3.one);
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
