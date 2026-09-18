using UnityEngine;

namespace EnElCamino
{
    public class ExitTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerController>() != null)
                GameFlow.Instance?.TryFinish();
        }
    }
}
