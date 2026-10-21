using UnityEngine;

namespace EnElCamino
{
    public class CheckPoint : MonoBehaviour
    {
        [SerializeField] private GameFlow gameFlow;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<CarDriver>() != null)
                gameFlow.TryFinish();
        }
    }
}
