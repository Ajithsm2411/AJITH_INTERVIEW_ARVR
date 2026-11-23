using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.XR;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

namespace InterviewRoom.VR
{
    /// <summary>
    /// Master VR Goggles Rig supporting 6-DoF Head-Mounted Displays (Oculus/Meta Quest,
    /// HTC Vive, Valve Index, OpenXR runtimes), tracked hand controllers with laser pointers,
    /// dynamic seat switching (Candidate / Interviewer / Free-Roam), and Desktop VR simulation fallback.
    /// </summary>
    public class VRGogglesRig : MonoBehaviour
    {
        public enum VRRigSeat
        {
            CandidateChair = 0,
            InterviewerChair = 1,
            FreeRoam = 2,
            DirectorView = 3
        }

        [Header("Components")]
        public Camera vrCamera;
        public Transform cameraOffset;
        public Transform leftController;
        public Transform rightController;
        public VRControllerPointer leftPointer;
        public VRControllerPointer rightPointer;
        public VRSpatialVirtualScreen virtualScreen;

        [Header("Seat Anchors")]
        public Transform candidateSeatPoint;
        public Transform interviewerSeatPoint;
        public Transform directorSeatPoint;

        [Header("State")]
        public VRRigSeat currentSeat = VRRigSeat.InterviewerChair;
        public bool isVRHardwareActive = false;
        public bool isVRModeActive = true;
        public bool showDesktopVisorOverlay = false;

        [Header("Desktop Simulation Settings")]
        public float mouseSensitivity = 2.0f;
        public float walkSpeed = 2.2f;

        private Vector3 recenterOffset = Vector3.zero;
        private Quaternion recenterRotation = Quaternion.identity;

        private XRInputDevice hmdDevice;
        private XRInputDevice leftHandDevice;
        private XRInputDevice rightHandDevice;

        private float simPitch = 0f;
        private float simYaw = 0f;

        // Avatar Head Culling
        private List<Renderer> candidateHeadRenderers = new List<Renderer>();
        private List<Renderer> interviewerHeadRenderers = new List<Renderer>();

        private float hardwarePollTimer = 0f;
        private float snapTurnCooldown = 0f;
        private bool wasGripPressed = false;
        private bool wasPrimaryButtonPressed = false;
        private bool wasSecondaryButtonPressed = false;

        private void OnEnable()
        {
            InputDevices.deviceConnected += OnXRDeviceChanged;
            InputDevices.deviceDisconnected += OnXRDeviceChanged;
        }

        private void OnDisable()
        {
            InputDevices.deviceConnected -= OnXRDeviceChanged;
            InputDevices.deviceDisconnected -= OnXRDeviceChanged;
        }

        public const float SEATED_EYE_HEIGHT = 1.20f;
        public static readonly Vector3 CANDIDATE_CHAIR_POS = new Vector3(0f, 0f, -1.25f);
        public static readonly Vector3 INTERVIEWER_CHAIR_POS = new Vector3(0f, 0f, 1.25f);
        public static readonly Quaternion CANDIDATE_ROT = Quaternion.Euler(0f, 0f, 0f); // faces +Z toward Interviewer
        public static readonly Quaternion INTERVIEWER_ROT = Quaternion.Euler(0f, 180f, 0f); // faces -Z toward Candidate

        private void OnXRDeviceChanged(XRInputDevice device)
        {
            CheckVRHardware();
        }

        private void Awake()
        {
            EnsureRigHierarchy();
            CheckVRHardware();
            CacheAvatarHeadRenderers();
        }

        private void Start()
        {
            EnsureRigHierarchy();
            BuildControllerVisualsIfMissing();
            SetVRViewMode(VRRigSeat.CandidateChair);
        }

        public void EnsureRigHierarchy()
        {
            if (cameraOffset == null)
            {
                Transform existingOffset = transform.Find("CameraOffset");
                if (existingOffset != null) cameraOffset = existingOffset;
                else
                {
                    GameObject offsetObj = new GameObject("CameraOffset");
                    offsetObj.transform.SetParent(transform, false);
                    offsetObj.transform.localPosition = new Vector3(0f, SEATED_EYE_HEIGHT, 0f);
                    offsetObj.transform.localRotation = Quaternion.identity;
                    cameraOffset = offsetObj.transform;
                }
            }

            if (vrCamera == null)
            {
                vrCamera = GetComponentInChildren<Camera>();
                if (vrCamera == null) vrCamera = Camera.main;
            }

            if (vrCamera != null && vrCamera.transform.parent != cameraOffset)
            {
                vrCamera.transform.SetParent(cameraOffset, false);
                vrCamera.transform.localPosition = Vector3.zero;
                vrCamera.transform.localRotation = Quaternion.identity;
            }

            if (leftController != null && leftController.parent != cameraOffset)
            {
                leftController.SetParent(cameraOffset, false);
            }

            if (rightController != null && rightController.parent != cameraOffset)
            {
                rightController.SetParent(cameraOffset, false);
            }
        }

