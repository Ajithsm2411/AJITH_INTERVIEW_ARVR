using System;
using System.Collections.Generic;
using UnityEngine;

namespace InterviewRoom.VR
{
    /// <summary>
    /// Interactive 3D Virtual Screen placed in the virtual interview room.
    /// When running in VR goggles, this screen displays live interview telepresence,
    /// questions, avatar dialogue, mic audio VU meters, and interactive 3D spatial buttons
    /// that can be clicked with the VR controller laser pointers.
    /// </summary>
    public class VRSpatialVirtualScreen : MonoBehaviour
    {
        [Header("References")]
        public InterviewRoomController roomController;
        public VRGogglesRig vrRig;

        [Header("Screen Settings")]
        public float screenWidth = 1.45f;
        public float screenHeight = 1.05f;
        public float screenCurveAngle = 12f;

        [Header("Materials")]
        public Material frameMaterial;
        public Material screenGlassMaterial;
        public Material buttonNormalMat;
        public Material buttonHoverMat;
        public Material buttonActiveMat;
        public Material vuGreenMat;
        public Material vuYellowMat;
        public Material vuRedMat;

        // UI Components on Virtual Screen
        private TextMesh titleTextMesh;
        private TextMesh statusTextMesh;
        private TextMesh interviewerTextMesh;
        private TextMesh candidateTextMesh;
        private List<GameObject> vuMeterBars = new List<GameObject>();

        private Transform buttonContainer;
        private VRVirtualButton btnCandidateSeat;
        private VRVirtualButton btnInterviewerSeat;
        private VRVirtualButton btnFreeRoam;
        private VRVirtualButton btnNextDialogue;
        private VRVirtualButton btnToggleHolograms;
        private VRVirtualButton btnToggleMic;
        private VRVirtualButton btnRecenterVR;
        private VRVirtualButton btnDistanceToggle;

        private float currentDistanceOffset = 0f;
        private Vector3 baseLocalPosition;

        private void Awake()
        {
            // Remove the virtual screen between interviewer and candidate
            gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        private void Start()
        {
            gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        private void Update()
        {
        }

        public void BuildVirtualScreenHierarchy()
        {
            // Clear any previous generated children
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying) Destroy(transform.GetChild(i).gameObject);
                else DestroyImmediate(transform.GetChild(i).gameObject);
            }
            vuMeterBars.Clear();

            InitScreenMaterials();

            // 1. Sleek Outer Bezel Frame (Slim profile)
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Screen_Bezel_Frame";
            frame.transform.SetParent(transform, false);
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localScale = new Vector3(screenWidth + 0.05f, screenHeight + 0.05f, 0.025f);
            ApplyMaterial(frame, frameMaterial);

            // 2. Translucent Curved Glass Screen Display
            GameObject screenDisplay = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screenDisplay.name = "Screen_Glass_Display";
            screenDisplay.transform.SetParent(transform, false);
            screenDisplay.transform.localPosition = new Vector3(0, 0, 0.008f);
            screenDisplay.transform.localScale = new Vector3(screenWidth, screenHeight, 0.008f);
            ApplyMaterial(screenDisplay, screenGlassMaterial);

            // 3. Top Glowing Header Light Strip
            GameObject headerStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headerStrip.name = "Header_Glow_Strip";
            headerStrip.transform.SetParent(transform, false);
            headerStrip.transform.localPosition = new Vector3(0, (screenHeight * 0.5f) - 0.012f, 0.018f);
            headerStrip.transform.localScale = new Vector3(screenWidth * 0.95f, 0.010f, 0.010f);
            ApplyMaterial(headerStrip, buttonHoverMat);

            // 4. Header Titles
            GameObject titleObj = new GameObject("Screen_Header_Title");
            titleObj.transform.SetParent(transform, false);
            titleObj.transform.localPosition = new Vector3(-screenWidth * 0.46f, (screenHeight * 0.5f) - 0.045f, 0.022f);
            titleTextMesh = titleObj.AddComponent<TextMesh>();
            titleTextMesh.text = "VR INTERVIEW TRANSCRIPTION & CONTROLS";
            titleTextMesh.fontSize = 17;
            titleTextMesh.characterSize = 0.0052f;
            titleTextMesh.color = new Color(0.2f, 0.95f, 1f);
            titleTextMesh.fontStyle = FontStyle.Bold;

            // Status Bar
            GameObject statusObj = new GameObject("Screen_Header_Status");
            statusObj.transform.SetParent(transform, false);
            statusObj.transform.localPosition = new Vector3(-screenWidth * 0.46f, (screenHeight * 0.5f) - 0.092f, 0.022f);
            statusTextMesh = statusObj.AddComponent<TextMesh>();
            statusTextMesh.text = "VR TRACKING: 6-DoF ACTIVE  |  SEAT: CANDIDATE  |  MIC: LIVE";
            statusTextMesh.fontSize = 13;
            statusTextMesh.characterSize = 0.0040f;
            statusTextMesh.color = new Color(0.3f, 1.0f, 0.5f);

            // 5. Dialogue Captions Box
            BuildDialogueDisplayBoxes();

            // 6. Interactive 3D Spatial Buttons (2 Sleek Rows)
            BuildSpatialVRButtons();

            // 7. Dynamic 3D VU Meter on Screen Bottom
            BuildScreenVUMeter();
        }

