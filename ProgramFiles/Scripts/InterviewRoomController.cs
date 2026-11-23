using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using InterviewRoom.VR;
using InterviewRoom.SpeechToText;

namespace InterviewRoom
{
    public enum InterviewRole
    {
        Interviewer = 0,
        Candidate = 1
    }

    public enum CameraViewMode
    {
        Overview = 0,
        InterviewerView = 1,
        CandidateARView = 2,
        SideBroadcast = 3,
        FreeFly = 4,
        VRGogglesMode = 5
    }

    public enum DialoguePhase
    {
        Idle = 0,
        InterviewerAskingQuestion = 1,
        UserVoiceRecording = 2,
        UserVoiceReview = 3,
        CandidateDeliveringResponse = 4,
        InterviewerAcknowledging = 5,
        CandidateSpeakingSimulated = 6,
        AnswerCompleted = 7
    }

    /// <summary>
    /// Master controller for the VR Interview Telepresence Platform:
    /// Supports two immersive perspectives: Candidate View (seated as Alex Mercer) and Interviewer View (seated as Dr. Sarah Vance)
    /// with true face-to-face spatial layout (1.3m distance across table, eye-level 1.20m).
    /// </summary>
    [ExecuteAlways]
    public class InterviewRoomController : MonoBehaviour
    {
        [Header("Role Selection & Perspective")]
        public InterviewRole activeRole = InterviewRole.Candidate;
        public float interviewTimer = 0f;
        public bool isTimerRunning = true;

        [Header("Voice Answering Mode (Real Microphone)")]
        [Tooltip("When enabled, candidate answers are recorded live from your VR headset / PC microphone!")]
        public bool answerWithOwnVoice = true;
        [Tooltip("When enabled, the interviewer automatically generates questions and conversation flows naturally!")]
        public bool autoInterviewFlow = true;
        public DialoguePhase currentDialoguePhase = DialoguePhase.Idle;
        public float userSpeechDuration = 0f;
        public string lastVoiceAnswerTranscript = "";
        public bool isCandidatePlaybackActive = false;

        [Header("Candidate Profile Dossier")]
        public string candidateName = "Alex Mercer";
        public string candidateRole = "Senior Spatial Systems Engineer";
        public string candidateExperience = "8 Years Experience";
        public int candidateMatchScore = 94;
        public string[] candidateSkills = new string[] { "WebXR", "Three.js", "OpenXR", "C# / Unity", "Real-Time Volumetric Video" };

        [Header("Interviewer Profile")]
        public string interviewerName = "Dr. Sarah Vance";
        public string interviewerTitle = "VP of Spatial Systems";

        [Header("Question Scoring Rubric (1-5 Stars)")]
        public int[] questionScores = new int[] { 5, 4, 5, 0 };

        [Header("Notes & Feedback")]
        public string interviewerNotes = "Candidate demonstrates exceptional grasp of 6-DoF tracking pipelines, predictive pose extrapolation, and WebRTC blendshape synchronization.\nClear architectural communication.";
        public string candidateNotes = "Key Architectural Points:\n• Decouple tracking loops to prevent framerate jitter\n• Predictive pose extrapolation with async timewarp\n• Synchronized blendshapes for audio visemes";
        public string[] quickEvaluationTags = new string[] { "Strong Spatial Knowledge", "Clear Architecture", "Low Latency Mindset", "Cultural Fit" };

        [Header("End Interview Modal")]
        public bool showEndInterviewModal = false;
        public bool isInterviewConcluded = false;

        [Header("Camera Rig")]
        public Camera mainCamera;
        public Transform overviewCamPoint;
        public Transform interviewerEyePoint;
        public Transform candidateEyePoint;
        public Transform sideViewPoint;
        public FreeFlyCamera freeFlyController;

        [Header("Avatars")]
        public AvatarAnimationController interviewerAvatar;
        public AvatarAnimationController candidateAvatar;
        public GameObject vrHeadsetVisor;

        [Header("Holographic & AR Elements")]
        public GameObject[] arHologramObjects;

        [Header("Room Lighting")]
        public Light mainDirectionalLight;
        public Light[] ceilingSpotlights;

        [Header("Microphone & Audio System")]
        public InterviewRoomAudioController audioController;

        [Header("VR Goggles & Virtual Screen")]
        public VRGogglesRig vrRig;
        public VRSpatialVirtualScreen virtualScreen;

        [Header("Speech-to-Text Transcription")]
        public SpeechToTextTranscriptionController sttController;

        [Header("Big Font Closed Captions")]
        public bool showLiveCaptionsOnScreen = true;
        public bool useGiantSubtitles = false;

        [Header("State")]
        [Header("POV / Camera Eye-Level Calibration")]
        [Tooltip("Target eye height for true face-to-face line of sight (default: 1.20m)")]
        public float faceToFaceEyeHeight = 1.20f;
        [Tooltip("Pitch angle tilt in degrees. 0 = direct horizontal eye contact face-to-face.")]
        public float cameraPitchOffset = 0f;

        public CameraViewMode currentView = CameraViewMode.CandidateARView;
        public bool arHologramActive = true;
        public bool isDialogueActive = false;
        public int currentQuestionIndex = 0;

        private float transitionProgress = 1f;
        private Vector3 startCamPos;
        private Quaternion startCamRot;
        private Vector3 targetCamPos;
        private Quaternion targetCamRot;
        private string hoveredAvatarName = null;

        [Header("AI Interview Question Generator")]
        public AIInterviewQuestionGenerator aiQuestionGenerator;
        public bool useAiGeneratedQuestions = true;

        public string GetCurrentQuestionText()
        {
            if (useAiGeneratedQuestions && aiQuestionGenerator != null)
            {
                return aiQuestionGenerator.GetQuestionText(currentQuestionIndex);
            }
            if (interviewQuestions != null && interviewQuestions.Length > 0)
            {
                int idx = Mathf.Clamp(currentQuestionIndex, 0, interviewQuestions.Length - 1);
                return interviewQuestions[idx];
            }
            return "\"Could you introduce your background in spatial computing?\"";
        }

        public void GenerateNextAIQuestion()
        {
            if (aiQuestionGenerator != null)
            {
                currentQuestionIndex++;
                aiQuestionGenerator.GenerateDynamicAIQuestion(currentQuestionIndex, candidateRole, null);
                AskSpecificQuestion(currentQuestionIndex);
                if (audioController != null) audioController.PlayUIChime();
            }
        }

        public void GenerateAIFollowUpQuestion()
        {
            if (aiQuestionGenerator != null)
            {
                currentQuestionIndex++;
                aiQuestionGenerator.GenerateDynamicAIQuestion(currentQuestionIndex, candidateRole, lastVoiceAnswerTranscript);
                AskSpecificQuestion(currentQuestionIndex);
                if (audioController != null) audioController.PlayUIChime();
            }
        }

        [Header("6-Step Automatic AI Interview Workflow")]
        public bool showInterviewSetupModal = false;
        public bool showReportModal = false;
        public bool isInterviewPaused = false;
        public bool isInterviewStarted = false;
        public float sessionDurationMinutes = 15f;
        public InterviewReportData sessionReport = null;
        public string textFallbackInput = "";
        public bool isTextFallbackActive = false;
        private Vector2 reportScrollPos = Vector2.zero;

        public void StartInterviewSession()
        {
            showInterviewSetupModal = false;
            showReportModal = false;
            showEndInterviewModal = false;
            isInterviewStarted = true;
            isInterviewPaused = false;
            isInterviewConcluded = false;
            interviewTimer = 0f;
            isTimerRunning = true;
            currentQuestionIndex = 0;

            if (aiQuestionGenerator != null)
            {
                aiQuestionGenerator.InitializeQuestionsForConfig(aiQuestionGenerator.currentConfig);
                candidateRole = aiQuestionGenerator.currentConfig.GetEffectiveRoleName();
                sessionDurationMinutes = (float)aiQuestionGenerator.currentConfig.duration;
            }

            if (audioController != null)
            {
                audioController.CheckMicrophonePermissions();
                audioController.PlayUIChime();
            }

            EnsureBothAvatarsSeatedInChairs();
            SelectRole(InterviewRole.Candidate, instant: true);

            Debug.Log($"[InterviewRoomController] Interview session started for {candidateRole} ({sessionDurationMinutes} min duration)!");
            AskSpecificQuestion(0);
        }

        public void TogglePauseInterview()
        {
            isInterviewPaused = !isInterviewPaused;
            isTimerRunning = !isInterviewPaused;
            if (audioController != null) audioController.PlayUIChime();
            Debug.Log("[InterviewRoomController] Interview paused state: " + isInterviewPaused);
        }

        public void RepeatCurrentQuestion()
        {
            if (activeDialogueRoutine != null) StopCoroutine(activeDialogueRoutine);
            AskSpecificQuestion(currentQuestionIndex);
            if (audioController != null) audioController.PlayUIChime();
            Debug.Log($"[InterviewRoomController] Repeating Question {currentQuestionIndex + 1}.");
        }

        public void EndInterviewAndShowReport()
        {
            isInterviewConcluded = true;
            isTimerRunning = false;
            currentDialoguePhase = DialoguePhase.AnswerCompleted;

            if (audioController != null)
            {
                audioController.StopRecordingUserVoice();
                audioController.PlayUIChime();
            }

            if (interviewerAvatar != null) interviewerAvatar.SetAvatarState(AvatarAnimationController.AvatarState.InterviewCompleted);
            if (candidateAvatar != null) candidateAvatar.SetAvatarState(AvatarAnimationController.AvatarState.InterviewCompleted);

            if (aiQuestionGenerator != null)
            {
                sessionReport = aiQuestionGenerator.GenerateFinalInterviewReport();
            }

            showReportModal = true;
            Debug.Log("[InterviewRoomController] Interview session concluded! Generated report with score: " + (sessionReport != null ? sessionReport.overallScore : 88));
        }

        public void SubmitTypedFallbackAnswer()
        {
            if (string.IsNullOrEmpty(textFallbackInput)) return;

            lastVoiceAnswerTranscript = textFallbackInput;
            userSpeechDuration = Mathf.Max(2.5f, textFallbackInput.Split(' ').Length / 2.5f);
            textFallbackInput = "";
            isTextFallbackActive = false;

            if (currentDialoguePhase == DialoguePhase.UserVoiceRecording)
            {
                FinishVoiceAnswerRecording();
            }
            SubmitVoiceAnswer();
        }

        private readonly string[] interviewQuestions = new string[]
        {
            "\"Welcome Alex! Could you introduce your background in Spatial Computing and AR telepresence?\"",
            "\"How do you optimize 6-DoF tracking and reduce motion-to-photon latency in multi-user VR?\"",
            "\"Walk us through how you would architect real-time volumetric avatar streaming over WebRTC.\"",
            "\"Impressive solution. Do you have any questions for us regarding our Spatial Operating System?\""
        };

        private readonly string[] candidateAnswers = new string[]
        {
            "\"Thanks Dr. Vance! I specialize in real-time neural rendering and interactive spatial interfaces.\"",
            "\"We leverage predictive pose estimation, async timewarp, and foveated rendering to hold 12ms round-trip.\"",
            "\"I partition mesh LODs with client-side interpolation and stream audio-synchronized spatial blendshapes.\"",
            "\"Yes! I'd love to learn more about your real-time spatial physics engine and neural gesture pipelines.\""
        };

        private readonly string[] interviewerAcknowledgments = new string[]
        {
            "\"Thank you, Alex. That gives us great insight into your spatial telepresence background.\"",
            "\"Impressive breakdown on the 6-DoF tracking pipeline and latency mitigation. That aligns well with our benchmarks.\"",
            "\"Very solid architectural approach to volumetric avatar streaming and audio-synchronized viseme channels.\"",
            "\"Fantastic discussion today! You've addressed all the core challenges in our Spatial Operating System roadmap.\""
        };

        private void OnEnable()
        {
            EnsureBothAvatarsSeatedInChairs();
        }

        private void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (freeFlyController == null && mainCamera != null)
            {
                freeFlyController = mainCamera.GetComponent<FreeFlyCamera>();
            }

            if (audioController == null)
            {
                audioController = Object.FindAnyObjectByType<InterviewRoomAudioController>();
            }

            if (vrRig == null)
            {
                vrRig = Object.FindAnyObjectByType<VRGogglesRig>();
            }

            if (virtualScreen == null)
            {
                virtualScreen = Object.FindAnyObjectByType<VRSpatialVirtualScreen>();
            }

            if (sttController == null)
            {
                sttController = Object.FindAnyObjectByType<SpeechToTextTranscriptionController>();
            }

            if (aiQuestionGenerator == null)
            {
                aiQuestionGenerator = Object.FindAnyObjectByType<AIInterviewQuestionGenerator>();
                if (aiQuestionGenerator == null)
                {
                    aiQuestionGenerator = gameObject.AddComponent<AIInterviewQuestionGenerator>();
                }
            }

            EnsureBothAvatarsSeatedInChairs();

