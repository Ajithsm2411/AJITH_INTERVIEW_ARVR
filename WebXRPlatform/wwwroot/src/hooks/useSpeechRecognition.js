import { MicrophoneCapture } from '../audio/MicrophoneCapture.js';

/**
 * Speech Recognition controller connecting Web Speech API with microphone capture
 * Supports automatic restart on unexpected end, error recovery, and transcript resets.
 */
export class SpeechRecognitionController {
  constructor() {
    this.mic = new MicrophoneCapture();
    this.recognition = null;
    this.finalTranscript = '';
    this.interimTranscript = '';
    this.onTranscriptUpdate = null;
    this.onError = null;
    this.isListening = false;
    this.shouldKeepListening = false;

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
        if (this.onError) this.onError(e.error);
      };

      this.recognition.onend = () => {
        // Auto-restart if we are still supposed to be listening
        if (this.shouldKeepListening && this.isListening) {
          try {
            this.recognition.start();
          } catch (err) {
            // Wait brief moment before retry
            setTimeout(() => {
              if (this.shouldKeepListening) {
                try { this.recognition.start(); } catch (e) {}
              }
            }, 300);
          }
        }
      };
    } else {
      console.warn('[SpeechRecognition] Web Speech API not supported; relying on audio upload & text fallback.');
    }
  }

  resetTranscript() {
    this.finalTranscript = '';
    this.interimTranscript = '';
    if (this.onTranscriptUpdate) {
      this.onTranscriptUpdate({ final: '', interim: '', full: '' });
    }
  }

  async startListening() {
    this.resetTranscript();
    this.isListening = true;
    this.shouldKeepListening = true;

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
    this.shouldKeepListening = false;
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

/**
 * Dedicated Speech Recognition for the Interviewer (Prompt Requirement 1)
 * Allows human interviewer to speak questions into microphone
 */
export class InterviewerSpeechRecognizer {
  constructor() {
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    this.recognition = SpeechRecognition ? new SpeechRecognition() : null;
    this.isListening = false;
    this.currentTranscript = '';
    this.onTranscript = null;
    this.onFinalQuestion = null;

    if (this.recognition) {
      this.recognition.continuous = false;
      this.recognition.interimResults = true;
      this.recognition.lang = 'en-US';

      this.recognition.onresult = (event) => {
        let transcript = '';
        for (let i = event.resultIndex; i < event.results.length; ++i) {
          transcript += event.results[i][0].transcript;
        }
        this.currentTranscript = transcript;
        if (this.onTranscript) this.onTranscript(transcript);
      };

      this.recognition.onend = () => {
        this.isListening = false;
        if (this.onFinalQuestion && this.currentTranscript.trim().length > 0) {
          this.onFinalQuestion(this.currentTranscript.trim());
        }
      };
    }
  }

  start() {
    if (!this.recognition) return false;
    this.currentTranscript = '';
    this.isListening = true;
    try {
      this.recognition.start();
      return true;
    } catch (e) {
      return false;
    }
  }

  stop() {
    if (!this.recognition) return;
    this.isListening = false;
    try {
      this.recognition.stop();
    } catch (e) {}
  }
}

