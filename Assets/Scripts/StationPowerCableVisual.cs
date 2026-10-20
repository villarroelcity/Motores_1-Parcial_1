using UnityEngine;

namespace EnElCamino
{
    /// <summary>Visible generator lead, socket plug and live indicator for the station power box.</summary>
    [ExecuteAlways]
    public class StationPowerCableVisual : MonoBehaviour
    {
        [SerializeField] private Transform generatorOutlet;
        [SerializeField] private Transform boxSocket;
        [SerializeField] private LineRenderer cable;
        [SerializeField] private Renderer plugIndicator;
        [SerializeField] private Light statusLamp;
        private bool connected;

        private void LateUpdate()
        {
            if (cable == null || generatorOutlet == null || boxSocket == null) return;
            Vector3 start = generatorOutlet.position;
            Vector3 towardGenerator = (start - boxSocket.position).normalized;
            Vector3 end = connected ? boxSocket.position : boxSocket.position + towardGenerator * 0.8f;
            if (plugIndicator != null)
                plugIndicator.transform.position = connected ? boxSocket.position : end;
            Vector3 sag = Vector3.down * Mathf.Min(0.3f, Vector3.Distance(start, end) * 0.08f);
            cable.SetPosition(0, start);
            cable.SetPosition(1, Vector3.Lerp(start, end, 0.33f) + sag);
            cable.SetPosition(2, Vector3.Lerp(start, end, 0.67f) + sag);
            cable.SetPosition(3, end);
        }

        public void SetConnected(bool value)
        {
            connected = value;
            Color color = value ? new Color(0.15f, 0.95f, 0.28f) : new Color(0.9f, 0.12f, 0.04f);
            if (plugIndicator != null)
            {
                foreach (Material material in plugIndicator.materials)
                {
                    if (material == null) continue;
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                    if (material.HasProperty("_Color")) material.color = color;
                    if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", value ? color * 1.5f : Color.black);
                }
            }
            if (statusLamp != null)
            {
                statusLamp.color = color;
                statusLamp.enabled = value;
            }
        }
    }
}
