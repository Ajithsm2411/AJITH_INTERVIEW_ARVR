using System.Collections.Generic;
using UnityEngine;

namespace InterviewRoom
{
    /// <summary>
    /// Advanced avatar animation controller providing realistic procedural speaking actions:
    /// - Dynamic Lip-Sync & Viseme mouth movement opening/closing with spoken syllables
    /// - Expressive speaking gestures (Open-palm question gestures, laptop/notes reference, AR hologram points)
    /// - Conversational head nods, inquiring head tilts, and look-at gaze tracking
    /// - Inquiring conversational posture (torso leans forward while speaking questions)
    /// - Natural eye blinking and idle breathing motion
    /// - Active listening nods when listening to the other speaker
    /// </summary>
    public class AvatarAnimationController : MonoBehaviour
    {
        public enum AvatarState
        {
            Idle = 0,
            Speaking = 1,
            Listening = 2,
            Thinking = 3,
            Waiting = 4,
            InterviewCompleted = 5
        }

        public enum SpeakingGestureType
        {
            OpenPalmQuestion,     // Interviewer lifts hand in open-palm questioning pose
            DeskNotesReference,   // Interviewer gestures down toward laptop & folder notes
            ConversationalBeats,  // Rhythmic hand and head emphasis
            ExplainingTwoHands,   // Candidate two-handed explanation
            SpatialHologramPoint  // Candidate points towards AR floating chart
        }

        [Header("Avatar State Machine")]
        public AvatarState currentState = AvatarState.Idle;

        public void SetAvatarState(AvatarState newState)
        {
            currentState = newState;
            isSpeaking = (newState == AvatarState.Speaking);
            if (newState == AvatarState.Listening)
            {
                listeningNodTimer = 0.5f;
                listeningNodProgress = 1.0f;
            }
        }

        [Header("Rig Elements")]
        public Transform head;
        public Transform chest;
        public Transform leftArm;
        public Transform rightArm;
        public Transform leftHand;
        public Transform rightHand;
        public Transform mouth;       // "Smile" or mouth mesh
        public Transform leftEye;
        public Transform rightEye;
        public Transform lookTarget;

        [Header("Role Settings")]
        public bool isInterviewer = false;
        public bool isCandidateVR = false;

        [Header("Speaking State")]
        public bool isSpeaking = false;
        public SpeakingGestureType currentGesture = SpeakingGestureType.OpenPalmQuestion;
        [Range(0.5f, 3.0f)]
        public float speechGestureIntensity = 1.3f;

        [Header("Live Microphone Lip-Sync & Visemes")]
        public float liveMicVolume = 0f;
        public bool isDrivenByLiveMic = false;

        [Header("Breathing Settings")]
        [Range(0.5f, 3f)]
        public float breathingSpeed = 1.1f;
        public float breathingAmount = 0.022f;

        // Initial Transforms
        private Vector3 initialHeadPos;
        private Quaternion initialHeadRot;
        private Vector3 initialChestPos;
        private Quaternion initialChestRot;
        private Vector3 initialChestScale;
        private Quaternion initialLeftArmRot;
        private Quaternion initialRightArmRot;
        private Quaternion initialLeftHandRot;
        private Quaternion initialRightHandRot;
        private Vector3 initialMouthPos;
        private Vector3 initialMouthScale;
        private Vector3 initialLeftEyeScale;
        private Vector3 initialRightEyeScale;

        // Dynamics & Timers
        private float seed;
        private float speechTimer;
        private float gestureCycleTimer;
        private float wordEmphasisPulse = 0f;
        private float blinkTimer = 3.0f;
        private float blinkProgress = 0f;
        private bool isBlinking = false;
        private float listeningNodTimer = 2.0f;
        private float listeningNodProgress = 0f;

        private void Awake()
        {
            seed = Random.Range(0f, 100f);
            AutoDiscoverRigElements();
            CacheInitialTransforms();
        }