        private void Update()
        {
            EnsureRigHierarchy();
            PollXRDevices();

            // Periodic re-check if VR hardware was not ready during initial Awake
            if (!isVRHardwareActive)
            {
                hardwarePollTimer += Time.deltaTime;
                if (hardwarePollTimer >= 1.5f)
                {
                    hardwarePollTimer = 0f;
                    CheckVRHardware();
                }
            }

            if (snapTurnCooldown > 0f) snapTurnCooldown -= Time.deltaTime;

            if (isVRHardwareActive)
            {
                UpdateHardwareVRTracking();
            }
            else
            {
                UpdateDesktopSimulator();
            }

            HandleVRKeyShortcuts();
        }

        public void CheckVRHardware()
        {
            isVRHardwareActive = false;

            // 1. Check UnityEngine.XR.XRSettings
            if (XRSettings.isDeviceActive && !string.IsNullOrEmpty(XRSettings.loadedDeviceName))
            {
                isVRHardwareActive = true;
                Debug.Log($"[VRGogglesRig] Active XR Goggles detected: {XRSettings.loadedDeviceName}");
            }

            // 2. Check InputDevices for HeadMounted
            var hmdList = new List<XRInputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, hmdList);
            if (hmdList.Count > 0)
            {
                hmdDevice = hmdList[0];
                isVRHardwareActive = true;
                Debug.Log($"[VRGogglesRig] Connected HMD Goggles device: {hmdDevice.name} ({hmdDevice.manufacturer})");
            }
        }

        private void PollXRDevices()
        {
            if (!leftHandDevice.isValid)
            {
                var leftDevices = new List<XRInputDevice>();
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Left, leftDevices);
                if (leftDevices.Count > 0) leftHandDevice = leftDevices[0];
            }

