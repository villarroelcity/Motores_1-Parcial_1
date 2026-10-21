using UnityEngine;

namespace EnElCamino
{
    /// <summary>
    /// Compatibility component kept for old scene references. The project uses
    /// ThirdPersonCamera as its only gameplay camera; creating a second
    /// Cinemachine camera here would leave the view fixed on the station when
    /// the player boards the vehicle.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class CinemachineFollowSetup : MonoBehaviour
    {
        private void Awake()
        {
            ThirdPersonCamera legacyCamera = GetComponent<ThirdPersonCamera>();
            if (legacyCamera != null)
            {
                legacyCamera.enabled = true;
                enabled = false;
            }
        }

        public void SetTarget(Transform newTarget)
        {
            ThirdPersonCamera legacyCamera = GetComponent<ThirdPersonCamera>();
            if (legacyCamera == null || newTarget == null) return;

            legacyCamera.enabled = true;
            legacyCamera.SetTarget(newTarget);
            legacyCamera.SnapToTarget();
        }
    }
}
