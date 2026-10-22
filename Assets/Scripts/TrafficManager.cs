using UnityEngine;

namespace EnElCamino
{
    /// <summary>
    /// Genera autos VPP visuales en los dos carriles y los mantiene circulando.
    /// 60 km/h equivalen a 16.67 metros por segundo.
    /// </summary>
    public class TrafficManager : MonoBehaviour
    {
        [SerializeField] private GameObject[] vehiclePrefabs;
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

            if (road == null || vehiclePrefabs == null || vehiclePrefabs.Length == 0)
            {
                Debug.LogWarning("TrafficManager necesita la ruta y al menos un prefab de vehiculo.", this);
                return;
            }

            if (spawnReference == null)
            {
                spawnReference = road;
            }

            Bounds roadBounds = GetRoadBounds();
            float minZ = roadBounds.min.z + routePadding;
            float maxZ = roadBounds.max.z - routePadding;
            if (minZ >= maxZ)
            {
                minZ = road.position.z - 5000f;
                maxZ = road.position.z + 5000f;
            }

            float speedMetersPerSecond = speedKmh * MetersPerKilometer / SecondsPerHour;
            Vector3 forward = Vector3.forward;
            float centerX = road.position.x;
            float startZ = Mathf.Clamp(spawnReference.position.z + firstSpawnDistance, minZ, maxZ);
            float height = road.position.y + vehicleHeight;

            for (int i = 0; i < vehiclesPerDirection; i++)
            {
                SpawnVehicle(i, centerX + laneOffset, startZ + i * spacing, forward, speedMetersPerSecond, minZ, maxZ, height);
                SpawnVehicle(i + vehiclesPerDirection, centerX - laneOffset, startZ + i * spacing, -forward, speedMetersPerSecond, minZ, maxZ, height);
            }
        }

        private Bounds GetRoadBounds()
        {
            Renderer roadRenderer = road.GetComponent<Renderer>();
            if (roadRenderer != null)
            {
                return roadRenderer.bounds;
            }

            return new Bounds(road.position, new Vector3(10f, 1f, 10000f));
        }

        private void SpawnVehicle(int index, float x, float z, Vector3 direction, float speedMetersPerSecond, float minZ, float maxZ, float height)
        {
            GameObject prefab = vehiclePrefabs[index % vehiclePrefabs.Length];
            if (prefab == null) return;

            Vector3 position = new Vector3(x, height, WrapZ(z, minZ, maxZ));
            GameObject vehicle = Instantiate(prefab, position, Quaternion.LookRotation(direction, Vector3.up), transform);
            vehicle.name = "Trafico_" + (index + 1).ToString("00");

            TrafficVehicle trafficVehicle = vehicle.AddComponent<TrafficVehicle>();
            trafficVehicle.Configure(direction, speedMetersPerSecond, minZ, maxZ, height);
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
