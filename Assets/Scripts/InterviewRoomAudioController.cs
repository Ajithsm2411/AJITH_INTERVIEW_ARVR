using System;
using System.Collections.Generic;
using UnityEngine;

namespace InterviewRoom
{
    /// <summary>
    /// Comprehensive Microphone and Audio Controller for the Interview Room:
    /// - Live PC microphone input capture with volume metering
    /// - 3D spatial procedural dialogue vocalizations
    /// - Room ambiance and UI sound effects
    /// - Dynamic microphone LED status ring visualization (Active green / Muted red)
    /// </summary>
    public class InterviewRoomAudioController : MonoBehaviour
    {
        [Header("Audio Sources")]
        public AudioSource interviewerAudioSource;
        public AudioSource candidateAudioSource;
        public AudioSource conferenceMicAudioSource;
        public AudioSource ambientAudioSource;

        [Header("Microphone 3D Indicators")]
        public Renderer interviewerMicLed;
        public Renderer candidateMicLed;
        public Renderer conferencePuckLed;

        [Header("Volume & Master Playback Controls")]
        [Range(0.0f, 1.0f)]
        public float masterVolume = 1.0f;
        public bool hasMicrophonePermission = true;
        public string micPermissionStatusMessage = "Microphone Ready (VR Headset / PC)";
        public bool feedbackPreventionActive = false; // When true (e.g. interviewer speaking), mic transcription input is muted to prevent echoing interviewer audio

        [Header("Microphone Input Settings")]
        public bool isMicEnabled = true;
        public bool isMuted = false;
        public string selectedMicDevice = null;
        [Range(0.1f, 5.0f)]
        public float micSensitivity = 1.8f;
        public int selectedMicIndex = 0;

        [Header("User Voice Recording")]
        public bool isRecordingUserVoice = false;
        public float userRecordingDuration = 0f;
        public float maxRecordDuration = 180f;
        public AudioClip userVoiceRecordClip = null;
        public AudioClip lastRecordedUserClip = null;
        public bool hasRecordedUserAnswer = false;
        public float voiceActivityThreshold = 0.018f;
        public bool isUserVoiceDetected = false;
        public float userSilenceDuration = 0f;

        [Header("Current Audio Levels")]
        public float currentMicLoudness = 0f;
        public float peakLoudness = 0f;

        private AudioClip micClip;
        private int micSampleWindow = 128;
        private Material matLedActive;
        private Material matLedMuted;
        private Material matLedPulse;

        [Header("Dialogue Voice Audio Clips")]
        public AudioClip[] interviewerQuestionClips;
        public AudioClip[] candidateAnswerClips;
        public AudioClip[] interviewerAckClips;
        public AudioClip interviewerWelcomeClip;

        // Procedural Audio Clips
        private AudioClip clipRoomAmbient;
        private AudioClip clipInterviewerSpeech;
        private AudioClip clipCandidateSpeech;
        private AudioClip clipMicUnmute;
        private AudioClip clipMicMute;
        private AudioClip clipChime;
        private static readonly Dictionary<string, AudioClip> synthesizedClipsCache = new Dictionary<string, AudioClip>();

        private void Awake()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorUtility.audioMasterMute)
            {
                UnityEditor.EditorUtility.audioMasterMute = false;
                Debug.Log("[InterviewAudio] Automatically unmuted Unity Editor Audio Master in Game View!");
            }
#endif
            AudioListener.volume = 1.0f;
            AudioListener.pause = false;

            CreateProceduralMaterials();
            GenerateProceduralAudioClips();
            InitializeVoiceAudioClips();
        }

        private void Start()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.audioMasterMute = false;
#endif
            AudioListener.volume = 1.0f;
            AudioListener.pause = false;

            SetupAmbientAudio();
            InitializeLiveMicrophone();
            UpdateLedIndicators();
        }

        private void Update()
        {
            // Hotkey 'M' to toggle mute
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.mKey.wasPressedThisFrame)
            {
                ToggleMute();
            }
#else
            if (Input.GetKeyDown(KeyCode.M))
            {
                ToggleMute();
            }
