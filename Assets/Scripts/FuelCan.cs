using UnityEngine;

namespace EnElCamino
{
    public class FuelCan : MonoBehaviour
    {
        private void Start()
        {
            Destroy(gameObject, 12f);
        }
    }
}
