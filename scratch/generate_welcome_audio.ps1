Add-Type -AssemblyName System.Speech

$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer

# Select female voice (Microsoft Zira Desktop)
$voices = $synth.GetInstalledVoices()
foreach ($v in $voices) {
    if ($v.VoiceInfo.Name -like "*Zira*" -or $v.VoiceInfo.Gender -eq [System.Speech.Synthesis.VoiceGender]::Female) {
        $synth.SelectVoice($v.VoiceInfo.Name)
        Write-Host "Selected voice: $($v.VoiceInfo.Name)"
        break
    }
}

$synth.Rate = 0
$synth.Volume = 100

$dialogueDir = "C:\InterviewRoom1\Assets\Resources\Audio\Dialogue"
if (-not (Test-Path $dialogueDir)) {
    New-Item -ItemType Directory -Path $dialogueDir -Force | Out-Null
}

$wavPath = Join-Path $dialogueDir "Interviewer_Welcome.wav"

$synth.SetOutputToWaveFile($wavPath)
$welcomeSpeech = "Welcome to Wipro Private Limited. I am Dr. Sarah Vance, VP of Spatial Systems. We are very excited to welcome you to our executive interview suite. Whenever you are ready, let us begin."
$synth.Speak($welcomeSpeech)
$synth.SetOutputToNull()
$synth.Dispose()

Write-Host "Successfully generated Welcome audio at: $wavPath"
$file = Get-Item $wavPath
Write-Host "File size: $($file.Length) bytes"

# Also copy to WebXR
$webxrAudioDir = "C:\InterviewRoom1\WebXRPlatform\wwwroot\audio"
if (-not (Test-Path $webxrAudioDir)) {
    New-Item -ItemType Directory -Path $webxrAudioDir -Force | Out-Null
}
Copy-Item $wavPath (Join-Path $webxrAudioDir "Interviewer_Welcome.wav") -Force
Write-Host "Copied to WebXR audio folder."