        private void Start()
        {
            if (lookTarget == null)
            {
                AutoFindLookTarget();
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float time = Time.time * breathingSpeed + seed;

            UpdateEyeBlinking(dt);
            UpdateBreathingAndTorso(dt, time);
            UpdateSpeakingGestures(dt, time);
            UpdateMouthVisemes(dt);
            UpdateHeadActionsAndGaze(dt, time);
        }

        /// <summary>
        /// Automatically finds child parts if not manually wired in Inspector.
        /// </summary>
        public void AutoDiscoverRigElements()
        {
            if (head == null) head = transform.Find("Head");
            if (chest == null)
            {
                chest = transform.Find("Suit Torso");
                if (chest == null) chest = transform.Find("Chest");
            }

            if (leftArm == null)
            {
                leftArm = transform.Find("Left Arm");
                if (leftArm == null) leftArm = transform.Find("Arm_Upper_L");
            }
            if (rightArm == null)
            {
                rightArm = transform.Find("Right Arm");
                if (rightArm == null) rightArm = transform.Find("Arm_Upper_R");
            }

            if (leftArm != null && leftHand == null)
            {
                leftHand = leftArm.Find("Left Hand");
                if (leftHand == null) leftHand = leftArm.Find("Hand_L");
            }
            if (rightArm != null && rightHand == null)
            {
                rightHand = rightArm.Find("Right Hand");
                if (rightHand == null) rightHand = rightArm.Find("Hand_R");
            }

            if (head != null)
            {
                if (mouth == null)
                {
                    mouth = head.Find("Smile");
                    if (mouth == null) mouth = head.Find("Mouth");
                }

                if (leftEye == null)
                {
                    leftEye = head.Find("Left Eye");
                    if (leftEye == null) leftEye = head.Find("Eye_L");
                }
                if (rightEye == null)
                {
                    rightEye = head.Find("Right Eye");
                    if (rightEye == null) rightEye = head.Find("Eye_R");
                }
            }
        }

        private void CacheInitialTransforms()
        {
            if (head != null)
            {
                initialHeadPos = head.localPosition;
                initialHeadRot = head.localRotation;
            }

            if (chest != null)
            {
                initialChestPos = chest.localPosition;
                initialChestRot = chest.localRotation;
                initialChestScale = chest.localScale;
            }

            if (leftArm != null) initialLeftArmRot = leftArm.localRotation;
            if (rightArm != null) initialRightArmRot = rightArm.localRotation;

            if (leftHand != null) initialLeftHandRot = leftHand.localRotation;
            if (rightHand != null) initialRightHandRot = rightHand.localRotation;

            if (mouth != null)
            {
                initialMouthPos = mouth.localPosition;
                initialMouthScale = mouth.localScale;
            }

            if (leftEye != null) initialLeftEyeScale = leftEye.localScale;
            if (rightEye != null) initialRightEyeScale = rightEye.localScale;
        }

        private void AutoFindLookTarget()
        {
            GameObject env = GameObject.Find("InterviewRoom_Environment");
            if (env == null) return;

            if (isInterviewer)
            {
                Transform cand = env.transform.Find("Avatar_Candidate_Alex_ARVR");
                if (cand != null) lookTarget = cand.Find("Head");
            }
            else
            {
                Transform interv = env.transform.Find("Avatar_Interviewer_DrSarah");
                if (interv != null) lookTarget = interv.Find("Head");
            }
        }

        /// <summary>
        /// Triggered in real time for each spoken word by SpeechToTextTranscriptionController.
        /// Pulses visemes, triggers syllable emphasis nod, and transitions gestures.
        /// </summary>
        public void OnWordSpoken(string word)
        {
            wordEmphasisPulse = 1.0f;
            speechTimer += 0.25f;

            // Transition speaking gesture on emphatic punctuation or transition words
            string w = word.Trim().ToLower();
            if (w.Contains("?") || w.Contains("why") || w.Contains("how") || w.Contains("what") || w.Contains("tell"))
            {
                currentGesture = isInterviewer ? SpeakingGestureType.OpenPalmQuestion : SpeakingGestureType.ExplainingTwoHands;
            }
            else if (w.Contains("experience") || w.Contains("resume") || w.Contains("background") || w.Contains("project"))
            {
                currentGesture = isInterviewer ? SpeakingGestureType.DeskNotesReference : SpeakingGestureType.SpatialHologramPoint;
            }
            else if (Random.value < 0.22f)
            {
                CycleSpeakingGesture();
            }
        }

        private void CycleSpeakingGesture()
        {
            if (isInterviewer)
            {
                currentGesture = (currentGesture == SpeakingGestureType.OpenPalmQuestion)
                    ? SpeakingGestureType.DeskNotesReference
                    : (currentGesture == SpeakingGestureType.DeskNotesReference ? SpeakingGestureType.ConversationalBeats : SpeakingGestureType.OpenPalmQuestion);
            }
            else
            {
                currentGesture = (currentGesture == SpeakingGestureType.ExplainingTwoHands)
                    ? SpeakingGestureType.SpatialHologramPoint
                    : (currentGesture == SpeakingGestureType.SpatialHologramPoint ? SpeakingGestureType.ConversationalBeats : SpeakingGestureType.ExplainingTwoHands);
            }
        }

        // =========================================================
        // 1. Natural Breathing & Conversational Posture
        // =========================================================
        private void UpdateBreathingAndTorso(float dt, float time)
        {
            if (chest == null) return;

            // Breathing expansion
            float breath = Mathf.Sin(time) * breathingAmount;
            chest.localScale = initialChestScale + new Vector3(breath * 0.4f, breath, breath * 0.4f);

            // Conversational Torso Lean:
            // When speaking or answering via live mic, avatar leans forward towards the desk.
            // When listening, avatar settles back into a relaxed posture.
            bool activeSpeaking = isSpeaking || (isDrivenByLiveMic && liveMicVolume > 0.025f);
            float targetPitchForward = 0f;
            if (activeSpeaking)
            {
                targetPitchForward = isInterviewer ? 5.5f : 4.0f;
            }

            Quaternion targetChestRot = initialChestRot * Quaternion.Euler(targetPitchForward, 0f, 0f);
            chest.localRotation = Quaternion.Slerp(chest.localRotation, targetChestRot, dt * 2.5f);
        }

        // =========================================================
        // 2. Real-Time Lip-Sync & Viseme Mouth Movement
        // =========================================================
        private void UpdateMouthVisemes(float dt)
        {
            if (mouth == null) return;

            float mouthOpen = 0f;
            if (isDrivenByLiveMic)
            {
                // Real-time live microphone audio drives avatar mouth directly
                float smoothedVol = Mathf.Clamp01(liveMicVolume * 3.8f);
                if (smoothedVol > 0.035f)
                {
                    speechTimer += dt * 14.0f;
                    float flutter = Mathf.Abs(Mathf.Sin(speechTimer * 1.8f)) * 0.35f;
                    mouthOpen = Mathf.Clamp01(smoothedVol * 0.75f + flutter * smoothedVol);
                    wordEmphasisPulse = Mathf.Max(wordEmphasisPulse, smoothedVol * 1.5f);
                }
            }
            else if (isSpeaking)
            {
                speechTimer += dt * 11.0f;
                // High-fidelity speech rhythm: combines fast phoneme flap with slower syllable envelope
                float fastViseme = Mathf.Abs(Mathf.Sin(speechTimer * 1.5f));
                float slowEnvelope = Mathf.Abs(Mathf.Sin(speechTimer * 0.6f) * Mathf.Cos(speechTimer * 0.3f));
                float syllableBeat = Mathf.Clamp01(wordEmphasisPulse);

                mouthOpen = Mathf.Clamp01((fastViseme * 0.55f + slowEnvelope * 0.35f + syllableBeat * 0.45f));
            }

            // Decay word emphasis pulse
            wordEmphasisPulse = Mathf.MoveTowards(wordEmphasisPulse, 0f, dt * 4.0f);

            // Scale and vertical position offset for mouth mesh
            Vector3 targetScale = new Vector3(
                initialMouthScale.x * (1.0f + mouthOpen * 0.25f),
                initialMouthScale.y * (1.0f + mouthOpen * 1.4f),
                initialMouthScale.z
            );

            Vector3 targetPos = initialMouthPos + new Vector3(0f, -mouthOpen * 0.007f, 0f);

            mouth.localScale = Vector3.Lerp(mouth.localScale, targetScale, dt * 18f);
            mouth.localPosition = Vector3.Lerp(mouth.localPosition, targetPos, dt * 18f);
        }

        // =========================================================
        // 3. Conversational Head Actions, Nods & Gaze
        // =========================================================
        private void UpdateHeadActionsAndGaze(float dt, float time)
        {
            if (head == null) return;

            float headNod = Mathf.Sin(time * 0.6f) * 1.2f;
            float headTilt = Mathf.Cos(time * 0.45f) * 0.8f;
            float headYaw = Mathf.Sin(time * 0.25f) * 1.5f;

            if (isSpeaking)
            {
                // Dynamic speaking head gestures:
                // - Emphatic nod beats on syllables
                // - Inquiring head tilt when asking questions
                headNod += Mathf.Sin(speechTimer * 0.4f) * 4.2f + (wordEmphasisPulse * 3.5f);
                headTilt += Mathf.Cos(speechTimer * 0.25f) * 3.2f * (isInterviewer ? 1.2f : 0.8f);
                headYaw += Mathf.Sin(speechTimer * 0.2f) * 2.8f;
            }
            else
            {
                // Active Listening Nod: If listening to the other avatar, occasionally nod in agreement
                listeningNodTimer -= dt;
                if (listeningNodTimer <= 0f)
                {
                    listeningNodTimer = Random.Range(3.0f, 6.0f);
                    listeningNodProgress = 1.0f;
                }

                if (listeningNodProgress > 0f)
                {
                    listeningNodProgress = Mathf.MoveTowards(listeningNodProgress, 0f, dt * 2.2f);
                    headNod += Mathf.Sin((1.0f - listeningNodProgress) * Mathf.PI * 2f) * 4.0f;
                }
            }

            // Gaze tracking towards the other person
            Quaternion baseLookRot = initialHeadRot;
            if (lookTarget != null)
            {
                Vector3 dir = (lookTarget.position - head.position).normalized;
                if (dir != Vector3.zero)
                {
                    Quaternion worldRot = Quaternion.LookRotation(dir, Vector3.up);
                    Quaternion localRot = Quaternion.Inverse(transform.rotation) * worldRot;
                    baseLookRot = Quaternion.Slerp(initialHeadRot, localRot, 0.42f);
                }
            }

            Quaternion gestureRot = Quaternion.Euler(headNod, headYaw, headTilt);
            head.localRotation = Quaternion.Slerp(head.localRotation, baseLookRot * gestureRot, dt * 5.0f);
        }

        // =========================================================
        // 4. Expressive Arm & Hand Speaking Actions
        // =========================================================
        private void UpdateSpeakingGestures(float dt, float time)
        {
            gestureCycleTimer += dt;
            bool activeSpeaking = isSpeaking || (isDrivenByLiveMic && liveMicVolume > 0.035f);
            if (activeSpeaking && gestureCycleTimer > 3.2f)
            {
                gestureCycleTimer = 0f;
                CycleSpeakingGesture();
            }

            if (isInterviewer)
            {
                UpdateInterviewerGestures(dt, time);
            }
            else
            {
                UpdateCandidateGestures(dt, time);
            }
        }

        private void UpdateInterviewerGestures(float dt, float time)
        {
            Quaternion targetLeftArm = initialLeftArmRot;
            Quaternion targetRightArm = initialRightArmRot;
            Quaternion targetRightHand = (rightHand != null) ? initialRightHandRot : Quaternion.identity;

            if (currentState == AvatarState.Thinking)
            {
                // Thoughtful hand near chin pose
                targetRightArm = initialRightArmRot * Quaternion.Euler(-32f, 22f, -14f);
                if (rightHand != null) targetRightHand = initialRightHandRot * Quaternion.Euler(25f, 20f, 0f);
                targetLeftArm = initialLeftArmRot * Quaternion.Euler(-4f, 0f, 0f);
            }
            else if (currentState == AvatarState.Waiting)
            {
                // Patient resting hands on desk
                targetLeftArm = initialLeftArmRot;
                targetRightArm = initialRightArmRot;
            }
            else if (currentState == AvatarState.InterviewCompleted)
            {
                // Appreciative finish gesture
                targetRightArm = initialRightArmRot * Quaternion.Euler(-18f, 12f, -6f);
                targetLeftArm = initialLeftArmRot * Quaternion.Euler(-18f, -12f, 6f);
            }
            else if (isSpeaking)
            {
                float beat = Mathf.Sin(speechTimer * 0.35f) * 3.5f * speechGestureIntensity;

                switch (currentGesture)
                {
                    case SpeakingGestureType.OpenPalmQuestion:
                        // Right arm lifts forward from desk into open-palm questioning pose
                        targetRightArm = initialRightArmRot * Quaternion.Euler(-26f + beat, 18f, -14f);
                        if (rightHand != null) targetRightHand = initialRightHandRot * Quaternion.Euler(15f, 25f, 0f);

                        // Left arm rests stably near desk
                        targetLeftArm = initialLeftArmRot * Quaternion.Euler(4f, 0f, 2f);
                        break;

                    case SpeakingGestureType.DeskNotesReference:
                        // Right hand points gently down toward laptop screen & interview notes
                        targetRightArm = initialRightArmRot * Quaternion.Euler(12f + beat * 0.6f, -10f, 6f);
                        if (rightHand != null) targetRightHand = initialRightHandRot * Quaternion.Euler(-18f, 0f, 10f);
                        targetLeftArm = initialLeftArmRot * Quaternion.Euler(-6f, 0f, -4f);
                        break;

                    case SpeakingGestureType.ConversationalBeats:
                    default:
                        // Conversational emphasis beats with both hands slightly articulated
                        targetRightArm = initialRightArmRot * Quaternion.Euler(-14f + beat * 1.4f, 10f, -8f);
                        targetLeftArm = initialLeftArmRot * Quaternion.Euler(-8f - beat * 0.8f, -6f, 4f);
                        break;
                }
            }
            else
            {
                // Resting posture: Hands resting on desk / typing lightly on laptop
                float microWiggle = Mathf.Sin(time * 3f) * 1.2f;
                targetLeftArm = initialLeftArmRot * Quaternion.Euler(microWiggle, 0f, 0f);
                targetRightArm = initialRightArmRot * Quaternion.Euler(microWiggle * 0.8f, 0f, 0f);
            }

            if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, targetLeftArm, dt * 3.5f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, targetRightArm, dt * 3.5f);
            if (rightHand != null) rightHand.localRotation = Quaternion.Slerp(rightHand.localRotation, targetRightHand, dt * 4.0f);
        }

