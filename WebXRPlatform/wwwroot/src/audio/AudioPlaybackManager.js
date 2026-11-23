import { spatialAudio } from './SpatialAudioEngine.js';

export class AudioPlaybackManager {
  constructor() {
    this.synth = window.speechSynthesis || null;
    this.isSpeaking = false;
    this.onSpeechStart = null;
    this.onSpeechEnd = null;
    this.currentUtterance = null;
  }

  speak(text, onViseme = null, voiceName = 'female') {
    if (!this.synth) {
      console.warn('[AudioPlaybackManager] Web Speech Synthesis not supported in this browser.');
      this._simulateProceduralSpeech(text, onViseme);
      return;
    }

    this.synth.cancel(); // Stop prior speech

    const utterance = new SpeechSynthesisUtterance(text);
    utterance.rate = 1.0;
    utterance.pitch = voiceName === 'female' ? 1.15 : 0.95;

    // Select natural voice if available
    const voices = this.synth.getVoices();
    const preferredVoice = voices.find(v => v.lang.startsWith('en') && (voiceName === 'female' ? v.name.includes('Female') || v.name.includes('Samantha') || v.name.includes('Zira') : v.name.includes('David')));
    if (preferredVoice) utterance.voice = preferredVoice;

    utterance.onstart = () => {
      this.isSpeaking = true;
      if (this.onSpeechStart) this.onSpeechStart();
    };

    utterance.onend = () => {
      this.isSpeaking = false;
      if (this.onSpeechEnd) this.onSpeechEnd();
    };

    utterance.onerror = (e) => {
      console.warn('[AudioPlaybackManager] Synthesis error:', e);
      this.isSpeaking = false;
      if (this.onSpeechEnd) this.onSpeechEnd();
    };

    this.currentUtterance = utterance;
    this.synth.speak(utterance);

    // Viseme simulation synced to word boundary
    utterance.onboundary = (event) => {
      if (onViseme && event.name === 'word') {
        onViseme({ mouthOpen: 0.75, lipPucker: 0.4 });
        setTimeout(() => onViseme({ mouthOpen: 0.2, lipPucker: 0.1 }), 120);
      }
    };
  }

  stop() {
    if (this.synth) this.synth.cancel();
    this.isSpeaking = false;
    if (this.onSpeechEnd) this.onSpeechEnd();
  }

  playUIChime() {
    spatialAudio.playSpatialTone(587.33, 0.15); // D5
    setTimeout(() => spatialAudio.playSpatialTone(880.00, 0.25), 120); // A5
  }

  _simulateProceduralSpeech(text, onViseme) {
    const duration = Math.max(2.0, text.split(/\s+/).length * 0.4);
    if (this.onSpeechStart) this.onSpeechStart();

    const interval = setInterval(() => {
      if (onViseme) onViseme({ mouthOpen: Math.random() * 0.8, lipPucker: 0.3 });
    }, 100);

    setTimeout(() => {
      clearInterval(interval);
      if (onViseme) onViseme({ mouthOpen: 0.0, lipPucker: 0.0 });
      if (this.onSpeechEnd) this.onSpeechEnd();
    }, duration * 1000);
  }
}

export const audioPlayback = new AudioPlaybackManager();
export default audioPlayback;
