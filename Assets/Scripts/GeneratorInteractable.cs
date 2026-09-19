using UnityEngine;

namespace EnElCamino
{
    public class GeneratorInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private Renderer indicator;
        [SerializeField] private Light indicatorLight;
        private bool generatorOn;

        public string Prompt
        {
            get
            {
                if (generatorOn) return "Generador encendido";
                return "[E] Presionar botón rojo";
            }
        }

        public void Interact()
        {
            if (generatorOn) return;

            generatorOn = true;
            indicator.material.color = Color.green;
            indicatorLight.enabled = true;
            gameFlow.TurnOnGenerator();
        }
    }
}
