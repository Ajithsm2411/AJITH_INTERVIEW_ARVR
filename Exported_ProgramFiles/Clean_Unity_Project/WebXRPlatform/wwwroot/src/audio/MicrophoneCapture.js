/**
 * Real Microphone Audio Capture with Voice Activity Detection (VAD)
 * Spec Section 14: Handles microphone permissions gracefully & prevents overlapping speech.
 */
export class MicrophoneCapture {
  constructor() {
    this.audioContext = null;
    this.mediaStream = null;
    this.analyser = null;
    this.mediaRecorder = null;
    this.audioChunks = [];
    this.isRecording = false;
    this.hasPermission = false;
    this.permissionError = null;

    // VAD & Loudness parameters
    this.currentLoudness = 0.0;
    this.isSpeaking = false;
    this.silenceTimer = 0.0;
    this.silenceThresholdSeconds = 1.8;
    this.onSilenceDetected = null;
    this.onLoudnessUpdate = null;
    this.animationFrameId = null;

    // Speech Overlap Prevention Flag (Spec 14)
    this.isInterviewerSpeaking = false;
  }

  async requestPermission() {
    try {
      this.mediaStream = await navigator.mediaDevices.getUserMedia({
        audio: {
          echoCancellation: true,
          noiseSuppression: true,
          autoGainControl: true
        }
      });
      this.hasPermission = true;
      this.permissionError = null;
      console.log('[MicrophoneCapture] Microphone hardware access granted.');
      return true;
    } catch (err) {
      this.hasPermission = false;
      this.permissionError = err.name === 'NotAllowedError'
        ? 'Microphone permission was denied by the user or browser.'
        : `Microphone unavailable: ${err.message}`;
      console.warn('[MicrophoneCapture]', this.permissionError);
      return false;
    }
  }

  setInterviewerSpeaking(speaking) {
    this.isInterviewerSpeaking = speaking;
    // Spec 14: Prevent overlapping speech and candidate recording
    if (speaking && this.isRecording) {
      console.warn('[MicrophoneCapture] Interviewer is speaking; pausing candidate microphone capture.');
    }
  }

  async startRecording() {
    if (this.isInterviewerSpeaking) {
      console.warn('[MicrophoneCapture] Cannot record while interviewer is speaking.');
      return false;
    }

    if (!this.hasPermission) {
      const ok = await this.requestPermission();
      if (!ok) return false;
    }

    if (!this.audioContext) {
      this.audioContext = new (window.AudioContext || window.webkitAudioContext)();
    }
    if (this.audioContext.state === 'suspended') {
      await this.audioContext.resume();
    }

    const source = this.audioContext.createMediaStreamSource(this.mediaStream);
    this.analyser = this.audioContext.createAnalyser();
    this.analyser.fftSize = 256;
    source.connect(this.analyser);

    this.audioChunks = [];
    this.mediaRecorder = new MediaRecorder(this.mediaStream, { mimeType: 'audio/webm' });
    this.mediaRecorder.ondataavailable = (e) => {
      if (e.data && e.data.size > 0) this.audioChunks.push(e.data);
    };
    this.mediaRecorder.start(100);

    this.isRecording = true;
    this.silenceTimer = 0.0;
    this._startLoudnessAnalysis();
    return true;
  }

  _startLoudnessAnalysis() {
    const dataArray = new Uint8Array(this.analyser.frequencyBinCount);
    let lastTime = performance.now();

    const checkLevel = () => {
      if (!this.isRecording) return;

      this.analyser.getByteFrequencyData(dataArray);
      let sum = 0;
      for (let i = 0; i < dataArray.length; i++) sum += dataArray[i];
      const avg = sum / dataArray.length;
      this.currentLoudness = Math.min(1.0, avg / 128.0);

      const now = performance.now();
      const dt = (now - lastTime) / 1000;
      lastTime = now;

      if (this.currentLoudness > 0.08) {
        this.isSpeaking = true;
        this.silenceTimer = 0.0;
      } else {
        if (this.isSpeaking) {
          this.silenceTimer += dt;
          if (this.silenceTimer >= this.silenceThresholdSeconds) {
            this.isSpeaking = false;
            if (this.onSilenceDetected) this.onSilenceDetected();
          }
        }
      }

      if (this.onLoudnessUpdate) {
        this.onLoudnessUpdate(this.currentLoudness, this.isSpeaking);
      }

      this.animationFrameId = requestAnimationFrame(checkLevel);
    };

    this.animationFrameId = requestAnimationFrame(checkLevel);
  }

  async stopRecording() {
    if (!this.isRecording) return null;
    this.isRecording = false;

    if (this.animationFrameId) cancelAnimationFrame(this.animationFrameId);

    return new Promise((resolve) => {
      this.mediaRecorder.onstop = () => {
        const audioBlob = new Blob(this.audioChunks, { type: 'audio/webm' });
        resolve(audioBlob);
      };
      this.mediaRecorder.stop();
    });
  }

  dispose() {
    if (this.isRecording) this.stopRecording();
    if (this.mediaStream) {
      this.mediaStream.getTracks().forEach(t => t.stop());
    }
    if (this.audioContext) {
      this.audioContext.close();
    }
  }
}
