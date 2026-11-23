/**
 * Live Transcription Overlay with Push-To-Talk, Auto-Listening (VAD),
 * Re-record controls, and Headset Device Selection (Prompt Requirements 2, 3 & 4)
 */
export class LiveTranscriptionOverlay {
  constructor(container, { onSubmitAnswer, onRepeatAnswer, onModeToggle, onDeviceSelect }) {
    this.container = container;
    this.onSubmitAnswer = onSubmitAnswer;
    this.onRepeatAnswer = onRepeatAnswer;
    this.onModeToggle = onModeToggle;
    this.onDeviceSelect = onDeviceSelect;

    this.element = null;
    this.listeningMode = 'PTT';
    this.render();
  }

  render() {
    this.element = document.createElement('div');
    this.element.className = 'transcription-overlay';
    this.element.innerHTML = `
      <div class="transcription-card">
        <div class="status-row">
          <div class="status-left">
            <span class="recording-badge" id="recording-indicator">🔴 LIVE MICROPHONE READY</span>
            <span class="phase-title" id="phase-title">QUESTION #1</span>
          </div>
          <div class="status-right">
            <!-- Mode Toggle Pill -->
            <button type="button" id="btn-mode-toggle" class="btn-mode-pill">🎧 Mode: PTT</button>
            <!-- Headset Device Selector -->
            <select id="select-audio-device" class="device-select" title="Select VR Headset Microphone">
              <option value="">🎙️ Default VR / System Mic</option>
            </select>
          </div>
        </div>

        <!-- Interviewer Question Display -->
        <div class="interviewer-question-box" id="interviewer-box">
          <div class="speaker-header">
            <span class="speaker-tag">👩‍💼 Dr. Sarah Vance (Interviewer):</span>
            <span class="speaking-wave-indicator" id="interviewer-wave" style="display:none;">🔊 Speaking...</span>
          </div>
          <p id="interviewer-speech-text" class="speech-quote">"Welcome! Let's begin with your background..."</p>
        </div>

        <!-- Candidate Live Response & VU Display -->
        <div class="candidate-response-box" id="candidate-box">
          <div class="speaker-header">
            <span class="speaker-tag">🎙️ Alex Mercer (Your VR Headset Spoken Answer):</span>
            <span class="mic-mode-tag" id="mic-mode-tag">HOLD TRIGGER OR SPACE TO TALK</span>
          </div>
          <p id="candidate-live-transcript" class="candidate-transcript">Listening for your speech...</p>
          
          <div class="vu-meter-row">
            <span class="vu-label">Mic VU:</span>
            <div class="vu-bar-container">
              <div class="vu-bar-fill" id="vu-fill"></div>
            </div>
            <span class="vu-status" id="vu-status">👂 READY</span>
          </div>
        </div>

        <!-- Text Input Fallback -->
        <div class="text-fallback-row">
          <input type="text" id="text-fallback-input" placeholder="Type answer here if microphone is unavailable..." autocomplete="off">
          <button type="button" class="btn-fallback" id="btn-submit-typed">Submit Typed</button>
        </div>

        <!-- Control Action Row (Prompt Req 2 & 4: Submit, Repeat, PTT) -->
        <div class="action-row">
          <button type="button" class="btn-ptt-toggle" id="btn-ptt-action">🎙️ PUSH TO TALK [Hold Space / Trigger]</button>
          <button type="button" class="btn-repeat-answer" id="btn-re-record">🔁 RE-RECORD ANSWER</button>
          <button type="button" class="btn-deliver" id="btn-finish-recording">✅ SUBMIT TO DR. VANCE</button>
        </div>
      </div>
    `;

    this._setupEvents();
    this._populateAudioDevices();
    this.container.appendChild(this.element);
  }

  async _populateAudioDevices() {
    try {
      const devices = await navigator.mediaDevices.enumerateDevices();
      const audioInputs = devices.filter(d => d.kind === 'audioinput');
      const select = this.element.querySelector('#select-audio-device');
      if (select && audioInputs.length > 0) {
        select.innerHTML = '<option value="">🎙️ Default VR / System Mic</option>';
        audioInputs.forEach((d, idx) => {
          const opt = document.createElement('option');
          opt.value = d.deviceId;
          opt.textContent = `🎙️ ${d.label || `Microphone ${idx + 1}`}`;
          select.appendChild(opt);
        });
      }
    } catch (e) {
      console.warn('[LiveTranscriptionOverlay] Could not enumerate audio devices:', e);
    }
  }

  _setupEvents() {
    const input = this.element.querySelector('#text-fallback-input');
    const submitBtn = this.element.querySelector('#btn-submit-typed');
    const finishBtn = this.element.querySelector('#btn-finish-recording');
    const reRecordBtn = this.element.querySelector('#btn-re-record');
    const pttBtn = this.element.querySelector('#btn-ptt-action');
    const modeBtn = this.element.querySelector('#btn-mode-toggle');
    const deviceSelect = this.element.querySelector('#select-audio-device');

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

    reRecordBtn.addEventListener('click', () => {
      if (this.onRepeatAnswer) this.onRepeatAnswer();
    });

    pttBtn.addEventListener('click', () => {
      if (this.onSubmitAnswer) this.onSubmitAnswer();
    });

    modeBtn.addEventListener('click', () => {
      this.listeningMode = this.listeningMode === 'PTT' ? 'AUTO' : 'PTT';
      modeBtn.textContent = `🎧 Mode: ${this.listeningMode}`;
      const modeTag = this.element.querySelector('#mic-mode-tag');
      if (modeTag) {
        modeTag.textContent = this.listeningMode === 'PTT' ? 'PUSH-TO-TALK ACTIVE' : 'AUTOMATIC VAD LISTENING';
      }
      if (this.onModeToggle) this.onModeToggle(this.listeningMode);
    });

    deviceSelect.addEventListener('change', (e) => {
      if (this.onDeviceSelect) this.onDeviceSelect(e.target.value);
    });

    // Spacebar shortcut to finish or submit
    window.addEventListener('keydown', (e) => {
      if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return;
      if (e.code === 'Space') {
        e.preventDefault();
        finishBtn.click();
      }
    });
  }

  setInterviewerSpeech(text, isSpeaking = false) {
    const el = this.element.querySelector('#interviewer-speech-text');
    const wave = this.element.querySelector('#interviewer-wave');
    const box = this.element.querySelector('#interviewer-box');
    if (el) el.textContent = `"${text}"`;
    if (wave) wave.style.display = isSpeaking ? 'inline-block' : 'none';
    if (box) box.classList.toggle('speaking-active', isSpeaking);
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
      badge.textContent = isRecording ? '🔴 LIVE MICROPHONE RECORDING' : '✓ ANSWER RECORDED / REVIEW';
      badge.style.color = isRecording ? '#ff4444' : '#00e5ff';
    }
  }
}

