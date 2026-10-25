using UnityEngine;
using System.Collections;

namespace EnElCamino
{
    /// <summary>
    /// Activa autos ya preparados en la escena cuando el jugador empieza a conducir.
    /// </summary>
    public class Trafico : MonoBehaviour
    {
        [SerializeField] private Juego gameFlow;
        [SerializeField] private Transform road;
        [SerializeField] private Transform spawnReference;
        [SerializeField] private int trafficVehiclesPerDirection = 19;
        [SerializeField] private int initialTrafficPerDirection = 4;
        [SerializeField] private float speedKmh = 60f;
        [SerializeField] private float laneOffset = 1.5f;
        [SerializeField] private float trafficSpacing = 125f;
        [SerializeField] private float firstSpawnDistance = 420f;
        [SerializeField] private float oncomingStartDistance = 650f;
        [SerializeField] private float trafficSpawnDistance = 700f;
        [SerializeField] private float spawnIntervalMin = 4f;
        [SerializeField] private float spawnIntervalMax = 7f;
        [SerializeField] private float routePadding = 40f;

        [SerializeField] private TraficoAutos[] trafficPool;

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
        private AutoJugador playerCar;

        private void Start()
        {
            if (gameFlow == null) gameFlow = FindFirstObjectByType<Juego>();
            playerCar = FindFirstObjectByType<AutoJugador>();

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

            if (road == null || trafficPool == null || trafficPool.Length == 0)
            {
                Debug.LogWarning("Trafico necesita la ruta y los autos de la reserva.", this);
                return;
            }

            if (spawnReference == null) spawnReference = road;

            // Mezclar las referencias no crea autos nuevos.
            for (int i = trafficPool.Length - 1; i > 0; i--)
            {
                int other = Random.Range(0, i + 1);
                TraficoAutos saved = trafficPool[i];
                trafficPool[i] = trafficPool[other];
                trafficPool[other] = saved;
            }
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

            if (playerCar == null) playerCar = FindFirstObjectByType<AutoJugador>();
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

                SpawnVehicle(centerX + laneOffset, sameDirectionZ, Vector3.forward);
                SpawnVehicle(centerX - laneOffset, oncomingZ, Vector3.back);

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

                if (playerCar == null) playerCar = FindFirstObjectByType<AutoJugador>();
                float playerZ = playerCar != null
                    ? playerCar.transform.position.z
                    : spawnReference != null ? spawnReference.position.z : startZ;
                float sameDirectionZ = WrapZ(playerZ + Random.Range(trafficSpawnDistance * 0.75f,
                    trafficSpawnDistance * 0.9f), minZ, maxZ);
                float oncomingZ = WrapZ(playerZ + Random.Range(trafficSpawnDistance * 1.0f,
                    trafficSpawnDistance * 1.2f), minZ, maxZ);

                SpawnVehicle(centerX + laneOffset, sameDirectionZ, Vector3.forward);
                SpawnVehicle(centerX - laneOffset, oncomingZ, Vector3.back);
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

        private void SpawnVehicle(float x, float z, Vector3 direction)
        {
            if (nextVehicleIndex >= trafficPool.Length) return;
            TraficoAutos vehicle = trafficPool[nextVehicleIndex];
            float height = roadSurfaceY + vehicle.GroundOffset;
            if (Physics.CheckBox(new Vector3(x, height + 0.9f, z), new Vector3(1.05f, 0.55f, 10f),
                Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;
            nextVehicleIndex++;
            vehicle.transform.SetPositionAndRotation(new Vector3(x, height, z),
                Quaternion.LookRotation(direction, Vector3.up));
            vehicle.gameObject.SetActive(true);
            vehicle.Configure(direction, speedMetersPerSecond, minZ, maxZ, height, x);
            vehicle.SetHeadlights(true);
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
