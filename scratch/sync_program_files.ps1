# Sync and organize all program files to separate folders
param()

$ErrorActionPreference = "Stop"

$workspace = "C:\InterviewRoom1"
$extFolder = "C:\InterviewRoom1_ProgramFiles"
$localFolder = "C:\InterviewRoom1\Exported_ProgramFiles"

$destinations = @($extFolder, $localFolder)

foreach ($dest in $destinations) {
    Write-Host "Syncing to $dest ..."
    if (-not (Test-Path $dest)) {
        New-Item -ItemType Directory -Path $dest -Force | Out-Null
    }

    # 1. Source_Scripts_And_Code folder
    $srcCodeDest = Join-Path $dest "Source_Scripts_And_Code"
    if (-not (Test-Path $srcCodeDest)) {
        New-Item -ItemType Directory -Path $srcCodeDest -Force | Out-Null
    }

    # Copy Scripts
    $scriptsSrc = Join-Path $workspace "Assets\Scripts"
    $scriptsDest = Join-Path $srcCodeDest "Scripts"
    robocopy $scriptsSrc $scriptsDest /MIR /NP /NFL /NDL /NJH /NJS

    # Copy Editor
    $editorSrc = Join-Path $workspace "Assets\Editor"
    $editorDest = Join-Path $srcCodeDest "Editor"
    robocopy $editorSrc $editorDest /MIR /NP /NFL /NDL /NJH /NJS

    # Copy Solution and Project files
    $projFiles = @(
        "InterviewRoom1.sln",
        "Assembly-CSharp.csproj",
        "Assembly-CSharp-Editor.csproj",
        ".vsconfig"
    )
    foreach ($pf in $projFiles) {
        $pPath = Join-Path $workspace $pf
        if (Test-Path $pPath) {
            Copy-Item -Path $pPath -Destination $srcCodeDest -Force
        }
    }

    # Copy Input Actions
    $inputSrc = Join-Path $workspace "Assets\InputSystem_Actions.inputactions"
    if (Test-Path $inputSrc) {
        Copy-Item -Path $inputSrc -Destination $srcCodeDest -Force
    }

    # Copy WebXRPlatform if present
    $webxrSrc = Join-Path $workspace "WebXRPlatform"
    if (Test-Path $webxrSrc) {
        $webxrDest = Join-Path $srcCodeDest "WebXRPlatform"
        robocopy $webxrSrc $webxrDest /MIR /XD bin obj /NP /NFL /NDL /NJH /NJS
    }

    # 2. Clean_Unity_Project folder (standalone, runnable clone of Assets, Packages, ProjectSettings)
    $cleanProjDest = Join-Path $dest "Clean_Unity_Project"
    if (-not (Test-Path $cleanProjDest)) {
        New-Item -ItemType Directory -Path $cleanProjDest -Force | Out-Null
    }

    # Copy Assets (excluding Exported_ProgramFiles, scratch, ReportAssets)
    $assetsSrc = Join-Path $workspace "Assets"
    $assetsDest = Join-Path $cleanProjDest "Assets"
    robocopy $assetsSrc $assetsDest /MIR /XD Exported_ProgramFiles /NP /NFL /NDL /NJH /NJS

    # Copy Packages
    $packagesSrc = Join-Path $workspace "Packages"
    $packagesDest = Join-Path $cleanProjDest "Packages"
    robocopy $packagesSrc $packagesDest /MIR /NP /NFL /NDL /NJH /NJS

    # Copy ProjectSettings
    $settingsSrc = Join-Path $workspace "ProjectSettings"
    $settingsDest = Join-Path $cleanProjDest "ProjectSettings"
    robocopy $settingsSrc $settingsDest /MIR /NP /NFL /NDL /NJH /NJS

    # Also copy WebXRPlatform inside Clean_Unity_Project
    if (Test-Path $webxrSrc) {
        $cleanWebxrDest = Join-Path $cleanProjDest "WebXRPlatform"
        robocopy $webxrSrc $cleanWebxrDest /MIR /XD bin obj /NP /NFL /NDL /NJH /NJS
    }

    # Create README.md in destination
    $readmeContent = @"
# Interview Room 1 - Program Files & Source Code Package

This folder contains a clean copy of all program files and source code for the **Unity 6 Meta Quest 3 VR Telepresence Interview System**.

## Folder Structure

### 1. `Source_Scripts_And_Code/`
Direct access to all C# scripts, solution files, and editor tools without needing to search through asset hierarchies:
- **`Scripts/`**:
  - `AIInterviewQuestionGenerator.cs` - Dynamic AI question generation, adaptive contextual follow-ups, and answer evaluation.
  - `InterviewRoomController.cs` - Dialogue loop, automated question/answer flow, candidate microphone voice recording, and candidate delivery response.
  - `InterviewRoomAudioController.cs` - Microphone capture, headset audio playback, spatial audio.
  - `AvatarAnimationController.cs` - Seated character procedural gestures, idle fidgeting, head tracking, and real-time lip flutter.
  - `InterviewRoomRuntimeBootstrap.cs` - Headless and runtime initialization, camera binding to candidate seated view.
  - `FreeFlyCamera.cs` - Desktop test camera controls.
  - `HologramEffect.cs` - Holographic candidate shader and visual shimmer effects.
  - `VR/VRGogglesRig.cs` - VR Headset rig with seated eye-level camera anchor ($1.20\text{m}$) and character skull/neck mesh culling.
  - `VR/VRControllerPointer.cs` - VR Controller raycasting and interaction.
  - `VR/VRSpatialVirtualScreen.cs` - In-world 3D curved virtual display and evaluation teleprompter.
  - `SpeechToText/AvatarSpeechBubble3D.cs` - Real-time animated speech bubble above talking avatars.
  - `SpeechToText/SpeechToTextTranscriptionController.cs` - Real-time STT / Whisper transcription module.
- **`Editor/`**:
  - `InterviewRoomBuilder.cs` - Automated scene generator and room setup.
  - `MetaQuest3SetupWizard.cs` - One-click Quest 3 build validator and URP/XR config.
- **`ProjectFiles/`**:
  - `InterviewRoom1.sln`
  - `Assembly-CSharp.csproj`
  - `Assembly-CSharp-Editor.csproj`
  - `InputSystem_Actions.inputactions`
- **`WebXRPlatform/`**:
  - Standalone ASP.NET Core WebXR server & browser client (`wwwroot/interview-app.js`, `wwwroot/engine.js`, `Program.cs`).

---

### 2. `Clean_Unity_Project/`
A complete, lightweight Unity project root (`Assets/`, `Packages/`, `ProjectSettings/`, and `WebXRPlatform/`) with all transient cache folders (`Library/`, `Temp/`, `Logs/`, `obj/`, `bin/`) excluded.
Can be moved, zipped, or opened directly in Unity 6000.0.32f1.

---
Created on: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
"@

    Set-Content -Path (Join-Path $dest "README.md") -Value $readmeContent -Encoding UTF8

    # Create / update zip file of source scripts
    $zipFile = Join-Path $dest "InterviewRoom1_Source_Scripts.zip"
    if (Test-Path $zipFile) { Remove-Item -Path $zipFile -Force }
    Compress-Archive -Path (Join-Path $srcCodeDest "*") -DestinationPath $zipFile -Force

    Write-Host "Completed $dest successfully."
}

Write-Host "All program files copied, verified, and zipped successfully!"