            // Default to Candidate Role perspective so VR headset user is seated as Alex Mercer facing Dr. Sarah Vance
            SelectRole(InterviewRole.Candidate, instant: true);
            if (vrRig != null)
            {
                vrRig.SetVRViewMode(VRGogglesRig.VRRigSeat.CandidateChair);
            }

            if (Application.isPlaying && autoInterviewFlow)
            {
                StartCoroutine(AutoStartInterviewWithDelay(1.8f));
            }
        }

        private IEnumerator AutoStartInterviewWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (currentDialoguePhase == DialoguePhase.Idle && !isDialogueActive)
            {
                Debug.Log("[InterviewRoomController] Auto-Interview starting: Dr. Sarah Vance asks Question 1 to Candidate.");
                AskSpecificQuestion(0);
            }
        }

        public void ToggleAutoInterview()
        {
            autoInterviewFlow = !autoInterviewFlow;
            Debug.Log($"[InterviewRoomController] Auto Interview flow toggled: {(autoInterviewFlow ? "ON (Hands-Free)" : "OFF (Manual Step-by-Step)")}");
            if (audioController != null) audioController.PlayUIChime();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                if (interviewerAvatar == null || candidateAvatar == null)
                {
                    EnsureBothAvatarsSeatedInChairs();
                }
                return;
            }

            if (isTimerRunning && !isInterviewConcluded)
            {
                interviewTimer += Time.deltaTime;
            }

            // Real-time synchronization during live voice answering
            if (currentDialoguePhase == DialoguePhase.UserVoiceRecording)
            {
                float micLoudness = (audioController != null) ? audioController.currentMicLoudness : 0f;
                bool isSpeaking = (audioController != null) && audioController.isUserVoiceDetected;

                if (candidateAvatar != null)
                {
                    candidateAvatar.isDrivenByLiveMic = true;
                    candidateAvatar.liveMicVolume = micLoudness;
                }

                if (sttController != null && sttController.candidateBubble != null && audioController != null)
                {
                    sttController.candidateBubble.DisplayLiveRecordingStatus(
                        "Alex Mercer (You)",
                        audioController.userRecordingDuration,
                        micLoudness,
                        isSpeaking
                    );
                }

                if (interviewerAvatar != null)
                {
                    interviewerAvatar.isSpeaking = false;
                }

                // Auto-advance if candidate has spoken and natural conversational pause occurs
                if (autoInterviewFlow && audioController != null && audioController.userRecordingDuration >= 2.0f && audioController.userSilenceDuration >= 2.0f)
                {
                    Debug.Log("[InterviewRoomController] Natural pause detected during voice recording. Finishing and delivering response.");
                    FinishVoiceAnswerRecording();
                    SubmitVoiceAnswer();
                }

                // If audio controller auto-stopped recording (e.g. max duration reached)
                if (audioController != null && !audioController.isRecordingUserVoice && audioController.hasRecordedUserAnswer)
                {
                    FinishVoiceAnswerRecording();
                    if (autoInterviewFlow)
                    {
                        SubmitVoiceAnswer();
                    }
                }
            }
            else if (candidateAvatar != null && candidateAvatar.isDrivenByLiveMic && !isCandidatePlaybackActive)
            {
                candidateAvatar.isDrivenByLiveMic = false;
                candidateAvatar.liveMicVolume = 0f;
            }

            HandleKeyInputs();
            HandleMouseClickInteractions();

            // Smooth camera transition when not in free-fly and not driven by VR tracking
            if (currentView != CameraViewMode.FreeFly && mainCamera != null)
            {
                bool isVRDrivingCamera = (vrRig != null && (vrRig.isVRHardwareActive || (currentView == CameraViewMode.VRGogglesMode && vrRig.isVRModeActive)));
                if (!isVRDrivingCamera)
                {
                    if (transitionProgress < 1f)
                    {
                        transitionProgress += Time.deltaTime * 3.5f;
                        float t = Mathf.SmoothStep(0f, 1f, transitionProgress);
                        mainCamera.transform.position = Vector3.Lerp(startCamPos, targetCamPos, t);
                        mainCamera.transform.rotation = Quaternion.Slerp(startCamRot, targetCamRot, t);
                    }
                    else if (targetCamPos != Vector3.zero)
                    {
                        mainCamera.transform.position = targetCamPos;
                        mainCamera.transform.rotation = targetCamRot;
                    }
                }
            }
        }

        private void HandleKeyInputs()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) SelectRole(InterviewRole.Interviewer);
                if (kb.digit2Key.wasPressedThisFrame) SelectRole(InterviewRole.Candidate);
                if (kb.digit3Key.wasPressedThisFrame) SwitchView(CameraViewMode.Overview);
                if (kb.digit4Key.wasPressedThisFrame) SwitchView(CameraViewMode.SideBroadcast);
                if (kb.digit5Key.wasPressedThisFrame) SwitchView(CameraViewMode.FreeFly);
                if (kb.digit6Key.wasPressedThisFrame || kb.vKey.wasPressedThisFrame) SwitchView(CameraViewMode.VRGogglesMode);
                if (kb.hKey.wasPressedThisFrame) ToggleARHolograms();
                if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) NextDialogue();
                if (kb.mKey.wasPressedThisFrame && audioController != null) audioController.ToggleMute();
                if (kb.pKey.wasPressedThisFrame) ResetToFaceToFacePOV();
                if (kb.lKey.wasPressedThisFrame) ToggleAnswerWithOwnVoice();
                if (kb.aKey.wasPressedThisFrame) ToggleAutoInterview();
                if (kb.gKey.wasPressedThisFrame) GenerateNextAIQuestion();
                if (kb.fKey.wasPressedThisFrame) GenerateAIFollowUpQuestion();
                if (kb.rKey.wasPressedThisFrame && currentDialoguePhase == DialoguePhase.UserVoiceReview) ReRecordVoiceAnswer();
                if (kb.rightArrowKey.wasPressedThisFrame) NextQuestion();
                if (kb.leftArrowKey.wasPressedThisFrame) PrevQuestion();
            }
#else
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectRole(InterviewRole.Interviewer);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SelectRole(InterviewRole.Candidate);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchView(CameraViewMode.Overview);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchView(CameraViewMode.SideBroadcast);
            if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchView(CameraViewMode.FreeFly);
            if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.V)) SwitchView(CameraViewMode.VRGogglesMode);
            if (Input.GetKeyDown(KeyCode.H)) ToggleARHolograms();
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) NextDialogue();
            if (Input.GetKeyDown(KeyCode.M) && audioController != null) audioController.ToggleMute();
            if (Input.GetKeyDown(KeyCode.P)) ResetToFaceToFacePOV();
            if (Input.GetKeyDown(KeyCode.L)) ToggleAnswerWithOwnVoice();
            if (Input.GetKeyDown(KeyCode.A)) ToggleAutoInterview();
            if (Input.GetKeyDown(KeyCode.G)) GenerateNextAIQuestion();
            if (Input.GetKeyDown(KeyCode.F)) GenerateAIFollowUpQuestion();
            if (Input.GetKeyDown(KeyCode.R) && currentDialoguePhase == DialoguePhase.UserVoiceReview) ReRecordVoiceAnswer();
            if (Input.GetKeyDown(KeyCode.RightArrow)) NextQuestion();
            if (Input.GetKeyDown(KeyCode.LeftArrow)) PrevQuestion();
#endif
        }

        /// <summary>
        /// Switches between Interviewer and Candidate roles, moving the camera,
        /// adjusting head culling so the active user doesn't clip into themselves,
        /// and aligning opposite avatar across the 1.3m table.
        /// </summary>
        public void SelectRole(InterviewRole newRole, bool instant = false)
        {
            activeRole = newRole;

            if (newRole == InterviewRole.Interviewer)
            {
                SwitchView(CameraViewMode.InterviewerView, instant);
                if (vrRig != null)
                {
                    vrRig.SetVRViewMode(VRGogglesRig.VRRigSeat.InterviewerChair);
                }
            }
            else
            {
                SwitchView(CameraViewMode.CandidateARView, instant);
                if (vrRig != null)
                {
                    vrRig.SetVRViewMode(VRGogglesRig.VRRigSeat.CandidateChair);
                }
            }

            ApplyRoleAvatarCulling();
        }

        private void ApplyRoleAvatarCulling()
        {
            bool isFirstPersonInterviewer = (activeRole == InterviewRole.Interviewer && (currentView == CameraViewMode.InterviewerView || currentView == CameraViewMode.VRGogglesMode));
            bool isFirstPersonCandidate = (activeRole == InterviewRole.Candidate && (currentView == CameraViewMode.CandidateARView || currentView == CameraViewMode.VRGogglesMode));

            // Interviewer: Hide Head & Neck only if first-person interviewer (suit and arms stay visible)
            if (interviewerAvatar != null)
            {
                Transform head = interviewerAvatar.head != null ? interviewerAvatar.head : interviewerAvatar.transform.Find("Head");
                if (head != null)
                {
                    foreach (var r in head.GetComponentsInChildren<Renderer>(true))
                    {
                        if (r != null) r.enabled = !isFirstPersonInterviewer;
                    }
                }
                Transform neck = interviewerAvatar.transform.Find("Neck");
                if (neck != null)
                {
                    foreach (var r in neck.GetComponentsInChildren<Renderer>(true))
                    {
                        if (r != null) r.enabled = !isFirstPersonInterviewer;
                    }
                }
            }

            // Candidate: Hide Head & Neck only if first-person candidate (hoodie, arms, and sneakers stay visible)
            if (candidateAvatar != null)
            {
                Transform head = candidateAvatar.head != null ? candidateAvatar.head : candidateAvatar.transform.Find("Head");
                if (head != null)
                {
                    foreach (var r in head.GetComponentsInChildren<Renderer>(true))
                    {
                        if (r != null) r.enabled = !isFirstPersonCandidate;
                    }
                }
                Transform neck = candidateAvatar.transform.Find("Neck");
                if (neck != null)
                {
                    foreach (var r in neck.GetComponentsInChildren<Renderer>(true))
                    {
                        if (r != null) r.enabled = !isFirstPersonCandidate;
                    }
                }
            }

            if (vrRig != null)
            {
                vrRig.ApplyAvatarHeadCulling();
            }
        }

        public void SwitchView(CameraViewMode newView, bool instant = false)
        {
            currentView = newView;
            if (mainCamera == null) return;

            if (newView == CameraViewMode.InterviewerView)
            {
                activeRole = InterviewRole.Interviewer;
            }
            else if (newView == CameraViewMode.CandidateARView)
            {
                activeRole = InterviewRole.Candidate;
            }

            Transform targetPoint = overviewCamPoint;
            switch (newView)
            {
                case CameraViewMode.Overview: targetPoint = overviewCamPoint; break;
                case CameraViewMode.InterviewerView: targetPoint = interviewerEyePoint; break;
                case CameraViewMode.CandidateARView: targetPoint = candidateEyePoint; break;
                case CameraViewMode.SideBroadcast: targetPoint = sideViewPoint; break;
                case CameraViewMode.VRGogglesMode:
                    targetPoint = (activeRole == InterviewRole.Interviewer) ? interviewerEyePoint : candidateEyePoint;
                    break;
                case CameraViewMode.FreeFly: break;
            }

            if (vrRig != null)
            {
                if (newView == CameraViewMode.InterviewerView)
                {
                    vrRig.SetVRViewMode(VRGogglesRig.VRRigSeat.InterviewerChair);
                }
                else if (newView == CameraViewMode.CandidateARView)
                {
                    vrRig.SetVRViewMode(VRGogglesRig.VRRigSeat.CandidateChair);
                }
                else if (newView == CameraViewMode.VRGogglesMode)
                {
                    vrRig.isVRModeActive = true;
                    vrRig.SetVRViewMode(activeRole == InterviewRole.Interviewer ? VRGogglesRig.VRRigSeat.InterviewerChair : VRGogglesRig.VRRigSeat.CandidateChair);
                }
                else if (newView == CameraViewMode.Overview)
                {
                    vrRig.SetVRViewMode(VRGogglesRig.VRRigSeat.DirectorView);
                }
            }

            if (newView == CameraViewMode.FreeFly)
            {
                if (freeFlyController != null) freeFlyController.enabled = true;
                return;
            }

            if (freeFlyController != null) freeFlyController.enabled = false;

            if (targetPoint != null)
            {
                targetCamPos = targetPoint.position;
                targetCamRot = targetPoint.rotation;
                startCamPos = mainCamera.transform.position;
                startCamRot = mainCamera.transform.rotation;

                if (instant)
                {
                    mainCamera.transform.position = targetCamPos;
                    mainCamera.transform.rotation = targetCamRot;
                    transitionProgress = 1f;
                }
                else
                {
                    transitionProgress = 0f;
                    if (audioController != null)
                    {
                        audioController.PlayUIChime();
                    }
                }
            }

            ApplyRoleAvatarCulling();
        }

        /// <summary>
        /// Detects mouse hover and click on the 3D Interviewer or Candidate in the scene.
        /// Clicking the Interviewer smoothly changes the camera to the Interviewer Person's View (facing Candidate).
        /// </summary>
        private void HandleMouseClickInteractions()
        {
            if (mainCamera == null) return;

            Vector2 mouseScreenPos = Vector2.zero;
            bool leftClickDown = false;

#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                mouseScreenPos = mouse.position.ReadValue();
                leftClickDown = mouse.leftButton.wasPressedThisFrame;
            }
