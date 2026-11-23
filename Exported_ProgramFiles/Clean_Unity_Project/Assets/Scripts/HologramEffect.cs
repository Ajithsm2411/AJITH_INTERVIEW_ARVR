using UnityEngine;

namespace InterviewRoom
{
    /// <summary>
    /// Creates a pulsating AR holographic floating effect with gentle bobbing,
    /// rotation, and emissive glow oscillation.
    /// </summary>
    public class HologramEffect : MonoBehaviour
    {
        [Header("Motion Settings")]
        public float bobSpeed = 1.5f;
        public float bobHeight = 0.03f;
        public float rotateSpeed = 15f;
        public bool rotateY = true;

        [Header("Glow & Pulse")]
        public MeshRenderer targetRenderer;
        public Color baseEmission = new Color(0f, 0.75f, 1f, 1f);
        public float pulseSpeed = 2f;
        public float minIntensity = 0.6f;
        public float maxIntensity = 1.4f;

        private Vector3 startLocalPos;
        private Material matInstance;
        private float seed;

        private void Start()
        {
            startLocalPos = transform.localPosition;
            seed = Random.Range(0f, 10f);

            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<MeshRenderer>();
            }

            if (targetRenderer != null)
            {
                matInstance = targetRenderer.material;
            }
        }

        private void Update()
        {
            float t = Time.time + seed;

            // Subtle vertical floating bob
            float yOffset = Mathf.Sin(t * bobSpeed) * bobHeight;
            transform.localPosition = startLocalPos + new Vector3(0, yOffset, 0);

            // Subtle rotation for 3D holographic icons/mesh
            if (rotateY)
            {
                transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.Self);
            }

            // Pulsing emission
            if (matInstance != null)
            {
                float pulse = Mathf.Lerp(minIntensity, maxIntensity, (Mathf.Sin(t * pulseSpeed) + 1f) * 0.5f);
                Color currentColor = baseEmission * pulse;
                matInstance.SetColor("_EmissionColor", currentColor);
            }
        }

        public void SetHologramActive(bool active)
        {
            gameObject.SetActive(active);
        }

        private void OnDestroy()
        {
            if (matInstance != null)
            {
                Destroy(matInstance);
            }
        }
    }
}
