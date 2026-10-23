using UnityEngine;
using System.Collections;

namespace EnElCamino
{
    /// <summary>
    /// Crea trafico de los prefabs incluidos con VPP cuando el jugador empieza a conducir.
    /// </summary>
    public class TrafficManager : MonoBehaviour
    {
        [SerializeField] private GameObject[] trafficVehiclePrefabs;
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private Transform road;
        [SerializeField] private Transform spawnReference;
        [SerializeField] private int trafficVehiclesPerDirection = 24;
        [SerializeField] private int initialTrafficPerDirection = 5;
        [SerializeField] private float speedKmh = 60f;
        [SerializeField] private float laneOffset = 1.5f;
        [SerializeField] private float trafficSpacing = 100f;
        [SerializeField] private float firstSpawnDistance = 420f;
        [SerializeField] private float oncomingStartDistance = 650f;
        [SerializeField] private float trafficSpawnDistance = 700f;
        [SerializeField] private float spawnIntervalMin = 4f;
        [SerializeField] private float spawnIntervalMax = 7f;
        [SerializeField] private float routePadding = 40f;

        private const float MetersPerKilometer = 1000f;
        private const float SecondsPerHour = 3600f;
        private float minZ;
        private float maxZ;
        private float roadSurfaceY;
        private float centerX;
        private float startZ;
        private float speedMetersPerSecond;
        private int nextVehicleIndex;
        private bool trafficSpawned;
        private CarDriver playerCar;