            if (!rightHandDevice.isValid)
            {
                var rightDevices = new List<XRInputDevice>();
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Right, rightDevices);
                if (rightDevices.Count > 0) rightHandDevice = rightDevices[0];
            }
        }

        private bool hasInitialSeatedRecenter = false;

        private void UpdateHardwareVRTracking()
        {
            EnsureRigHierarchy();

            // 6-DoF HMD Head Tracking for Meta Quest 3
            if (hmdDevice.isValid && vrCamera != null)
            {
                bool hasPos = hmdDevice.TryGetFeatureValue(XRCommonUsages.devicePosition, out Vector3 hmdPos);
                bool hasRot = hmdDevice.TryGetFeatureValue(XRCommonUsages.deviceRotation, out Quaternion hmdRot);

                if (hasPos && !hasInitialSeatedRecenter)
                {
                    hasInitialSeatedRecenter = true;
                    RecenterVRTracking();
                }

                // If camera isn't automatically updated by Unity native XR driver, update locally
                if (vrCamera.transform.parent == cameraOffset)
                {
                    if (hasPos && vrCamera.transform.localPosition == Vector3.zero)
                    {
                        vrCamera.transform.localPosition = hmdPos;
                    }
                    if (hasRot && vrCamera.transform.localRotation == Quaternion.identity)
                    {
                        vrCamera.transform.localRotation = hmdRot;
                    }
                }
            }

            // Left Controller Tracking (Meta Quest Touch Plus Left)
            if (leftHandDevice.isValid && leftController != null)
            {
                if (leftHandDevice.TryGetFeatureValue(XRCommonUsages.devicePosition, out Vector3 lPos))
                    leftController.localPosition = lPos;
                if (leftHandDevice.TryGetFeatureValue(XRCommonUsages.deviceRotation, out Quaternion lRot))
                    leftController.localRotation = lRot;
            }

            // Right Controller Tracking (Meta Quest Touch Plus Right)
            if (rightHandDevice.isValid && rightController != null)
            {
                if (rightHandDevice.TryGetFeatureValue(XRCommonUsages.devicePosition, out Vector3 rPos))
                    rightController.localPosition = rPos;
                if (rightHandDevice.TryGetFeatureValue(XRCommonUsages.deviceRotation, out Quaternion rRot))
                    rightController.localRotation = rRot;
            }

            // Touch Plus Thumbstick, Buttons & Recenter
            HandleQuestControllerButtons();
        }

        private void HandleQuestControllerButtons()
        {
            // 1. Right Thumbstick Snap Turn
            if (rightHandDevice.isValid && snapTurnCooldown <= 0f)
            {
                if (rightHandDevice.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 axis) && Mathf.Abs(axis.x) > 0.65f)
                {
                    float snapAngle = axis.x > 0 ? 45f : -45f;
                    transform.Rotate(0, snapAngle, 0);
                    snapTurnCooldown = 0.35f;
                }
            }

            // 2. Thumbstick Click: Recenter Tracking
            if (rightHandDevice.isValid)
            {
                if (rightHandDevice.TryGetFeatureValue(XRCommonUsages.primary2DAxisClick, out bool click) && click)
                {
                    RecenterVRTracking();
                }
            }

            // 3. Grip Button: Switch Seats (Alex Mercer <-> Dr. Sarah Vance)
            bool gripNow = false;
            if (rightHandDevice.isValid && rightHandDevice.TryGetFeatureValue(XRCommonUsages.gripButton, out bool rg) && rg) gripNow = true;
            if (leftHandDevice.isValid && leftHandDevice.TryGetFeatureValue(XRCommonUsages.gripButton, out bool lg) && lg) gripNow = true;

            if (gripNow && !wasGripPressed)
            {
                VRRigSeat nextSeat = (currentSeat == VRRigSeat.CandidateChair) ? VRRigSeat.InterviewerChair : VRRigSeat.CandidateChair;
                SetVRViewMode(nextSeat);
            }
            wasGripPressed = gripNow;

            // 4. Primary Button (A on Right / X on Left): Advance Question
            bool primaryNow = false;
            if (rightHandDevice.isValid && rightHandDevice.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool ra) && ra) primaryNow = true;
            if (leftHandDevice.isValid && leftHandDevice.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool lx) && lx) primaryNow = true;

            if (primaryNow && !wasPrimaryButtonPressed)
            {
                var controller = FindAnyObjectByType<InterviewRoomController>();
                if (controller != null) controller.NextQuestion();
            }
            wasPrimaryButtonPressed = primaryNow;

            // 5. Secondary Button (B on Right / Y on Left): Toggle Hologram
            bool secNow = false;
            if (rightHandDevice.isValid && rightHandDevice.TryGetFeatureValue(XRCommonUsages.secondaryButton, out bool rb) && rb) secNow = true;
            if (leftHandDevice.isValid && leftHandDevice.TryGetFeatureValue(XRCommonUsages.secondaryButton, out bool ly) && ly) secNow = true;

            if (secNow && !wasSecondaryButtonPressed)
            {
                var controller = FindAnyObjectByType<InterviewRoomController>();
                if (controller != null) controller.ToggleARHolograms();
            }
            wasSecondaryButtonPressed = secNow;
        }

        private void UpdateDesktopSimulator()
        {
            if (!isVRModeActive || vrCamera == null) return;

            // Mouse Look in VR Goggles Simulator
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            if (mouse != null && (mouse.rightButton.isPressed || showDesktopVisorOverlay))
            {
                Vector2 delta = mouse.delta.ReadValue() * (mouseSensitivity * 0.12f);
                simYaw += delta.x;
                simPitch = Mathf.Clamp(simPitch - delta.y, -80f, 80f);
                vrCamera.transform.localRotation = Quaternion.Euler(simPitch, simYaw, 0f);
            }

            // Free-Roam Walk Movement
            if (currentSeat == VRRigSeat.FreeRoam && kb != null)
            {
                Vector3 moveDir = Vector3.zero;
                if (kb.wKey.isPressed) moveDir += vrCamera.transform.forward;
                if (kb.sKey.isPressed) moveDir -= vrCamera.transform.forward;
                if (kb.aKey.isPressed) moveDir -= vrCamera.transform.right;
                if (kb.dKey.isPressed) moveDir += vrCamera.transform.right;
                moveDir.y = 0;

                if (moveDir.sqrMagnitude > 0.001f)
                {
                    transform.position += moveDir.normalized * (walkSpeed * Time.deltaTime);
                }
            }
#else
            if (Input.GetMouseButton(1) || showDesktopVisorOverlay)
            {
                simYaw += Input.GetAxis("Mouse X") * mouseSensitivity * 2.0f;
                simPitch = Mathf.Clamp(simPitch - Input.GetAxis("Mouse Y") * mouseSensitivity * 2.0f, -80f, 80f);
                vrCamera.transform.localRotation = Quaternion.Euler(simPitch, simYaw, 0f);
            }

            if (currentSeat == VRRigSeat.FreeRoam)
            {
                Vector3 moveDir = Vector3.zero;
                if (Input.GetKey(KeyCode.W)) moveDir += vrCamera.transform.forward;
                if (Input.GetKey(KeyCode.S)) moveDir -= vrCamera.transform.forward;
                if (Input.GetKey(KeyCode.A)) moveDir -= vrCamera.transform.right;
                if (Input.GetKey(KeyCode.D)) moveDir += vrCamera.transform.right;
                moveDir.y = 0;

                if (moveDir.sqrMagnitude > 0.001f)
                {
                    transform.position += moveDir.normalized * (walkSpeed * Time.deltaTime);
                }
            }
#endif

            // Update Simulated Hand Controllers positioned naturally relative to camera
            UpdateSimulatedHands();
        }

        private void UpdateSimulatedHands()
        {
            if (vrCamera == null) return;

            Vector3 camPos = vrCamera.transform.position;
            Quaternion camRot = vrCamera.transform.rotation;

            // Natural hand resting pose holding controllers forward
            if (leftController != null)
            {
                Vector3 lOffset = camRot * new Vector3(-0.20f, -0.22f, 0.45f);
                leftController.position = camPos + lOffset;
                leftController.rotation = camRot * Quaternion.Euler(8f, -4f, 0f);
            }

            if (rightController != null)
            {
                Vector3 rOffset = camRot * new Vector3(0.20f, -0.22f, 0.45f);
                rightController.position = camPos + rOffset;
                rightController.rotation = camRot * Quaternion.Euler(8f, 4f, 0f);
            }
        }

        private void HandleVRKeyShortcuts()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.vKey.wasPressedThisFrame) ToggleVRMode();
                if (kb.rKey.wasPressedThisFrame && kb.ctrlKey.isPressed) RecenterVRTracking();
            }