        private void InitScreenMaterials()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (lit == null) lit = Shader.Find("Standard");
            if (unlit == null) unlit = Shader.Find("Unlit/Color");

            if (frameMaterial == null)
            {
                frameMaterial = new Material(lit);
                frameMaterial.color = new Color(0.08f, 0.10f, 0.14f);
                frameMaterial.SetFloat("_Metallic", 0.9f);
                frameMaterial.SetFloat("_Smoothness", 0.85f);
            }

            if (screenGlassMaterial == null)
            {
                screenGlassMaterial = new Material(lit);
                screenGlassMaterial.color = new Color(0.02f, 0.05f, 0.10f, 0.65f); // Translucent sleek dark glass
                screenGlassMaterial.SetFloat("_Smoothness", 0.95f);
                screenGlassMaterial.SetFloat("_Surface", 1); // Transparent
                screenGlassMaterial.SetFloat("_Blend", 0);
                screenGlassMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                screenGlassMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                screenGlassMaterial.SetInt("_ZWrite", 0);
                screenGlassMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                screenGlassMaterial.EnableKeyword("_ALPHABLEND_ON");
                screenGlassMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                if (screenGlassMaterial.HasProperty("_EmissionColor"))
                {
                    screenGlassMaterial.EnableKeyword("_EMISSION");
                    screenGlassMaterial.SetColor("_EmissionColor", new Color(0.01f, 0.04f, 0.08f));
                }
            }

            if (buttonNormalMat == null)
            {
                buttonNormalMat = new Material(lit);
                buttonNormalMat.color = new Color(0.12f, 0.20f, 0.30f);
                buttonNormalMat.SetFloat("_Metallic", 0.7f);
                buttonNormalMat.SetFloat("_Smoothness", 0.8f);
                buttonNormalMat.EnableKeyword("_EMISSION");
                buttonNormalMat.SetColor("_EmissionColor", new Color(0.05f, 0.22f, 0.40f));
            }

            if (buttonHoverMat == null)
            {
                buttonHoverMat = new Material(lit);
                buttonHoverMat.color = new Color(0.0f, 0.75f, 0.95f);
                buttonHoverMat.EnableKeyword("_EMISSION");
                buttonHoverMat.SetColor("_EmissionColor", new Color(0.0f, 0.85f, 1.0f) * 1.8f);
            }

            if (buttonActiveMat == null)
            {
                buttonActiveMat = new Material(lit);
                buttonActiveMat.color = new Color(0.1f, 0.9f, 0.35f);
                buttonActiveMat.EnableKeyword("_EMISSION");
                buttonActiveMat.SetColor("_EmissionColor", new Color(0.15f, 0.95f, 0.4f) * 2.0f);
            }

