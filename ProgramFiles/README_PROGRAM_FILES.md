# VR Telepresence AI Interview Platform — Program Files

This directory contains the complete, clean source code and architecture of the **VR Telepresence AI Interview Platform**.

---

## 📂 Directory Structure

```
ProgramFiles/
├── Scripts/                                     # Unity C# Runtime Scripts
│   ├── InterviewRoomController.cs               # Central 6-Step AI interview lifecycle manager & VR HUD
│   ├── InterviewRoomAudioController.cs          # Microphone VAD, procedural audio, master volume & permissions
│   ├── AvatarAnimationController.cs             # 6 Avatar states (Idle, Speaking, Listening, Thinking, Waiting, InterviewCompleted)
│   ├── AIInterviewQuestionGenerator.cs          # AI Question Matrix, system instructions, and comprehensive scoring report
│   ├── InterviewRoomRuntimeBootstrap.cs         # Scene bootstrap initializing cameras and audio
│   ├── FreeFlyCamera.cs                         # Desktop inspection camera controller
│   ├── HologramEffect.cs                        # Procedural AR holographic charts & telemetry
│   ├── SpeechToText/
│   │   ├── AvatarSpeechBubble3D.cs              # In-world curved 3D candidate speech bubbles
│   │   └── SpeechToTextTranscriptionController.cs # Candidate speech transcription & closed captions
│   └── VR/
│       ├── VRGogglesRig.cs                      # 6-DoF seated eye-level VR headset rig (1.20m, neck-culling)
│       ├── VRControllerPointer.cs               # Meta Quest 3 laser pointers & haptic feedback
│       └── VRSpatialVirtualScreen.cs            # Floating 3D telepresence display panel
├── Editor/                                      # Unity Editor Automation Tools
│   ├── InterviewRoomBuilder.cs                  # Procedural executive boardroom environment builder
│   └── MetaQuest3SetupWizard.cs                 # One-click OpenXR & Android Quest 3 deployment tool
├── WebXRPlatform/                               # ASP.NET Core & WebXR Multi-User Backend
│   ├── Program.cs                               # 9 REST API endpoints & WebSockets real-time sync
│   ├── WebXRInterviewRoom.csproj                # ASP.NET Core Web App project
│   └── wwwroot/
│       ├── index.html                           # WebXR 3D Spatial Room front-end
│       ├── interview-app.js                     # WebXR controller & audio speech bridge
│       ├── engine.js                            # Three.js WebXR spatial rendering engine
│       └── style.css                            # Glassmorphic UI stylesheet
├── InterviewRoom1.sln                           # Visual Studio / Rider Solution file
├── Assembly-CSharp.csproj                       # Unity Runtime C# Project file
└── Assembly-CSharp-Editor.csproj                # Unity Editor C# Project file
```

---

## 🎯 6-Step Automatic AI Interview Workflow

1. **Step 1: Configuration**:
   - Modal configuration supporting Job Roles (`Software Engineer`, `Frontend Developer`, `Backend Developer`, `Data Analyst`, `HR`, `Custom Role`).
   - Experience Levels (`Fresher`, `Junior`, `Mid-Level`, `Senior`).
   - Interview Types (`Technical`, `HR`, `Behavioral`, `Mixed`).
   - Difficulty (`Easy`, `Medium`, `Hard`).
   - Duration (`5`, `10`, `15`, `30` minutes).

2. **Step 2: Start Interview**:
   - Avatars are seated face-to-face in executive chairs at natural $1.20\text{m}$ eye level.
   - Microphone hardware permissions verified.
   - Dr. Sarah Vance speaks Question 1 using procedural cadence, visemes, and natural gestures (`AvatarState.Speaking`).

3. **Step 3: Candidate Spoken Answer**:
   - Candidate answers via real VR headset microphone (Alex Mercer).
   - Live VU meter, silence detection (>1.8s) or manual finish button (`[Space]` / VR Trigger).
   - Text input fallback available if microphone is unavailable.

4. **Step 4: Answer Delivery & Evaluation**:
   - Candidate avatar delivers response to Dr. Vance with user's voice playback (`AvatarState.Speaking`).
   - Dr. Vance enters thoughtful chin-holding pose (`AvatarState.Thinking`).
   - Answer evaluated across 4 dimensions: Relevance, Clarity, Technical Accuracy, Completeness.
   - Dr. Vance speaks acknowledgment reaction.

5. **Step 5: Adaptive Question Progression**:
   - AI generates the next question or adaptive follow-up without repetition.
   - Checks session duration remaining.

6. **Step 6: Comprehensive Performance Report**:
   - Overall Score (0-100) & Category Scores (Relevance, Clarity, Technical, Completeness).
   - Identified Strengths & Weaknesses.
   - Actionable Improvement Suggestions.
   - Suggested Topics to Practice Before a Real Interview.
   - Full chronological Q&A review.
   - Clear disclaimer: *"AI-generated scores are automated estimates for practice, not definitive hiring judgments."*

---

## 🌐 Backend REST API Endpoints

- `POST /api/interviews` — Create an interview session with role, level, type, difficulty, duration.
- `POST /api/interviews/{id}/start` — Start session and receive Question 1.
- `POST /api/interviews/{id}/questions/next` — Generate next question or adaptive follow-up.
- `POST /api/interviews/{id}/answers` — Submit candidate's answer transcript for evaluation.
- `POST /api/speech/transcribe` — Speech recognition / text fallback.
- `POST /api/speech/synthesize` — Synthesize interviewer speech timing and cadence metadata.
- `POST /api/interviews/{id}/finish` — Finalize session and generate report.
- `GET /api/interviews/{id}/report` — Retrieve the comprehensive performance report.
- `GET /api/interviews/{id}/transcript` — Retrieve full chronological Q&A transcript.

---

## 🥽 VR Seated Position & Controls

- **Candidate Chair Position**: `(0, 0, -1.25)` at $1.20\text{m}$ eye level.
- **Interviewer Chair Position**: `(0, 0, 1.25)` at $1.20\text{m}$ eye level.
- **Head & Neck Culling**: Prevents clipping inside the avatar's head while allowing user to see body/arms when looking down.
- **VR Control Bar**:
  - `⏯️ Pause / Resume [P]`
  - `🔄 Repeat Question [R]`
  - `⏹️ End Interview [Esc]`
  - `🎙️ Mic Mute / Unmute [M]`
  - `🔊 Master Volume Slider (0-100%)`
  - `💬 Captions [C]`
  - `⚙️ Setup [O]`