        private void UpdateCandidateGestures(float dt, float time)
        {
            Quaternion targetLeftArm = initialLeftArmRot;
            Quaternion targetRightArm = initialRightArmRot;

            if (currentState == AvatarState.Thinking)
            {
                // Thoughtful hand chin pose
                targetRightArm = initialRightArmRot * Quaternion.Euler(-28f, 18f, -10f);
                targetLeftArm = initialLeftArmRot * Quaternion.Euler(-6f, -4f, 0f);
            }
            else if (currentState == AvatarState.Listening || currentState == AvatarState.Waiting)
            {
                // Attentive listening resting hands on lap/desk
                targetLeftArm = initialLeftArmRot;
                targetRightArm = initialRightArmRot;
            }
            else if (isSpeaking)
            {
                float beat = Mathf.Sin(speechTimer * 0.38f) * 4.5f * speechGestureIntensity;

                switch (currentGesture)
                {
                    case SpeakingGestureType.SpatialHologramPoint:
                        // Right arm reaches up towards the floating AR holographic charts
                        targetRightArm = initialRightArmRot * Quaternion.Euler(-38f + beat, 16f, -10f);
                        targetLeftArm = initialLeftArmRot * Quaternion.Euler(-12f - beat * 0.5f, -8f, 4f);
                        break;

                    case SpeakingGestureType.ExplainingTwoHands:
                    default:
                        // Two-handed explanatory conversational gesture
                        targetRightArm = initialRightArmRot * Quaternion.Euler(-22f + beat, 14f, -8f);
                        targetLeftArm = initialLeftArmRot * Quaternion.Euler(-20f - beat, -14f, 8f);
                        break;
                }
            }
            else
            {
                // Attentive resting hands
                targetLeftArm = initialLeftArmRot;
                targetRightArm = initialRightArmRot;
            }

            if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, targetLeftArm, dt * 3.5f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, targetRightArm, dt * 3.5f);
        }