            if (vuGreenMat == null)
            {
                vuGreenMat = new Material(unlit);
                vuGreenMat.color = new Color(0.1f, 1f, 0.4f);
            }
            if (vuYellowMat == null)
            {
                vuYellowMat = new Material(unlit);
                vuYellowMat.color = new Color(1f, 0.85f, 0.1f);
            }
            if (vuRedMat == null)
            {
                vuRedMat = new Material(unlit);
                vuRedMat.color = new Color(1f, 0.2f, 0.2f);
            }
        }

        private void BuildDialogueDisplayBoxes()
        {
            // Interviewer Speech Display Box
            GameObject qBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            qBox.name = "Interviewer_Dialogue_Panel";
            qBox.transform.SetParent(transform, false);
            qBox.transform.localPosition = new Vector3(0, 0.22f, 0.015f);
            qBox.transform.localScale = new Vector3(screenWidth * 0.94f, 0.27f, 0.008f);
            ApplyMaterial(qBox, buttonNormalMat);

            GameObject qTextObj = new GameObject("Interviewer_Text");
            qTextObj.transform.SetParent(transform, false);
            qTextObj.transform.localPosition = new Vector3(-screenWidth * 0.44f, 0.33f, 0.024f);
            interviewerTextMesh = qTextObj.AddComponent<TextMesh>();
            interviewerTextMesh.text = "<b>DR. VANCE (INTERVIEWER):</b>\n\"Welcome Alex! Let's explore your background in Spatial VR systems.\"";
            interviewerTextMesh.fontSize = 28;
            interviewerTextMesh.characterSize = 0.0068f;
            interviewerTextMesh.color = new Color(1.0f, 0.92f, 0.2f);
            interviewerTextMesh.fontStyle = FontStyle.Bold;

            // Candidate Speech Display Box
            GameObject aBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            aBox.name = "Candidate_Dialogue_Panel";
            aBox.transform.SetParent(transform, false);
            aBox.transform.localPosition = new Vector3(0, -0.07f, 0.015f);
            aBox.transform.localScale = new Vector3(screenWidth * 0.94f, 0.27f, 0.008f);
            ApplyMaterial(aBox, buttonNormalMat);

            GameObject aTextObj = new GameObject("Candidate_Text");
            aTextObj.transform.SetParent(transform, false);
            aTextObj.transform.localPosition = new Vector3(-screenWidth * 0.44f, 0.04f, 0.024f);
            candidateTextMesh = aTextObj.AddComponent<TextMesh>();
            candidateTextMesh.text = "<b>ALEX MERCER (CANDIDATE IN VR):</b>\n\"Thanks Dr. Vance! Excited to demo real-time volumetric streaming and 6-DoF telepresence.\"";
            candidateTextMesh.fontSize = 28;
            candidateTextMesh.characterSize = 0.0068f;
            candidateTextMesh.color = new Color(0.35f, 0.95f, 1f);
            candidateTextMesh.fontStyle = FontStyle.Bold;
        }

