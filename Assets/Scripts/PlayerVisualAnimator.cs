using UnityEngine;

namespace EnElCamino
{
    /// <summary>
    /// Builds a simple character silhouette and animates the limbs procedurally.
    /// Footsteps are emitted when the feet cross their contact phase.
    /// </summary>
    [ExecuteAlways]
    public class PlayerVisualAnimator : MonoBehaviour
    {
        [SerializeField] private float walkCycleSpeed = 8f;
        [SerializeField] private float limbSwing = 28f;
        [SerializeField] private float stepVolume = 0.32f;

        private CharacterController controller;
        private Transform leftArm;
        private Transform rightArm;
        private Transform leftLeg;
        private Transform rightLeg;
        private Transform leftFoot;
        private Transform rightFoot;
        private Transform head;
        private AudioSource footstepSource;
        private AudioClip footstepClip;
        private float walkPhase;
        private Vector3 idleHeadPosition;
        private bool built;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            BuildCharacterVisual();
        }

        private void OnEnable()
        {
            if (!built) BuildCharacterVisual();
        }

        private void Update()
        {
            if (!Application.isPlaying || !built || controller == null) return;

            Vector3 horizontalVelocity = controller.velocity;
            horizontalVelocity.y = 0f;
            bool walking = horizontalVelocity.sqrMagnitude > 0.04f;

            if (walking)
            {
                float speedFactor = Mathf.Clamp(horizontalVelocity.magnitude / 4.5f, 0.65f, 1.35f);
                float previousPhase = walkPhase;
                walkPhase += Time.deltaTime * walkCycleSpeed * speedFactor;
                AnimateWalk();

                float previousSign = Mathf.Sign(Mathf.Sin(previousPhase));
                float currentSign = Mathf.Sign(Mathf.Sin(walkPhase));
                if (previousSign != 0f && currentSign != 0f && previousSign != currentSign)
                    PlayFootstep();
            }
            else
            {
                walkPhase = Mathf.Lerp(walkPhase, 0f, Time.deltaTime * 8f);
                AnimateIdle();
            }
        }

        private void BuildCharacterVisual()
        {
            if (transform.Find("PlayerArms") != null) return;

            Renderer bodyRenderer = GetComponentInChildren<Renderer>();
            Material bodyMaterial = bodyRenderer != null ? bodyRenderer.sharedMaterial : null;

            GameObject group = new GameObject("PlayerArms");
            group.transform.SetParent(transform, false);

            leftArm = CreateLimb("BrazoIzquierdo", PrimitiveType.Capsule, group.transform, new Vector3(-0.48f, 1.15f, 0f), new Vector3(0.14f, 0.42f, 0.14f), bodyMaterial);
            rightArm = CreateLimb("BrazoDerecho", PrimitiveType.Capsule, group.transform, new Vector3(0.48f, 1.15f, 0f), new Vector3(0.14f, 0.42f, 0.14f), bodyMaterial);
            leftLeg = CreateLimb("PiernaIzquierda", PrimitiveType.Capsule, group.transform, new Vector3(-0.2f, 0.42f, 0f), new Vector3(0.17f, 0.48f, 0.17f), bodyMaterial);
            rightLeg = CreateLimb("PiernaDerecha", PrimitiveType.Capsule, group.transform, new Vector3(0.2f, 0.42f, 0f), new Vector3(0.17f, 0.48f, 0.17f), bodyMaterial);
            leftFoot = CreateLimb("PieIzquierdo", PrimitiveType.Cube, group.transform, new Vector3(-0.2f, 0.08f, 0.1f), new Vector3(0.24f, 0.14f, 0.42f), bodyMaterial);
            rightFoot = CreateLimb("PieDerecho", PrimitiveType.Cube, group.transform, new Vector3(0.2f, 0.08f, 0.1f), new Vector3(0.24f, 0.14f, 0.42f), bodyMaterial);

            head = CreateLimb("Cabeza", PrimitiveType.Sphere, transform, new Vector3(0f, 1.86f, 0f), new Vector3(0.32f, 0.32f, 0.32f), bodyMaterial);
            idleHeadPosition = head.localPosition;

            if (Application.isPlaying)
            {
                footstepSource = gameObject.AddComponent<AudioSource>();
                footstepSource.playOnAwake = false;
                footstepSource.spatialBlend = 0.35f;
                footstepSource.volume = stepVolume;
                footstepClip = CreateFootstepClip();
            }
            built = true;
        }

        private Transform CreateLimb(string limbName, PrimitiveType primitive, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject limb = GameObject.CreatePrimitive(primitive);
            limb.name = limbName;
            limb.transform.SetParent(parent, false);
            limb.transform.localPosition = localPosition;
            limb.transform.localScale = localScale;

            Collider collider = limb.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }

            Renderer renderer = limb.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            return limb.transform;
        }

        private void AnimateWalk()
        {
            float swing = Mathf.Sin(walkPhase) * limbSwing;
            leftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
            rightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            leftArm.localRotation = Quaternion.Euler(-swing * 0.65f, 0f, 0f);
            rightArm.localRotation = Quaternion.Euler(swing * 0.65f, 0f, 0f);
            leftFoot.localRotation = Quaternion.Euler(-swing * 0.35f, 0f, 0f);
            rightFoot.localRotation = Quaternion.Euler(swing * 0.35f, 0f, 0f);
            head.localPosition = idleHeadPosition + Vector3.up * (Mathf.Abs(Mathf.Sin(walkPhase * 2f)) * 0.025f);
        }

        private void AnimateIdle()
        {
            leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.identity, Time.deltaTime * 8f);
            rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.identity, Time.deltaTime * 8f);
            leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.identity, Time.deltaTime * 8f);
            rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.identity, Time.deltaTime * 8f);
            leftFoot.localRotation = Quaternion.Slerp(leftFoot.localRotation, Quaternion.identity, Time.deltaTime * 8f);
            rightFoot.localRotation = Quaternion.Slerp(rightFoot.localRotation, Quaternion.identity, Time.deltaTime * 8f);
            head.localPosition = Vector3.Lerp(head.localPosition, idleHeadPosition, Time.deltaTime * 8f);
        }

        private void PlayFootstep()
        {
            if (footstepSource != null && footstepClip != null)
            {
                footstepSource.pitch = Random.Range(0.92f, 1.06f);
                footstepSource.PlayOneShot(footstepClip, Random.Range(0.82f, 1f));
            }
        }

        private AudioClip CreateFootstepClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.11f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            AudioClip clip = AudioClip.Create("FootstepProcedural", sampleCount, 1, sampleRate, false);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)samples.Length;
                float envelope = Mathf.Exp(-t * 24f);
                float thump = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(110f, 70f, t) * i / sampleRate);
                float noise = Random.Range(-1f, 1f) * 0.18f;
                samples[i] = (thump * 0.65f + noise) * envelope;
            }
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
