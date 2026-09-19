using UnityEngine;

namespace EnElCamino
{
    public class PhysicsCrate : MonoBehaviour
    {
        [SerializeField] private GameUI gameUI;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.GetComponent<PlayerController>() != null)
                gameUI.ShowMessage("La caja se movió.");
        }
    }
}
