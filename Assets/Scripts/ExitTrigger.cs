using UnityEngine;

namespace EnElCamino
{
    public class ExitTrigger : MonoBehaviour
    {
        [SerializeField] private GameFlow gameFlow;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<CarDriver>() != null)
                gameFlow.TryFinish();
        }
    }
}
