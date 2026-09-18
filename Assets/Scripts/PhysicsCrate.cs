using UnityEngine;

namespace EnElCamino
{
    public class PhysicsCrate : MonoBehaviour
    {
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.GetComponent<PlayerController>() != null)
                GameUI.Instance?.ShowMessage("La caja se movió.");
        }
    }
}
