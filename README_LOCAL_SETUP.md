# VR Telepresence AI Interview Platform — Local Setup & Architecture Guide

This guide documents the complete setup, database schema, environment configuration, and execution instructions for the **VR Telepresence AI Interview Platform** as specified across Sections 12 through 16.

---

## 🏗️ 1. Project Directory Structure (Section 15)

```
InterviewRoom1/
├── client/                                      # WebXR & 3D Spatial Client
│   ├── package.json                             # Client dependencies (Three.js, Vite)
│   ├── index.html                               # HTML5 entry point & Three.js importmap
│   └── src/
│       ├── main.js                              # Application bootstrap
│       ├── style.css                            # Glassmorphic dark design system
│       ├── components/                          # UI Overlay components
│       │   ├── InterviewSetupModal.js           # Step 1: Configuration modal (Role, Level, Type, Diff, Dur)
│       │   ├── VRControlsBar.js                 # Controls (Pause, Repeat, End, Mic Mute, Volume, Timer)
│       │   ├── LiveTranscriptionOverlay.js      # Speech subtitles, VU meter, and Typed Text Fallback
│       │   ├── EvaluationReportModal.js         # Step 6: Final evaluation report & rubric breakdown
│       │   ├── ConsentModal.js                  # Explicit audio retention consent & deletion controls
│       │   └── NonVRFallbackBanner.js           # Desktop 3D preview banner when WebXR is unsupported
│       ├── vr/                                  # WebXR rendering & comfort
│       │   ├── WebXRManager.js                  # Immersive-VR session management & Quest 3 controllers
│       │   ├── SpatialEnvironment.js            # 3D executive boardroom (table, chairs, lighting)
│       │   └── VRTeleportation.js               # Seated eye-level comfort alignment
│       ├── avatars/                             # 3D avatars & animations
│       │   ├── AvatarManager.js                 # Coordinates both avatars at seated 1.20m eye level
│       │   ├── InterviewerAvatar.js             # Dr. Sarah Vance (6 states: Idle, Speak, Listen, Think, etc.)
│       │   ├── CandidateAvatar.js               # Alex Mercer (conversational gestures, head culling for VR)
│       │   └── LipSyncAnimator.js               # Real-time mouth morphing driven by audio amplitudes
│       ├── audio/                               # Web Audio & spatial acoustics
│       │   ├── MicrophoneCapture.js             # Real microphone capture, VAD, silence detection (>1.8s)
│       │   ├── SpatialAudioEngine.js            # Web Audio HRTF 3D PannerNode across the table
│       │   └── AudioPlaybackManager.js          # Speech synthesis & UI chimes
│       ├── hooks/                               # State & speech hooks
│       │   ├── useInterviewState.js             # 6-step finite state machine orchestrator
│       │   └── useSpeechRecognition.js          # Web Speech API + Whisper + typed fallback bridge
│       ├── pages/                               # Main assembly page
│       │   └── InterviewRoomPage.js             # Connects 3D canvas, avatars, audio, and UI
│       └── services/                            # Network services
│           ├── apiClient.js                     # REST API client with timeout & retry handling
│           └── webSocketClient.js               # Low-latency telemetry & avatar state stream
├── server/                                      # Backend REST API & Database
│   ├── package.json                             # Server dependencies (Express, CORS, SQLite3, Dotenv)
│   ├── .env.example                             # Environment variable template
│   └── src/
│       ├── index.js                             # Express HTTP / HTTPS server & middleware
│       ├── database/
│       │   ├── schema.sql                       # 6 SQLite database tables & indices
│       │   └── database.js                      # Connection manager & automatic migration runner
│       ├── models/                              # Data access models
│       │   ├── User.js                          # Candidate / User record
│       │   ├── InterviewSession.js              # Session config, timestamps, consent
│       │   ├── InterviewQuestion.js             # Question order, text, category, rubric criteria
│       │   ├── CandidateAnswer.js               # Spoken transcript, duration, audio deletion controls
│       │   ├── InterviewEvaluation.js           # Multi-criteria scoring & evidence-based feedback
│       │   └── InterviewReport.js               # Transparent report, rubrics, and limitations disclaimer
│       ├── middleware/                          # Security & Reliability (Section 14)
│       │   ├── authMiddleware.js                # Session protection against unauthorized access
│       │   ├── audioValidator.js                # Audio payload validation (<25MB) & consent checks
│       │   ├── rateLimiter.js                   # In-memory sliding window rate limiter
│       │   └── errorHandler.js                  # Request timeouts (15s default) & global error handling
│       ├── services/
│       │   ├── ai/
│       │   │   ├── geminiService.js             # Gemini / LLM client with retries & exponential backoff
│       │   │   ├── questionGenerator.js         # Dynamic AI question synthesizer without repetition
│       │   │   └── answerEvaluator.js           # 6-criteria rubric evaluator & report compiler
│       │   └── speech/
│       │       ├── sttService.js                # Whisper speech-to-text & text fallback processor
│       │       └── ttsService.js                # Viseme generation & speech cadence metadata
│       ├── controllers/
│       │   ├── interviewController.js           # Session lifecycle & question progression
│       │   ├── speechController.js              # Transcription, synthesis, audio deletion
│       │   └── reportController.js              # Report scorecard & chronological Q&A transcript
│       └── routes/
│           ├── interviewRoutes.js               # /api/interviews
│           ├── speechRoutes.js                  # /api/speech
│           └── reportRoutes.js                  # /api/reports
├── shared/                                      # Shared types and constants
│   └── types/
│       ├── index.js                             # Enums: JobRole, ExperienceLevel, DialoguePhase, etc.
│       └── index.d.ts                           # Complete TypeScript interfaces
└── WebXRPlatform/                               # Integrated Standalone C# .NET WebXR Runner
    ├── Program.cs                               # Native ASP.NET Core server hosting WebXR at :5000
    └── WebXRInterviewRoom.csproj                # Standalone C# project (runs with `dotnet run`)
```