#else
            if (!leftClickDown && Input.GetMouseButtonDown(0))
            {
                leftClickDown = true;
                mouseScreenPos = Input.mousePosition;
            }
            else if (mouseScreenPos == Vector2.zero)
            {
                mouseScreenPos = Input.mousePosition;
            }
#endif

            // Check if cursor is over IMGUI panels or EventSystem UI
            if (IsPointerOverUI(mouseScreenPos))
            {
                hoveredAvatarName = null;
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 60f))
            {
                if (IsInterviewerObject(hit.transform))
                {
                    hoveredAvatarName = "Interviewer";
                    if (leftClickDown)
                    {
                        if (currentView == CameraViewMode.InterviewerView || activeRole == InterviewRole.Interviewer)
                        {
                            Debug.Log("[InterviewRoomController] Interviewer clicked while in Interviewer POV! Asking question...");
                            NextDialogue();
                        }
                        else
                        {
                            Debug.Log("[InterviewRoomController] Interviewer avatar clicked! Switching camera to Interviewer Person's View (facing Candidate).");
                            SelectRole(InterviewRole.Interviewer);
                        }
                    }
                }
                else if (IsCandidateObject(hit.transform))
                {
                    hoveredAvatarName = "Candidate";
                    if (leftClickDown)
                    {
                        if (currentView == CameraViewMode.InterviewerView || activeRole == InterviewRole.Interviewer)
                        {
                            Debug.Log("[InterviewRoomController] Candidate avatar clicked while in Interviewer POV! Asking question to candidate...");
                            NextDialogue();
                        }
                        else
                        {
                            Debug.Log("[InterviewRoomController] Candidate avatar clicked! Switching camera to Candidate Person's View (facing Interviewer).");
                            SelectRole(InterviewRole.Candidate);
                        }
                    }
                }
                else
                {
                    hoveredAvatarName = null;
                }
            }
            else
            {
                hoveredAvatarName = null;
            }
        }

        private bool IsPointerOverUI(Vector2 mouseScreenPos)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return true;
            }

            float guiX = mouseScreenPos.x;
            float guiY = Screen.height - mouseScreenPos.y;
            Vector2 guiPoint = new Vector2(guiX, guiY);

            int topBarW = 900;
            int topBarH = 48;
            int topBarX = (Screen.width - topBarW) / 2;
            int topBarY = 16;
            Rect topBarRect = new Rect(topBarX, topBarY, topBarW, topBarH);
            if (topBarRect.Contains(guiPoint)) return true;

            int panelW = 440;
            int panelH = activeRole == InterviewRole.Interviewer ? 500 : 430;
            Rect leftPanelRect = new Rect(20, 76, panelW, panelH);
            if (leftPanelRect.Contains(guiPoint)) return true;

            if (currentDialoguePhase == DialoguePhase.UserVoiceRecording || currentDialoguePhase == DialoguePhase.UserVoiceReview)
            {
                int hudW = 660;
                int hudH = 230;
                int hudX = (Screen.width - hudW) / 2;
                int hudY = Screen.height - hudH - 150;
                Rect hudRect = new Rect(hudX, hudY, hudW, hudH);
                if (hudRect.Contains(guiPoint)) return true;
            }

            if (showEndInterviewModal)
            {
                int modalW = 460;
                int modalH = 240;
                Rect modalRect = new Rect((Screen.width - modalW) / 2, (Screen.height - modalH) / 2, modalW, modalH);
                if (modalRect.Contains(guiPoint)) return true;
            }

            if (showLiveCaptionsOnScreen)
            {
                int bannerW = Mathf.Clamp(Screen.width - 80, 720, 1180);
                int bannerH = useGiantSubtitles ? 145 : 120;
                int bannerX = (Screen.width - bannerW) / 2;
                int bannerY = Screen.height - bannerH - 18;
                Rect bannerRect = new Rect(bannerX, bannerY, bannerW, bannerH);
                if (bannerRect.Contains(guiPoint)) return true;
            }

            return false;
        }

        public bool IsInterviewerObject(Transform t)
        {
            if (t == null) return false;
            if (interviewerAvatar != null && (t == interviewerAvatar.transform || t.IsChildOf(interviewerAvatar.transform)))
                return true;

            Transform curr = t;
            while (curr != null)
            {
                string n = curr.name.ToLowerInvariant();
                if (n.Contains("interviewer") || n.Contains("drsarah") || n.Contains("sarah"))
                    return true;
                curr = curr.parent;
            }
            return false;
        }

        public bool IsCandidateObject(Transform t)
        {
            if (t == null) return false;
            if (candidateAvatar != null && (t == candidateAvatar.transform || t.IsChildOf(candidateAvatar.transform)))
                return true;

            Transform curr = t;
            while (curr != null)
            {
                string n = curr.name.ToLowerInvariant();
                if (n.Contains("candidate") || n.Contains("alex") || n.Contains("mercer"))
                    return true;
                curr = curr.parent;
            }
            return false;
        }

        public void ToggleARHolograms()
        {
            arHologramActive = !arHologramActive;
            if (arHologramObjects != null)
            {
                foreach (var obj in arHologramObjects)
                {
                    if (obj != null) obj.SetActive(arHologramActive);
                }
            }
        }

        public void NextQuestion()
        {
            currentQuestionIndex = (currentQuestionIndex + 1) % interviewQuestions.Length;
        }

        public void PrevQuestion()
        {
            currentQuestionIndex = (currentQuestionIndex - 1 + interviewQuestions.Length) % interviewQuestions.Length;
        }

        private Coroutine activeDialogueRoutine;

        public void NextDialogue()
        {
            if (currentDialoguePhase == DialoguePhase.UserVoiceRecording)
            {
                FinishVoiceAnswerRecording();
                return;
            }
            if (currentDialoguePhase == DialoguePhase.UserVoiceReview)
            {
                SubmitVoiceAnswer();
                return;
            }
            if (currentDialoguePhase == DialoguePhase.AnswerCompleted)
            {
                NextQuestion();
            }

            if (activeDialogueRoutine != null)
            {
                StopCoroutine(activeDialogueRoutine);
            }

            isDialogueActive = true;
            activeDialogueRoutine = StartCoroutine(QuestionAndAnswerRoutine(currentQuestionIndex));
        }

        public void AskSpecificQuestion(int questionIndex)
        {
            if (activeDialogueRoutine != null)
            {
                StopCoroutine(activeDialogueRoutine);
            }

            currentQuestionIndex = Mathf.Clamp(questionIndex, 0, interviewQuestions.Length - 1);
            isDialogueActive = true;
            activeDialogueRoutine = StartCoroutine(QuestionAndAnswerRoutine(currentQuestionIndex));
        }

        private IEnumerator QuestionAndAnswerRoutine(int questionIdx)
        {
            isDialogueActive = true;
            currentQuestionIndex = Mathf.Clamp(questionIdx, 0, interviewQuestions.Length - 1);

            // ── PHASE 1: INTERVIEWER ASKS THE QUESTION (AUDIO & LIP-SYNC) ──
            currentDialoguePhase = DialoguePhase.InterviewerAskingQuestion;
            if (interviewerAvatar != null) interviewerAvatar.SetAvatarState(AvatarAnimationController.AvatarState.Speaking);
            if (candidateAvatar != null)
            {
                candidateAvatar.SetAvatarState(AvatarAnimationController.AvatarState.Listening);
                candidateAvatar.isDrivenByLiveMic = false;
            }

            string question = GetCurrentQuestionText();
            float questionDuration = 3.5f;

            if (audioController != null)
            {
                questionDuration = audioController.PlayCustomDialogueLine(isInterviewer: true, question);
            }

            if (sttController != null)
            {
                sttController.currentActiveSpeaker = "Dr. Vance (AI Interviewer) ✨";
                sttController.TranscribeInterviewerSpeech(question, questionDuration);
            }

            // Wait for interviewer to complete speaking question + natural conversational pause
            yield return new WaitForSeconds(questionDuration + 0.45f);

            if (interviewerAvatar != null)
            {
                interviewerAvatar.SetAvatarState(AvatarAnimationController.AvatarState.Listening);
            }

            // ── PHASE 2: CANDIDATE ANSWERS THE QUESTION (MICROPHONE) ──
            if (answerWithOwnVoice)
            {
                // USER ANSWERS WITH THEIR OWN VOICE VIA VR HEADSET / PC MICROPHONE
                StartVoiceAnswerRecording();

                // Wait while user speaks into VR headset / reviews their voice answer
                while (currentDialoguePhase == DialoguePhase.UserVoiceRecording || currentDialoguePhase == DialoguePhase.UserVoiceReview)
                {
                    yield return null;
                }

                // ── PHASE 3: CANDIDATE CHARACTER RESPONDS TO INTERVIEWER ──
                currentDialoguePhase = DialoguePhase.CandidateDeliveringResponse;
                if (interviewerAvatar != null)
                {
                    interviewerAvatar.SetAvatarState(AvatarAnimationController.AvatarState.Listening);
                }
                if (candidateAvatar != null)
                {
                    candidateAvatar.isDrivenByLiveMic = false;
                    candidateAvatar.SetAvatarState(AvatarAnimationController.AvatarState.Speaking);
                }

                float responseDuration = (audioController != null) ? audioController.PlayRecordedUserVoice() : 0f;
                if (responseDuration <= 0.2f)
                {
                    // Fallback: If no audio was captured, play speech line so conversation flows seamlessly
                    if (audioController != null)
                    {
                        responseDuration = audioController.PlayDialogueVoiceLine(isInterviewer: false, currentQuestionIndex);
                    }
                    else
                    {
                        responseDuration = 3.2f;
                    }
                }

                if (sttController != null)
                {
                    sttController.TranscribeUserVoiceAnswer(lastVoiceAnswerTranscript, responseDuration);
                }

                // Candidate avatar speaks, gestures, and articulates lips delivering answer to Dr. Vance
                yield return new WaitForSeconds(responseDuration + 0.4f);

                if (candidateAvatar != null)
                {
                    candidateAvatar.SetAvatarState(AvatarAnimationController.AvatarState.Waiting);
                }

                // ── PHASE 4: INTERVIEWER EVALUATES & ACKNOWLEDGES RESPONSE ──
                currentDialoguePhase = DialoguePhase.InterviewerAcknowledging;
                if (interviewerAvatar != null)
                {
                    interviewerAvatar.SetAvatarState(AvatarAnimationController.AvatarState.Thinking);
                }

                int evalScore = 5;
                string ackText = "\"Thank you, Alex. Great answer.\"";
                string evalFeedback = "Clear spoken delivery.";

                if (aiQuestionGenerator != null)
                {
                    aiQuestionGenerator.EvaluateCandidateAnswer(currentQuestionIndex, lastVoiceAnswerTranscript, userSpeechDuration, out evalScore, out ackText, out evalFeedback);
                }

                if (currentQuestionIndex < questionScores.Length)
                {
                    questionScores[currentQuestionIndex] = evalScore;
                }

                interviewerNotes += $"\n• Q{currentQuestionIndex + 1} Spoken Response ({evalScore}★): {evalFeedback}";

                // Brief thoughtful pause while interviewer evaluates
                yield return new WaitForSeconds(0.75f);

                if (interviewerAvatar != null)
                {
                    interviewerAvatar.SetAvatarState(AvatarAnimationController.AvatarState.Speaking);
                }

                float ackDuration = 2.4f;
                if (audioController != null)
                {
                    audioController.PlayUIChime();
                    ackDuration = audioController.PlayCustomDialogueLine(isInterviewer: true, ackText);
                }

                if (sttController != null)
                {
                    sttController.currentActiveSpeaker = "Dr. Vance (Interviewer)";
                    sttController.TranscribeInterviewerSpeech(ackText, ackDuration);
                }

                yield return new WaitForSeconds(ackDuration + 0.35f);

                if (interviewerAvatar != null)
                {
                    interviewerAvatar.SetAvatarState(AvatarAnimationController.AvatarState.Idle);
                }
                if (candidateAvatar != null)
                {
                    candidateAvatar.SetAvatarState(AvatarAnimationController.AvatarState.Idle);
                }

                // ── PHASE 5: COMPLETE & AUTO ADVANCE TO NEXT AI QUESTION OR END ──
                currentDialoguePhase = DialoguePhase.AnswerCompleted;

                // Check if session duration expired
                if (interviewTimer >= sessionDurationMinutes * 60f)
                {
                    Debug.Log($"[InterviewRoomController] Interview duration ({sessionDurationMinutes} min) reached. Concluding interview session.");
                    EndInterviewAndShowReport();
                    yield break;
                }

                if (autoInterviewFlow)
                {
                    yield return new WaitForSeconds(1.2f);
                    currentQuestionIndex++;
                    if (useAiGeneratedQuestions && aiQuestionGenerator != null)
                    {
                        aiQuestionGenerator.GenerateDynamicAIQuestion(currentQuestionIndex, candidateRole, lastVoiceAnswerTranscript);
                    }
                    activeDialogueRoutine = StartCoroutine(QuestionAndAnswerRoutine(currentQuestionIndex));
                    yield break;
                }
                else if (!useAiGeneratedQuestions && currentQuestionIndex >= interviewQuestions.Length - 1)
                {
                    EndInterviewAndShowReport();
                }
            }
            else
            {
                // Canned automated candidate answer
                currentDialoguePhase = DialoguePhase.CandidateSpeakingSimulated;
                if (candidateAvatar != null)
                {
                    candidateAvatar.isSpeaking = true;
                }

                string answer = candidateAnswers[currentQuestionIndex];
                float answerDuration = 4.0f;

                if (audioController != null)
                {
                    answerDuration = audioController.PlayDialogueVoiceLine(isInterviewer: false, currentQuestionIndex);
                }

                if (sttController != null)
                {
                    sttController.TranscribeCandidateSpeech(answer, answerDuration);
                }

                yield return new WaitForSeconds(answerDuration + 0.35f);

                if (candidateAvatar != null)
                {
                    candidateAvatar.isSpeaking = false;
                }

                currentDialoguePhase = DialoguePhase.AnswerCompleted;
            }

            isDialogueActive = false;
            activeDialogueRoutine = null;
        }

        #region Live Voice Answering Workflow
        public void ToggleAnswerWithOwnVoice()
        {
            answerWithOwnVoice = !answerWithOwnVoice;
            Debug.Log($"[InterviewRoomController] Answer mode toggled: {(answerWithOwnVoice ? "My Own Voice (Microphone)" : "Automated Audio")}");
            if (audioController != null) audioController.PlayUIChime();
        }

        public void StartVoiceAnswerRecording()
        {
            currentDialoguePhase = DialoguePhase.UserVoiceRecording;
            if (interviewerAvatar != null) interviewerAvatar.isSpeaking = false;
            if (candidateAvatar != null)
            {
                candidateAvatar.isSpeaking = true;
                candidateAvatar.isDrivenByLiveMic = true;
            }

            if (audioController != null)
            {
                audioController.StartRecordingUserVoice();
                audioController.PlayUIChime();
            }

            if (sttController != null)
            {
                sttController.currentLiveSubtitleSpeaker = "Alex Mercer (You)";
                sttController.currentLiveSubtitleText = "🔴 RECORDING LIVE VOICE... Speak your answer now into your VR headset / PC microphone.";
                sttController.isCurrentSpeakerInterviewer = false;
                sttController.subtitleDisplayRemainingTimer = 60f;
            }

            Debug.Log("[InterviewRoomController] Started Live Voice Recording for candidate answer!");
        }

        public void FinishVoiceAnswerRecording()
        {
            if (currentDialoguePhase != DialoguePhase.UserVoiceRecording) return;

            if (audioController != null)
            {
                audioController.StopRecordingUserVoice();
                userSpeechDuration = audioController.userRecordingDuration;
            }
            else
            {
                userSpeechDuration = 4.0f;
            }

            if (candidateAvatar != null)
            {
                candidateAvatar.isDrivenByLiveMic = false;
                candidateAvatar.isSpeaking = false;
            }

            currentDialoguePhase = DialoguePhase.UserVoiceReview;
            lastVoiceAnswerTranscript = GenerateContextualAnswerTranscript(currentQuestionIndex, userSpeechDuration);

            if (sttController != null)
            {
                sttController.currentLiveSubtitleSpeaker = "Alex Mercer (You)";
                sttController.currentLiveSubtitleText = $"✓ Answer Recorded ({userSpeechDuration:F1}s). Press [Space] to Submit to Dr. Vance or [R] to Re-record.";
                sttController.isCurrentSpeakerInterviewer = false;
                sttController.subtitleDisplayRemainingTimer = 30f;
            }

            Debug.Log($"[InterviewRoomController] Finished Live Voice Recording ({userSpeechDuration:F1}s). Ready for review/submit.");
        }

        public void PlaybackVoiceAnswer()
        {
            if (audioController == null || !audioController.hasRecordedUserAnswer) return;

            float dur = audioController.PlayRecordedUserVoice();
            if (dur > 0f)
            {
                StartCoroutine(CandidateVoicePlaybackRoutine(dur));
            }
        }

        private IEnumerator CandidateVoicePlaybackRoutine(float duration)
        {
            isCandidatePlaybackActive = true;
            if (candidateAvatar != null) candidateAvatar.isSpeaking = true;

            yield return new WaitForSeconds(duration + 0.2f);

            if (candidateAvatar != null) candidateAvatar.isSpeaking = false;
            isCandidatePlaybackActive = false;
        }

        public void ReRecordVoiceAnswer()
        {
            if (audioController != null)
            {
                audioController.DiscardRecordedUserVoice();
            }
            StartVoiceAnswerRecording();
        }

        public void SubmitVoiceAnswer()
        {
            if (currentDialoguePhase != DialoguePhase.UserVoiceRecording && currentDialoguePhase != DialoguePhase.UserVoiceReview) return;

            if (currentDialoguePhase == DialoguePhase.UserVoiceRecording)
            {
                FinishVoiceAnswerRecording();
            }

            currentDialoguePhase = DialoguePhase.CandidateDeliveringResponse;
            if (audioController != null)
            {
                audioController.PlayUIChime();
            }

            Debug.Log($"[InterviewRoomController] Voice answer submitted for Q{currentQuestionIndex + 1}! Candidate character (Alex Mercer) is now delivering the response to Dr. Vance.");
        }

        public string GenerateContextualAnswerTranscript(int qIndex, float duration)
        {
            if (sttController != null && !string.IsNullOrEmpty(sttController.latestCandidateTranscript) && 
                !sttController.latestCandidateTranscript.StartsWith("🔴"))
            {
                return $"[Spoken via VR Headset • {duration:F1}s]: \"{sttController.latestCandidateTranscript}\"";
            }

            string[] contextTranscripts = new string[]
            {
                "I specialize in real-time neural rendering, low-latency 6-DoF tracking pipelines, and interactive spatial telepresence for WebXR and VR headsets.",
                "We leverage predictive pose estimation, asynchronous timewarp, and foveated rendering to maintain a sub-12ms motion-to-photon latency loop.",
                "I partition volumetric mesh LODs with client-side temporal interpolation and stream audio-synchronized spatial blendshapes over WebRTC data channels.",
                "To scale distributed telepresence sessions, I implement interest-management spatial culling, UDP packet delta compression, and offload physics to Unity DOTS Job threads.",
                "Under thermal and GPU throttling, I prioritize fixed foveated rendering, downscale peripheral render targets, and clamp dynamic shadows to protect the 90Hz display budget."
            };

            int idx = qIndex % contextTranscripts.Length;
            string baseAnswer = contextTranscripts[idx];

            return $"[Spoken via VR Headset • {duration:F1}s]: \"{baseAnswer}\"";
        }
        #endregion

        private void SwitchToCandidateAnswer()
        {
            if (candidateAvatar != null) candidateAvatar.isSpeaking = true;
            if (interviewerAvatar != null) interviewerAvatar.isSpeaking = false;
            if (audioController != null) audioController.PlayDialogueVoiceLine(isInterviewer: false, currentQuestionIndex);
        }

        private void ResetSpeaking()
        {
            if (candidateAvatar != null)
            {
                candidateAvatar.isSpeaking = false;
                candidateAvatar.isDrivenByLiveMic = false;
            }
            if (interviewerAvatar != null) interviewerAvatar.isSpeaking = false;
            isDialogueActive = false;
            currentDialoguePhase = DialoguePhase.Idle;
        }

        private void OnGUI()
        {
            int screenW = Screen.width;
            int screenH = Screen.height;

            // ── TOP ROLE SELECTOR & STATUS BAR ──
            int topBarW = 1040;
            int topBarH = 48;
            int topBarX = (screenW - topBarW) / 2;
            int topBarY = 16;

            GUI.Box(new Rect(topBarX, topBarY, topBarW, topBarH), GUIContent.none);
            GUILayout.BeginArea(new Rect(topBarX + 8, topBarY + 8, topBarW - 16, topBarH - 16));
            GUILayout.BeginHorizontal();

            GUIStyle roleBtnStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            if (activeRole == InterviewRole.Interviewer) GUI.color = new Color(0.2f, 0.85f, 1f);
            if (GUILayout.Button("👔 [1: Interviewer]", roleBtnStyle, GUILayout.Width(135)))
            {
                SelectRole(InterviewRole.Interviewer);
            }
            GUI.color = Color.white;

            if (activeRole == InterviewRole.Candidate) GUI.color = new Color(0.2f, 1.0f, 0.5f);
            if (GUILayout.Button("🧑‍💻 [2: Candidate]", roleBtnStyle, GUILayout.Width(135)))
            {
                SelectRole(InterviewRole.Candidate);
            }
            GUI.color = Color.white;

            // Live Voice Mode Toggle Button
            if (answerWithOwnVoice) GUI.color = new Color(0.2f, 1.0f, 0.45f);
            if (GUILayout.Button(answerWithOwnVoice ? "🎙️ Voice: MY MIC [L]" : "🤖 Voice: AUTO [L]", GUILayout.Width(140)))
            {
                ToggleAnswerWithOwnVoice();
            }
            GUI.color = Color.white;

            // Auto-Interview Flow Toggle Button
            if (autoInterviewFlow) GUI.color = new Color(0.3f, 1.0f, 0.6f);
            if (GUILayout.Button(autoInterviewFlow ? "⚡ Flow: AUTO [A]" : "⏸️ Flow: STEP [A]", GUILayout.Width(130)))
            {
                ToggleAutoInterview();
            }
            GUI.color = Color.white;

            if (GUILayout.Button("POV [P]", GUILayout.Width(70))) ResetToFaceToFacePOV();
            if (GUILayout.Button("3: Overview", GUILayout.Width(80))) SwitchView(CameraViewMode.Overview);
            if (GUILayout.Button("6: VR [V]", GUILayout.Width(70))) SwitchView(CameraViewMode.VRGogglesMode);
            if (GUILayout.Button(arHologramActive ? "AR: ON" : "AR: OFF", GUILayout.Width(65))) ToggleARHolograms();
            if (GUILayout.Button("⚙️ Setup [O]", GUILayout.Width(90))) { showInterviewSetupModal = !showInterviewSetupModal; }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // ── MAIN INTERACTIVE PANEL (ROLE SENSITIVE) ──
            if (activeRole == InterviewRole.Interviewer)
            {
                RenderInterviewerRoleGUI();
            }
            else
            {
                RenderCandidateRoleGUI();
            }

            // ── END INTERVIEW MODAL POPUP ──
            if (showEndInterviewModal)
            {
                RenderEndInterviewModal(screenW, screenH);
            }

            // ── AI INTERVIEW CONFIGURATION MODAL (STEP 1) ──
            if (showInterviewSetupModal)
            {
                RenderInterviewSetupModal(screenW, screenH);
            }

            // ── COMPREHENSIVE PERFORMANCE REPORT MODAL (STEP 6) ──
            if (showReportModal)
            {
                RenderReportModal(screenW, screenH);
            }

            // ── VR ACCESSIBLE CONTROLS BAR (SECTION 10) ──
            RenderVRControlsBar(screenW, screenH);

            // ── 3D CHARACTER HOVER TOOLTIP ──
            if (!string.IsNullOrEmpty(hoveredAvatarName))
            {
                RenderHoverTooltip();
            }

            // ── LIVE SPEECH-TO-TEXT CLOSED CAPTIONING BANNER (BIG FONT) ──
            if (showLiveCaptionsOnScreen)
            {
                RenderBigFontSpeechToTextBanner(screenW, screenH);
            }

            // ── LIVE VOICE ANSWER CENTER HUD (DURING RECORDING, REVIEW, & DELIVERY) ──
            if (currentDialoguePhase == DialoguePhase.UserVoiceRecording || 
                currentDialoguePhase == DialoguePhase.UserVoiceReview ||
                currentDialoguePhase == DialoguePhase.CandidateDeliveringResponse ||
                currentDialoguePhase == DialoguePhase.InterviewerAcknowledging)
            {
                RenderLiveVoiceAnswerCenterHUD(screenW, screenH);
            }
        }

        private void RenderHoverTooltip()
        {
            Vector2 mousePos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) mousePos = Mouse.current.position.ReadValue();
