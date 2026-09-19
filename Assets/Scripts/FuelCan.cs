using UnityEngine;

namespace EnElCamino
{
    public class FuelCan : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private Renderer fuelCanRenderer;
        [SerializeField] private Transform player;
        private bool taken;

        public string Prompt
        {
            get
            {
                if (taken) return "Bidón tomado";
                return "[E] Tomar bidón vacío";
            }
        }

        public void Interact()
        {
            if (taken) return;

            if (!gameFlow.generatorOn)
            {
                gameFlow.ShowMessage("Primero encendé el generador.");
                return;
            }

            if (!gameFlow.garageOpen)
            {
                gameFlow.ShowMessage("Primero abrí el portón desde adentro del garage.");
                return;
            }

            taken = true;
            GetComponent<Collider>().enabled = false;
            transform.SetParent(player);
            transform.localPosition = new Vector3(0.6f, 1f, 0.3f);
            transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            gameFlow.TakeFuelCan();
        }

        public void Fill()
        {
            fuelCanRenderer.material.color = Color.green;
        }

        public bool IsTaken()
        {
            return taken;
        }

        public void Use()
        {
            gameObject.SetActive(false);
        }
    }
}
