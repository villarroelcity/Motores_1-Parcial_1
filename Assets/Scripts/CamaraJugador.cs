using UnityEngine;
using Unity.Cinemachine;

namespace EnElCamino
{
    // Cinemachine mueve la camara. Este puente solo cambia el objetivo.
    [DefaultExecutionOrder(-100)]
    public class CamaraJugador : MonoBehaviour
    {
        [SerializeField] private Juego gameFlow;
        [SerializeField] private CinemachineCamera followCamera;
        [SerializeField] private CinemachineBrain brain;

        public void SetTarget(Transform target)
        {
            if (target == null || followCamera == null) return;
            followCamera.Follow = target;
            followCamera.LookAt = target;
            SnapToTarget();
        }

        public void SetVehicleTarget(Transform target) => SetTarget(target);

        public void SnapToTarget()
        {
            if (followCamera == null || brain == null) return;
            followCamera.PreviousStateIsValid = false;
        }

        private void Update()
        {
            if (brain != null && gameFlow != null)
                brain.enabled = !gameFlow.finished && !gameFlow.gameOver;
        }
    }
}
