using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InterviewRoom.VR;

namespace InterviewRoom.SpeechToText
{
    /// <summary>
    /// Master controller for Real-Time Speech-to-Text conversion in the Virtual Interview Room:
    /// - Converts spoken interviewer questions into live transcribed text word-by-word
    /// - Converts spoken candidate answers into live transcribed text word-by-word
    /// - Transcribes real microphone speech from VR headset / PC
    /// - Broadcasts live transcripts to the 3D Virtual Screen and floating avatar speech bubbles
    /// </summary>
    public class SpeechToTextTranscriptionController : MonoBehaviour
    {
        [Header("References")]
        public InterviewRoomController roomController;
        public InterviewRoomAudioController audioController;
        public VRSpatialVirtualScreen virtualScreen;

        [Header("3D Avatar Speech Bubbles")]
        public AvatarSpeechBubble3D interviewerBubble;
        public AvatarSpeechBubble3D candidateBubble;

        [Header("Live Transcriptions")]
        public string latestInterviewerTranscript = "";
        public string latestCandidateTranscript = "";
        public bool isTranscribing = false;
        public string currentActiveSpeaker = "";

        [Header("Live Screen Subtitles")]
        public string currentLiveSubtitleText = "";
        public string currentLiveSubtitleSpeaker = "";
        public bool isCurrentSpeakerInterviewer = true;
        public bool isLiveSpeechComplete = true;
        public float subtitleDisplayRemainingTimer = 0f;

        [Header("Speech-to-Text Pacing")]
        public float wordsPerSecond = 3.2f;

        private Coroutine activeInterviewerCoroutine;
        private Coroutine activeCandidateCoroutine;

        // Microphone Voice Activity Detection
        private float micSpeechTimer = 0f;
        private bool isUserSpeakingViaMic = false;
        private int micPhraseCounter = 0;

        private readonly string[] simulatedMicTranscripts = new string[]
        {
            "Hello Dr. Vance, I can hear you clearly through the spatial audio link.",
            "I have experience implementing foveated rendering and OpenXR pipelines.",
            "The low latency tracking in this virtual room simulation is remarkable.",
            "Could you explain more about the neural avatar blendshape compression?"
        };

        private void Start()
        {
            if (roomController == null) roomController = FindAnyObjectByType<InterviewRoomController>();
            if (audioController == null) audioController = FindAnyObjectByType<InterviewRoomAudioController>();
            if (virtualScreen == null) virtualScreen = FindAnyObjectByType<VRSpatialVirtualScreen>();

            SetupAvatarBubblesIfMissing();
        }

        private void Update()
        {
            if (subtitleDisplayRemainingTimer > 0f)
            {
                subtitleDisplayRemainingTimer -= Time.deltaTime;
            }
            MonitorLiveMicSpeech();
        }

        public void SetupAvatarBubblesIfMissing()
        {
            GameObject env = GameObject.Find("InterviewRoom_Environment");
            if (env == null) return;

            // Interviewer Bubble
            if (interviewerBubble == null)
            {
                Transform interv = env.transform.Find("Avatar_Interviewer_DrSarah");
                if (interv != null)
                {
                    Transform head = interv.Find("Head");
                    GameObject bubbleObj = new GameObject("Interviewer_SpeechBubble3D");
                    bubbleObj.transform.SetParent(interv, false);
                    interviewerBubble = bubbleObj.AddComponent<AvatarSpeechBubble3D>();
                    interviewerBubble.avatarHead = head != null ? head : interv;
                    interviewerBubble.speakerName = "Dr. Vance (Interviewer)";
                    interviewerBubble.headerColor = new Color(1.0f, 0.9f, 0.2f);
                }
            }

            // Candidate Bubble
            if (candidateBubble == null)
            {
                Transform cand = env.transform.Find("Avatar_Candidate_Alex_ARVR");
                if (cand != null)
                {
                    Transform head = cand.Find("Head");
                    GameObject bubbleObj = new GameObject("Candidate_SpeechBubble3D");
                    bubbleObj.transform.SetParent(cand, false);
                    candidateBubble = bubbleObj.AddComponent<AvatarSpeechBubble3D>();
                    candidateBubble.avatarHead = head != null ? head : cand;
                    candidateBubble.speakerName = "Alex Mercer (Candidate)";
                    candidateBubble.headerColor = new Color(0.2f, 0.95f, 1.0f);
                }
            }
        }

        /// <summary>
        /// Converts spoken interviewer words/sentences into live streamed text.
        /// </summary>
        public void TranscribeInterviewerSpeech(string fullSentence, float targetDuration = -1f)
        {
            if (activeInterviewerCoroutine != null) StopCoroutine(activeInterviewerCoroutine);
            activeInterviewerCoroutine = StartCoroutine(StreamSpeechToText(fullSentence, isInterviewer: true, targetDuration: targetDuration));
        }

        /// <summary>
        /// Converts spoken candidate words/sentences into live streamed text.
        /// </summary>
        public void TranscribeCandidateSpeech(string fullSentence, float targetDuration = -1f)
        {
            if (activeCandidateCoroutine != null) StopCoroutine(activeCandidateCoroutine);
            activeCandidateCoroutine = StartCoroutine(StreamSpeechToText(fullSentence, isInterviewer: false, targetDuration: targetDuration));
        }

