using System.Collections;
using UnityEngine;
using VehiclePhysics;

namespace EnElCamino
{
    [RequireComponent(typeof(CarDriver), typeof(Rigidbody))]
    public class CarArrivalSequence : MonoBehaviour
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private GameObject player;
        [SerializeField] private Transform stopPoint;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private float cruiseSpeed = 12f;
        [SerializeField] private float coastDistance = 180f;
        [SerializeField] private float fuelStarvationDuration = 1.2f;

        private IEnumerator Start()
        {
            CarDriver driver = GetComponent<CarDriver>();
            Rigidbody body = GetComponent<Rigidbody>();
            VPVehicleController vehicle = GetComponent<VPVehicleController>();
            driver.SetVehicleReady(false);
            driver.SetIntroFullThrottle(true);
            driver.StartEngineAudio(false);
            if (gameFlow != null) gameFlow.movementLocked = true;
            if (player != null) player.SetActive(false);
            CinemachineFollowSetup camera = FindFirstObjectByType<CinemachineFollowSetup>();
            ThirdPersonCamera legacyCamera = FindFirstObjectByType<ThirdPersonCamera>();
            if (camera != null) camera.SetTarget(transform);
            else if (legacyCamera != null) legacyCamera.SetTarget(transform);

            // VPP keeps suspension and tire contacts for the entire sequence.
            // Preserve the authored starting pose; the stop marker supplies X/Z only.
            body.isKinematic = false;
            body.detectCollisions = true;
            Vector3 destination = stopPoint != null ? stopPoint.position : transform.position;
            Vector3 laneOrigin = body.position;
            GameObject road = GameObject.Find("Ruta");
            Vector3 direction = Vector3.ProjectOnPlane(road != null ? road.transform.forward : transform.forward, Vector3.up).normalized;
            if (direction.sqrMagnitude < 0.01f) direction = transform.forward;
            if (Vector3.Dot(destination - laneOrigin, direction) < 0f) direction = -direction;
            // The arrival marker determines distance along the road, not a lateral
            // lane change from the hand-placed car to the marker's X coordinate.
            destination = laneOrigin + direction * Mathf.Max(0f, Vector3.Dot(destination - laneOrigin, direction));
            // Leave only the first fifth under power on shorter approaches. Most
            // of the arrival must visibly happen with the engine already off.
            float fuelCutoffDistance = Mathf.Min(coastDistance, Vector3.Distance(laneOrigin, destination) * 0.8f);
            RigidbodyConstraints originalConstraints = body.constraints;
            body.rotation = Quaternion.LookRotation(direction, Vector3.up);
            body.constraints = originalConstraints | RigidbodyConstraints.FreezeRotationY;
            body.linearVelocity = direction * cruiseSpeed;
            body.WakeUp();
            while (vehicle != null && !vehicle.initialized) yield return new WaitForFixedUpdate();

            bool starved = false;
            bool engineStopped = false;
            float starvationTime = 0f;
            while (true)
            {
                yield return new WaitForFixedUpdate();
                if (gameFlow != null && (gameFlow.gameOver || gameFlow.finished)) yield break;
                Vector3 horizontal = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
                float remaining = Vector3.Dot(destination - body.position, direction);
                float currentSpeed = Mathf.Max(0f, Vector3.Dot(horizontal, direction));
                if (!starved && remaining <= fuelCutoffDistance)
                {
                    starved = true;
                    body.constraints = originalConstraints;
                    driver.BeginFuelCutoffControl();
                    DayTimer clock = FindFirstObjectByType<DayTimer>();
                    if (clock != null) clock.BeginAtFuelCutoff();
                }
                if (starved)
                {
                    starvationTime += Time.fixedDeltaTime;
                    float fuel = 1f - Mathf.Clamp01(starvationTime / Mathf.Max(0.1f, fuelStarvationDuration));
                    driver.SetArrivalFuel(fuel);
                    if (fuel <= 0f && !engineStopped)
                    {
                        driver.StopEngineAudio();
                        engineStopped = true;
                    }
                    // Control is now physical: no route corrections, velocity
                    // forces or yaw locks may fight the player's steering/brakes.
                    if (fuel <= 0f && horizontal.magnitude <= 0.25f) break;
                    continue;
                }

                // Guide horizontal speed along the arrival route. Grounding stays
                // entirely physical; no bounds-based snap or final teleport.
                float desiredSpeed = cruiseSpeed;
                desiredSpeed = Mathf.Min(desiredSpeed, Mathf.Max(0f, remaining) / Time.fixedDeltaTime);
                Vector3 lateralError = Vector3.ProjectOnPlane(body.position - laneOrigin, Vector3.up);
                lateralError -= direction * Vector3.Dot(lateralError, direction);
                Vector3 laneCorrection = Vector3.ClampMagnitude(-lateralError * 3f, 1f);
                body.AddForce((direction * desiredSpeed + laneCorrection - horizontal) / Time.fixedDeltaTime, ForceMode.Acceleration);
                if (remaining <= 0.12f && currentSpeed <= 0.35f) break;
            }

            driver.SetArrivalFuel(0f);
            driver.StopEngineAudio();
            driver.EndFuelCutoffControl();
            driver.SetVehicleReady(true);
            // Same rigidbody and suspension while parked. Only the player appears.
            float settle = 0f;
            while (settle < 0.5f)
            {
                settle += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            body.constraints = originalConstraints;
            driver.HoldArrivalParking();
            if (player != null)
            {
                PlacePlayerBesideCar();
                player.SetActive(true);
            }
            if (gameFlow != null)
            {
                gameFlow.movementLocked = false;
                gameFlow.ShowMessage("El auto se quedó sin combustible. Acercate a la estación abandonada.");
            }
            if (camera != null && player != null) camera.SetTarget(player.transform);
            else if (legacyCamera != null && player != null) legacyCamera.SetTarget(player.transform);
        }

        private void PlacePlayerBesideCar()
        {
            // Markers may become stale when the author moves the starting car.
            // Compute a safe door-side position from the actual stopped vehicle.
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            foreach (Collider item in GetComponentsInChildren<Collider>())
                if (item.enabled && !item.isTrigger) bounds.Encapsulate(item.bounds);

            // Driver's door is always on the vehicle's local left side.
            Vector3 side = Vector3.ProjectOnPlane(-transform.right, Vector3.up).normalized;
            CharacterController controller = player.GetComponent<CharacterController>();
            float radius = controller != null ? controller.radius * Mathf.Abs(player.transform.lossyScale.x) : 0.4f;
            float halfWidth = Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(side.x), 0f, Mathf.Abs(side.z)));
            Vector3 position = bounds.center + side * (halfWidth + radius + 0.65f);
            RaycastHit[] hits = Physics.RaycastAll(position + Vector3.up * 5f, Vector3.down, 30f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float ground = float.NegativeInfinity;
            foreach (RaycastHit hit in hits)
            {
                if (hit.normal.y < 0.5f || hit.transform == transform || hit.transform.IsChildOf(transform) ||
                    hit.transform == player.transform || hit.transform.IsChildOf(player.transform)) continue;
                ground = Mathf.Max(ground, hit.point.y);
            }
            if (!float.IsNegativeInfinity(ground))
            {
                float footOffset = controller != null
                    ? (controller.center.y - controller.height * 0.5f) * player.transform.lossyScale.y : 0f;
                position.y = ground - footOffset + 0.04f;
            }
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;
            player.transform.SetPositionAndRotation(position, exitPoint != null ? exitPoint.rotation : transform.rotation);
            PlayerController movement = player.GetComponent<PlayerController>();
            if (movement != null) movement.ResetFallTracking();
            if (controller != null) controller.enabled = wasEnabled;
        }
    }
}
