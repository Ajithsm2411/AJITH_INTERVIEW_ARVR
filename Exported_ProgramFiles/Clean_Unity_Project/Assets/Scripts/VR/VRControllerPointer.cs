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
    /// VR Controller Laser Pointer that projects a beam from the VR controller / hand
    /// and allows interacting with the Virtual Screen and 3D objects in the room.
    /// Supports Oculus/Meta Quest, HTC Vive, Index, OpenXR controllers, and Desktop Simulator.
    /// </summary>
    public class VRControllerPointer : MonoBehaviour
    {
        public bool isLeftHand = false;
        public float maxRayDistance = 8.0f;
        public LayerMask interactableLayer = ~0;

        [Header("Visuals")]
        public Color normalBeamColor = new Color(0.0f, 0.85f, 1.0f, 0.6f);
        public Color hoverBeamColor = new Color(0.2f, 1.0f, 0.45f, 0.9f);
        public Color clickBeamColor = new Color(1.0f, 0.9f, 0.1f, 1.0f);

        private LineRenderer lineRenderer;
        private GameObject reticle;
        private Renderer reticleRenderer;

        public bool isHovering { get; private set; }
        public RaycastHit currentHit { get; private set; }
        public VRSpatialVirtualScreen.VRVirtualButton hoveredButton { get; private set; }

        private bool wasTriggerPressed = false;
        private XRInputDevice xrControllerDevice;

        private void Awake()
        {
            SetupLaserVisuals();
        }

        private void SetupLaserVisuals()
        {
            // Laser Line
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();

            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.008f;
            lineRenderer.endWidth = 0.003f;

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null) unlit = Shader.Find("Unlit/Color");
            if (unlit == null) unlit = Shader.Find("Sprites/Default");

            Material beamMat = new Material(unlit);
            beamMat.color = normalBeamColor;
            lineRenderer.material = beamMat;

            // Reticle dot
            reticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reticle.name = isLeftHand ? "LaserReticle_L" : "LaserReticle_R";
            reticle.transform.SetParent(transform, false);
            reticle.transform.localScale = Vector3.one * 0.035f;

            Collider c = reticle.GetComponent<Collider>();
            if (c != null) Destroy(c);

            reticleRenderer = reticle.GetComponent<MeshRenderer>();
            if (reticleRenderer != null)
            {
                Material reticleMat = new Material(unlit);
                reticleMat.color = normalBeamColor;
                reticleRenderer.material = reticleMat;
            }
        }

        private void Update()
        {
            UpdateXRDevice();
            PerformRaycast();
            HandleTriggerInput();
        }

        private void UpdateXRDevice()
        {
            if (!xrControllerDevice.isValid)
            {
                var charFlags = isLeftHand ? (InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Left)
                                           : (InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Right);
                var devices = new System.Collections.Generic.List<XRInputDevice>();
                InputDevices.GetDevicesWithCharacteristics(charFlags, devices);
                if (devices.Count > 0)
                {
                    xrControllerDevice = devices[0];
                }
            }
        }

        private void PerformRaycast()
        {
            Ray ray = new Ray(transform.position, transform.forward);
            isHovering = false;
            hoveredButton = null;

            Vector3 endPoint = ray.origin + ray.direction * maxRayDistance;

            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, interactableLayer, QueryTriggerInteraction.Collide))
            {
                currentHit = hit;
                endPoint = hit.point;
                isHovering = true;

                // Check for Virtual Screen button
                var btn = hit.collider.GetComponent<VRSpatialVirtualScreen.VRVirtualButton>();
                if (btn != null)
                {
                    hoveredButton = btn;
                    btn.OnLaserHover(this);
                }
            }

            // Update Line Renderer
            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, endPoint);

                Color targetCol = isHovering ? (hoveredButton != null ? hoverBeamColor : normalBeamColor) : normalBeamColor;
                if (wasTriggerPressed) targetCol = clickBeamColor;

                lineRenderer.startColor = targetCol;
                lineRenderer.endColor = new Color(targetCol.r, targetCol.g, targetCol.b, 0.15f);
                if (lineRenderer.material != null) lineRenderer.material.color = targetCol;
            }

            // Update Reticle
            if (reticle != null)
            {
                reticle.transform.position = endPoint;
                if (reticleRenderer != null && reticleRenderer.material != null)
                {
                    reticleRenderer.material.color = isHovering ? hoverBeamColor : normalBeamColor;
                }
                reticle.SetActive(isHovering);
            }
        }

        private void HandleTriggerInput()
        {
            bool triggerPressed = false;

            // 1. Hardware VR Controller Trigger (Oculus Quest, OpenXR, Vive, Index)
            if (xrControllerDevice.isValid)
            {
                if (xrControllerDevice.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool tb) && tb)
                    triggerPressed = true;
                else if (xrControllerDevice.TryGetFeatureValue(XRCommonUsages.trigger, out float tv) && tv > 0.45f)
                    triggerPressed = true;
                else if (xrControllerDevice.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool pb) && pb)
                    triggerPressed = true;
            }

            // 2. Unity Input System XR Controller
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null && (isLeftHand ? mouse.rightButton.isPressed : mouse.leftButton.isPressed))
            {
                triggerPressed = true;
            }
#else
            if (isLeftHand ? Input.GetMouseButton(1) : Input.GetMouseButton(0))
            {
                triggerPressed = true;
            }
#endif

            // Trigger Down event
            if (triggerPressed && !wasTriggerPressed)
            {
                OnTriggerClick();
            }

            wasTriggerPressed = triggerPressed;
        }

        private void OnTriggerClick()
        {
            // Send haptic impulse to VR controller if supported
            if (xrControllerDevice.isValid)
            {
                if (xrControllerDevice.TryGetHapticCapabilities(out HapticCapabilities caps) && caps.supportsImpulse)
                {
                    xrControllerDevice.SendHapticImpulse(0u, 0.5f, 0.1f);
                }
            }

            // Trigger Button click on Virtual Screen
            if (hoveredButton != null)
            {
                hoveredButton.OnLaserClick(this);
            }
            else
            {
                var roomCtrl = Object.FindAnyObjectByType<InterviewRoomController>();
                if (roomCtrl != null)
                {
                    // VR Controller trigger shortcut during voice answering
                    if (roomCtrl.currentDialoguePhase == DialoguePhase.UserVoiceRecording)
                    {
                        roomCtrl.FinishVoiceAnswerRecording();
                        return;
                    }
                    if (roomCtrl.currentDialoguePhase == DialoguePhase.UserVoiceReview)
                    {
                        roomCtrl.SubmitVoiceAnswer();
                        return;
                    }

                    if (currentHit.collider != null)
                    {
                        if (roomCtrl.IsInterviewerObject(currentHit.collider.transform))
                        {
                            if (roomCtrl.activeRole == InterviewRole.Interviewer)
                            {
                                roomCtrl.NextDialogue();
                            }
                            else
                            {
                                roomCtrl.SelectRole(InterviewRole.Interviewer);
                            }
                        }
                        else if (roomCtrl.IsCandidateObject(currentHit.collider.transform))
                        {
                            if (roomCtrl.activeRole == InterviewRole.Interviewer)
                            {
                                roomCtrl.NextDialogue();
                            }
                            else
                            {
                                roomCtrl.SelectRole(InterviewRole.Candidate);
                            }
                        }
                    }
                    else
                    {
                        // Pulling trigger in empty space also advances dialogue or asks next question
                        roomCtrl.NextDialogue();
                    }
                }
            }
        }
    }
}