#endif

            // Update live microphone level
            if (isMicEnabled && !isMuted)
            {
                if (isRecordingUserVoice && userVoiceRecordClip != null && !string.IsNullOrEmpty(selectedMicDevice))
                {
                    int micPos = Microphone.GetPosition(selectedMicDevice);
                    if (micPos > 0)
                    {
                        userRecordingDuration = (float)micPos / (float)userVoiceRecordClip.frequency;

                        int sampleOffset = Mathf.Max(0, micPos - micSampleWindow);
                        float[] waveData = new float[micSampleWindow];
                        userVoiceRecordClip.GetData(waveData, sampleOffset);

                        float sum = 0f;
                        for (int i = 0; i < micSampleWindow; i++)
                        {
                            sum += waveData[i] * waveData[i];
                        }
                        currentMicLoudness = Mathf.Sqrt(sum / micSampleWindow) * micSensitivity;
                        peakLoudness = Mathf.Max(currentMicLoudness, Mathf.Lerp(peakLoudness, 0f, Time.deltaTime * 3f));

                        if (currentMicLoudness > voiceActivityThreshold)
                        {
                            isUserVoiceDetected = true;
                            userSilenceDuration = 0f;
                        }
                        else
                        {
                            isUserVoiceDetected = false;
                            userSilenceDuration += Time.deltaTime;
                        }

                        if (userRecordingDuration >= maxRecordDuration)
                        {
                            StopRecordingUserVoice();
                        }
                    }
                }
                else if (micClip != null)
                {
                    currentMicLoudness = GetMicRmsLoudness() * micSensitivity;
                    peakLoudness = Mathf.Max(currentMicLoudness, Mathf.Lerp(peakLoudness, 0f, Time.deltaTime * 3f));
                    isUserVoiceDetected = currentMicLoudness > voiceActivityThreshold;
                }
            }
            else
            {
                currentMicLoudness = 0f;
                peakLoudness = Mathf.Lerp(peakLoudness, 0f, Time.deltaTime * 5f);
                isUserVoiceDetected = false;
            }

            UpdateLedPulse();
        }

        public float GetActiveOutputLoudness()
        {
            if (isMuted) return 0f;

            if (isMicEnabled && currentMicLoudness > 0.005f)
            {
                return currentMicLoudness;
            }

            if ((interviewerAudioSource != null && interviewerAudioSource.isPlaying) ||
                (candidateAudioSource != null && candidateAudioSource.isPlaying))
            {
                AudioSource activeSrc = (interviewerAudioSource != null && interviewerAudioSource.isPlaying) ? interviewerAudioSource : candidateAudioSource;
                float[] data = new float[64];
                activeSrc.GetOutputData(data, 0);
                float sum = 0f;
                for (int i = 0; i < data.Length; i++) sum += data[i] * data[i];
                float rms = Mathf.Sqrt(sum / data.Length);
                return Mathf.Clamp01(rms * 4.5f + 0.15f);
            }

            return 0f;
        }

        #region User Voice Recording & Playback Workflow
        public bool StartRecordingUserVoice()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("[InterviewAudio] No microphone devices available to record voice.");
                return false;
            }

            if (string.IsNullOrEmpty(selectedMicDevice))
            {
                selectedMicDevice = Microphone.devices[0];
            }

            try
            {
                if (Microphone.IsRecording(selectedMicDevice))
                {
                    Microphone.End(selectedMicDevice);
                }

                userVoiceRecordClip = Microphone.Start(selectedMicDevice, false, Mathf.CeilToInt(maxRecordDuration), 44100);
                isRecordingUserVoice = true;
                userRecordingDuration = 0f;
                userSilenceDuration = 0f;
                isUserVoiceDetected = false;
                Debug.Log($"[InterviewAudio] Started recording user voice on '{selectedMicDevice}' (Max {maxRecordDuration}s)...");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InterviewAudio] Failed to start microphone recording: {ex.Message}");
                isRecordingUserVoice = false;
                return false;
            }
        }

        public AudioClip StopRecordingUserVoice()
        {
            if (!isRecordingUserVoice) return lastRecordedUserClip;

            int micPos = 0;
            if (!string.IsNullOrEmpty(selectedMicDevice))
            {
                micPos = Microphone.GetPosition(selectedMicDevice);
                Microphone.End(selectedMicDevice);
            }

            isRecordingUserVoice = false;

            if (micPos > 1000 && userVoiceRecordClip != null)
            {
                // Add 0.12s safety padding (approx 5300 samples at 44.1kHz) to prevent cutting off trailing consonants
                int samplesToKeep = Mathf.Min(micPos + 5300, userVoiceRecordClip.samples);
                float[] samples = new float[samplesToKeep * userVoiceRecordClip.channels];
                userVoiceRecordClip.GetData(samples, 0);

                lastRecordedUserClip = AudioClip.Create("UserRecordedAnswer", samplesToKeep, userVoiceRecordClip.channels, userVoiceRecordClip.frequency, false);
                lastRecordedUserClip.SetData(samples, 0);
                userRecordingDuration = (float)samplesToKeep / (float)userVoiceRecordClip.frequency;
                hasRecordedUserAnswer = true;
                Debug.Log($"[InterviewAudio] Stopped recording user voice. Duration: {userRecordingDuration:F2}s ({samplesToKeep} samples)");
            }
            else
            {
                Debug.LogWarning("[InterviewAudio] User recording was too short or empty.");
            }

            // Resume regular background microphone monitor
            if (isMicEnabled && !string.IsNullOrEmpty(selectedMicDevice))
            {
                try
                {
                    micClip = Microphone.Start(selectedMicDevice, true, 10, 44100);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[InterviewAudio] Could not restart mic background loop: {ex.Message}");
                }
            }

            return lastRecordedUserClip;
        }

        public float PlayRecordedUserVoice()
        {
            if (lastRecordedUserClip == null)
            {
                Debug.LogWarning("[InterviewAudio] No recorded user clip to play back.");
                return 0f;
            }

            if (candidateAudioSource != null)
            {
                candidateAudioSource.Stop();
                candidateAudioSource.clip = lastRecordedUserClip;
                candidateAudioSource.volume = 1.0f;
                candidateAudioSource.spatialBlend = 0.85f;
                candidateAudioSource.pitch = 1.0f;
                candidateAudioSource.Play();
                Debug.Log($"[InterviewAudio] Playing back user's recorded answer clip ({lastRecordedUserClip.length:F2}s)");
                return lastRecordedUserClip.length;
            }

            return lastRecordedUserClip.length;
        }

        public void DiscardRecordedUserVoice()
        {
            if (isRecordingUserVoice)
            {
                StopRecordingUserVoice();
            }
            lastRecordedUserClip = null;
            hasRecordedUserAnswer = false;
            userRecordingDuration = 0f;
            Debug.Log("[InterviewAudio] Discarded recorded user voice.");
        }

        public void CycleNextMicrophoneDevice()
        {
            if (Microphone.devices.Length <= 1) return;

            selectedMicIndex = (selectedMicIndex + 1) % Microphone.devices.Length;
            selectedMicDevice = Microphone.devices[selectedMicIndex];

            if (Microphone.IsRecording(null))
            {
                Microphone.End(null);
            }

            InitializeLiveMicrophone();
            Debug.Log($"[InterviewAudio] Switched microphone device to: {selectedMicDevice}");
        }

        public string GetSelectedMicName()
        {
            if (string.IsNullOrEmpty(selectedMicDevice))
            {
                return (Microphone.devices.Length > 0) ? Microphone.devices[0] : "No Microphone Detected";
            }
            return selectedMicDevice;
        }
        #endregion

        private void CreateProceduralMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            matLedActive = new Material(litShader);
            matLedActive.color = new Color(0.0f, 0.9f, 0.4f);
            matLedActive.EnableKeyword("_EMISSION");
            matLedActive.SetColor("_EmissionColor", new Color(0.0f, 1.0f, 0.45f) * 2.8f);

            matLedMuted = new Material(litShader);
            matLedMuted.color = new Color(0.9f, 0.15f, 0.15f);
            matLedMuted.EnableKeyword("_EMISSION");
            matLedMuted.SetColor("_EmissionColor", new Color(1.0f, 0.1f, 0.1f) * 2.2f);

            matLedPulse = new Material(litShader);
            matLedPulse.color = new Color(0.0f, 0.7f, 1.0f);
            matLedPulse.EnableKeyword("_EMISSION");
            matLedPulse.SetColor("_EmissionColor", new Color(0.0f, 0.8f, 1.0f) * 3.0f);
        }

        [Header("VR Headset Hardware Detection")]
        public bool isUsingVRHeadsetMic = false;

        public string GetSelectedMicType()
        {
            return isUsingVRHeadsetMic ? "Meta Quest 3 / VR Headset Mic" : "Standard PC Microphone";
        }

        public void InitializeLiveMicrophone()
        {
            if (Microphone.devices.Length > 0)
            {
                selectedMicDevice = Microphone.devices[0];
                isUsingVRHeadsetMic = false;

                // Prioritize Meta Quest / Oculus / SteamVR / VR Headset mic devices
                string[] vrKeywords = new string[] { "oculus", "quest", "virtual", "headset", "steamvr", "vive", "rift", "hmd", "wireless", "usb" };

                foreach (var device in Microphone.devices)
                {
                    string devLower = device.ToLowerInvariant();
                    foreach (var kw in vrKeywords)
                    {
                        if (devLower.Contains(kw))
                        {
                            selectedMicDevice = device;
                            isUsingVRHeadsetMic = true;
                            break;
                        }
                    }
                    if (isUsingVRHeadsetMic) break;
                }

                #if UNITY_ANDROID
                isUsingVRHeadsetMic = true;
                #endif

                try
                {
                    micClip = Microphone.Start(selectedMicDevice, true, 10, 44100);
                    isMicEnabled = true;
                    Debug.Log($"[InterviewAudio] Initialized {(isUsingVRHeadsetMic ? "VR Headset" : "PC")} microphone: '{selectedMicDevice}'");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[InterviewAudio] Could not start physical microphone: {e.Message}");
                    isMicEnabled = false;
                }
            }
            else
            {
                Debug.Log("[InterviewAudio] No hardware microphone detected. Simulated audio enabled.");
                isMicEnabled = false;
            }
        }

        private float GetMicRmsLoudness()
        {
            if (micClip == null) return 0f;
            int micPosition = Microphone.GetPosition(selectedMicDevice);
            if (micPosition < micSampleWindow) return 0f;

            float[] waveData = new float[micSampleWindow];
            micClip.GetData(waveData, micPosition - micSampleWindow);

            float sum = 0f;
            for (int i = 0; i < micSampleWindow; i++)
            {
                sum += waveData[i] * waveData[i];
            }
            return Mathf.Sqrt(sum / micSampleWindow);
        }

        public void SetMasterVolume(float vol)
        {
            masterVolume = Mathf.Clamp01(vol);
            AudioListener.volume = masterVolume;
            if (interviewerAudioSource != null) interviewerAudioSource.volume = masterVolume;
            if (candidateAudioSource != null) candidateAudioSource.volume = masterVolume;
            if (ambientAudioSource != null) ambientAudioSource.volume = masterVolume * 0.25f;
            Debug.Log($"[InterviewAudio] Master volume set to {Mathf.RoundToInt(masterVolume * 100)}%");
        }

        public void CheckMicrophonePermissions()
        {
            if (Microphone.devices.Length == 0)
            {
                hasMicrophonePermission = false;
                micPermissionStatusMessage = "No microphone hardware detected. Text input fallback available.";
                Debug.LogWarning("[InterviewAudio] " + micPermissionStatusMessage);
            }
            else
            {
                hasMicrophonePermission = true;
                micPermissionStatusMessage = $"Microphone Authorized: {GetSelectedMicName()} ({GetSelectedMicType()})";
                Debug.Log("[InterviewAudio] " + micPermissionStatusMessage);
            }
        }

        public void ToggleMute()
        {
            isMuted = !isMuted;
            UpdateLedIndicators();

            if (conferenceMicAudioSource != null)
            {
                conferenceMicAudioSource.PlayOneShot(isMuted ? clipMicMute : clipMicUnmute, 0.7f);
            }
        }

        private void UpdateLedIndicators()
        {
            Material targetMat = isMuted ? matLedMuted : matLedActive;

            if (interviewerMicLed != null) interviewerMicLed.sharedMaterial = targetMat;
            if (candidateMicLed != null) candidateMicLed.sharedMaterial = targetMat;
            if (conferencePuckLed != null) conferencePuckLed.sharedMaterial = targetMat;
        }

        private void UpdateLedPulse()
        {
            if (isMuted) return;

            float loudness = GetActiveOutputLoudness();

            // Voice intensity dynamically modulates emission glow
            float glowFactor = 1.4f + Mathf.Clamp01(loudness * 2.5f) * 3.5f;
            Color baseColor = loudness > 0.05f ? new Color(0.0f, 0.85f, 1.0f) : new Color(0.0f, 1.0f, 0.45f);
            Color glowColor = baseColor * glowFactor;

            if (matLedActive != null)
            {
                matLedActive.SetColor("_EmissionColor", glowColor);
            }
        }

        private void SetupAmbientAudio()
        {
            if (ambientAudioSource == null)
            {
                ambientAudioSource = gameObject.AddComponent<AudioSource>();
            }

            ambientAudioSource.clip = clipRoomAmbient;
            ambientAudioSource.loop = true;
            ambientAudioSource.volume = 0.18f;
            ambientAudioSource.spatialBlend = 0.2f;
            ambientAudioSource.Play();
        }

        /// <summary>
        /// Loads spoken dialogue audio clips from Resources or WAV files on disk.
        /// Dr. Sarah Vance (Interviewer) and Alex Mercer (Candidate).
        /// </summary>
        public void InitializeVoiceAudioClips()
        {
            if (interviewerQuestionClips == null || interviewerQuestionClips.Length < 5)
            {
                interviewerQuestionClips = new AudioClip[5];
            }

            if (candidateAnswerClips == null || candidateAnswerClips.Length < 5)
            {
                candidateAnswerClips = new AudioClip[5];
            }

            if (interviewerAckClips == null || interviewerAckClips.Length < 5)
            {
                interviewerAckClips = new AudioClip[5];
            }

            string dialogueFolder = System.IO.Path.Combine(Application.dataPath, "Resources", "Audio", "Dialogue");

            for (int i = 0; i < 5; i++)
            {
                // 1. Interviewer Questions (Q0..Q4)
                if (interviewerQuestionClips[i] == null)
                {
                    interviewerQuestionClips[i] = Resources.Load<AudioClip>($"Audio/Dialogue/Interviewer_Q{i}");
                    if (interviewerQuestionClips[i] == null)
                    {
                        string qPath = System.IO.Path.Combine(dialogueFolder, $"Interviewer_Q{i}.wav");
                        interviewerQuestionClips[i] = WavUtility.FromFile(qPath, $"Interviewer_Q{i}");
                    }
                }

                // 2. Candidate Answers (A0..A4)
                if (candidateAnswerClips[i] == null)
                {
                    candidateAnswerClips[i] = Resources.Load<AudioClip>($"Audio/Dialogue/Candidate_A{i}");
                    if (candidateAnswerClips[i] == null)
                    {
                        string aPath = System.IO.Path.Combine(dialogueFolder, $"Candidate_A{i}.wav");
                        candidateAnswerClips[i] = WavUtility.FromFile(aPath, $"Candidate_A{i}");
                    }
                }

                // 3. Interviewer Acknowledgements (Ack0..Ack4)
                if (interviewerAckClips[i] == null)
                {
                    interviewerAckClips[i] = Resources.Load<AudioClip>($"Audio/Dialogue/Interviewer_Ack{i}");
                    if (interviewerAckClips[i] == null)
                    {
                        string ackPath = System.IO.Path.Combine(dialogueFolder, $"Interviewer_Ack{i}.wav");
                        interviewerAckClips[i] = WavUtility.FromFile(ackPath, $"Interviewer_Ack{i}");
                    }
                }
            }

            if (interviewerWelcomeClip == null)
            {
                interviewerWelcomeClip = Resources.Load<AudioClip>("Audio/Dialogue/Interviewer_Welcome");
                if (interviewerWelcomeClip == null)
                {
                    string wPath = System.IO.Path.Combine(dialogueFolder, "Interviewer_Welcome.wav");
                    interviewerWelcomeClip = WavUtility.FromFile(wPath, "Interviewer_Welcome");
                }
            }

            Debug.Log("[InterviewAudio] Spoken dialogue voice clips initialized for Interviewer (Dr. Vance) & Candidate (Alex Mercer).");
        }

        /// <summary>
        /// Plays natural spoken voice dialogue for either the Interviewer (asking question) or Candidate (answering).
        /// Returns the exact spoken length in seconds.
        /// </summary>
        public float PlayDialogueVoiceLine(bool isInterviewer, int questionIndex)
        {
            if (interviewerQuestionClips == null || candidateAnswerClips == null)
            {
                InitializeVoiceAudioClips();
            }

            AudioSource targetSource = isInterviewer ? interviewerAudioSource : candidateAudioSource;
            if (targetSource == null) return 3.0f;

            int clampedIndex = Mathf.Clamp(questionIndex, 0, 4);
            AudioClip clip = isInterviewer 
                ? (interviewerQuestionClips != null && clampedIndex < interviewerQuestionClips.Length ? interviewerQuestionClips[clampedIndex] : null)
                : (candidateAnswerClips != null && clampedIndex < candidateAnswerClips.Length ? candidateAnswerClips[clampedIndex] : null);

            if (clip != null)
            {
                targetSource.Stop();
                targetSource.pitch = 1.0f;
                targetSource.clip = clip;
                targetSource.volume = Mathf.Clamp01(masterVolume > 0.05f ? masterVolume : 1.0f);
                targetSource.spatialBlend = 0.12f; // Crystal clear in VR headset / headphones with subtle spatial cue
                targetSource.minDistance = 25.0f;  // Prevent distance falloff in room
                targetSource.maxDistance = 80.0f;
                targetSource.mute = false;
                targetSource.bypassListenerEffects = true;
                targetSource.Play();

                Debug.Log($"[InterviewAudio] Playing spoken voice: {(isInterviewer ? "Dr. Vance (Interviewer)" : "Alex Mercer (Candidate)")} for Question #{clampedIndex + 1} (Duration: {clip.length:F2}s)");
                return clip.length;
            }

            return 3.0f;
        }

        public void PlayDialogueSpeech(bool isInterviewer)
        {
            PlayDialogueVoiceLine(isInterviewer, 0);
        }

        /// <summary>
        /// Plays spoken voice audio for any interview question, greeting, or acknowledgement:
        /// 1. Pre-recorded studio WAV files (Q0..Q4, Welcome, Ack0..Ack4)
        /// 2. For automatically generated AI questions, synthesizes crystal-clear spoken audio using Windows Speech Engine (SAPI)
        /// 3. Returns the exact duration so candidate VR headset mic activates only after interviewer finishes speaking.
        /// </summary>
        public float PlayCustomDialogueLine(bool isInterviewer, string text)
        {
            AudioSource targetSource = isInterviewer ? interviewerAudioSource : candidateAudioSource;
            if (targetSource == null)
            {
                targetSource = GetComponent<AudioSource>();
                if (targetSource == null) targetSource = gameObject.AddComponent<AudioSource>();
            }

            if (interviewerQuestionClips == null || interviewerQuestionClips.Length < 5)
            {
                InitializeVoiceAudioClips();
            }

            string cleanText = (text ?? "").Trim().Trim('"');
            string lower = cleanText.ToLowerInvariant();

            AudioClip voiceClip = null;
            float duration = 0f;

            if (isInterviewer)
            {
                // 1. Check for dedicated Welcome Greeting
                if (lower.Contains("welcome to wipro") || (lower.Contains("welcome") && lower.Contains("sarah vance")))
                {
                    voiceClip = interviewerWelcomeClip;
                }
                // 2. Check for matching studio question audio clips
                else if (lower.Contains("journey") || (lower.Contains("welcome alex") && lower.Contains("introduce")) || lower.Contains("telepresence setup"))
                {
                    voiceClip = (interviewerQuestionClips != null && interviewerQuestionClips.Length > 0) ? interviewerQuestionClips[0] : null;
                }
                else if (lower.Contains("6-dof") || lower.Contains("motion-to-photon") || lower.Contains("latency in multi-user vr") || lower.Contains("foveated rendering"))
                {
                    voiceClip = (interviewerQuestionClips != null && interviewerQuestionClips.Length > 1) ? interviewerQuestionClips[1] : null;
                }
                else if (lower.Contains("webrtc") || lower.Contains("volumetric avatar") || lower.Contains("facial blendshapes") || lower.Contains("mesh streaming"))
                {
                    voiceClip = (interviewerQuestionClips != null && interviewerQuestionClips.Length > 2) ? interviewerQuestionClips[2] : null;
                }
                else if (lower.Contains("operating system") || lower.Contains("questions for us") || lower.Contains("impressive solution"))
                {
                    voiceClip = (interviewerQuestionClips != null && interviewerQuestionClips.Length > 3) ? interviewerQuestionClips[3] : null;
                }
                else if (lower.Contains("potential") || lower.Contains("excited") || lower.Contains("wrap up") || lower.Contains("thank you for sharing your experience"))
                {
                    voiceClip = (interviewerQuestionClips != null && interviewerQuestionClips.Length > 4) ? interviewerQuestionClips[4] : null;
                }
                // 3. Check for acknowledgements
                else if (lower.Contains("great insight") || lower.Contains("thank you") || lower.Contains("solid strategy") || lower.Contains("excellent"))
                {
                    voiceClip = (interviewerAckClips != null && interviewerAckClips.Length > 0) ? interviewerAckClips[0] : null;
                }
            }

            // 4. For automatically generated dynamic AI questions, synthesize real voice via Windows SAPI
            if (voiceClip == null && !string.IsNullOrEmpty(cleanText))
            {
                voiceClip = SynthesizeSpeechToAudioClip(cleanText, isInterviewer ? "DrVance_Question" : "Candidate_Answer", isFemale: isInterviewer);
            }

            // 5. Play voice through AudioSource
            if (voiceClip != null && targetSource != null)
            {
                targetSource.Stop();
                targetSource.pitch = 1.0f;
                targetSource.clip = voiceClip;
                targetSource.volume = Mathf.Clamp01(masterVolume > 0.05f ? masterVolume : 1.0f);
                targetSource.spatialBlend = 0.12f; // Crystal clear in VR headset / headphones with subtle spatial cue
                targetSource.minDistance = 25.0f;  // Prevent distance attenuation across room
                targetSource.maxDistance = 80.0f;
                targetSource.rolloffMode = AudioRolloffMode.Logarithmic;
                targetSource.mute = false;
                targetSource.bypassListenerEffects = true;
                targetSource.Play();
                duration = voiceClip.length;
                Debug.Log($"[InterviewAudio] Playing spoken voice for {(isInterviewer ? "Dr. Vance" : "Candidate")}: \"{cleanText.Substring(0, Mathf.Min(45, cleanText.Length))}...\" ({duration:F2}s)");
            }
            else
            {
                // Fallback: Speak directly via Windows SAPI SpVoice
                duration = Mathf.Clamp(cleanText.Split(new char[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length / 2.4f, 2.8f, 10f);
                SpeakViaSAPI(cleanText, isFemale: isInterviewer);
                Debug.Log($"[InterviewAudio] Spoke via Windows SAPI for {(isInterviewer ? "Dr. Vance" : "Candidate")}: ({duration:F2}s)");
            }

            return duration > 0.5f ? duration : 3.5f;
        }

        public static void SpeakViaSAPI(string text, bool isFemale = true)
        {
            try
            {
                Type voiceType = Type.GetTypeFromProgID("SAPI.SpVoice");
                if (voiceType != null)
                {
                    object voice = Activator.CreateInstance(voiceType);
                    ConfigureVoiceGender(voice, voiceType, isFemale);
                    voiceType.InvokeMember("Rate", System.Reflection.BindingFlags.SetProperty, null, voice, new object[] { 0 });
                    voiceType.InvokeMember("Volume", System.Reflection.BindingFlags.SetProperty, null, voice, new object[] { 100 });
                    voiceType.InvokeMember("Speak", System.Reflection.BindingFlags.InvokeMethod, null, voice, new object[] { text, 1 /* SVSFlagsAsync */ });
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SAPI] Direct speech failed: {ex.Message}");
            }
        }

        public static AudioClip SynthesizeSpeechToAudioClip(string text, string clipName, bool isFemale = true)
        {
            if (string.IsNullOrEmpty(text)) return null;

            string cacheKey = $"{isFemale}_{text.Trim()}";
            if (synthesizedClipsCache.TryGetValue(cacheKey, out AudioClip cachedClip) && cachedClip != null)
            {
                return cachedClip;
            }

            try
            {
                Type voiceType = Type.GetTypeFromProgID("SAPI.SpVoice");
                Type streamType = Type.GetTypeFromProgID("SAPI.SpFileStream");
                if (voiceType == null || streamType == null) return null;

                object voice = Activator.CreateInstance(voiceType);
                object stream = Activator.CreateInstance(streamType);

                ConfigureVoiceGender(voice, voiceType, isFemale);
                voiceType.InvokeMember("Rate", System.Reflection.BindingFlags.SetProperty, null, voice, new object[] { 0 });
                voiceType.InvokeMember("Volume", System.Reflection.BindingFlags.SetProperty, null, voice, new object[] { 100 });

                string tempDir = System.IO.Path.Combine(Application.temporaryCachePath, "TTS");
                if (!System.IO.Directory.Exists(tempDir)) System.IO.Directory.CreateDirectory(tempDir);
                string tempWav = System.IO.Path.Combine(tempDir, $"{clipName}_{Guid.NewGuid():N}.wav");

                // 3 = SSFMCreateForWrite
                streamType.InvokeMember("Open", System.Reflection.BindingFlags.InvokeMethod, null, stream, new object[] { tempWav, 3, false });
                voiceType.InvokeMember("AudioOutputStream", System.Reflection.BindingFlags.SetProperty, null, voice, new object[] { stream });
                voiceType.InvokeMember("Speak", System.Reflection.BindingFlags.InvokeMethod, null, voice, new object[] { text, 0 });
                streamType.InvokeMember("Close", System.Reflection.BindingFlags.InvokeMethod, null, stream, null);
                voiceType.InvokeMember("AudioOutputStream", System.Reflection.BindingFlags.SetProperty, null, voice, new object[] { null });

                if (System.IO.File.Exists(tempWav))
                {
                    AudioClip clip = WavUtility.FromFile(tempWav, clipName);
                    try { System.IO.File.Delete(tempWav); } catch { }
                    if (clip != null)
                    {
                        synthesizedClipsCache[cacheKey] = clip;
                    }
                    return clip;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TTS] SAPI Synthesis exception: {ex.Message}");
            }
            return null;
        }

        private static void ConfigureVoiceGender(object voice, Type voiceType, bool isFemale)
        {
            try
            {
                object voices = voiceType.InvokeMember("GetVoices", System.Reflection.BindingFlags.InvokeMethod, null, voice, null);
                int count = (int)voices.GetType().InvokeMember("Count", System.Reflection.BindingFlags.GetProperty, null, voices, null);
                for (int i = 0; i < count; i++)
                {
                    object item = voices.GetType().InvokeMember("Item", System.Reflection.BindingFlags.InvokeMethod, null, voices, new object[] { i });
                    string desc = (string)item.GetType().InvokeMember("GetDescription", System.Reflection.BindingFlags.InvokeMethod, null, item, null);
                    if (isFemale && (desc.Contains("Zira") || desc.Contains("Eva") || desc.Contains("Female") || desc.Contains("Hazel") || desc.Contains("Susan") || desc.Contains("Samantha")))
                    {
                        voiceType.InvokeMember("Voice", System.Reflection.BindingFlags.SetProperty, null, voice, new object[] { item });
                        break;
                    }
                    else if (!isFemale && (desc.Contains("David") || desc.Contains("Mark") || desc.Contains("Male") || desc.Contains("George")))
                    {
                        voiceType.InvokeMember("Voice", System.Reflection.BindingFlags.SetProperty, null, voice, new object[] { item });
                        break;
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Plays the Interviewer's official Welcome greeting audio line:
        /// "Welcome to Wipro Private Limited. I am Dr. Sarah Vance, VP of Spatial Systems..."
        /// Returns the exact spoken length in seconds.
        /// </summary>
        public float PlayInterviewerWelcome()
        {
            if (interviewerWelcomeClip == null)
            {
                InitializeVoiceAudioClips();
            }

            if (interviewerAudioSource != null && interviewerWelcomeClip != null)
            {
                interviewerAudioSource.Stop();
                interviewerAudioSource.pitch = 1.0f;
                interviewerAudioSource.clip = interviewerWelcomeClip;
                interviewerAudioSource.volume = Mathf.Clamp01(masterVolume > 0.05f ? masterVolume : 1.0f);
                interviewerAudioSource.spatialBlend = 0.12f;
                interviewerAudioSource.minDistance = 25.0f;
                interviewerAudioSource.maxDistance = 80.0f;
                interviewerAudioSource.mute = false;
                interviewerAudioSource.bypassListenerEffects = true;
                interviewerAudioSource.Play();

                Debug.Log($"[InterviewAudio] Playing Interviewer Welcome audio greeting ({interviewerWelcomeClip.length:F2}s)");
                return interviewerWelcomeClip.length;
            }

            return 5.5f;
        }

        public void PlayUIChime()
        {
            if (conferenceMicAudioSource != null && clipChime != null)
            {
                conferenceMicAudioSource.PlayOneShot(clipChime, 0.5f);
            }
        }

        #region Procedural Audio Synthesis
        private void GenerateProceduralAudioClips()
        {
            clipRoomAmbient = CreateAmbientRoomToneClip();
            clipInterviewerSpeech = CreateSpeechCadenceClip(baseFreq: 140f, duration: 2.2f);
            clipCandidateSpeech = CreateSpeechCadenceClip(baseFreq: 185f, duration: 2.5f);
            clipMicUnmute = CreateBeepClip(freq: 880f, duration: 0.12f, pitchUp: true);
            clipMicMute = CreateBeepClip(freq: 440f, duration: 0.15f, pitchUp: false);
            clipChime = CreateChimeClip();
        }

        private AudioClip CreateAmbientRoomToneClip()
        {
            int sampleRate = 44100;
            int length = sampleRate * 4; // 4 second loop
            float[] samples = new float[length];
            System.Random rand = new System.Random(42);

            float filter = 0f;
            for (int i = 0; i < length; i++)
            {
                float whiteNoise = (float)(rand.NextDouble() * 2.0 - 1.0) * 0.03f;
                float acHum = Mathf.Sin(2f * Mathf.PI * 60f * i / sampleRate) * 0.02f;
                float acSub = Mathf.Sin(2f * Mathf.PI * 120f * i / sampleRate) * 0.01f;

                filter = Mathf.Lerp(filter, whiteNoise, 0.08f);
                samples[i] = filter + acHum + acSub;
            }

            AudioClip clip = AudioClip.Create("AmbientRoomTone", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateSpeechCadenceClip(float baseFreq, float duration)
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * duration);
            float[] samples = new float[length];

            // Harmonic speech-like formant frequencies
            float[] syllables = { 1.0f, 1.15f, 1.25f, 1.0f, 1.4f, 1.2f, 0.9f, 1.1f, 1.0f };
            float syllableDuration = duration / syllables.Length;

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                int sylIndex = Mathf.Clamp((int)(t / syllableDuration), 0, syllables.Length - 1);
                float currentFreq = baseFreq * syllables[sylIndex];

                // Envelope modulation per syllable
                float sylT = (t % syllableDuration) / syllableDuration;
                float env = Mathf.Sin(sylT * Mathf.PI);

                float f1 = Mathf.Sin(2f * Mathf.PI * currentFreq * t);
                float f2 = Mathf.Sin(2f * Mathf.PI * currentFreq * 2f * t) * 0.4f;
                float f3 = Mathf.Sin(2f * Mathf.PI * currentFreq * 3f * t) * 0.2f;

                float sample = (f1 + f2 + f3) * env * 0.35f;

                // Overall fade-in / fade-out
                if (t < 0.05f) sample *= (t / 0.05f);
                if (t > duration - 0.05f) sample *= ((duration - t) / 0.05f);

                samples[i] = sample;
            }

            AudioClip clip = AudioClip.Create("SpeechCadence", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateBeepClip(float freq, float duration, bool pitchUp)
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * duration);
            float[] samples = new float[length];

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / length;
                float currentFreq = pitchUp ? Mathf.Lerp(freq * 0.8f, freq * 1.3f, t) : Mathf.Lerp(freq * 1.2f, freq * 0.7f, t);
                float env = Mathf.Sin(t * Mathf.PI);
                samples[i] = Mathf.Sin(2f * Mathf.PI * currentFreq * (float)i / sampleRate) * env * 0.4f;
            }

            AudioClip clip = AudioClip.Create("BeepClip", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateChimeClip()
        {
            int sampleRate = 44100;
            float duration = 0.45f;
            int length = (int)(sampleRate * duration);
            float[] samples = new float[length];

            float[] freqs = { 523.25f, 659.25f, 783.99f }; // C5, E5, G5 major triad
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 8f);
                float val = 0f;
                foreach (float f in freqs)
                {
                    val += Mathf.Sin(2f * Mathf.PI * f * t);
                }
                samples[i] = (val / freqs.Length) * env * 0.5f;
            }

            AudioClip clip = AudioClip.Create("ChimeClip", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        #endregion
    }

    /// <summary>
    /// Lightweight, zero-dependency WAV audio parser for Unity.
    /// Loads standard PCM 16-bit / 8-bit mono or stereo WAV files directly into native AudioClips.
    /// </summary>
    public static class WavUtility
    {
        public static AudioClip FromFile(string filePath, string clipName)
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                return null;
            }

            try
            {
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                return FromBytes(fileBytes, clipName);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WavUtility] Failed to read audio file '{filePath}': {ex.Message}");
                return null;
            }
        }

        public static AudioClip FromBytes(byte[] fileBytes, string clipName)
        {
            if (fileBytes == null || fileBytes.Length < 44)
            {
                return null;
            }

            try
            {
                // Verify RIFF and WAVE magic headers
                if (fileBytes[0] != 'R' || fileBytes[1] != 'I' || fileBytes[2] != 'F' || fileBytes[3] != 'F')
                {
                    return null;
                }
                if (fileBytes[8] != 'W' || fileBytes[9] != 'A' || fileBytes[10] != 'V' || fileBytes[11] != 'E')
                {
                    return null;
                }

                int channels = BitConverter.ToInt16(fileBytes, 22);
                int sampleRate = BitConverter.ToInt32(fileBytes, 24);
                int bitsPerSample = BitConverter.ToInt16(fileBytes, 34);

                if (channels <= 0 || sampleRate <= 0 || (bitsPerSample != 16 && bitsPerSample != 8))
                {
                    return null;
                }

                // Locate "data" chunk
                int pos = 12;
                int dataStart = -1;
                int dataSize = 0;

                while (pos + 8 <= fileBytes.Length)
                {
                    if (fileBytes[pos] == 'd' && fileBytes[pos + 1] == 'a' && fileBytes[pos + 2] == 't' && fileBytes[pos + 3] == 'a')
                    {
                        dataSize = BitConverter.ToInt32(fileBytes, pos + 4);
                        dataStart = pos + 8;
                        break;
                    }
                    int chunkSize = BitConverter.ToInt32(fileBytes, pos + 4);
                    pos += 8 + Math.Max(0, chunkSize);
                }

                if (dataStart < 0 || dataStart >= fileBytes.Length)
                {
                    dataStart = 44;
                    dataSize = fileBytes.Length - dataStart;
                }
                else if (dataStart + dataSize > fileBytes.Length)
                {
                    dataSize = fileBytes.Length - dataStart;
                }

                int bytesPerSample = bitsPerSample / 8;
                int totalSamples = dataSize / bytesPerSample;
                int sampleCountPerChannel = totalSamples / channels;

                if (totalSamples <= 0 || sampleCountPerChannel <= 0)
                {
                    return null;
                }

                float[] samples = new float[totalSamples];
                if (bitsPerSample == 16)
                {
                    for (int i = 0; i < totalSamples; i++)
                    {
                        int byteIdx = dataStart + i * 2;
                        if (byteIdx + 1 < fileBytes.Length)
                        {
                            short sampleVal = BitConverter.ToInt16(fileBytes, byteIdx);
                            samples[i] = sampleVal / 32768.0f;
                        }
                    }
                }
                else if (bitsPerSample == 8)
                {
                    for (int i = 0; i < totalSamples; i++)
                    {
                        int byteIdx = dataStart + i;
                        if (byteIdx < fileBytes.Length)
                        {
                            samples[i] = (fileBytes[byteIdx] - 128) / 128.0f;
                        }
                    }
                }

                AudioClip audioClip = AudioClip.Create(clipName ?? "WavAudioClip", sampleCountPerChannel, channels, sampleRate, false);
                audioClip.SetData(samples, 0);
                return audioClip;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WavUtility] Error parsing WAV bytes: {ex.Message}");
                return null;
            }
        }

        public static byte[] ToWavBytes(AudioClip clip)
        {
            if (clip == null) return null;

            int channels = clip.channels;
            int frequency = clip.frequency;
            int samplesCount = clip.samples * channels;
            float[] samples = new float[samplesCount];
            clip.GetData(samples, 0);

            int byteRate = frequency * channels * 2;
            int dataChunkSize = samplesCount * 2;
            int fileSize = 44 + dataChunkSize;

            byte[] bytes = new byte[fileSize];

            // RIFF header
            System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(bytes, 0);
            BitConverter.GetBytes(fileSize - 8).CopyTo(bytes, 4);
            System.Text.Encoding.ASCII.GetBytes("WAVE").CopyTo(bytes, 8);

            // fmt chunk
            System.Text.Encoding.ASCII.GetBytes("fmt ").CopyTo(bytes, 12);
            BitConverter.GetBytes(16).CopyTo(bytes, 16);
            BitConverter.GetBytes((short)1).CopyTo(bytes, 20); // PCM
            BitConverter.GetBytes((short)channels).CopyTo(bytes, 22);
            BitConverter.GetBytes(frequency).CopyTo(bytes, 24);
            BitConverter.GetBytes(byteRate).CopyTo(bytes, 28);
            BitConverter.GetBytes((short)(channels * 2)).CopyTo(bytes, 32);
            BitConverter.GetBytes((short)16).CopyTo(bytes, 34);

            // data chunk
            System.Text.Encoding.ASCII.GetBytes("data").CopyTo(bytes, 36);
            BitConverter.GetBytes(dataChunkSize).CopyTo(bytes, 40);

            int offset = 44;
            for (int i = 0; i < samplesCount; i++)
            {
                short val = (short)Mathf.Clamp(samples[i] * 32767f, -32768f, 32767f);
                BitConverter.GetBytes(val).CopyTo(bytes, offset);
                offset += 2;
            }

            return bytes;
        }

        public static bool SaveToFile(AudioClip clip, string filePath)
        {
            try
            {
                byte[] bytes = ToWavBytes(clip);
                if (bytes == null || bytes.Length == 0) return false;

                string dir = System.IO.Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }

                System.IO.File.WriteAllBytes(filePath, bytes);
                Debug.Log($"[WavUtility] Successfully saved WAV file to '{filePath}' ({bytes.Length} bytes)");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WavUtility] Failed to save WAV file to '{filePath}': {ex.Message}");
                return false;
            }
        }
    }
}
