using UnityEngine;

namespace EnElCamino
{
    public class GeneratorInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Renderer indicator;
        [SerializeField] private Material offMaterial;
        [SerializeField] private Material onMaterial;
        [SerializeField] private Light indicatorLight;
        private bool active;

        public string Prompt => active ? "Generador encendido" : "[E] Encender generador";

        public void Configure(Renderer targetIndicator, Material off, Material on, Light lightSource)
        {
            indicator = targetIndicator;
            offMaterial = off;
            onMaterial = on;
            indicatorLight = lightSource;
            ApplyVisuals();
        }

        public void Interact()
        {
            if (active) return;
            active = true;
            ApplyVisuals();
            GameFlow.Instance?.ActivateGenerator();
        }

        private void ApplyVisuals()
        {
            if (indicator != null && (active ? onMaterial : offMaterial) != null)
                indicator.material = active ? onMaterial : offMaterial;
            if (indicatorLight != null) indicatorLight.enabled = active;
        }
    }
}