#else
            if (Input.GetKeyDown(KeyCode.V)) ToggleVRMode();
            if (Input.GetKeyDown(KeyCode.R) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) RecenterVRTracking();
#endif
        }

        public void ToggleVRMode()
        {
            isVRModeActive = !isVRModeActive;
            showDesktopVisorOverlay = isVRModeActive && !isVRHardwareActive;
            Debug.Log($"[VRGogglesRig] VR Mode Active: {isVRModeActive} (Hardware: {isVRHardwareActive})");
        }

        public void SetVRViewMode(VRRigSeat seat)
        {
            currentSeat = seat;
            EnsureRigHierarchy();

            Vector3 targetFloorPos;
            Quaternion targetRot;

            switch (seat)
            {
                case VRRigSeat.CandidateChair:
                    targetFloorPos = CANDIDATE_CHAIR_POS;
                    targetRot = CANDIDATE_ROT;
                    break;
                case VRRigSeat.InterviewerChair:
                    targetFloorPos = INTERVIEWER_CHAIR_POS;
                    targetRot = INTERVIEWER_ROT;
                    break;
                case VRRigSeat.DirectorView:
                    targetFloorPos = new Vector3(2.6f, 0f, -2.2f);
                    targetRot = Quaternion.Euler(0f, -48f, 0f);
                    break;
                default:
                    return;
            }

            transform.position = targetFloorPos;
            transform.rotation = targetRot;

            RecenterVRTracking();
            ApplyAvatarHeadCulling();
            AlignVirtualScreenToVRUser();
        }

        public void RecenterVRTracking()
        {
            EnsureRigHierarchy();
            simYaw = 0f;
            simPitch = 0f;

            if (isVRHardwareActive && hmdDevice.isValid)
            {
                bool hasPos = hmdDevice.TryGetFeatureValue(XRCommonUsages.devicePosition, out Vector3 hmdPos);
                bool hasRot = hmdDevice.TryGetFeatureValue(XRCommonUsages.deviceRotation, out Quaternion hmdRot);

                if (cameraOffset != null)
                {
                    // Calibrate camera offset so the user's eye level in chair equals SEATED_EYE_HEIGHT (1.20m)
                    float targetY = SEATED_EYE_HEIGHT;
                    float offsetY = hasPos ? (targetY - hmdPos.y) : targetY;
                    float offsetX = hasPos ? -hmdPos.x : 0f;
                    float offsetZ = hasPos ? -hmdPos.z : 0f;

                    cameraOffset.localPosition = new Vector3(offsetX, offsetY, offsetZ);

                    if (hasRot)
                    {
                        cameraOffset.localRotation = Quaternion.Inverse(Quaternion.Euler(0f, hmdRot.eulerAngles.y, 0f));
                    }
                    else
                    {
                        cameraOffset.localRotation = Quaternion.identity;
                    }
                }
            }
            else
            {
                // Desktop simulation: CameraOffset placed at seated eye height
                if (cameraOffset != null)
                {
                    cameraOffset.localPosition = new Vector3(0f, SEATED_EYE_HEIGHT, 0f);
                    cameraOffset.localRotation = Quaternion.identity;
                }
                if (vrCamera != null)
                {
                    vrCamera.transform.localPosition = Vector3.zero;
                    vrCamera.transform.localRotation = Quaternion.identity;
                }
            }

            Debug.Log($"[VRGogglesRig] Seated at {currentSeat}: Eye level calibrated at {SEATED_EYE_HEIGHT}m looking directly across table.");
        }

        private void AlignVirtualScreenToVRUser()
        {
            if (virtualScreen != null)
            {
                virtualScreen.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(virtualScreen.gameObject);
                else DestroyImmediate(virtualScreen.gameObject);
                virtualScreen = null;
            }
        }

        private void CacheAvatarHeadRenderers()
        {
            GameObject env = GameObject.Find("InterviewRoom_Environment");
            if (env == null) return;

            candidateHeadRenderers.Clear();
            interviewerHeadRenderers.Clear();

            // Find Candidate Head & Neck only (body, hoodie, arms, and legs stay visible looking down in chair!)
            Transform cand = env.transform.Find("Avatar_Candidate_Alex_ARVR");
            if (cand == null) cand = env.transform.Find("Avatar_Candidate");
            if (cand != null)
            {
                Transform head = cand.Find("Head");
                if (head != null) candidateHeadRenderers.AddRange(head.GetComponentsInChildren<Renderer>(true));
                Transform neck = cand.Find("Neck");
                if (neck != null) candidateHeadRenderers.AddRange(neck.GetComponentsInChildren<Renderer>(true));
            }

            // Find Interviewer Head & Neck only (suit and arms stay visible)
            Transform interv = env.transform.Find("Avatar_Interviewer_DrSarah");
            if (interv != null)
            {
                Transform head = interv.Find("Head");
                if (head != null) interviewerHeadRenderers.AddRange(head.GetComponentsInChildren<Renderer>(true));
                Transform neck = interv.Find("Neck");
                if (neck != null) interviewerHeadRenderers.AddRange(neck.GetComponentsInChildren<Renderer>(true));
            }
        }

        public void ApplyAvatarHeadCulling()
        {
            if (candidateHeadRenderers.Count == 0 || interviewerHeadRenderers.Count == 0)
            {
                CacheAvatarHeadRenderers();
            }

            // In Candidate chair: hide Candidate's Head/Neck so camera doesn't clip into face,
            // while Candidate's hoodie, arms, and sneakers remain visible looking down. Dr. Vance across table is 100% visible!
            bool hideCandidateHead = (currentSeat == VRRigSeat.CandidateChair);
            foreach (var r in candidateHeadRenderers)
            {
                if (r != null) r.enabled = !hideCandidateHead;
            }

            // In Interviewer chair: hide Interviewer's Head/Neck so camera doesn't clip into face,
            // while suit and arms remain visible. Alex Mercer across table is 100% visible!
            bool hideInterviewerHead = (currentSeat == VRRigSeat.InterviewerChair);
            foreach (var r in interviewerHeadRenderers)
            {
                if (r != null) r.enabled = !hideInterviewerHead;
            }
        }

        private void BuildControllerVisualsIfMissing()
        {
            if (leftController == null)
            {
                GameObject lObj = new GameObject("VR_Controller_Left");
                lObj.transform.SetParent(transform, false);
                leftController = lObj.transform;
            }

            if (rightController == null)
            {
                GameObject rObj = new GameObject("VR_Controller_Right");
                rObj.transform.SetParent(transform, false);
                rightController = rObj.transform;
            }

            if (leftPointer == null)
            {
                leftPointer = leftController.GetComponent<VRControllerPointer>();
                if (leftPointer == null) leftPointer = leftController.gameObject.AddComponent<VRControllerPointer>();
                leftPointer.isLeftHand = true;
            }

            if (rightPointer == null)
            {
                rightPointer = rightController.GetComponent<VRControllerPointer>();
                if (rightPointer == null) rightPointer = rightController.gameObject.AddComponent<VRControllerPointer>();
                rightPointer.isLeftHand = false;
            }

            CreateVRControllerModel(leftController, isLeft: true);
            CreateVRControllerModel(rightController, isLeft: false);
        }

        private void CreateVRControllerModel(Transform parent, bool isLeft)
        {
            if (parent.Find("Controller_Model") != null) return;

            GameObject modelRoot = new GameObject("Controller_Model");
            modelRoot.transform.SetParent(parent, false);

            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = Shader.Find("Standard");

            Material bodyMat = new Material(lit);
            bodyMat.color = new Color(0.12f, 0.14f, 0.16f);
            bodyMat.SetFloat("_Metallic", 0.85f);
            bodyMat.SetFloat("_Smoothness", 0.75f);

            Material ringMat = new Material(lit);
            ringMat.color = new Color(0.0f, 0.85f, 1.0f);
            ringMat.EnableKeyword("_EMISSION");
            ringMat.SetColor("_EmissionColor", new Color(0.0f, 0.85f, 1.0f) * 2.2f);

            // 1. Grip Handle
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "Grip";
            handle.transform.SetParent(modelRoot.transform, false);
            handle.transform.localPosition = new Vector3(0, -0.06f, -0.02f);
            handle.transform.localScale = new Vector3(0.034f, 0.06f, 0.034f);
            handle.transform.localRotation = Quaternion.Euler(18f, 0, 0);
            Destroy(handle.GetComponent<Collider>());
            handle.GetComponent<Renderer>().material = bodyMat;

            // 2. Controller Tracking Ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Tracking_Ring";
            ring.transform.SetParent(modelRoot.transform, false);
            ring.transform.localPosition = new Vector3(0, 0.025f, 0.025f);
            ring.transform.localScale = new Vector3(0.10f, 0.008f, 0.10f);
            ring.transform.localRotation = Quaternion.Euler(42f, 0, 0);
            Destroy(ring.GetComponent<Collider>());
            ring.GetComponent<Renderer>().material = ringMat;

            // 3. Trigger & Thumbstick
            GameObject trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trigger.name = "Trigger";
            trigger.transform.SetParent(modelRoot.transform, false);
            trigger.transform.localPosition = new Vector3(0, -0.025f, 0.02f);
            trigger.transform.localScale = new Vector3(0.018f, 0.025f, 0.016f);
            Destroy(trigger.GetComponent<Collider>());
            trigger.GetComponent<Renderer>().material = ringMat;

            // 4. Laser Emitter Tip
            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = "Laser_Emitter_Tip";
            tip.transform.SetParent(modelRoot.transform, false);
            tip.transform.localPosition = new Vector3(0, 0.01f, 0.06f);
            tip.transform.localScale = Vector3.one * 0.018f;
            Destroy(tip.GetComponent<Collider>());
            tip.GetComponent<Renderer>().material = ringMat;
        }

        private void OnGUI()
        {
            // Desktop VR Goggles Visor Overlay
            if (showDesktopVisorOverlay && !isVRHardwareActive)
            {
                GUIStyle visorStyle = new GUIStyle(GUI.skin.box);
                visorStyle.normal.textColor = new Color(0.2f, 0.95f, 1f);
                visorStyle.fontStyle = FontStyle.Bold;
                visorStyle.fontSize = 12;

                int w = Screen.width;
                int h = Screen.height;

                // Cyan visor corner brackets
                GUI.Box(new Rect(20, 20, 180, 28), "[ VR GOGGLES ACTIVE ]", visorStyle);
                GUI.Label(new Rect(25, 48, 300, 22), "Mouse Right-Click: Gaze Look | Click: Interact");

                // Crosshair Reticle in center
                int cx = w / 2;
                int cy = h / 2;
                GUI.color = new Color(0.0f, 0.85f, 1.0f, 0.6f);
                GUI.DrawTexture(new Rect(cx - 3, cy - 3, 6, 6), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }
    }
}
