using UnityEngine;

namespace EnElCamino
{
    /// <summary>
    /// Low-poly player made from scene primitives with a simple walk cycle.
    /// The limbs are authored into the scene in edit mode and animated in Play Mode.
    /// </summary>
    [ExecuteAlways]
    public class PlayerVisualAnimator : MonoBehaviour
    {
        [SerializeField] private float walkCycleSpeed = 8f;
        [SerializeField] private float limbSwing = 28f;
        [SerializeField] private float stepVolume = 0.32f;

        private CharacterController controller;
        private Transform torso;
        private Transform head;
        private Transform leftArm;
        private Transform rightArm;
        private Transform leftLeg;
        private Transform rightLeg;
        private Transform leftFoot;
        private Transform rightFoot;
        private AudioSource footstepSource;
        private AudioClip footstepClip;
        private Vector3 headRestPosition;
        private Vector3 torsoRestPosition;
        private float walkPhase;
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
            Transform existingModel = transform.Find("PlayerModel");
            if (existingModel != null)
            {
                CacheRuntimeReferences(existingModel);
                EnsureFootstepAudio();
                built = true;
                return;
            }

            Transform oldGroup = transform.Find("PlayerArms");
            if (oldGroup != null) DestroyGeneratedObject(oldGroup.gameObject);
            Transform oldHead = transform.Find("Cabeza");
            if (oldHead != null) DestroyGeneratedObject(oldHead.gameObject);

            Transform legacyVisual = transform.Find("VisualJugador");
            Renderer bodyRenderer = legacyVisual != null ? legacyVisual.GetComponent<Renderer>() : null;
            Renderer eyeRenderer = legacyVisual != null && legacyVisual.Find("OjoIzquierdo") != null
                ? legacyVisual.Find("OjoIzquierdo").GetComponent<Renderer>() : null;
            Material bodyMaterial = bodyRenderer != null ? bodyRenderer.sharedMaterial : null;
            Material eyeMaterial = eyeRenderer != null ? eyeRenderer.sharedMaterial : bodyMaterial;
            if (legacyVisual != null) legacyVisual.gameObject.SetActive(false);

            GameObject model = new GameObject("PlayerModel");
            model.transform.SetParent(transform, false);

            torso = CreatePart("Torso", PrimitiveType.Capsule, model.transform, new Vector3(0f, 1.05f, 0f), new Vector3(0.43f, 0.55f, 0.30f), bodyMaterial);
            head = CreatePart("Cabeza", PrimitiveType.Sphere, model.transform, new Vector3(0f, 1.84f, 0f), new Vector3(0.32f, 0.32f, 0.32f), bodyMaterial);
            CreatePart("OjoIzquierdo", PrimitiveType.Sphere, head, new Vector3(-0.11f, 0.03f, 0.28f), new Vector3(0.09f, 0.09f, 0.045f), eyeMaterial);
            CreatePart("OjoDerecho", PrimitiveType.Sphere, head, new Vector3(0.11f, 0.03f, 0.28f), new Vector3(0.09f, 0.09f, 0.045f), eyeMaterial);

            leftArm = CreatePivot("HombroIzquierdo", model.transform, new Vector3(-0.43f, 1.32f, 0f));
            CreatePart("BrazoIzquierdo", PrimitiveType.Capsule, leftArm, new Vector3(0f, -0.20f, 0f), new Vector3(0.12f, 0.23f, 0.12f), bodyMaterial);
            CreatePart("ManoIzquierda", PrimitiveType.Sphere, leftArm, new Vector3(0f, -0.48f, 0f), new Vector3(0.14f, 0.14f, 0.14f), bodyMaterial);

            rightArm = CreatePivot("HombroDerecho", model.transform, new Vector3(0.43f, 1.32f, 0f));
            CreatePart("BrazoDerecho", PrimitiveType.Capsule, rightArm, new Vector3(0f, -0.20f, 0f), new Vector3(0.12f, 0.23f, 0.12f), bodyMaterial);
            CreatePart("ManoDerecha", PrimitiveType.Sphere, rightArm, new Vector3(0f, -0.48f, 0f), new Vector3(0.14f, 0.14f, 0.14f), bodyMaterial);

            leftLeg = CreatePivot("CaderaIzquierda", model.transform, new Vector3(-0.19f, 0.68f, 0f));
            CreatePart("MusloIzquierdo", PrimitiveType.Capsule, leftLeg, new Vector3(0f, -0.18f, 0f), new Vector3(0.15f, 0.23f, 0.15f), bodyMaterial);
            CreatePart("PantorrillaIzquierda", PrimitiveType.Capsule, leftLeg, new Vector3(0f, -0.48f, 0f), new Vector3(0.13f, 0.20f, 0.13f), bodyMaterial);
            leftFoot = CreatePart("PieIzquierdo", PrimitiveType.Cube, leftLeg, new Vector3(0f, -0.73f, 0.11f), new Vector3(0.23f, 0.14f, 0.38f), bodyMaterial);