#else
            if (mousePos == Vector2.zero) mousePos = Input.mousePosition;
#endif
            float guiX = mousePos.x + 18;
            float guiY = Screen.height - mousePos.y - 12;

            string tipText = "";
            Color badgeColor = Color.white;

            if (hoveredAvatarName == "Interviewer")
            {
                badgeColor = new Color(0.3f, 0.85f, 1f);
                tipText = (currentView == CameraViewMode.InterviewerView || activeRole == InterviewRole.Interviewer)
                    ? "👔 <b>Dr. Sarah Vance (Interviewer)</b>\n▶ Click / [Space] to Ask Question & Hear Candidate Answer"
                    : "👔 <b>Click Interviewer</b>\n▶ Switch to Interviewer Person's View (See Candidate)";
            }
            else if (hoveredAvatarName == "Candidate")
            {
                badgeColor = new Color(0.2f, 1.0f, 0.5f);
                tipText = (currentView == CameraViewMode.InterviewerView || activeRole == InterviewRole.Interviewer)
                    ? "🧑‍💻 <b>Alex Mercer (Candidate)</b>\n▶ Click / [Space] to Ask Question & Hear Candidate Answer"
                    : (currentView == CameraViewMode.CandidateARView || activeRole == InterviewRole.Candidate
                        ? "🧑‍💻 <b>Alex Mercer (Candidate)</b>\n<i>Currently in Candidate POV</i>"
                        : "🧑‍💻 <b>Click Candidate</b>\n▶ Switch to Candidate Person's View (See Interviewer)");
            }

            if (!string.IsNullOrEmpty(tipText))
            {
                GUIContent content = new GUIContent(tipText);
                GUIStyle tipStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 12,
                    richText = true,
                    alignment = TextAnchor.MiddleLeft
                };
                tipStyle.normal.textColor = Color.white;

                Vector2 size = tipStyle.CalcSize(content);
                size.x += 16;
                size.y += 8;

                // Clamp to screen bounds
                if (guiX + size.x > Screen.width - 10) guiX = mousePos.x - size.x - 10;
                if (guiY + size.y > Screen.height - 10) guiY = Screen.height - size.y - 10;
                if (guiY < 10) guiY = 10;

                Color prevCol = GUI.color;
                GUI.color = badgeColor;
                GUI.Box(new Rect(guiX, guiY, size.x, size.y), content, tipStyle);
                GUI.color = prevCol;
            }
        }

        private void RenderBigFontSpeechToTextBanner(int screenW, int screenH)
        {
            int bannerW = Mathf.Clamp(screenW - 80, 720, 1180);
            int bannerH = useGiantSubtitles ? 145 : 120;
            int bannerX = (screenW - bannerW) / 2;
            int bannerY = screenH - bannerH - 18;

            string speakerTitle = "🎙️ LIVE SPEECH-TO-TEXT CLOSED CAPTIONING";
            string transcriptText = "";
            bool isInterviewer = true;
            bool isStreaming = false;

            if (currentDialoguePhase == DialoguePhase.UserVoiceRecording)
            {
                isStreaming = true;
                isInterviewer = false;
                speakerTitle = $"🔴 ALEX MERCER (YOU) — LIVE VOICE RECORDING IN PROGRESS";
                transcriptText = "Speak your answer clearly into your headset / PC microphone. Candidate avatar is matching your live voice! Press [Spacebar] or click 'Finish Recording' when finished.";
            }
            else if (currentDialoguePhase == DialoguePhase.UserVoiceReview)
            {
                isStreaming = false;
                isInterviewer = false;
                speakerTitle = $"✓ ALEX MERCER (YOU) — VOICE ANSWER RECORDED ({userSpeechDuration:F1}s)";
                transcriptText = $"{lastVoiceAnswerTranscript}  [Press Space to Submit or R to Re-record]";
            }
            else if (sttController != null && (!string.IsNullOrEmpty(sttController.currentLiveSubtitleText) || sttController.isTranscribing))
            {
                isStreaming = sttController.isTranscribing;
                isInterviewer = sttController.isCurrentSpeakerInterviewer;
                speakerTitle = isInterviewer 
                    ? $"👔 DR. SARAH VANCE (INTERVIEWER) — QUESTION #{currentQuestionIndex + 1}"
                    : "🧑‍💻 ALEX MERCER (CANDIDATE) — ANSWER";
                transcriptText = sttController.currentLiveSubtitleText;
            }
            else if (isDialogueActive)
            {
                isStreaming = true;
                bool intervSpeaking = interviewerAvatar != null && interviewerAvatar.isSpeaking;
                isInterviewer = intervSpeaking;
                speakerTitle = intervSpeaking
                    ? $"👔 DR. SARAH VANCE (INTERVIEWER) — QUESTION #{currentQuestionIndex + 1}"
                    : "🧑‍💻 ALEX MERCER (CANDIDATE) — ANSWER";
                transcriptText = intervSpeaking ? interviewQuestions[currentQuestionIndex] : candidateAnswers[currentQuestionIndex];
            }
            else
            {
                speakerTitle = $"👔 INTERVIEW QUESTION #{currentQuestionIndex + 1}/{interviewQuestions.Length}";
                transcriptText = interviewQuestions[currentQuestionIndex];
            }

            if (string.IsNullOrEmpty(transcriptText))
            {
                transcriptText = interviewQuestions[currentQuestionIndex];
            }

            // Dark semi-transparent container box
            GUI.Box(new Rect(bannerX, bannerY, bannerW, bannerH), GUIContent.none);

            // Glowing top accent line (3px)
            Color accentColor = isStreaming 
                ? (isInterviewer ? new Color(1.0f, 0.88f, 0.2f) : new Color(0.2f, 0.95f, 1.0f))
                : new Color(0.35f, 0.75f, 0.9f);

            Color prevCol = GUI.color;
            GUI.color = accentColor;
            GUI.DrawTexture(new Rect(bannerX + 2, bannerY + 1, bannerW - 4, 3), Texture2D.whiteTexture);
            GUI.color = prevCol;

            // Content Area inside Banner
            GUILayout.BeginArea(new Rect(bannerX + 16, bannerY + 8, bannerW - 32, bannerH - 16));
            GUILayout.BeginHorizontal();

            GUIStyle speakerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            speakerStyle.normal.textColor = accentColor;
            GUILayout.Label(speakerTitle, speakerStyle);

            GUILayout.FlexibleSpace();

            // Status Badge
            if (isStreaming)
            {
                GUIStyle pulseStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 12 };
                pulseStyle.normal.textColor = new Color(0.0f, 0.95f, 1.0f);
                string dot = (Time.time % 0.6f < 0.3f) ? "●" : "○";
                GUILayout.Label($"{dot} LIVE STT CONVERTING...", pulseStyle);
            }
            else
            {
                GUIStyle doneStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 12 };
                doneStyle.normal.textColor = new Color(0.2f, 1.0f, 0.5f);
                GUILayout.Label("✓ SPEECH-TO-TEXT READY", doneStyle);
            }

            GUILayout.Space(12);

            // Font Size Switcher Button (BIG vs GIANT)
            if (GUILayout.Button(useGiantSubtitles ? "🗚 Font: GIANT (26pt)" : "🗛 Font: BIG (21pt)", GUILayout.Height(22), GUILayout.Width(150)))
            {
                useGiantSubtitles = !useGiantSubtitles;
            }

            GUILayout.EndHorizontal();

            // Main Big Font Dialogue Text
            GUILayout.Space(2);
            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = useGiantSubtitles ? 26 : 21,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft
            };
            textStyle.normal.textColor = isInterviewer ? new Color(1.0f, 0.96f, 0.45f) : new Color(0.85f, 0.98f, 1.0f);

            string cursor = isStreaming ? ((Time.time % 0.6f < 0.3f) ? " ▌" : "") : "";
            string displayText = transcriptText.Trim('"');
            GUILayout.Label($"\"{displayText}\"{cursor}", textStyle);

            GUILayout.EndArea();
        }

        private void RenderLiveVoiceAnswerCenterHUD(int screenW, int screenH)
        {
            int hudW = 700;
            int hudH = 265;
            int hudX = (screenW - hudW) / 2;
            int hudY = screenH - hudH - 65;

            GUI.Box(new Rect(hudX, hudY, hudW, hudH), GUIContent.none);

            bool isRecording = (currentDialoguePhase == DialoguePhase.UserVoiceRecording);
            bool isDelivering = (currentDialoguePhase == DialoguePhase.CandidateDeliveringResponse);
            bool isAcknowledging = (currentDialoguePhase == DialoguePhase.InterviewerAcknowledging);

            Color accentColor = isRecording ? new Color(1.0f, 0.25f, 0.25f) :
                               (isDelivering ? new Color(0.2f, 0.9f, 1.0f) :
                               (isAcknowledging ? new Color(1.0f, 0.85f, 0.25f) : new Color(0.2f, 0.95f, 0.5f)));

            // Glowing top accent bar
            Color prevColor = GUI.color;
            GUI.color = accentColor;
            GUI.DrawTexture(new Rect(hudX + 2, hudY + 1, hudW - 4, 3), Texture2D.whiteTexture);
            GUI.color = prevColor;

            GUILayout.BeginArea(new Rect(hudX + 16, hudY + 10, hudW - 32, hudH - 20));

            // Header Row
            GUILayout.BeginHorizontal();
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = accentColor;

            if (isRecording)
            {
                string dot = (Time.time % 0.6f < 0.3f) ? "🔴" : "⭕";
                GUILayout.Label($"{dot} LIVE VOICE RECORDING — ANSWERING QUESTION #{currentQuestionIndex + 1}", titleStyle);
            }
            else if (isDelivering)
            {
                GUILayout.Label($"🗣️ ALEX MERCER (YOU) — DELIVERING RESPONSE TO DR. VANCE", titleStyle);
            }
            else if (isAcknowledging)
            {
                GUILayout.Label($"👔 DR. SARAH VANCE — ACKNOWLEDGING ANSWER", titleStyle);
            }
            else
            {
                GUILayout.Label($"✓ VOICE ANSWER RECORDED — QUESTION #{currentQuestionIndex + 1}", titleStyle);
            }

            GUILayout.FlexibleSpace();

            // Timer
            float duration = isRecording 
                ? (audioController != null ? audioController.userRecordingDuration : 0f) 
                : userSpeechDuration;
            int mins = Mathf.FloorToInt(duration / 60f);
            int secs = Mathf.FloorToInt(duration % 60f);
            GUIStyle timerStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            timerStyle.normal.textColor = Color.white;
            GUILayout.Label($"⏱️ {mins:D2}:{secs:D2}", timerStyle);

            GUILayout.EndHorizontal();

            // Question prompt
            GUIStyle qPromptStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 12, fontStyle = FontStyle.Italic };
            qPromptStyle.normal.textColor = new Color(1.0f, 0.95f, 0.6f);
            GUILayout.Label($"Dr. Vance asks (AI Generated): \"{GetCurrentQuestionText().Trim('"')}\"", qPromptStyle);

            if (isRecording)
            {
                // Live VU Meter & Status
                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Mic Input Level:", GUILayout.Width(110));
                float loudness = audioController != null ? audioController.currentMicLoudness : 0f;
                int bars = 24;
                int filled = Mathf.Clamp(Mathf.RoundToInt(loudness * 4.5f * bars), 0, bars);
                GUIStyle vuStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 13 };
                vuStyle.normal.textColor = (loudness > 0.02f) ? new Color(0.0f, 1.0f, 0.45f) : Color.gray;
                GUILayout.Label("[" + new string('|', filled) + new string('.', bars - filled) + "]", vuStyle);

                GUILayout.FlexibleSpace();
                bool speaking = (audioController != null && audioController.isUserVoiceDetected);
                GUIStyle speakStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                speakStyle.normal.textColor = speaking ? new Color(0.2f, 1.0f, 0.5f) : Color.gray;
                GUILayout.Label(speaking ? "🗣️ SPEAKING DETECTED" : "👂 LISTENING...", speakStyle);
                GUILayout.EndHorizontal();

                // Mic hardware device label
                GUILayout.Space(2);
                string micName = (audioController != null) ? audioController.GetSelectedMicName() : "Default Microphone";
                string micType = (audioController != null) ? audioController.GetSelectedMicType() : "VR Headset Mic";
                GUILayout.Label($"Hardware: <color=#00e5ff><b>{micName}</b></color> (<color=#00ff88><b>{micType}</b></color>)  |  Avatar lip-sync active", GUI.skin.label);

                // Permission warning if not authorized
                if (audioController != null && !audioController.hasMicrophonePermission)
                {
                    GUIStyle warnStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                    warnStyle.normal.textColor = new Color(1.0f, 0.35f, 0.35f);
                    GUILayout.Label("⚠️ " + audioController.micPermissionStatusMessage, warnStyle);
                }

                // Text Fallback Input Row (Section 1 requirement)
                GUILayout.Space(2);
                GUILayout.BeginHorizontal();
                GUILayout.Label("⌨️ Text Fallback:", GUILayout.Width(110));
                textFallbackInput = GUILayout.TextField(textFallbackInput, GUILayout.Height(24));
                if (!string.IsNullOrEmpty(textFallbackInput))
                {
                    GUI.color = new Color(0.2f, 1.0f, 0.5f);
                    if (GUILayout.Button("Submit Typed Answer", GUILayout.Width(160), GUILayout.Height(24)))
                    {
                        SubmitTypedFallbackAnswer();
                    }
                    GUI.color = Color.white;
                }
                GUILayout.EndHorizontal();

                // Action Buttons
                GUILayout.Space(6);
                GUILayout.BeginHorizontal();

                GUI.color = new Color(1.0f, 0.25f, 0.3f);
                if (GUILayout.Button("⏹ FINISH & DELIVER [Space / Trigger]", GUILayout.Height(34)))
                {
                    FinishVoiceAnswerRecording();
                    SubmitVoiceAnswer();
                }
                GUI.color = Color.white;

                if (GUILayout.Button("Switch Mic", GUILayout.Height(34), GUILayout.Width(95)))
                {
                    if (audioController != null) audioController.CycleNextMicrophoneDevice();
                }

                if (GUILayout.Button("Cancel", GUILayout.Height(34), GUILayout.Width(75)))
                {
                    if (audioController != null) audioController.DiscardRecordedUserVoice();
                    currentDialoguePhase = DialoguePhase.Idle;
                    isDialogueActive = false;
                }

                GUILayout.EndHorizontal();
            }
            else if (isDelivering)
            {
                GUILayout.Space(6);
                GUIStyle delivStyle = new GUIStyle(GUI.skin.box) { wordWrap = true, fontSize = 13 };
                delivStyle.normal.textColor = new Color(0.3f, 0.95f, 1.0f);
                GUILayout.Label($"<b>Candidate Avatar Responding to Interviewer:</b>\n{lastVoiceAnswerTranscript}", delivStyle);

                GUILayout.Space(4);
                GUILayout.Label("Candidate avatar is speaking your recorded voice to Dr. Vance with animated visemes and gestures across the table.", GUI.skin.label);
            }
            else if (isAcknowledging)
            {
                GUILayout.Space(6);
                GUIStyle ackBoxStyle = new GUIStyle(GUI.skin.box) { wordWrap = true, fontSize = 13 };
                ackBoxStyle.normal.textColor = new Color(1.0f, 0.95f, 0.45f);
                string ackStr = (interviewerAcknowledgments != null && currentQuestionIndex < interviewerAcknowledgments.Length)
                    ? interviewerAcknowledgments[currentQuestionIndex]
                    : "\"Thank you, Alex. Great answer.\"";
                GUILayout.Label($"<b>Dr. Vance Evaluates:</b>\n{ackStr}", ackBoxStyle);

                GUILayout.Space(4);
                GUILayout.Label("Interviewer is evaluating response and automatically preparing the next question...", GUI.skin.label);
            }
            else // Review Phase
            {
                GUILayout.Space(4);
                GUIStyle transcriptBoxStyle = new GUIStyle(GUI.skin.box) { wordWrap = true, fontSize = 13 };
                transcriptBoxStyle.normal.textColor = new Color(0.3f, 0.95f, 1.0f);
                GUILayout.Label($"<b>Spoken Transcript ({userSpeechDuration:F1}s):</b>\n{lastVoiceAnswerTranscript}", transcriptBoxStyle);

                GUILayout.Space(6);
                GUILayout.BeginHorizontal();

                GUI.color = new Color(0.2f, 0.95f, 0.45f);
                if (GUILayout.Button("✅ DELIVER ANSWER TO DR. VANCE [Space / Trigger]", GUILayout.Height(36)))
                {
                    SubmitVoiceAnswer();
                }
                GUI.color = Color.white;

                GUI.color = new Color(0.3f, 0.85f, 1.0f);
                if (GUILayout.Button(isCandidatePlaybackActive ? "🔊 Playing..." : "🔊 Play Back Audio", GUILayout.Height(36), GUILayout.Width(140)))
                {
                    PlaybackVoiceAnswer();
                }
                GUI.color = Color.white;

                if (GUILayout.Button("🔄 Re-record [R]", GUILayout.Height(36), GUILayout.Width(110)))
                {
                    ReRecordVoiceAnswer();
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
        }

        private void RenderInterviewerRoleGUI()
        {
            int panelW = 440;
            int panelH = 540;
            int margin = 20;

            GUI.Box(new Rect(margin, 76, panelW, panelH), GUIContent.none);
            GUILayout.BeginArea(new Rect(margin + 12, 86, panelW - 24, panelH - 20));

            // Header: Candidate Dossier
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = new Color(0.3f, 0.85f, 1f);
            GUILayout.Label("👔 INTERVIEWER CONTROL CONSOLE", headerStyle);

            int mins = Mathf.FloorToInt(interviewTimer / 60f);
            int secs = Mathf.FloorToInt(interviewTimer % 60f);
            GUILayout.Label($"Session Timer: <color=#ffffff><b>{mins:D2}:{secs:D2}</b></color>  |  Spatial Distance: <b>1.3m (Opposite)</b>", GUI.skin.label);

            GUILayout.Space(6);
            GUILayout.Label("<b>Candidate Profile:</b>", GUI.skin.label);
            GUIStyle cardStyle = new GUIStyle(GUI.skin.box);
            cardStyle.normal.textColor = Color.white;
            GUILayout.Label($"<b>{candidateName}</b> — {candidateRole}\n{candidateExperience}  |  Match Score: <color=#00ff88>{candidateMatchScore}%</color>\nSkills: {string.Join(" • ", candidateSkills)}", cardStyle);

            GUILayout.Space(6);
            string qCategory = (aiQuestionGenerator != null && currentQuestionIndex < aiQuestionGenerator.GetTotalQuestionCount()) 
                ? aiQuestionGenerator.GetQuestionData(currentQuestionIndex)?.category 
                : "AI Technical Evaluation";
            GUILayout.Label($"<b>✨ AI Question {currentQuestionIndex + 1} ({qCategory}):</b>", GUI.skin.label);
            GUIStyle qStyle = new GUIStyle(GUI.skin.box) { wordWrap = true, fontSize = 15, fontStyle = FontStyle.Bold };
            qStyle.normal.textColor = Color.yellow;
            GUILayout.Label(GetCurrentQuestionText(), qStyle);

            // AI Action Buttons
            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.2f, 0.9f, 1f);
            if (GUILayout.Button("✨ New AI Question [G]", GUILayout.Height(24)))
            {
                GenerateNextAIQuestion();
            }
            GUI.color = new Color(1.0f, 0.85f, 0.3f);
            if (GUILayout.Button("✨ AI Follow-Up [F]", GUILayout.Height(24)))
            {
                GenerateAIFollowUpQuestion();
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            if (sttController != null && !string.IsNullOrEmpty(sttController.latestCandidateTranscript))
            {
                GUILayout.Space(4);
                GUILayout.Label("<b>Candidate Spoken Answer (Live STT):</b>", GUI.skin.label);
                GUIStyle aStyle = new GUIStyle(GUI.skin.box) { wordWrap = true, fontSize = 14, fontStyle = FontStyle.Bold };
                aStyle.normal.textColor = new Color(0.35f, 0.95f, 1f);
                GUILayout.Label(sttController.latestCandidateTranscript, aStyle);
            }

            // 1-5 Star Grading Rubric Buttons
            GUILayout.BeginHorizontal();
            GUILayout.Label("Score:", GUILayout.Width(45));
            for (int star = 1; star <= 5; star++)
            {
                bool isSelected = questionScores[currentQuestionIndex] >= star;
                GUI.color = isSelected ? new Color(1.0f, 0.85f, 0.2f) : Color.gray;
                if (GUILayout.Button(isSelected ? "★" : "☆", GUILayout.Width(30)))
                {
                    questionScores[currentQuestionIndex] = star;
                }
            }
            GUI.color = Color.white;
            if (GUILayout.Button("◀ Prev", GUILayout.Width(55))) PrevQuestion();
            if (GUILayout.Button("Next ▶", GUILayout.Width(55))) NextQuestion();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("<b>Interviewer Notes:</b>", GUI.skin.label);
            interviewerNotes = GUILayout.TextArea(interviewerNotes, GUILayout.Height(55));

            // Quick Tag Buttons
            GUILayout.BeginHorizontal();
            foreach (var tag in quickEvaluationTags)
            {
                if (GUILayout.Button($"+ {tag}", GUILayout.Height(20)))
                {
                    interviewerNotes += $"\n• {tag}";
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("<b>Microphone & Answer Mode:</b>", GUI.skin.label);
            GUILayout.BeginHorizontal();
            if (answerWithOwnVoice) GUI.color = new Color(0.2f, 1.0f, 0.45f);
            if (GUILayout.Button(answerWithOwnVoice ? "🎙️ Mode: Real Mic" : "🤖 Mode: Auto", GUILayout.Width(130)))
            {
                ToggleAnswerWithOwnVoice();
            }
            GUI.color = Color.white;
            string micDev = audioController != null ? audioController.GetSelectedMicName() : "Mic";
            if (GUILayout.Button($"🎤 {micDev} [Switch]"))
            {
                if (audioController != null) audioController.CycleNextMicrophoneDevice();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            // Mic & Action Controls
            GUILayout.BeginHorizontal();
            if (audioController != null)
            {
                if (GUILayout.Button(audioController.isMuted ? "🎤 Unmute [M]" : "🔇 Mute [M]", GUILayout.Width(110)))
                {
                    audioController.ToggleMute();
                }
            }

            if (currentDialoguePhase == DialoguePhase.UserVoiceRecording)
            {
                GUI.color = new Color(1.0f, 0.25f, 0.3f);
                if (GUILayout.Button("⏹ Finish Voice Answer [Space]"))
                {
                    FinishVoiceAnswerRecording();
                }
                GUI.color = Color.white;
            }
            else if (currentDialoguePhase == DialoguePhase.UserVoiceReview)
            {
                GUI.color = new Color(0.2f, 0.95f, 0.45f);
                if (GUILayout.Button("✅ Accept & Submit [Space]"))
                {
                    SubmitVoiceAnswer();
                }
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = isDialogueActive ? new Color(0.2f, 0.9f, 1f) : Color.white;
                if (GUILayout.Button(isDialogueActive ? "🔊 Question In Progress..." : (answerWithOwnVoice ? "💬 Ask Question & Record Voice [Space]" : "💬 Ask Question [Space]")))
                {
                    AskSpecificQuestion(currentQuestionIndex);
                }
                GUI.color = Color.white;
            }

            GUI.color = new Color(1f, 0.35f, 0.35f);
            if (GUILayout.Button("⏹ End", GUILayout.Width(65)))
            {
                showEndInterviewModal = true;
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void RenderCandidateRoleGUI()
        {
            int panelW = 440;
            int panelH = 500;
            int margin = 20;

            GUI.Box(new Rect(margin, 76, panelW, panelH), GUIContent.none);
            GUILayout.BeginArea(new Rect(margin + 12, 86, panelW - 24, panelH - 20));

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = new Color(0.2f, 1f, 0.5f);
            GUILayout.Label("🧑‍💻 CANDIDATE TELEPRESENCE INTERFACE", headerStyle);

            int mins = Mathf.FloorToInt(interviewTimer / 60f);
            int secs = Mathf.FloorToInt(interviewTimer % 60f);
            GUILayout.Label($"Session Timer: <color=#ffffff><b>{mins:D2}:{secs:D2}</b></color>  |  Eye Contact: <b>Face-to-Face</b>", GUI.skin.label);

            GUILayout.Space(6);
            GUIStyle cardStyle = new GUIStyle(GUI.skin.box);
            GUILayout.Label($"<b>Interviewer:</b> {interviewerName}\n{interviewerTitle}  |  Spatial Audio: <b>Connected</b>", cardStyle);

            GUILayout.Space(6);
            GUILayout.Label("<b>✨ Question Being Asked by Dr. Vance (AI Generated):</b>", GUI.skin.label);
            GUIStyle qStyle = new GUIStyle(GUI.skin.box) { wordWrap = true, fontSize = 15, fontStyle = FontStyle.Bold };
            qStyle.normal.textColor = Color.yellow;
            GUILayout.Label(GetCurrentQuestionText(), qStyle);

            GUILayout.Space(6);
            GUILayout.Label("<b>Candidate Scratchpad / Answer Notes:</b>", GUI.skin.label);
            candidateNotes = GUILayout.TextArea(candidateNotes, GUILayout.Height(65));

            GUILayout.Space(4);
            GUILayout.Label("<b>Microphone & Answer Mode:</b>", GUI.skin.label);
            GUILayout.BeginHorizontal();
            if (answerWithOwnVoice) GUI.color = new Color(0.2f, 1.0f, 0.45f);
            if (GUILayout.Button(answerWithOwnVoice ? "🎙️ Mode: Real Mic" : "🤖 Mode: Auto", GUILayout.Width(130)))
            {
                ToggleAnswerWithOwnVoice();
            }
            GUI.color = Color.white;
            string candMic = audioController != null ? audioController.GetSelectedMicName() : "Mic";
            if (GUILayout.Button($"🎤 {candMic} [Switch]"))
            {
                if (audioController != null) audioController.CycleNextMicrophoneDevice();
            }
            GUILayout.EndHorizontal();

            // Live VU Meter
            if (audioController != null)
            {
                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Live Mic Level:", GUILayout.Width(95));
                float vuLevel = audioController.isMuted ? 0f : Mathf.Clamp01(audioController.GetActiveOutputLoudness() * 3.5f);
                int bars = 16;
                int filled = Mathf.RoundToInt(vuLevel * bars);
                GUIStyle vuStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                vuStyle.normal.textColor = (vuLevel > 0.05f) ? new Color(0.2f, 1f, 0.5f) : Color.gray;
                GUILayout.Label("[" + new string('|', filled) + new string('.', bars - filled) + "]", vuStyle);

                GUILayout.FlexibleSpace();
                if (currentDialoguePhase == DialoguePhase.UserVoiceRecording)
                {
                    GUIStyle recStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                    recStyle.normal.textColor = new Color(1.0f, 0.25f, 0.3f);
                    GUILayout.Label("🔴 RECORDING LIVE", recStyle);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (audioController != null)
            {
                if (GUILayout.Button(audioController.isMuted ? "🎤 Unmute [M]" : "🔇 Mute [M]", GUILayout.Width(110)))
                {
                    audioController.ToggleMute();
                }
            }

            if (currentDialoguePhase == DialoguePhase.UserVoiceRecording)
            {
                GUI.color = new Color(1.0f, 0.25f, 0.3f);
                if (GUILayout.Button("⏹ FINISH ANSWERING [Space]"))
                {
                    FinishVoiceAnswerRecording();
                }
                GUI.color = Color.white;
            }
            else if (currentDialoguePhase == DialoguePhase.UserVoiceReview)
            {
                GUI.color = new Color(0.2f, 0.95f, 0.45f);
                if (GUILayout.Button("✅ SUBMIT [Space]"))
                {
                    SubmitVoiceAnswer();
                }
                GUI.color = Color.white;

                if (GUILayout.Button("🔊 Play", GUILayout.Width(65)))
                {
                    PlaybackVoiceAnswer();
                }

                if (GUILayout.Button("🔄 Re-rec", GUILayout.Width(75)))
                {
                    ReRecordVoiceAnswer();
                }
            }
            else if (currentDialoguePhase == DialoguePhase.AnswerCompleted)
            {
                GUI.color = new Color(0.2f, 0.85f, 1.0f);
                if (GUILayout.Button("💬 Next Question ▶ [Space]"))
                {
                    NextQuestion();
                    AskSpecificQuestion(currentQuestionIndex);
                }
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.2f, 1.0f, 0.45f);
                if (GUILayout.Button(answerWithOwnVoice ? "🎙️ Answer with Voice [Space]" : "💬 Respond [Space]"))
                {
                    NextDialogue();
                }
                GUI.color = Color.white;
            }

            GUI.color = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Exit", GUILayout.Width(60)))
            {
                showEndInterviewModal = true;
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void RenderEndInterviewModal(int screenW, int screenH)
        {
            int modalW = 460;
            int modalH = 240;
            int modalX = (screenW - modalW) / 2;
            int modalY = (screenH - modalH) / 2;

            GUI.Box(new Rect(modalX, modalY, modalW, modalH), GUIContent.none);
            GUILayout.BeginArea(new Rect(modalX + 16, modalY + 16, modalW - 32, modalH - 32));

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = Color.white;
            GUILayout.Label("CONCLUDE INTERVIEW SESSION", titleStyle);
            GUILayout.Space(6);

            float totalScore = 0f;
            int scoredCount = 0;
            foreach (var s in questionScores)
            {
                if (s > 0) { totalScore += s; scoredCount++; }
            }
            float avgScore = scoredCount > 0 ? (totalScore / scoredCount) : 4.8f;

            GUILayout.Label($"Candidate: <b>{candidateName}</b> ({candidateRole})\n" +
                            $"Overall Rubric Assessment: <color=#00ff88><b>{avgScore:F1} / 5.0</b> (Recommendation: Strong Hire)</color>\n" +
                            $"Session Duration: <b>{Mathf.FloorToInt(interviewTimer / 60f):D2}:{Mathf.FloorToInt(interviewTimer % 60f):D2}</b>", GUI.skin.box);

            GUILayout.Space(12);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Return to Session", GUILayout.Height(32)))
            {
                showEndInterviewModal = false;
            }

            GUI.color = new Color(0.2f, 0.9f, 0.4f);
            if (GUILayout.Button("Submit Scorecard & Finalize", GUILayout.Height(32)))
            {
                showEndInterviewModal = false;
                EndInterviewAndShowReport();
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void RenderInterviewSetupModal(int screenW, int screenH)
        {
            int modalW = 760;
            int modalH = 550;
            int modalX = (screenW - modalW) / 2;
            int modalY = (screenH - modalH) / 2;

            GUI.Box(new Rect(modalX, modalY, modalW, modalH), GUIContent.none);
            Color prevColor = GUI.color;
            GUI.color = new Color(0.1f, 0.8f, 1.0f);
            GUI.DrawTexture(new Rect(modalX + 2, modalY + 1, modalW - 4, 3), Texture2D.whiteTexture);
            GUI.color = prevColor;

            GUILayout.BeginArea(new Rect(modalX + 20, modalY + 16, modalW - 40, modalH - 32));

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = new Color(0.2f, 0.9f, 1.0f);
            GUILayout.Label("🎯 STEP 1: CONFIGURE AI INTERVIEW SESSION", titleStyle);

            GUIStyle subtitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Italic };
            subtitleStyle.normal.textColor = Color.gray;
            GUILayout.Label("Configure target job role, experience level, interview focus, difficulty, and duration.", subtitleStyle);

            GUILayout.Space(10);

            var config = (aiQuestionGenerator != null) ? aiQuestionGenerator.currentConfig : new InterviewConfiguration();

            // 1. Job Role
            GUILayout.Label("<b>1. Target Job Role:</b>", GUI.skin.label);
            GUILayout.BeginHorizontal();
            if (DrawToggleOption("Software Engineer", config.jobRole == JobRoleOption.SoftwareEngineer)) config.jobRole = JobRoleOption.SoftwareEngineer;
            if (DrawToggleOption("Frontend Developer", config.jobRole == JobRoleOption.FrontendDeveloper)) config.jobRole = JobRoleOption.FrontendDeveloper;
            if (DrawToggleOption("Backend Developer", config.jobRole == JobRoleOption.BackendDeveloper)) config.jobRole = JobRoleOption.BackendDeveloper;
            if (DrawToggleOption("Data Analyst", config.jobRole == JobRoleOption.DataAnalyst)) config.jobRole = JobRoleOption.DataAnalyst;
            if (DrawToggleOption("HR", config.jobRole == JobRoleOption.HumanResources)) config.jobRole = JobRoleOption.HumanResources;
            if (DrawToggleOption("Custom", config.jobRole == JobRoleOption.CustomRole)) config.jobRole = JobRoleOption.CustomRole;
            GUILayout.EndHorizontal();

            if (config.jobRole == JobRoleOption.CustomRole)
            {
                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Custom Role Title:", GUILayout.Width(130));
                config.customRoleTitle = GUILayout.TextField(config.customRoleTitle, GUILayout.Height(24));
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8);

            // 2. Experience Level
            GUILayout.Label("<b>2. Experience Level:</b>", GUI.skin.label);
            GUILayout.BeginHorizontal();
            if (DrawToggleOption("Fresher (0-1y)", config.experienceLevel == ExperienceLevelOption.Fresher)) config.experienceLevel = ExperienceLevelOption.Fresher;
            if (DrawToggleOption("Junior (1-3y)", config.experienceLevel == ExperienceLevelOption.Junior)) config.experienceLevel = ExperienceLevelOption.Junior;
            if (DrawToggleOption("Mid-Level (3-6y)", config.experienceLevel == ExperienceLevelOption.MidLevel)) config.experienceLevel = ExperienceLevelOption.MidLevel;
            if (DrawToggleOption("Senior (6+y)", config.experienceLevel == ExperienceLevelOption.Senior)) config.experienceLevel = ExperienceLevelOption.Senior;
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // 3. Interview Type
            GUILayout.Label("<b>3. Interview Focus Type:</b>", GUI.skin.label);
            GUILayout.BeginHorizontal();
            if (DrawToggleOption("Technical Architecture", config.interviewType == InterviewTypeOption.Technical)) config.interviewType = InterviewTypeOption.Technical;
            if (DrawToggleOption("Behavioral (STAR)", config.interviewType == InterviewTypeOption.Behavioral)) config.interviewType = InterviewTypeOption.Behavioral;
            if (DrawToggleOption("HR / Culture", config.interviewType == InterviewTypeOption.HR)) config.interviewType = InterviewTypeOption.HR;
            if (DrawToggleOption("Mixed Competency", config.interviewType == InterviewTypeOption.Mixed)) config.interviewType = InterviewTypeOption.Mixed;
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // 4. Difficulty
            GUILayout.Label("<b>4. Interview Difficulty:</b>", GUI.skin.label);
            GUILayout.BeginHorizontal();
            if (DrawToggleOption("🟢 Easy", config.difficulty == DifficultyOption.Easy)) config.difficulty = DifficultyOption.Easy;
            if (DrawToggleOption("🟡 Medium", config.difficulty == DifficultyOption.Medium)) config.difficulty = DifficultyOption.Medium;
            if (DrawToggleOption("🔴 Hard", config.difficulty == DifficultyOption.Hard)) config.difficulty = DifficultyOption.Hard;
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // 5. Duration
            GUILayout.Label("<b>5. Session Duration:</b>", GUI.skin.label);
            GUILayout.BeginHorizontal();
            if (DrawToggleOption("⏱️ 5 Minutes", config.duration == InterviewDurationOption.FiveMinutes)) config.duration = InterviewDurationOption.FiveMinutes;
            if (DrawToggleOption("⏱️ 10 Minutes", config.duration == InterviewDurationOption.TenMinutes)) config.duration = InterviewDurationOption.TenMinutes;
            if (DrawToggleOption("⏱️ 15 Minutes", config.duration == InterviewDurationOption.FifteenMinutes)) config.duration = InterviewDurationOption.FifteenMinutes;
            if (DrawToggleOption("⏱️ 30 Minutes", config.duration == InterviewDurationOption.ThirtyMinutes)) config.duration = InterviewDurationOption.ThirtyMinutes;
            GUILayout.EndHorizontal();

            GUILayout.Space(16);

            // Action Buttons
            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.2f, 1.0f, 0.5f);
            if (GUILayout.Button("🚀 START VR INTERVIEW [STEP 2]", GUILayout.Height(42)))
            {
                StartInterviewSession();
            }
            GUI.color = Color.white;

            if (GUILayout.Button("Cancel", GUILayout.Height(42), GUILayout.Width(100)))
            {
                showInterviewSetupModal = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private bool DrawToggleOption(string label, bool isSelected)
        {
            Color prev = GUI.color;
            if (isSelected) GUI.color = new Color(0.2f, 0.95f, 1.0f);
            bool clicked = GUILayout.Button(label, GUILayout.Height(28));
            GUI.color = prev;
            return clicked;
        }

        private void RenderReportModal(int screenW, int screenH)
        {
            int modalW = 860;
            int modalH = 600;
            int modalX = (screenW - modalW) / 2;
            int modalY = (screenH - modalH) / 2;

            GUI.Box(new Rect(modalX, modalY, modalW, modalH), GUIContent.none);
            Color prevColor = GUI.color;
            GUI.color = new Color(0.2f, 1.0f, 0.5f);
            GUI.DrawTexture(new Rect(modalX + 2, modalY + 1, modalW - 4, 3), Texture2D.whiteTexture);
            GUI.color = prevColor;

            GUILayout.BeginArea(new Rect(modalX + 20, modalY + 16, modalW - 40, modalH - 32));

            var rep = sessionReport ?? (aiQuestionGenerator != null ? aiQuestionGenerator.GenerateFinalInterviewReport() : new InterviewReportData());

            // Header
            GUILayout.BeginHorizontal();
            GUIStyle repTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            repTitleStyle.normal.textColor = new Color(0.2f, 1.0f, 0.5f);
            GUILayout.Label("📊 STEP 6: COMPREHENSIVE INTERVIEW EVALUATION REPORT", repTitleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕ Close", GUILayout.Width(70)))
            {
                showReportModal = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label($"Candidate: <b>Alex Mercer (You)</b> | Role: <b>{rep.jobRole}</b> ({rep.experienceLevel}) | Focus: <b>{rep.interviewType}</b> ({rep.difficulty})", GUI.skin.label);

            GUILayout.Space(6);

            // Score Badges Row
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUIStyle scoreBigStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            scoreBigStyle.normal.textColor = new Color(0.1f, 1.0f, 0.6f);

            GUILayout.BeginVertical(GUILayout.Width(150));
            GUILayout.Label("OVERALL SCORE", GUI.skin.label);
            GUILayout.Label($"{rep.overallScore} / 100", scoreBigStyle);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label($"• Relevance: <b>{rep.relevanceScore}%</b>    • Clarity: <b>{rep.clarityScore}%</b>", GUI.skin.label);
            GUILayout.Label($"• Technical Accuracy: <b>{rep.technicalScore}%</b>    • Completeness: <b>{rep.completenessScore}%</b>", GUI.skin.label);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // Scrollable Details
            reportScrollPos = GUILayout.BeginScrollView(reportScrollPos, GUILayout.Height(350));

            // Strengths
            GUILayout.Label("<color=#00ff88><b>✓ Strengths & Core Competencies:</b></color>", GUI.skin.label);
            foreach (var s in rep.strengths) GUILayout.Label($"  • {s}", GUI.skin.label);

            GUILayout.Space(6);

            // Weaknesses
            GUILayout.Label("<color=#ffaa00><b>⚠️ Areas for Improvement & Identified Gaps:</b></color>", GUI.skin.label);
            foreach (var w in rep.weaknesses) GUILayout.Label($"  • {w}", GUI.skin.label);

            GUILayout.Space(6);

            // Actionable suggestions
            GUILayout.Label("<color=#00e5ff><b>💡 Actionable Recommendations:</b></color>", GUI.skin.label);
            foreach (var sug in rep.improvementSuggestions) GUILayout.Label($"  • {sug}", GUI.skin.label);

            GUILayout.Space(6);

            // Practice topics
            GUILayout.Label("<color=#ff77ff><b>📚 Suggested Topics to Practice Before Real Interview:</b></color>", GUI.skin.label);
            foreach (var top in rep.suggestedPracticeTopics) GUILayout.Label($"  • {top}", GUI.skin.label);

            GUILayout.Space(8);

            // Chronological Q&A review
            GUILayout.Label("<b>📝 Chronological Question & Answer Review:</b>", GUI.skin.label);
            if (rep.qnaReview != null)
            {
                foreach (var q in rep.qnaReview)
                {
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label($"<b>Q#{q.questionIndex + 1} ({q.category}):</b> \"{q.questionText.Trim('"')}\"", GUI.skin.label);
                    string ans = !string.IsNullOrEmpty(q.candidateTranscript) ? q.candidateTranscript : "(No spoken answer recorded)";
                    GUILayout.Label($"<b>Your Spoken Answer ({q.candidateSpeechDuration:F1}s):</b> {ans}", GUI.skin.label);
                    GUILayout.Label($"<color=#00e5ff>Score: {q.candidateScore}★</color> | <i>{q.internalEvaluation}</i>", GUI.skin.label);
                    GUILayout.EndVertical();
                    GUILayout.Space(4);
                }
            }

            GUILayout.EndScrollView();

            // Disclaimer
            GUIStyle discStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Italic, wordWrap = true };
            discStyle.normal.textColor = Color.gray;
            GUILayout.Label("ℹ️ " + rep.disclaimer, discStyle);

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.2f, 1.0f, 0.5f);
            if (GUILayout.Button("🔄 Start New Interview Session", GUILayout.Height(34)))
            {
                showReportModal = false;
                showInterviewSetupModal = true;
            }
            GUI.color = Color.white;

            if (GUILayout.Button("Close Report", GUILayout.Height(34), GUILayout.Width(130)))
            {
                showReportModal = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void RenderVRControlsBar(int screenW, int screenH)
        {
            int barW = 860;
            int barH = 46;
            int barX = (screenW - barW) / 2;
            int barY = screenH - barH - 12;

            GUI.Box(new Rect(barX, barY, barW, barH), GUIContent.none);
            GUILayout.BeginArea(new Rect(barX + 10, barY + 6, barW - 20, barH - 12));
            GUILayout.BeginHorizontal();

            // Pause / Resume
            GUI.color = isInterviewPaused ? new Color(1.0f, 0.85f, 0.2f) : Color.white;
            if (GUILayout.Button(isInterviewPaused ? "▶️ Resume [P]" : "⏸️ Pause [P]", GUILayout.Width(105), GUILayout.Height(32)))
            {
                TogglePauseInterview();
            }
            GUI.color = Color.white;

            // Repeat Question
            if (GUILayout.Button("🔄 Repeat [R]", GUILayout.Width(95), GUILayout.Height(32)))
            {
                RepeatCurrentQuestion();
            }

            // End Interview
            GUI.color = new Color(1.0f, 0.4f, 0.4f);
            if (GUILayout.Button("⏹️ End [Esc]", GUILayout.Width(95), GUILayout.Height(32)))
            {
                EndInterviewAndShowReport();
            }
            GUI.color = Color.white;

            // Mic On/Off
            bool micMuted = (audioController != null) && audioController.isMuted;
            GUI.color = micMuted ? new Color(1.0f, 0.35f, 0.35f) : new Color(0.2f, 1.0f, 0.5f);
            if (GUILayout.Button(micMuted ? "🔇 Mic OFF [M]" : "🎙️ Mic ON [M]", GUILayout.Width(110), GUILayout.Height(32)))
            {
                if (audioController != null) audioController.ToggleMute();
            }
            GUI.color = Color.white;

            // Volume
            GUILayout.Space(6);
            GUILayout.Label("🔊 Vol:", GUILayout.Width(45));
            float curVol = (audioController != null) ? audioController.masterVolume : 1.0f;
            float newVol = GUILayout.HorizontalSlider(curVol, 0.0f, 1.0f, GUILayout.Width(90), GUILayout.Height(20));
            if (audioController != null && Mathf.Abs(newVol - curVol) > 0.02f)
            {
                audioController.SetMasterVolume(newVol);
            }

            GUILayout.FlexibleSpace();

            // Progress Timer
            int elapsedMins = Mathf.FloorToInt(interviewTimer / 60f);
            int elapsedSecs = Mathf.FloorToInt(interviewTimer % 60f);
            int totalMins = Mathf.FloorToInt(sessionDurationMinutes);
            GUIStyle timerStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            timerStyle.normal.textColor = Color.white;
            GUILayout.Label($"⏱️ {elapsedMins:D2}:{elapsedSecs:D2} / {totalMins:D2}:00", timerStyle);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        public void EnsureBothAvatarsSeatedInChairs()
        {
            GameObject env = GameObject.Find("InterviewRoom_Environment");
            if (env == null) return;

            Transform existingInterv = env.transform.Find("Avatar_Interviewer_DrSarah");
            if (existingInterv != null)
            {
                if (interviewerAvatar == null)
                    interviewerAvatar = existingInterv.GetComponent<AvatarAnimationController>();
            }

            Transform existingCand = env.transform.Find("Avatar_Candidate_Alex_ARVR");
            if (existingCand != null)
            {
                if (candidateAvatar == null)
                    candidateAvatar = existingCand.GetComponent<AvatarAnimationController>();
            }

            if (interviewerAvatar != null && candidateAvatar != null)
            {
                if (interviewerAvatar.head != null) candidateAvatar.lookTarget = interviewerAvatar.head;
                if (candidateAvatar.head != null) interviewerAvatar.lookTarget = candidateAvatar.head;
            }

            if (interviewerAvatar != null && interviewerAvatar.GetComponent<Collider>() == null)
            {
                var box = interviewerAvatar.gameObject.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.85f, 0f);
                box.size = new Vector3(0.65f, 1.30f, 0.60f);
            }
            if (candidateAvatar != null && candidateAvatar.GetComponent<Collider>() == null)
            {
                var box = candidateAvatar.gameObject.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.85f, 0f);
                box.size = new Vector3(0.65f, 1.30f, 0.60f);
            }

            // Keep camera eye points calibrated to true face-to-face line of sight
            if (interviewerEyePoint != null)
            {
                Vector3 eyePos = new Vector3(0f, faceToFaceEyeHeight, 1.25f);
                if (interviewerAvatar != null)
                {
                    Vector3 headPos = (interviewerAvatar.head != null) ? interviewerAvatar.head.position : interviewerAvatar.transform.position + Vector3.up * faceToFaceEyeHeight;
                    eyePos = new Vector3(0f, faceToFaceEyeHeight, headPos.z + interviewerAvatar.transform.forward.z * 0.10f);
                }
                interviewerEyePoint.position = eyePos;

                Vector3 candHeadPos = (candidateAvatar != null && candidateAvatar.head != null)
                    ? candidateAvatar.head.position
                    : new Vector3(0f, faceToFaceEyeHeight, -1.25f);
                Vector3 toCandidate = (candHeadPos - interviewerEyePoint.position).normalized;
                interviewerEyePoint.rotation = (toCandidate != Vector3.zero)
                    ? Quaternion.LookRotation(toCandidate, Vector3.up)
                    : Quaternion.Euler(cameraPitchOffset, 180f, 0f);
            }
            if (candidateEyePoint != null)
            {
                Vector3 eyePos = new Vector3(0f, faceToFaceEyeHeight, -1.25f);
                if (candidateAvatar != null)
                {
                    Vector3 headPos = (candidateAvatar.head != null) ? candidateAvatar.head.position : candidateAvatar.transform.position + Vector3.up * faceToFaceEyeHeight;
                    eyePos = new Vector3(0f, faceToFaceEyeHeight, headPos.z + candidateAvatar.transform.forward.z * 0.10f);
                }
                candidateEyePoint.position = eyePos;

                Vector3 intervHeadPos = (interviewerAvatar != null && interviewerAvatar.head != null)
                    ? interviewerAvatar.head.position
                    : new Vector3(0f, faceToFaceEyeHeight, 1.25f);
                Vector3 toInterviewer = (intervHeadPos - candidateEyePoint.position).normalized;
                candidateEyePoint.rotation = (toInterviewer != Vector3.zero)
                    ? Quaternion.LookRotation(toInterviewer, Vector3.up)
                    : Quaternion.Euler(cameraPitchOffset, 0f, 0f);
            }

            ApplyRoleAvatarCulling();
        }

        /// <summary>
        /// Instantly snaps the camera to true face-to-face line of sight (1.20m eye height, 0 deg pitch)
        /// directly looking into the eyes of the opposite character seated across the table.
        /// </summary>
        public void ResetToFaceToFacePOV()
        {
            faceToFaceEyeHeight = 1.20f;
            cameraPitchOffset = 0f;

            if (interviewerEyePoint != null)
            {
                Vector3 eyePos = new Vector3(0f, faceToFaceEyeHeight, 1.25f);
                if (interviewerAvatar != null)
                {
                    Vector3 headPos = (interviewerAvatar.head != null) ? interviewerAvatar.head.position : interviewerAvatar.transform.position + Vector3.up * faceToFaceEyeHeight;
                    eyePos = new Vector3(0f, faceToFaceEyeHeight, headPos.z + interviewerAvatar.transform.forward.z * 0.10f);
                }
                interviewerEyePoint.position = eyePos;

                Vector3 candHeadPos = (candidateAvatar != null && candidateAvatar.head != null)
                    ? candidateAvatar.head.position
                    : new Vector3(0f, faceToFaceEyeHeight, -1.25f);
                Vector3 toCandidate = (candHeadPos - interviewerEyePoint.position).normalized;
                interviewerEyePoint.rotation = (toCandidate != Vector3.zero)
                    ? Quaternion.LookRotation(toCandidate, Vector3.up)
                    : Quaternion.Euler(0f, 180f, 0f);
            }
            if (candidateEyePoint != null)
            {
                Vector3 eyePos = new Vector3(0f, faceToFaceEyeHeight, -1.25f);
                if (candidateAvatar != null)
                {
                    Vector3 headPos = (candidateAvatar.head != null) ? candidateAvatar.head.position : candidateAvatar.transform.position + Vector3.up * faceToFaceEyeHeight;
                    eyePos = new Vector3(0f, faceToFaceEyeHeight, headPos.z + candidateAvatar.transform.forward.z * 0.10f);
                }
                candidateEyePoint.position = eyePos;

                Vector3 intervHeadPos = (interviewerAvatar != null && interviewerAvatar.head != null)
                    ? interviewerAvatar.head.position
                    : new Vector3(0f, faceToFaceEyeHeight, 1.25f);
                Vector3 toInterviewer = (intervHeadPos - candidateEyePoint.position).normalized;
                candidateEyePoint.rotation = (toInterviewer != Vector3.zero)
                    ? Quaternion.LookRotation(toInterviewer, Vector3.up)
                    : Quaternion.Euler(0f, 0f, 0f);
            }

            if (currentView == CameraViewMode.InterviewerView || currentView == CameraViewMode.CandidateARView)
            {
                SwitchView(currentView, instant: true);
            }
            else
            {
                SwitchView(activeRole == InterviewRole.Interviewer ? CameraViewMode.InterviewerView : CameraViewMode.CandidateARView, instant: true);
            }

            if (audioController != null) audioController.PlayUIChime();
            Debug.Log("[InterviewRoomController] POV reset to true face-to-face line of sight (Height: 1.20m, Pitch: 0 deg).");
        }
    }
}