        private void BuildSpatialVRButtons()
        {
            GameObject container = new GameObject("VR_Buttons_Row");
            container.transform.SetParent(transform, false);
            container.transform.localPosition = new Vector3(0, 0, 0.020f);
            buttonContainer = container.transform;

            float btnWidth = 0.27f;
            float btnHeight = 0.075f;
            float spacingX = 0.30f;
            float startX = -0.45f;

            // Row 1: Camera / Recenter Navigation (y = -0.275f)
            float row1Y = -0.275f;
            btnCandidateSeat = CreateVRButton(container.transform, new Vector3(startX + spacingX * 0, row1Y, 0), btnWidth, btnHeight, "Candidate\nVR Seat", () =>
            {
                if (roomController != null) roomController.SelectRole(InterviewRole.Candidate);
                else if (vrRig != null) vrRig.SetVRViewMode(VRGogglesRig.VRRigSeat.CandidateChair);
            });

            btnInterviewerSeat = CreateVRButton(container.transform, new Vector3(startX + spacingX * 1, row1Y, 0), btnWidth, btnHeight, "Interviewer\nSeat", () =>
            {
                if (roomController != null) roomController.SelectRole(InterviewRole.Interviewer);
                else if (vrRig != null) vrRig.SetVRViewMode(VRGogglesRig.VRRigSeat.InterviewerChair);
            });

            btnFreeRoam = CreateVRButton(container.transform, new Vector3(startX + spacingX * 2, row1Y, 0), btnWidth, btnHeight, "Free-Roam\nWalk", () =>
            {
                if (vrRig != null) vrRig.SetVRViewMode(VRGogglesRig.VRRigSeat.FreeRoam);
                else if (roomController != null) roomController.SwitchView(CameraViewMode.FreeFly);
            });

            btnRecenterVR = CreateVRButton(container.transform, new Vector3(startX + spacingX * 3, row1Y, 0), btnWidth, btnHeight, "Recenter\nVR Tracking", () =>
            {
                if (vrRig != null) vrRig.RecenterVRTracking();
            });

            // Row 2: Dialogue & Audio Controls (y = -0.370f)
            float row2Y = -0.370f;
            btnNextDialogue = CreateVRButton(container.transform, new Vector3(startX + spacingX * 0, row2Y, 0), btnWidth, btnHeight, "Next Q&A\nDialogue", () =>
            {
                if (roomController != null) roomController.NextDialogue();
            });

            btnToggleHolograms = CreateVRButton(container.transform, new Vector3(startX + spacingX * 1, row2Y, 0), btnWidth, btnHeight, "AR Hologram\n[Toggle]", () =>
            {
                if (roomController != null) roomController.ToggleARHolograms();
            });

            btnToggleMic = CreateVRButton(container.transform, new Vector3(startX + spacingX * 2, row2Y, 0), btnWidth, btnHeight, "Mic Mute\n[Toggle]", () =>
            {
                if (roomController != null && roomController.audioController != null)
                {
                    roomController.audioController.ToggleMute();
                }
            });

            btnDistanceToggle = CreateVRButton(container.transform, new Vector3(startX + spacingX * 3, row2Y, 0), btnWidth, btnHeight, "Screen Pos\nNear / Far", () =>
            {
                ToggleScreenDistance();
            });
        }

        private void BuildScreenVUMeter()
        {
            GameObject vuRoot = new GameObject("Screen_VUMeter_Bar");
            vuRoot.transform.SetParent(transform, false);
            vuRoot.transform.localPosition = new Vector3(0, -(screenHeight * 0.5f) + 0.025f, 0.018f);

            int barCount = 24;
            float totalWidth = screenWidth * 0.88f;
            float step = totalWidth / barCount;
            float barW = step * 0.75f;
            float barH = 0.018f;

            for (int i = 0; i < barCount; i++)
            {
                float x = -totalWidth * 0.5f + i * step + step * 0.5f;
                GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.name = $"VU_Segment_{i}";
                bar.transform.SetParent(vuRoot.transform, false);
                bar.transform.localPosition = new Vector3(x, 0, 0);
                bar.transform.localScale = new Vector3(barW, barH, 0.006f);

                Material segmentMat = vuGreenMat;
                if (i > barCount * 0.65f) segmentMat = vuYellowMat;
                if (i > barCount * 0.85f) segmentMat = vuRedMat;
                ApplyMaterial(bar, segmentMat);

                vuMeterBars.Add(bar);
            }
        }

        private void ToggleScreenDistance()
        {
            if (currentDistanceOffset == 0f)
            {
                currentDistanceOffset = -0.45f; // Closer to VR user
            }
            else
            {
                currentDistanceOffset = 0f; // Default room position
            }
            transform.localPosition = baseLocalPosition + transform.forward * currentDistanceOffset;
        }

        private string customInterviewerTranscript = null;
        private string customCandidateTranscript = null;

        public void SetCustomInterviewerTranscript(string text)
        {
            customInterviewerTranscript = text;
            if (interviewerTextMesh != null) interviewerTextMesh.text = WrapScreenText(text, 44);
        }

        public void SetCustomCandidateTranscript(string text)
        {
            customCandidateTranscript = text;
            if (candidateTextMesh != null) candidateTextMesh.text = WrapScreenText(text, 44);
        }