            rightLeg = CreatePivot("CaderaDerecha", model.transform, new Vector3(0.19f, 0.68f, 0f));
            CreatePart("MusloDerecho", PrimitiveType.Capsule, rightLeg, new Vector3(0f, -0.18f, 0f), new Vector3(0.15f, 0.23f, 0.15f), bodyMaterial);
            CreatePart("PantorrillaDerecha", PrimitiveType.Capsule, rightLeg, new Vector3(0f, -0.48f, 0f), new Vector3(0.13f, 0.20f, 0.13f), bodyMaterial);
            rightFoot = CreatePart("PieDerecho", PrimitiveType.Cube, rightLeg, new Vector3(0f, -0.73f, 0.11f), new Vector3(0.23f, 0.14f, 0.38f), bodyMaterial);

            CacheRuntimeReferences(model.transform);
            headRestPosition = head.localPosition;
            torsoRestPosition = torso.localPosition;

            EnsureFootstepAudio();
            built = true;
        }

        private void EnsureFootstepAudio()
        {
            if (!Application.isPlaying || footstepSource != null) return;
            footstepSource = GetComponent<AudioSource>();
            if (footstepSource == null) footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.spatialBlend = 0.35f;
            footstepSource.volume = stepVolume;
            footstepClip = CreateFootstepClip();
            footstepSource.clip = footstepClip;
        }

        private void CacheRuntimeReferences(Transform model)
        {
            torso = model.Find("Torso");
            head = model.Find("Cabeza");
            leftArm = model.Find("HombroIzquierdo");
            rightArm = model.Find("HombroDerecho");
            leftLeg = model.Find("CaderaIzquierda");
            rightLeg = model.Find("CaderaDerecha");
            leftFoot = leftLeg != null ? leftLeg.Find("PieIzquierdo") : null;
            rightFoot = rightLeg != null ? rightLeg.Find("PieDerecho") : null;
            if (head != null) headRestPosition = head.localPosition;
            if (torso != null) torsoRestPosition = torso.localPosition;
        }

        private Transform CreatePart(string partName, PrimitiveType primitive, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            return part.transform;
        }

        private Transform CreatePivot(string pivotName, Transform parent, Vector3 localPosition)
        {
            GameObject pivot = new GameObject(pivotName);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = localPosition;
            return pivot.transform;
        }

        private void DestroyGeneratedObject(GameObject generatedObject)
        {
            if (Application.isPlaying) Destroy(generatedObject);
            else DestroyImmediate(generatedObject);
        }

        private void AnimateWalk()
        {
            float swing = Mathf.Sin(walkPhase) * limbSwing;
            leftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
            rightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            leftArm.localRotation = Quaternion.Euler(-swing * 0.75f, 0f, -7f);
            rightArm.localRotation = Quaternion.Euler(swing * 0.75f, 0f, 7f);
            leftFoot.localRotation = Quaternion.Euler(-swing * 0.30f, 0f, 0f);
            rightFoot.localRotation = Quaternion.Euler(swing * 0.30f, 0f, 0f);
            torso.localPosition = torsoRestPosition + Vector3.up * (Mathf.Abs(Mathf.Sin(walkPhase * 2f)) * 0.025f);
            head.localPosition = headRestPosition + Vector3.up * (Mathf.Abs(Mathf.Sin(walkPhase * 2f)) * 0.04f);
        }

        private void AnimateIdle()
        {
            leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.identity, Time.deltaTime * 8f);
            rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.identity, Time.deltaTime * 8f);
            leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(0f, 0f, -7f), Time.deltaTime * 8f);
            rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(0f, 0f, 7f), Time.deltaTime * 8f);
            leftFoot.localRotation = Quaternion.Slerp(leftFoot.localRotation, Quaternion.identity, Time.deltaTime * 8f);
            rightFoot.localRotation = Quaternion.Slerp(rightFoot.localRotation, Quaternion.identity, Time.deltaTime * 8f);
            torso.localPosition = Vector3.Lerp(torso.localPosition, torsoRestPosition, Time.deltaTime * 8f);
            head.localPosition = Vector3.Lerp(head.localPosition, headRestPosition, Time.deltaTime * 8f);
        }

        private void PlayFootstep()
        {
            if (footstepSource == null || footstepClip == null) return;
            footstepSource.pitch = Random.Range(0.92f, 1.06f);
            footstepSource.PlayOneShot(footstepClip, Random.Range(0.82f, 1f));
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