        private void Start()
        {
            if (gameFlow == null) gameFlow = FindFirstObjectByType<GameFlow>();
            playerCar = FindFirstObjectByType<CarDriver>();

            if (road == null)
            {
                GameObject roadObject = GameObject.Find("Ruta");
                if (roadObject != null) road = roadObject.transform;
            }

            if (playerCar != null)
                spawnReference = playerCar.transform;
            else if (spawnReference == null)
            {
                GameObject referenceObject = GameObject.Find("AutoEntradaRuta");
                if (referenceObject != null) spawnReference = referenceObject.transform;
            }

            if (road == null || trafficVehiclePrefabs == null || trafficVehiclePrefabs.Length == 0)
            {
                Debug.LogWarning("TrafficManager necesita la ruta y los prefabs Traffic de VPP.", this);
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

            roadSurfaceY = roadBounds.max.y;
            centerX = road.position.x;
            startZ = Mathf.Clamp(spawnReference.position.z + firstSpawnDistance, minZ, maxZ);
            speedMetersPerSecond = speedKmh * MetersPerKilometer / SecondsPerHour;

            if (gameFlow != null)
            {
                gameFlow.DrivingStarted += SpawnDrivingTraffic;
                if (gameFlow.driving) SpawnDrivingTraffic();
            }
        }

        private void OnDestroy()
        {
            if (gameFlow == null) return;
            gameFlow.DrivingStarted -= SpawnDrivingTraffic;
        }

        private void SpawnDrivingTraffic()
        {
            if (trafficSpawned) return;
            trafficSpawned = true;

            if (playerCar == null) playerCar = FindFirstObjectByType<CarDriver>();
            float trafficOriginZ = playerCar != null
                ? playerCar.transform.position.z
                : spawnReference != null ? spawnReference.position.z : startZ;
            startZ = Mathf.Clamp(trafficOriginZ + firstSpawnDistance, minZ, maxZ);

            int configuredCount = Mathf.Clamp(trafficVehiclesPerDirection, 8, 32);
            int variation = Mathf.Max(1, configuredCount / 6);
            int totalVehicles = Random.Range(Mathf.Max(8, configuredCount - variation),
                Mathf.Min(33, configuredCount + variation + 1));
            int initialCount = Mathf.Min(totalVehicles,
                Mathf.Clamp(initialTrafficPerDirection, 3, 8));
            float sameDirectionDistance = Random.Range(0f, trafficSpacing * 0.35f);
            float oncomingDistance = Random.Range(oncomingStartDistance * 0.85f,
                oncomingStartDistance * 1.15f);

            for (int i = 0; i < initialCount; i++)
            {
                float sameDirectionZ = WrapZ(startZ + sameDirectionDistance, minZ, maxZ);
                float oncomingZ = WrapZ(trafficOriginZ + oncomingDistance, minZ, maxZ);

                SpawnVehicle(centerX + laneOffset, sameDirectionZ, Vector3.forward,
                    Random.Range(0, trafficVehiclePrefabs.Length));
                SpawnVehicle(centerX - laneOffset, oncomingZ, Vector3.back,
                    Random.Range(0, trafficVehiclePrefabs.Length));

                sameDirectionDistance += Random.Range(0.8f, 1.35f) * trafficSpacing;
                oncomingDistance += Random.Range(0.8f, 1.35f) * trafficSpacing;
            }

            StartCoroutine(SpawnRemainingTraffic(totalVehicles - initialCount));
        }

        private IEnumerator SpawnRemainingTraffic(int remaining)
        {
            while (remaining > 0)
            {
                if (gameFlow != null && (gameFlow.gameOver || gameFlow.finished)) yield break;
                yield return new WaitForSeconds(Random.Range(spawnIntervalMin, spawnIntervalMax));
                if (gameFlow != null && (gameFlow.gameOver || gameFlow.finished)) yield break;

                if (playerCar == null) playerCar = FindFirstObjectByType<CarDriver>();
                float playerZ = playerCar != null
                    ? playerCar.transform.position.z
                    : spawnReference != null ? spawnReference.position.z : startZ;
                float sameDirectionZ = WrapZ(playerZ + Random.Range(trafficSpawnDistance * 0.75f,
                    trafficSpawnDistance * 0.9f), minZ, maxZ);
                float oncomingZ = WrapZ(playerZ + Random.Range(trafficSpawnDistance * 1.0f,
                    trafficSpawnDistance * 1.2f), minZ, maxZ);

                SpawnVehicle(centerX + laneOffset, sameDirectionZ, Vector3.forward,
                    Random.Range(0, trafficVehiclePrefabs.Length));
                SpawnVehicle(centerX - laneOffset, oncomingZ, Vector3.back,
                    Random.Range(0, trafficVehiclePrefabs.Length));
                remaining--;
            }
        }

        private Bounds GetRoadBounds()
        {
            Renderer roadRenderer = road.GetComponent<Renderer>();
            if (roadRenderer != null) return roadRenderer.bounds;

            Collider roadCollider = road.GetComponent<Collider>();
            if (roadCollider != null) return roadCollider.bounds;

            return new Bounds(road.position, new Vector3(10f, 1f, 10000f));
        }

        private void SpawnVehicle(float x, float z, Vector3 direction, int modelIndex)
        {
            GameObject prefab = trafficVehiclePrefabs[modelIndex % trafficVehiclePrefabs.Length];
            if (prefab == null)
            {
                nextVehicleIndex++;
                return;
            }

            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            GameObject vehicle = Instantiate(prefab, new Vector3(x, roadSurfaceY, z), rotation, transform);
            vehicle.name = "TraficoVPP_" + nextVehicleIndex.ToString("00");

            Bounds visualBounds = GetRendererBounds(vehicle);
            float groundRootY = roadSurfaceY - visualBounds.min.y + 0.025f;
            vehicle.transform.position += Vector3.up * (groundRootY - vehicle.transform.position.y);

            Bounds localBounds = GetLocalRendererBounds(vehicle);
            Rigidbody body = vehicle.GetComponent<Rigidbody>();
            if (body == null) body = vehicle.AddComponent<Rigidbody>();

            foreach (Collider collider in vehicle.GetComponentsInChildren<Collider>(true))
            {
                if (collider != null && !collider.isTrigger) collider.enabled = false;
            }

            BoxCollider boxCollider = vehicle.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(Mathf.Max(0.8f, localBounds.size.x * 0.88f),
                Mathf.Max(0.55f, localBounds.size.y * 0.58f),
                Mathf.Max(1.8f, localBounds.size.z * 0.84f));
            boxCollider.center = new Vector3(localBounds.center.x,
                localBounds.min.y + boxCollider.size.y * 0.55f,
                localBounds.center.z);
            boxCollider.sharedMaterial = TrafficVehicle.GetNoBounceMaterial();

            body.isKinematic = false;
            body.useGravity = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.mass = Mathf.Max(body.mass, 1000f);
            body.maxDepenetrationVelocity = 2.5f;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.centerOfMass = new Vector3(localBounds.center.x,
                localBounds.min.y + localBounds.size.y * 0.28f,
                localBounds.center.z);

            ApplyTrafficColor(vehicle, nextVehicleIndex);
            TrafficVehicle trafficVehicle = vehicle.AddComponent<TrafficVehicle>();
            trafficVehicle.Configure(direction, speedMetersPerSecond, minZ, maxZ,
                roadSurfaceY, groundRootY, x, FindWheels(vehicle));
            trafficVehicle.SetHeadlights(true);
            nextVehicleIndex++;
        }

        private static Bounds GetRendererBounds(GameObject vehicle)
        {
            Renderer[] renderers = vehicle.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(vehicle.transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static Bounds GetLocalRendererBounds(GameObject vehicle)
        {
            Renderer[] renderers = vehicle.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Bounds result = new Bounds(Vector3.zero, Vector3.zero);

            foreach (Renderer renderer in renderers)
            {
                Bounds bounds = renderer.bounds;
                Vector3[] corners =
                {
                    new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
                    new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                    new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
                    new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
                    new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
                    new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
                    new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
                    new Vector3(bounds.max.x, bounds.max.y, bounds.max.z)
                };

                foreach (Vector3 corner in corners)
                {
                    Vector3 localCorner = vehicle.transform.InverseTransformPoint(corner);
                    if (!hasBounds)
                    {
                        result = new Bounds(localCorner, Vector3.zero);
                        hasBounds = true;
                    }
                    else result.Encapsulate(localCorner);
                }
            }

            return hasBounds ? result : new Bounds(Vector3.zero, Vector3.one);
        }

        private static Transform[] FindWheels(GameObject vehicle)
        {
            Transform[] wheels = new Transform[4];
            int count = 0;
            foreach (Transform item in vehicle.GetComponentsInChildren<Transform>(true))
            {
                string name = item.name.Replace("_", "").ToLowerInvariant();
                if (name != "wheelfl" && name != "wheelfr" && name != "wheelrl" && name != "wheelrr")
                    continue;
                if (count < wheels.Length) wheels[count++] = item;
            }
            return wheels;
        }

        private static void ApplyTrafficColor(GameObject vehicle, int index)
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
                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null || !material.name.ToLowerInvariant().Contains("paint")) continue;
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                    else if (material.HasProperty("_Color")) material.color = color;
                }
            }
        }

        private static float WrapZ(float value, float lower, float upper)
        {
            float length = upper - lower;
            if (length <= 0f) return lower;
            while (value > upper) value -= length;
            while (value < lower) value += length;
            return value;
        }
    }
}
