using UnityEngine;

namespace InterviewRoom.SpeechToText
{
    /// <summary>
    /// Floating 3D holographic speech bubble mounted above the avatar's head.
    /// Billboards to face the VR user / camera and displays live transcribed speech-to-text
    /// in real-time as words and sentences are spoken.
    /// </summary>
    public class AvatarSpeechBubble3D : MonoBehaviour
    {
        [Header("Target & Positioning")]
        public Transform avatarHead;
        public Vector3 offset = new Vector3(0, 0.45f, 0);
        public Camera targetCamera;

        [Header("Styling")]
        public string speakerName = "Speaker";
        public Color headerColor = new Color(0.2f, 0.95f, 1.0f);
        public Color textColor = Color.white;

        private GameObject panelObj;
        private TextMesh headerTextMesh;
        private TextMesh bodyTextMesh;
        private Renderer panelRenderer;

        private bool isVisible = false;
        private float autoHideTimer = 0f;

        private void Awake()
        {
            BuildSpeechBubbleVisuals();
            SetVisibility(false);
        }

        private void Start()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void LateUpdate()
        {
            // Follow avatar head position
            if (avatarHead != null)
            {
                transform.position = avatarHead.position + offset;
            }

            // Billboard towards VR headset / camera (upright yaw billboarding)
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera != null)
            {
                Vector3 camDir = transform.position - targetCamera.transform.position;
                camDir.y = 0f;
                if (camDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(camDir);
                }
            }

            // Auto-hide after inactivity
            if (isVisible && autoHideTimer > 0f)
            {
                autoHideTimer -= Time.deltaTime;
                if (autoHideTimer <= 0f)
                {
                    SetVisibility(false);
                }
            }
        }

        private void BuildSpeechBubbleVisuals()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = Shader.Find("Standard");

            // 1. Translucent Backing Panel
            panelObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panelObj.name = "SpeechBubble_Backdrop";
            panelObj.transform.SetParent(transform, false);
            panelObj.transform.localPosition = Vector3.zero;
            panelObj.transform.localScale = new Vector3(1.65f, 0.55f, 0.015f);

            Destroy(panelObj.GetComponent<Collider>());
            panelRenderer = panelObj.GetComponent<MeshRenderer>();

            Material bgMat = new Material(lit);
            bgMat.color = new Color(0.04f, 0.08f, 0.14f, 0.90f);
            bgMat.SetFloat("_Metallic", 0.6f);
            bgMat.SetFloat("_Smoothness", 0.8f);
            if (bgMat.HasProperty("_EmissionColor"))
            {
                bgMat.EnableKeyword("_EMISSION");
                bgMat.SetColor("_EmissionColor", new Color(0.02f, 0.12f, 0.22f));
            }
            panelRenderer.material = bgMat;

            // 2. Speaker Header Title
            GameObject headerObj = new GameObject("Header_Text");
            headerObj.transform.SetParent(transform, false);
            headerObj.transform.localPosition = new Vector3(-0.76f, 0.20f, -0.018f);
            headerTextMesh = headerObj.AddComponent<TextMesh>();
            headerTextMesh.text = $"[ {speakerName.ToUpper()} - SPEAKING ]";
            headerTextMesh.fontSize = 24;
            headerTextMesh.characterSize = 0.0068f;
            headerTextMesh.anchor = TextAnchor.UpperLeft;
            headerTextMesh.alignment = TextAlignment.Left;
            headerTextMesh.color = headerColor;
            headerTextMesh.fontStyle = FontStyle.Bold;

            // 3. Transcribed Speech Body Text
            GameObject bodyObj = new GameObject("Body_Text");
            bodyObj.transform.SetParent(transform, false);
            bodyObj.transform.localPosition = new Vector3(-0.76f, 0.09f, -0.018f);
            bodyTextMesh = bodyObj.AddComponent<TextMesh>();
            bodyTextMesh.text = "";
            bodyTextMesh.fontSize = 26;
            bodyTextMesh.characterSize = 0.0076f;
            bodyTextMesh.anchor = TextAnchor.UpperLeft;
            bodyTextMesh.alignment = TextAlignment.Left;
            bodyTextMesh.color = textColor;
            bodyTextMesh.fontStyle = FontStyle.Bold;
        }

        public void DisplayTranscribedSpeech(string speaker, string transcribedText, bool isComplete = false)
        {
            if (headerTextMesh != null)
            {
                headerTextMesh.text = isComplete ? $"[ {speaker.ToUpper()} - SPEECH CONVERTED ]"
                                                 : $"[ {speaker.ToUpper()} - TRANSCRIBING... ]";
            }

            if (bodyTextMesh != null)
            {
                // Format text with line breaks if long
                string formatted = FormatWrappedText(transcribedText, 38);
                bodyTextMesh.text = isComplete ? $"\"{formatted}\"" : $"\"{formatted} <color=#00e5ff>|</color>\"";
            }

            SetVisibility(true);
            autoHideTimer = isComplete ? 6.0f : 1.5f;
        }

        public void DisplayLiveRecordingStatus(string speaker, float durationSeconds, float micLoudness, bool isSpeaking)
        {
            if (headerTextMesh != null)
            {
                string recDot = (Time.time % 0.6f < 0.3f) ? "<color=#ff3344>●</color>" : "<color=#888888>○</color>";
                headerTextMesh.text = $"{recDot} [ {speaker.ToUpper()} - LIVE RECORDING ]";
            }

            if (bodyTextMesh != null)
            {
                int mins = Mathf.FloorToInt(durationSeconds / 60f);
                int secs = Mathf.FloorToInt(durationSeconds % 60f);
                int bars = 12;
                int filled = Mathf.Clamp(Mathf.RoundToInt(micLoudness * 4.5f * bars), 0, bars);
                string waveBar = new string('|', filled) + new string('.', bars - filled);
                string status = isSpeaking ? "<color=#00ff88>SPEAKING LIVE</color>" : "<color=#aaaaaa>LISTENING...</color>";

                bodyTextMesh.text = $"⏱️ {mins:D2}:{secs:D2}  |  Level: [{waveBar}]\n{status} - Speaking into VR/PC Mic...";
            }

            SetVisibility(true);
            autoHideTimer = 1.2f;
        }

        public void SetVisibility(bool visible)
        {
            isVisible = visible;
            if (panelRenderer != null) panelRenderer.enabled = visible;
            if (headerTextMesh != null) headerTextMesh.GetComponent<MeshRenderer>().enabled = visible;
            if (bodyTextMesh != null) bodyTextMesh.GetComponent<MeshRenderer>().enabled = visible;
        }

        private string FormatWrappedText(string text, int maxCharsPerLine)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string[] words = text.Split(' ');
            string result = "";
            int lineLen = 0;

            foreach (var word in words)
            {
                if (lineLen + word.Length > maxCharsPerLine)
                {
                    result += "\n";
                    lineLen = 0;
                }
                result += word + " ";
                lineLen += word.Length + 1;
            }

            return result.TrimEnd();
        }
    }
}
