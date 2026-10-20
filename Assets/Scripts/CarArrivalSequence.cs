using System.Collections;
using UnityEngine;

namespace EnElCamino
{
    /// <summary>
    /// Intro sequence: the VPP vehicle arrives on the road, stops outside the station,
    /// and only then lets the player get out and start the investigation on foot.
    /// </summary>
    [RequireComponent(typeof(CarDriver))]
    public class CarArrivalSequence : MonoBehaviour
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private GameObject player;
        [SerializeField] private Transform arrivalStart;
        [SerializeField] private Transform stopPoint;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private float duration = 7f;
        [SerializeField] private float engineCutoffLeadTime = 2.25f;

        private CarDriver carDriver;
        private Rigidbody vehicleBody;
        private Transform originalPlayerParent;
        private Behaviour vppController;

        private void Awake()
        {
            carDriver = GetComponent<CarDriver>();
            vehicleBody = GetComponent<Rigidbody>();
            vppController = GetComponent("VPVehicleController") as Behaviour;
        }

        private void Start()
        {
            StartCoroutine(ArriveAndUnload());
        }

        private IEnumerator ArriveAndUnload()
        {
            carDriver.SetVehicleReady(false);
            // The intro begins with the car already running; don't play the starter.
            carDriver.StartEngineAudio(false);
            if (gameFlow != null) gameFlow.movementLocked = true;
            if (player != null)
            {
                originalPlayerParent = player.transform.parent;
                player.transform.SetParent(null, true);
                player.SetActive(false);
            }

            CinemachineFollowSetup cameraFollow = FindFirstObjectByType<CinemachineFollowSetup>();
            if (cameraFollow != null) cameraFollow.SetTarget(transform);

            if (vehicleBody != null)
            {
                vehicleBody.linearVelocity = Vector3.zero;
                vehicleBody.angularVelocity = Vector3.zero;
                vehicleBody.isKinematic = true;
                vehicleBody.detectCollisions = false;
            }
            // Keep VPVehicleController enabled so its built-in engine audio remains active.

            if (arrivalStart != null)
            {
                if (vehicleBody != null)
                {
                    transform.SetPositionAndRotation(arrivalStart.position, arrivalStart.rotation);
                }
                else
                {
                    transform.SetPositionAndRotation(arrivalStart.position, arrivalStart.rotation);
                }
            }

            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            Vector3 targetPosition = stopPoint != null ? stopPoint.position : startPosition;
            Quaternion targetRotation = stopPoint != null ? stopPoint.rotation : startRotation;

            float elapsed = 0f;
            bool engineCutoff = false;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (!engineCutoff && elapsed >= Mathf.Max(0f, duration - engineCutoffLeadTime))
                {
                    carDriver.StopEngineAudio();
                    engineCutoff = true;
                }
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, duration));
                t = t * t * (3f - 2f * t);
                Vector3 nextPosition = Vector3.Lerp(startPosition, targetPosition, t);
                Quaternion nextRotation = Quaternion.Slerp(startRotation, targetRotation, t);
                transform.SetPositionAndRotation(nextPosition, nextRotation);
                yield return null;
            }

            if (vehicleBody != null)
            {
                transform.SetPositionAndRotation(targetPosition, targetRotation);
                vehicleBody.detectCollisions = true;
            }
            if (vehicleBody != null)
            {
                vehicleBody.isKinematic = false;
                vehicleBody.linearVelocity = Vector3.zero;
                vehicleBody.angularVelocity = Vector3.zero;
            }

            if (player != null)
            {
                player.transform.SetParent(originalPlayerParent, true);
                if (exitPoint != null)
                    player.transform.SetPositionAndRotation(exitPoint.position, exitPoint.rotation);
                player.SetActive(true);
            }

            carDriver.SetVehicleReady(true);
            carDriver.StopEngineAudio();
            if (gameFlow != null)
            {
                gameFlow.movementLocked = false;
                gameFlow.ShowMessage("El auto se detuvo en la ruta. Bajá y acercate a la estación abandonada.");
            }

            cameraFollow = FindFirstObjectByType<CinemachineFollowSetup>();
            if (cameraFollow != null && player != null)
                cameraFollow.SetTarget(player.transform);
        }
    }
}
