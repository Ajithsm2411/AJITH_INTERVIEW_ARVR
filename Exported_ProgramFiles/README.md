# Interview Room 1 - Program Files & Source Code Package

This folder contains a clean copy of all program files and source code for the **Unity 6 Meta Quest 3 VR Telepresence Interview System**.

## Folder Structure

### 1. Source_Scripts_And_Code/
Direct access to all C# scripts, solution files, and editor tools without needing to search through asset hierarchies:
- **Scripts/**:
  - AIInterviewQuestionGenerator.cs - Dynamic AI question generation, adaptive contextual follow-ups, and answer evaluation.
  - InterviewRoomController.cs - Dialogue loop, automated question/answer flow, candidate microphone voice recording, and candidate delivery response.
  - InterviewRoomAudioController.cs - Microphone capture, headset audio playback, spatial audio.
  - AvatarAnimationController.cs - Seated character procedural gestures, idle fidgeting, head tracking, and real-time lip flutter.
  - InterviewRoomRuntimeBootstrap.cs - Headless and runtime initialization, camera binding to candidate seated view.
  - FreeFlyCamera.cs - Desktop test camera controls.
  - HologramEffect.cs - Holographic candidate shader and visual shimmer effects.
  - VR/VRGogglesRig.cs - VR Headset rig with seated eye-level camera anchor (.20\text{m}$) and character skull/neck mesh culling.
  - VR/VRControllerPointer.cs - VR Controller raycasting and interaction.
  - VR/VRSpatialVirtualScreen.cs - In-world 3D curved virtual display and evaluation teleprompter.
  - SpeechToText/AvatarSpeechBubble3D.cs - Real-time animated speech bubble above talking avatars.
  - SpeechToText/SpeechToTextTranscriptionController.cs - Real-time STT / Whisper transcription module.
- **Editor/**:
  - InterviewRoomBuilder.cs - Automated scene generator and room setup.
  - MetaQuest3SetupWizard.cs - One-click Quest 3 build validator and URP/XR config.
- **ProjectFiles/**:
  - InterviewRoom1.sln
  - Assembly-CSharp.csproj
  - Assembly-CSharp-Editor.csproj
  - InputSystem_Actions.inputactions
- **WebXRPlatform/**:
  - Standalone ASP.NET Core WebXR server & browser client (wwwroot/interview-app.js, wwwroot/engine.js, Program.cs).

---

### 2. Clean_Unity_Project/
A complete, lightweight Unity project root (Assets/, Packages/, ProjectSettings/, and WebXRPlatform/) with all transient cache folders (Library/, Temp/, Logs/, obj/, in/) excluded.
Can be moved, zipped, or opened directly in Unity 6000.0.32f1.

---
Created on: 2026-10-23 09:55:36
