using UnityEngine;

namespace EnElCamino
{
    /// <summary>
    /// Genera vehiculos visuales simples en los dos carriles y los mantiene circulando.
    /// 60 km/h equivalen a 16.67 metros por segundo.
    /// </summary>
    public class TrafficManager : MonoBehaviour
    {
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

            if (road == null)
            {
                Debug.LogWarning("TrafficManager necesita la ruta.", this);
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
            Vector3 position = new Vector3(x, height, WrapZ(z, minZ, maxZ));
            GameObject vehicle = CrearVehiculo(index, position, direction);

            TrafficVehicle trafficVehicle = vehicle.AddComponent<TrafficVehicle>();
            trafficVehicle.Configure(direction, speedMetersPerSecond, minZ, maxZ, height, ObtenerRuedas(vehicle));
        }

        private GameObject CrearVehiculo(int index, Vector3 position, Vector3 direction)
        {
            GameObject vehicle = new GameObject("Trafico_" + (index + 1).ToString("00"));
            vehicle.transform.SetParent(transform);
            vehicle.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction, Vector3.up));

            Color bodyColor = ObtenerColor(index);
            Material bodyMaterial = CrearMaterial(bodyColor);
            Material windowMaterial = CrearMaterial(new Color(0.04f, 0.06f, 0.09f));
            Material tireMaterial = CrearMaterial(new Color(0.015f, 0.015f, 0.015f));

            if (index % 2 == 0)
            {
                CrearParte(vehicle.transform, PrimitiveType.Cube, new Vector3(0f, 0.28f, 0f),
                    new Vector3(1.7f, 0.5f, 3.8f), Quaternion.identity, bodyMaterial);
                CrearParte(vehicle.transform, PrimitiveType.Cube, new Vector3(0f, 0.68f, 0.25f),
                    new Vector3(1.35f, 0.38f, 1.55f), Quaternion.identity, windowMaterial);
            }
            else
            {
                CrearParte(vehicle.transform, PrimitiveType.Cube, new Vector3(0f, 0.28f, 0f),
                    new Vector3(1.85f, 0.5f, 3.9f), Quaternion.identity, bodyMaterial);
                CrearParte(vehicle.transform, PrimitiveType.Cube, new Vector3(0f, 0.72f, 0.55f),
                    new Vector3(1.45f, 0.7f, 1.45f), Quaternion.identity, windowMaterial);
                CrearParte(vehicle.transform, PrimitiveType.Cube, new Vector3(0f, 0.58f, -1.05f),
                    new Vector3(1.65f, 0.4f, 1.05f), Quaternion.identity, bodyMaterial);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                for (int axle = -1; axle <= 1; axle += 2)
                {
                    CrearParte(vehicle.transform, PrimitiveType.Cylinder,
                        new Vector3(side * 0.94f, -0.12f, axle * 1.15f),
                        new Vector3(0.32f, 0.14f, 0.32f), Quaternion.Euler(0f, 0f, 90f), tireMaterial);
                }
            }

            return vehicle;
        }

        private GameObject CrearParte(Transform parent, PrimitiveType type, Vector3 localPosition,
            Vector3 localScale, Quaternion localRotation, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.transform.SetParent(parent, false);
            part.transform.SetLocalPositionAndRotation(localPosition, localRotation);
            part.transform.localScale = localScale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            return part;
        }

        private Material CrearMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            Material material = new Material(shader);
            material.color = color;
            return material;
        }

        private Color ObtenerColor(int index)
        {
            Color[] colors =
            {
                new Color(0.75f, 0.05f, 0.04f),
                new Color(0.04f, 0.18f, 0.65f),
                new Color(0.85f, 0.85f, 0.82f),
                new Color(0.9f, 0.55f, 0.04f)
            };
            return colors[index % colors.Length];
        }

        private Transform[] ObtenerRuedas(GameObject vehicle)
        {
            Transform[] wheels = new Transform[4];
            int index = 0;
            foreach (Transform child in vehicle.transform)
            {
                if (child.name.StartsWith("Cylinder") && index < wheels.Length)
                {
                    wheels[index++] = child;
                }
            }
            return wheels;
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