---

## 🗄️ 2. Database Schema (Section 12)

The SQLite database (`server/data/interview_platform.db`) defines 6 normalized tables with relational foreign keys:

1. **`Users`**: Candidate profile (`id`, `email`, `name`, `role`, `created_at`).
2. **`InterviewSessions`**: Configuration parameters (`job_role`, `experience_level`, `interview_type`, `difficulty`, `duration_minutes`, `status`, `audio_consent_granted`, `started_at`, `ended_at`).
3. **`InterviewQuestions`**: Chronological questions (`session_id`, `question_index`, `question_text`, `category`, `expected_points`).
4. **`CandidateAnswers`**: Spoken answers (`session_id`, `question_id`, `transcript_text`, `speech_duration_seconds`, `is_typed_fallback`, `audio_storage_path`).
   - *Privacy Rule*: `audio_storage_path` is `NULL` by default. Raw audio is only stored if explicit consent was granted.
5. **`InterviewEvaluations`**: Rubrics (`relevance_score`, `technical_accuracy_score`, `clarity_score`, `completeness_score`, `problem_solving_score`, `confidence_indicators`, `evidence_feedback`, `missing_key_concepts`, `interviewer_ack_reaction`).
6. **`InterviewReports`**: Final report scorecard (`overall_score`, `relevance_percentage`, `technical_percentage`, `clarity_percentage`, `completeness_percentage`, `problem_solving_percentage`, `strengths_json`, `weaknesses_json`, `actionable_recommendations_json`, `suggested_practice_topics_json`, `score_limitations_disclaimer`).

---

## 🛡️ 3. Security, Privacy & Reliability (Section 14)

- **API Keys**: Stored strictly in `server/.env` backend environment variables (`GEMINI_API_KEY`, `OPENAI_API_KEY`).
- **Audio Consent & Deletion**: Default behavior never saves raw audio to disk. Deletion endpoint `DELETE /api/speech/audio/:answerId` allows permanent unlinking and erasure of recorded files.
- **Audio Upload Limiter**: Maximum upload size is enforced at 25MB (`MAX_AUDIO_UPLOAD_SIZE_BYTES=26214400`) with MIME validation (`audio/webm`, `audio/wav`, `audio/ogg`).
- **Request Timeouts & Retries**: All API requests timeout after 15 seconds (`REQUEST_TIMEOUT_MS=15000`) with exponential backoff retry handling.
- **Speech Overlap Gating**: Candidate microphone is automatically muted while Dr. Sarah Vance is speaking to prevent acoustic feedback and overlapping speech.
- **Non-VR Fallback**: When WebXR hardware is absent, the client seamlessly renders a seated desktop 3D view at eye level ($1.20\text{m}$) with mouse-look controls.
- **Text Input Fallback**: If microphone hardware is missing or permission is denied, candidates can type answers directly in the transcription overlay.

---

## 📊 4. Interview Evaluation Rubric (Section 13)

The evaluation rubric assesses 6 criteria:
1. **Relevance to Question** (1-5★)
2. **Technical Accuracy** (1-5★)
3. **Communication Clarity** (1-5★)
4. **Completeness** (1-5★)
5. **Problem-Solving Approach** (1-5★)
6. **Confidence & Articulation Indicators**: Evaluated purely through text structure, logical organization, and directness—**never** inferred from acoustic voice characteristics.

**Transparency & Score Limitations Disclaimer**:
> *"Notice: AI-generated evaluations, rubrics, and scores are automated simulated estimates designed for interview practice, reflection, and self-improvement. They do not constitute official hiring decisions, psychometric determinations, or definitive employability evaluations."*

---

## 🚀 5. How to Run Locally

You can run the platform locally in **either** Node.js or .NET standalone mode:

### Option A: Standalone .NET Runner (Instant, zero external installs)
The repository includes an integrated ASP.NET Core server in `WebXRPlatform/` serving both the WebXR frontend and REST API on port `5000`:

```bash
cd c:\InterviewRoom1
dotnet run --project WebXRPlatform/WebXRInterviewRoom.csproj
```
Open your browser or Meta Quest 3 browser to:
👉 **`http://localhost:5000`**

### Option B: Node.js Express Backend + Client
```bash
# 1. Setup Backend Server
cd c:\InterviewRoom1\server
cp .env.example .env
npm install
npm run dev

# 2. Setup Client (in a separate terminal)
cd c:\InterviewRoom1\client
npm install
npm run dev
```
Open:
👉 **`http://localhost:3000`** (or `http://localhost:5173`)

### Option C: Standalone Unity 6 OpenXR Headset Build
Open `c:\InterviewRoom1` in **Unity 6.00**, open scene `Assets/Scenes/InterviewRoom.unity`, and press **Play** or build for Meta Quest 3 via `InterviewRoom -> Setup Meta Quest 3 VR`.