        private string WrapScreenText(string text, int maxCharsPerLine = 44)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string[] lines = text.Split(new[] { "\n" }, System.StringSplitOptions.None);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int l = 0; l < lines.Length; l++)
            {
                string line = lines[l];
                if (line.Length <= maxCharsPerLine)
                {
                    sb.AppendLine(line);
                    continue;
                }

                string[] words = line.Split(' ');
                int curLen = 0;
                for (int w = 0; w < words.Length; w++)
                {
                    if (curLen + words[w].Length + 1 > maxCharsPerLine && curLen > 0)
                    {
                        sb.AppendLine();
                        curLen = 0;
                    }
                    sb.Append((curLen == 0 ? "" : " ") + words[w]);
                    curLen += words[w].Length + 1;
                }
                if (l < lines.Length - 1) sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        public void ClearCustomTranscripts()
        {
            customInterviewerTranscript = null;
            customCandidateTranscript = null;
        }

        private void UpdateVirtualScreenContent()
        {
            if (roomController == null) return;

            // Update dialogue strings if not actively driven by STT stream
            if (interviewerTextMesh != null && string.IsNullOrEmpty(customInterviewerTranscript))
            {
                string qText = roomController.isDialogueActive
                    ? $"<b>DR. VANCE (INTERVIEWER):</b>\n\"How do you optimize 6-DoF tracking and reduce motion-to-photon latency in multi-user VR?\""
                    : $"<b>DR. VANCE (INTERVIEWER):</b>\n\"Welcome Alex! Ready to demonstrate the VR Goggles telepresence setup?\"";

                if (roomController.currentQuestionIndex == 0 && roomController.isDialogueActive)
                    qText = "<b>DR. VANCE:</b> \"Welcome Alex! Could you introduce your background in Spatial Computing and AR telepresence?\"";
                else if (roomController.currentQuestionIndex == 1)
                    qText = "<b>DR. VANCE:</b> \"How do you optimize 6-DoF tracking and reduce motion-to-photon latency in multi-user VR?\"";
                else if (roomController.currentQuestionIndex == 2)
                    qText = "<b>DR. VANCE:</b> \"Walk us through how you would architect real-time volumetric avatar streaming over WebRTC.\"";
                else if (roomController.currentQuestionIndex == 3)
                    qText = "<b>DR. VANCE:</b> \"Impressive solution. Do you have any questions for us regarding our Spatial Operating System?\"";

                interviewerTextMesh.text = qText;
            }

            if (candidateTextMesh != null && string.IsNullOrEmpty(customCandidateTranscript))
            {
                string aText = "<b>ALEX (CANDIDATE IN VR):</b>\n\"Connected via Oculus/OpenXR VR Headset @ 120Hz. All telepresence anchors locked.\"";
                if (roomController.currentQuestionIndex == 0 && roomController.isDialogueActive)
                    aText = "<b>ALEX (VR CANDIDATE):</b> \"Thanks Dr. Vance! I specialize in real-time neural rendering and interactive spatial interfaces.\"";
                else if (roomController.currentQuestionIndex == 1)
                    aText = "<b>ALEX (VR CANDIDATE):</b> \"We leverage predictive pose estimation, async timewarp, and foveated rendering to hold 12ms round-trip.\"";
                else if (roomController.currentQuestionIndex == 2)
                    aText = "<b>ALEX (VR CANDIDATE):</b> \"I partition mesh LODs with client-side interpolation and stream audio-synchronized spatial blendshapes.\"";
                else if (roomController.currentQuestionIndex == 3)
                    aText = "<b>ALEX (VR CANDIDATE):</b> \"Yes! I'd love to learn more about your real-time spatial physics engine and neural gesture pipelines.\"";

                candidateTextMesh.text = aText;
            }

            // Update status string
            if (statusTextMesh != null)
            {
                bool isMuted = roomController.audioController != null && roomController.audioController.isMuted;
                string micStatus = isMuted ? "<color=#ff4444>[MIC: MUTED]</color>" : "<color=#44ff77>[MIC: LIVE 48kHz]</color>";
                string vrStatus = (vrRig != null && vrRig.isVRHardwareActive) ? "<color=#00e5ff>VR GOGGLES: DETECTED (6-DoF)</color>" : "<color=#ffd700>VR MODE: SIMULATOR / ACTIVE</color>";
                statusTextMesh.text = $"{vrStatus}  |  {micStatus}  |  AR HOLOGRAMS: {(roomController.arHologramActive ? "ON" : "OFF")}";
            }
        }

        private void UpdateVUMeterBars()
        {
            if (vuMeterBars == null || vuMeterBars.Count == 0) return;
            if (roomController == null || roomController.audioController == null) return;

            float loudness = roomController.audioController.isMuted ? 0f : Mathf.Clamp01(roomController.audioController.GetActiveOutputLoudness() * 3.5f);
            int activeBars = Mathf.RoundToInt(loudness * vuMeterBars.Count);

            for (int i = 0; i < vuMeterBars.Count; i++)
            {
                if (vuMeterBars[i] != null)
                {
                    vuMeterBars[i].SetActive(i < activeBars);
                }
            }
        }

        private VRVirtualButton CreateVRButton(Transform parent, Vector3 localPos, float width, float height, string label, Action onClick)
        {
            GameObject btnObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            btnObj.name = $"VR_Btn_{label.Replace("\n", "_")}";
            btnObj.transform.SetParent(parent, false);
            btnObj.transform.localPosition = localPos;
            btnObj.transform.localScale = new Vector3(width, height, 0.02f);
            ApplyMaterial(btnObj, buttonNormalMat);

            // Add Text
            GameObject txtObj = new GameObject("BtnLabel");
            txtObj.transform.SetParent(btnObj.transform, false);
            txtObj.transform.localPosition = new Vector3(0, 0, -0.6f);
            TextMesh tm = txtObj.AddComponent<TextMesh>();
            tm.text = label;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.fontSize = 18;
            tm.characterSize = 0.0055f;
            tm.color = Color.white;

            var button = btnObj.AddComponent<VRVirtualButton>();
            button.labelMesh = tm;
            button.normalMaterial = buttonNormalMat;
            button.hoverMaterial = buttonHoverMat;
            button.clickMaterial = buttonActiveMat;
            button.onClickedAction = onClick;
            button.audioController = roomController != null ? roomController.audioController : null;

            return button;
        }

        private void ApplyMaterial(GameObject go, Material mat)
        {
            if (go != null && mat != null)
            {
                var r = go.GetComponent<Renderer>();
                if (r != null) r.material = mat;
            }
        }

        /// <summary>
        /// Interactive Button on the Virtual Screen responsive to VR Controller Laser
        /// pointers and Desktop mouse clicks.
        /// </summary>
        public class VRVirtualButton : MonoBehaviour
        {
            public TextMesh labelMesh;
            public Material normalMaterial;
            public Material hoverMaterial;
            public Material clickMaterial;
            public Action onClickedAction;
            public InterviewRoomAudioController audioController;

            private Renderer btnRenderer;
            private bool isHovered = false;
            private float hoverTimeout = 0f;

            private void Awake()
            {
                btnRenderer = GetComponent<Renderer>();
            }

            private void Update()
            {
                if (isHovered)
                {
                    hoverTimeout -= Time.deltaTime;
                    if (hoverTimeout <= 0f)
                    {
                        isHovered = false;
                        if (btnRenderer != null && normalMaterial != null)
                        {
                            btnRenderer.material = normalMaterial;
                        }
                    }
                }
            }

            public void OnLaserHover(VRControllerPointer pointer)
            {
                isHovered = true;
                hoverTimeout = 0.15f;
                if (btnRenderer != null && hoverMaterial != null)
                {
                    btnRenderer.material = hoverMaterial;
                }
            }

            public void OnLaserClick(VRControllerPointer pointer)
            {
                if (btnRenderer != null && clickMaterial != null)
                {
                    btnRenderer.material = clickMaterial;
                }

                if (audioController != null)
                {
                    audioController.PlayUIChime();
                }

                onClickedAction?.Invoke();

                CancelInvoke(nameof(ResetMaterial));
                Invoke(nameof(ResetMaterial), 0.25f);
            }

            private void ResetMaterial()
            {
                if (btnRenderer != null)
                {
                    btnRenderer.material = isHovered ? hoverMaterial : normalMaterial;
                }
            }
        }
    }
}
