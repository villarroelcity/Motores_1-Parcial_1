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

        private CarDriver carDriver;
        private Rigidbody vehicleBody;

        private void Awake()
        {
            carDriver = GetComponent<CarDriver>();
            vehicleBody = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            StartCoroutine(ArriveAndUnload());
        }

        private IEnumerator ArriveAndUnload()
        {
            carDriver.SetVehicleReady(false);
            if (gameFlow != null) gameFlow.movementLocked = true;
            if (player != null) player.SetActive(false);

            if (arrivalStart != null)
                transform.SetPositionAndRotation(arrivalStart.position, arrivalStart.rotation);

            if (vehicleBody != null)
            {
                vehicleBody.isKinematic = true;
                vehicleBody.linearVelocity = Vector3.zero;
                vehicleBody.angularVelocity = Vector3.zero;
            }

            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            Vector3 targetPosition = stopPoint != null ? stopPoint.position : startPosition;
            Quaternion targetRotation = stopPoint != null ? stopPoint.rotation : startRotation;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, duration));
                t = t * t * (3f - 2f * t);
                transform.SetPositionAndRotation(Vector3.Lerp(startPosition, targetPosition, t), Quaternion.Slerp(startRotation, targetRotation, t));
                yield return null;
            }

            transform.SetPositionAndRotation(targetPosition, targetRotation);
            if (vehicleBody != null)
            {
                vehicleBody.isKinematic = false;
                vehicleBody.linearVelocity = Vector3.zero;
                vehicleBody.angularVelocity = Vector3.zero;
            }

            if (player != null)
            {
                if (exitPoint != null)
                    player.transform.SetPositionAndRotation(exitPoint.position, exitPoint.rotation);
                player.SetActive(true);
            }

            carDriver.SetVehicleReady(true);
            if (gameFlow != null)
            {
                gameFlow.movementLocked = false;
                gameFlow.ShowMessage("El auto se detuvo en la ruta. Bajá y acercate a la estación abandonada.");
            }

            CinemachineFollowSetup cameraFollow = FindFirstObjectByType<CinemachineFollowSetup>();
            if (cameraFollow != null && player != null)
                cameraFollow.SetTarget(player.transform);
        }
    }
}
