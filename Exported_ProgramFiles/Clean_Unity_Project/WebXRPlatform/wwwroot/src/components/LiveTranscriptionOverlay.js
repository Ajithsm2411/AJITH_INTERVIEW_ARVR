/**
 * Live Transcription Overlay with Text Fallback (Spec Section 1 & 16)
 */
export class LiveTranscriptionOverlay {
  constructor(container, { onSubmitAnswer }) {
    this.container = container;
    this.onSubmitAnswer = onSubmitAnswer;
    this.element = null;
    this.render();
  }

  render() {
    this.element = document.createElement('div');
    this.element.className = 'transcription-overlay';
    this.element.innerHTML = `
      <div class="transcription-card">
        <div class="status-row">
          <span class="recording-badge" id="recording-indicator">🔴 LIVE MICROPHONE RECORDING</span>
          <span class="phase-title" id="phase-title">ANSWERING QUESTION #1</span>
        </div>

        <div class="interviewer-question-box">
          <span class="speaker-tag">Dr. Sarah Vance (AI Interviewer):</span>
          <p id="interviewer-speech-text" class="speech-quote">"Welcome! Let's begin with your background..."</p>
        </div>

        <div class="candidate-response-box">
          <span class="speaker-tag">Alex Mercer (Your Spoken Answer):</span>
          <p id="candidate-live-transcript" class="candidate-transcript">Listening for your speech...</p>
          
          <div class="vu-meter-row">
            <span class="vu-label">Mic VU:</span>
            <div class="vu-bar-container">
              <div class="vu-bar-fill" id="vu-fill"></div>
            </div>
            <span class="vu-status" id="vu-status">👂 LISTENING</span>
          </div>
        </div>

        <!-- Section 1 & 14 Text Input Fallback -->
        <div class="text-fallback-row">
          <input type="text" id="text-fallback-input" placeholder="Type answer here if microphone is unavailable..." autocomplete="off">
          <button type="button" class="btn-fallback" id="btn-submit-typed">Submit Typed</button>
        </div>

        <div class="action-row">
          <button type="button" class="btn-deliver" id="btn-finish-recording">⏹ FINISH & DELIVER RESPONSE [Space / Trigger]</button>
        </div>
      </div>
    `;

    this._setupEvents();
    this.container.appendChild(this.element);
  }

  _setupEvents() {
    const input = this.element.querySelector('#text-fallback-input');
    const submitBtn = this.element.querySelector('#btn-submit-typed');
    const finishBtn = this.element.querySelector('#btn-finish-recording');

    submitBtn.addEventListener('click', () => {
      const text = input.value.trim();
      if (text.length > 0) {
        if (this.onSubmitAnswer) this.onSubmitAnswer(text);
        input.value = '';
      }
    });

    input.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') submitBtn.click();
    });

    finishBtn.addEventListener('click', () => {
      if (this.onSubmitAnswer) this.onSubmitAnswer();
    });

    // Spacebar shortcut to finish answer
    window.addEventListener('keydown', (e) => {
      if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return;
      if (e.code === 'Space') {
        e.preventDefault();
        finishBtn.click();
      }
    });
  }

  setInterviewerSpeech(text) {
    const el = this.element.querySelector('#interviewer-speech-text');
    if (el) el.textContent = `"${text}"`;
  }

  setCandidateTranscript(text) {
    const el = this.element.querySelector('#candidate-live-transcript');
    if (el) el.textContent = text || 'Listening for your speech...';
  }

  setLoudness(loudness, isSpeaking) {
    const fill = this.element.querySelector('#vu-fill');
    const status = this.element.querySelector('#vu-status');
    if (fill) fill.style.width = `${Math.min(100, Math.round(loudness * 250))}%`;
    if (status) {
      status.textContent = isSpeaking ? '🗣️ SPEAKING DETECTED' : '👂 LISTENING';
      status.style.color = isSpeaking ? '#00ff88' : '#8899aa';
    }
  }

  setPhase(phaseTitle, isRecording = true) {
    const title = this.element.querySelector('#phase-title');
    const badge = this.element.querySelector('#recording-indicator');
    if (title) title.textContent = phaseTitle;
    if (badge) {
      badge.textContent = isRecording ? '🔴 LIVE MICROPHONE RECORDING' : '✓ DELIVERING ANSWER';
      badge.style.color = isRecording ? '#ff4444' : '#00e5ff';
    }
  }
}
