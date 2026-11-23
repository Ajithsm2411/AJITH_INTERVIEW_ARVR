import { InterviewerSpeechRecognizer } from '../hooks/useSpeechRecognition.js';

/**
 * Interviewer Question Control Dock (Prompt Requirement 1)
 * Allows human interviewer or proctor to:
 * - Speak questions through a microphone with speech-to-text recognition
 * - Enter custom questions as text
 * - Generate AI contextual follow-up questions based on candidate's answers
 * - Broadcast question to candidate's VR headset and 3D in-VR spatial screen
 */
export class InterviewerControlDock {
  constructor(container, { onAskQuestion, onGenerateFollowup, onRepeatQuestion }) {
    this.container = container;
    this.onAskQuestion = onAskQuestion;
    this.onGenerateFollowup = onGenerateFollowup;
    this.onRepeatQuestion = onRepeatQuestion;

    this.element = null;
    this.speechRecognizer = new InterviewerSpeechRecognizer();
    this.isListening = false;
    this.isInterviewerSpeaking = false;
    this.candidateLastAnswer = '';

    this.render();
  }

  render() {
    this.element = document.createElement('div');
    this.element.className = 'interviewer-dock';
    this.element.innerHTML = `
      <div class="dock-header">
        <div class="dock-title">
          <span class="dock-icon">👩‍💼</span>
          <span>INTERVIEWER CONTROL CONSOLE</span>
        </div>
        <div class="dock-status-badge" id="dock-status-badge">READY</div>
      </div>

      <div class="dock-body">
        <!-- Voice Input Section -->
        <div class="dock-section">
          <label class="section-label">1. Speak Question (Voice Input)</label>
          <div class="mic-input-row">
            <button type="button" id="btn-interviewer-mic" class="btn-dock btn-primary">
              <span id="interviewer-mic-icon">🎙️</span>
              <span id="interviewer-mic-text">Speak Question via Mic</span>
            </button>
            <span class="mic-indicator-dot" id="interviewer-mic-dot"></span>
          </div>
          <div class="transcription-preview" id="interviewer-transcription-preview">
            Click "Speak Question via Mic" to speak your question into your microphone...
          </div>
        </div>

        <!-- Text Input Section -->
        <div class="dock-section">
          <label class="section-label">2. Or Type Custom Question</label>
          <div class="text-input-row">
            <textarea id="custom-question-input" rows="2" placeholder="Type custom question for the candidate in VR..."></textarea>
          </div>
          <div class="quick-suggest-row">
            <button type="button" class="btn-chip" data-q="Can you explain how you handle state synchronization across low-latency WebSockets?">📡 WebSockets Sync</button>
            <button type="button" class="btn-chip" data-q="Walk us through your approach to optimizing 6-DoF VR performance and frame timing.">🥽 6-DoF VR Perf</button>
            <button type="button" class="btn-chip" data-q="How do you architect fallback strategies when microphone or network connections degrade?">🛡️ Audio Fallback</button>
          </div>
        </div>

        <!-- Action Row -->
        <div class="dock-actions">
          <button type="button" id="btn-send-question" class="btn-dock btn-send">
            📢 Send Question to Candidate VR
          </button>
          <button type="button" id="btn-ai-followup" class="btn-dock btn-ai">
            ⚡ AI Follow-up Question
          </button>
          <button type="button" id="btn-repeat-q" class="btn-dock btn-secondary">
            🔄 Repeat Question
          </button>
        </div>
      </div>
    `;

    this._setupEvents();
    this.container.appendChild(this.element);
  }

  _setupEvents() {
    const micBtn = this.element.querySelector('#btn-interviewer-mic');
    const micText = this.element.querySelector('#interviewer-mic-text');
    const micDot = this.element.querySelector('#interviewer-mic-dot');
    const preview = this.element.querySelector('#interviewer-transcription-preview');
    const textInput = this.element.querySelector('#custom-question-input');
    const sendBtn = this.element.querySelector('#btn-send-question');
    const aiFollowupBtn = this.element.querySelector('#btn-ai-followup');
    const repeatBtn = this.element.querySelector('#btn-repeat-q');

    this.speechRecognizer.onTranscript = (transcript) => {
      preview.textContent = transcript;
      textInput.value = transcript;
    };

    this.speechRecognizer.onFinalQuestion = (finalText) => {
      this.isListening = false;
      micText.textContent = 'Speak Question via Mic';
      micBtn.classList.remove('active');
      micDot.classList.remove('active');
      preview.textContent = `✓ Recognized: "${finalText}"`;
      textInput.value = finalText;
    };

    micBtn.addEventListener('click', () => {
      if (this.isListening) {
        this.speechRecognizer.stop();
        this.isListening = false;
        micText.textContent = 'Speak Question via Mic';
        micBtn.classList.remove('active');
        micDot.classList.remove('active');
      } else {
        const ok = this.speechRecognizer.start();
        if (ok) {
          this.isListening = true;
          micText.textContent = '🔴 Listening... (Speak Now)';
          micBtn.classList.add('active');
          micDot.classList.add('active');
          preview.textContent = 'Listening to your speech... Speak your question.';
        } else {
          preview.textContent = 'Microphone speech recognition unavailable. Please type your question below.';
        }
      }
    });

    // Quick suggestions chips
    this.element.querySelectorAll('.btn-chip').forEach(btn => {
      btn.addEventListener('click', () => {
        textInput.value = btn.getAttribute('data-q');
      });
    });

    sendBtn.addEventListener('click', () => {
      const q = textInput.value.trim();
      if (!q) {
        alert('Please enter or speak a question first.');
        return;
      }
      if (this.onAskQuestion) {
        this.onAskQuestion(q);
      }
      textInput.value = '';
      preview.textContent = `Sent to VR headset: "${q}"`;
    });

    aiFollowupBtn.addEventListener('click', () => {
      if (this.onGenerateFollowup) {
        this.onGenerateFollowup();
      }
    });

    repeatBtn.addEventListener('click', () => {
      if (this.onRepeatQuestion) {
        this.onRepeatQuestion();
      }
    });
  }

  setCandidateAnswer(answerText) {
    this.candidateLastAnswer = answerText;
  }

  setInterviewerSpeaking(speaking) {
    this.isInterviewerSpeaking = speaking;
    const badge = this.element.querySelector('#dock-status-badge');
    const sendBtn = this.element.querySelector('#btn-send-question');
    if (badge) {
      badge.textContent = speaking ? 'DR. VANCE SPEAKING' : 'READY FOR QUESTION';
      badge.className = `dock-status-badge ${speaking ? 'speaking' : 'ready'}`;
    }
    if (sendBtn) {
      sendBtn.disabled = speaking;
      sendBtn.style.opacity = speaking ? '0.5' : '1.0';
    }
  }
}