        // =========================================================
        // 5. Eye Blinking System
        // =========================================================
        private void UpdateEyeBlinking(float dt)
        {
            if (leftEye == null && rightEye == null) return;

            blinkTimer -= dt;
            if (blinkTimer <= 0f && !isBlinking)
            {
                isBlinking = true;
                blinkProgress = 0f;
            }

            if (isBlinking)
            {
                blinkProgress += dt * 8.5f; // Fast, natural blink (~0.12 sec)
                float blinkScaleY = Mathf.Abs(Mathf.Sin(blinkProgress * Mathf.PI));

                // Scale eye Y down to 0.1 at peak of blink
                float currentY = Mathf.Lerp(1.0f, 0.1f, blinkScaleY);

                if (leftEye != null)
                {
                    leftEye.localScale = new Vector3(initialLeftEyeScale.x, initialLeftEyeScale.y * currentY, initialLeftEyeScale.z);
                }
                if (rightEye != null)
                {
                    rightEye.localScale = new Vector3(initialRightEyeScale.x, initialRightEyeScale.y * currentY, initialRightEyeScale.z);
                }

                if (blinkProgress >= 1.0f)
                {
                    isBlinking = false;
                    blinkTimer = Random.Range(2.5f, 5.0f);
                    if (leftEye != null) leftEye.localScale = initialLeftEyeScale;
                    if (rightEye != null) rightEye.localScale = initialRightEyeScale;
                }
            }
        }
    }
}
