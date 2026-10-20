using UnityEngine;

namespace EnElCamino
{
    /// <summary>Visible generator lead, socket plug and live indicator for the station power box.</summary>
    [ExecuteAlways]
    public class StationPowerCableVisual : MonoBehaviour
    {
        [SerializeField] private Transform generatorOutlet;
        [SerializeField] private Transform boxSocket;
        [SerializeField] private Transform wallPassThrough;
        [SerializeField] private float wallThickness = 0.4f;
        [SerializeField] private LineRenderer cable;
        [SerializeField] private Renderer plugIndicator;
        [SerializeField] private Light statusLamp;
        private bool connected;

        private void Awake()
        {
            if (Application.isPlaying) EnsureWallPassThrough();
        }

        private void LateUpdate()
        {
            if (cable == null || generatorOutlet == null || boxSocket == null) return;
            if (Application.isPlaying && wallPassThrough == null) EnsureWallPassThrough();
            Vector3 start = generatorOutlet.position;
            Vector3 towardGenerator = (start - boxSocket.position).normalized;
            Vector3 end = connected ? boxSocket.position : boxSocket.position + towardGenerator * 0.8f;
            if (wallPassThrough != null)
            {
                Vector3 wallNormal = wallPassThrough.up.normalized;
                float outsideSign = Mathf.Sign(Vector3.Dot(start - wallPassThrough.position, wallNormal));
                if (Mathf.Approximately(outsideSign, 0f)) outsideSign = 1f;
                Vector3 outsidePoint = wallPassThrough.position + wallNormal * outsideSign * (wallThickness * 0.5f + 0.035f);
                Vector3 insideDirection = -wallNormal * outsideSign;
                Vector3 insidePoint = wallPassThrough.position + insideDirection * (wallThickness * 0.5f + 0.035f);
                end = connected ? boxSocket.position : boxSocket.position + insideDirection * 0.65f;

                if (cable.positionCount != 5) cable.positionCount = 5;
                Vector3 outsideSag = Vector3.down * Mathf.Min(0.08f, Vector3.Distance(start, outsidePoint) * 0.02f);
                cable.SetPosition(0, start);
                cable.SetPosition(1, Vector3.Lerp(start, outsidePoint, 0.55f) + outsideSag);
                cable.SetPosition(2, outsidePoint);
                cable.SetPosition(3, insidePoint);
                cable.SetPosition(4, end);
            }
            else
            {
                if (cable.positionCount != 4) cable.positionCount = 4;
                Vector3 sag = Vector3.down * Mathf.Min(0.3f, Vector3.Distance(start, end) * 0.08f);
                cable.SetPosition(0, start);
                cable.SetPosition(1, Vector3.Lerp(start, end, 0.33f) + sag);
                cable.SetPosition(2, Vector3.Lerp(start, end, 0.67f) + sag);
                cable.SetPosition(3, end);
            }

            // Keep the authored plug transform untouched in Edit Mode; only animate it at runtime.
            if (Application.isPlaying && plugIndicator != null)
                plugIndicator.transform.position = connected ? boxSocket.position : end;
        }

        private void EnsureWallPassThrough()
        {
            if (wallPassThrough != null) return;
            Transform existing = transform.Find("PasacablePared");
            if (existing != null)
            {
                wallPassThrough = existing;
                return;
            }

            GameObject wallObject = GameObject.Find("TallerParedFondo");
            if (wallObject == null || boxSocket == null) return;

            Transform wall = wallObject.transform;
            Vector3 normal = wall.forward.normalized;
            Vector3 centerOnWall = boxSocket.position + normal * Vector3.Dot(wall.position - boxSocket.position, normal);
            BoxCollider wallCollider = wallObject.GetComponent<BoxCollider>();
            wallThickness = wallCollider != null ? wallCollider.size.z * wall.lossyScale.z : wall.lossyScale.z;

            GameObject sleeve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sleeve.name = "PasacablePared";
            sleeve.transform.SetParent(transform, true);
            sleeve.transform.SetPositionAndRotation(centerOnWall, Quaternion.FromToRotation(Vector3.up, normal));
            sleeve.transform.localScale = new Vector3(0.14f, wallThickness * 0.5f + 0.08f, 0.14f);
            Collider sleeveCollider = sleeve.GetComponent<Collider>();
            if (sleeveCollider != null) Destroy(sleeveCollider);
            Renderer sleeveRenderer = sleeve.GetComponent<Renderer>();
            if (sleeveRenderer != null && cable != null) sleeveRenderer.sharedMaterial = cable.sharedMaterial;
            wallPassThrough = sleeve.transform;
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
