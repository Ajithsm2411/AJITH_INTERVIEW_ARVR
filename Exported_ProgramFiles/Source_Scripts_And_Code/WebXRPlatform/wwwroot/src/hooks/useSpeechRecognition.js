import { MicrophoneCapture } from '../audio/MicrophoneCapture.js';

/**
 * Speech Recognition controller connecting Web Speech API with microphone capture
 */
export class SpeechRecognitionController {
  constructor() {
    this.mic = new MicrophoneCapture();
    this.recognition = null;
    this.finalTranscript = '';
    this.interimTranscript = '';
    this.onTranscriptUpdate = null;
    this.isListening = false;

    this._initWebSpeech();
  }

  _initWebSpeech() {
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (SpeechRecognition) {
      this.recognition = new SpeechRecognition();
      this.recognition.continuous = true;
      this.recognition.interimResults = true;
      this.recognition.lang = 'en-US';

      this.recognition.onresult = (event) => {
        let interim = '';
        for (let i = event.resultIndex; i < event.results.length; ++i) {
          if (event.results[i].isFinal) {
            this.finalTranscript += event.results[i][0].transcript + ' ';
          } else {
            interim += event.results[i][0].transcript;
          }
        }
        this.interimTranscript = interim;

        if (this.onTranscriptUpdate) {
          this.onTranscriptUpdate({
            final: this.finalTranscript.trim(),
            interim: this.interimTranscript.trim(),
            full: (this.finalTranscript + ' ' + this.interimTranscript).trim()
          });
        }
      };

      this.recognition.onerror = (e) => {
        console.warn('[SpeechRecognition] Speech API error:', e.error);
      };
    } else {
      console.warn('[SpeechRecognition] Web Speech API not supported; relying on audio upload & text fallback.');
    }
  }

  async startListening() {
    this.finalTranscript = '';
    this.interimTranscript = '';
    this.isListening = true;

    await this.mic.startRecording();
    if (this.recognition) {
      try {
        this.recognition.start();
      } catch (e) {
        // Already started
      }
    }
  }

  async stopListening() {
    this.isListening = false;
    if (this.recognition) {
      try {
        this.recognition.stop();
      } catch (e) {}
    }
    const audioBlob = await this.mic.stopRecording();
    return {
      transcript: this.finalTranscript.trim(),
      audioBlob
    };
  }
}