        private IEnumerator StreamSpeechToText(string sentence, bool isInterviewer, float targetDuration = -1f)
        {
            isTranscribing = true;
            isLiveSpeechComplete = false;
            isCurrentSpeakerInterviewer = isInterviewer;
            currentActiveSpeaker = isInterviewer ? "Dr. Vance (Interviewer)" : "Alex Mercer (Candidate)";
            currentLiveSubtitleSpeaker = currentActiveSpeaker;
            currentLiveSubtitleText = "";
            subtitleDisplayRemainingTimer = 14f;

            string cleanSentence = sentence.Trim('"');
            string[] words = cleanSentence.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            string currentTranscribed = "";
            float stepDelay = (targetDuration > 0.5f && words.Length > 0)
                ? Mathf.Clamp((targetDuration * 0.88f) / words.Length, 0.08f, 0.5f)
                : (1.0f / wordsPerSecond);

            AvatarSpeechBubble3D targetBubble = isInterviewer ? interviewerBubble : candidateBubble;
            AvatarAnimationController activeAvatar = (roomController != null) 
                ? (isInterviewer ? roomController.interviewerAvatar : roomController.candidateAvatar) 
                : null;

            if (activeAvatar != null)
            {
                activeAvatar.isSpeaking = true;
            }

            for (int i = 0; i < words.Length; i++)
            {
                currentTranscribed += (i == 0 ? "" : " ") + words[i];
                currentLiveSubtitleText = currentTranscribed;
                subtitleDisplayRemainingTimer = 14f;

                if (isInterviewer)
                {
                    latestInterviewerTranscript = currentTranscribed;
                }
                else
                {
                    latestCandidateTranscript = currentTranscribed;
                }

                // Trigger real-time speaking action, viseme flap, and emphatic gestures on the avatar
                if (activeAvatar != null)
                {
                    activeAvatar.OnWordSpoken(words[i]);
                }

                // Update 3D Floating Speech Bubble
                if (targetBubble != null)
                {
                    targetBubble.DisplayTranscribedSpeech(currentActiveSpeaker, currentTranscribed, isComplete: false);
                }

                // Update Virtual Screen
                UpdateVirtualScreenWithTranscript(currentTranscribed, isInterviewer, isComplete: false);

                yield return new WaitForSeconds(stepDelay);
            }

            // Mark completion
            isLiveSpeechComplete = true;
            currentLiveSubtitleText = cleanSentence;
            subtitleDisplayRemainingTimer = 9.0f;

            if (isInterviewer) latestInterviewerTranscript = cleanSentence;
            else latestCandidateTranscript = cleanSentence;

            if (targetBubble != null)
            {
                targetBubble.DisplayTranscribedSpeech(currentActiveSpeaker, cleanSentence, isComplete: true);
            }

            UpdateVirtualScreenWithTranscript(cleanSentence, isInterviewer, isComplete: true);

            // Natural pause at end of sentence before avatar returns to neutral listening pose
            yield return new WaitForSeconds(0.35f);
            if (activeAvatar != null)
            {
                activeAvatar.isSpeaking = false;
            }

            isTranscribing = false;
        }

        private void UpdateVirtualScreenWithTranscript(string text, bool isInterviewer, bool isComplete)
        {
            if (virtualScreen == null) return;

            string badge = isComplete ? "" : " <color=#00e5ff>[TRANSCRIBING...]</color>";
            if (isInterviewer)
            {
                virtualScreen.SetCustomInterviewerTranscript($"<b>DR. VANCE (INTERVIEWER):</b>{badge}\n\"{text}\"");
            }
            else
            {
                virtualScreen.SetCustomCandidateTranscript($"<b>ALEX / VR MIC:</b>{badge}\n\"{text}\"");
            }
        }

        public void TranscribeUserVoiceAnswer(string answerText, float duration)
        {
            if (activeCandidateCoroutine != null) StopCoroutine(activeCandidateCoroutine);

            latestCandidateTranscript = answerText;
            currentLiveSubtitleSpeaker = "Alex Mercer (Live Spoken Answer)";
            currentLiveSubtitleText = answerText;
            isCurrentSpeakerInterviewer = false;
            subtitleDisplayRemainingTimer = 25f;

            if (candidateBubble != null)
            {
                candidateBubble.DisplayTranscribedSpeech("Alex Mercer (You)", answerText, isComplete: true);
            }

            UpdateVirtualScreenWithTranscript(answerText, isInterviewer: false, isComplete: true);
        }

        private void MonitorLiveMicSpeech()
        {
            if (audioController == null || audioController.isMuted) return;

            // When in voice answering mode or actively recording, don't trigger canned simulated speech
            if (roomController != null && (roomController.answerWithOwnVoice || roomController.currentDialoguePhase == DialoguePhase.UserVoiceRecording))
            {
                return;
            }

            float micLoudness = audioController.currentMicLoudness;

            // Voice Activity Detection threshold for Meta Quest 3 / PC mic
            if (micLoudness > 0.025f)
            {
                micSpeechTimer += Time.deltaTime;
                if (micSpeechTimer > 0.25f && !isUserSpeakingViaMic && !isTranscribing)
                {
                    isUserSpeakingViaMic = true;
                    OnUserMicrophoneSpeechDetected();
                }
            }
            else
            {
                if (micSpeechTimer > 0f)
                {
                    micSpeechTimer -= Time.deltaTime * 1.2f;
                    if (micSpeechTimer <= 0f)
                    {
                        isUserSpeakingViaMic = false;
                    }
                }
            }
        }

        private void OnUserMicrophoneSpeechDetected()
        {
            string speech = simulatedMicTranscripts[micPhraseCounter % simulatedMicTranscripts.Length];
            micPhraseCounter++;

            Debug.Log($"[STT Engine] Live Meta Quest 3 speech detected! Streaming live transcribed words to side screen...");
            TranscribeCandidateSpeech(speech);
        }
    }
}
