import { spatialAudio } from '../audio/SpatialAudioEngine.js';

/**
 * VR Controls Bar Component (Spec Section 14 & 16)
 */
export class VRControlsBar {
  constructor(container, { onPause, onRepeat, onEnd, onEnterVR }) {
    this.container = container;
    this.onPause = onPause;
    this.onRepeat = onRepeat;
    this.onEnd = onEnd;
    this.onEnterVR = onEnterVR;
    this.element = null;
    this.isPaused = false;
    this.isMuted = false;
    this.render();
  }

  render() {
    this.element = document.createElement('div');
    this.element.className = 'vr-controls-bar';
    this.element.innerHTML = `
      <div class="controls-left">
        <button id="btn-pause" class="btn-ctrl">⏸️ Pause [P]</button>
        <button id="btn-repeat" class="btn-ctrl">🔄 Repeat [R]</button>
        <button id="btn-end" class="btn-ctrl btn-danger">⏹️ End [Esc]</button>
      </div>

      <div class="controls-center">
        <button id="btn-mic" class="btn-ctrl btn-success">🎙️ Mic ON [M]</button>
        <div class="vol-control">
          <span>🔊</span>
          <input type="range" id="vol-slider" min="0" max="1" step="0.05" value="1.0">
        </div>
      </div>

      <div class="controls-right">
        <span id="session-timer" class="timer-badge">⏱️ 00:00 / 15:00</span>
        <button id="btn-vr-mode" class="btn-ctrl btn-vr">🥽 ENTER VR</button>
      </div>
    `;

    this._setupEvents();
    this.container.appendChild(this.element);
  }

  _setupEvents() {
    const pauseBtn = this.element.querySelector('#btn-pause');
    pauseBtn.addEventListener('click', () => {
      this.isPaused = !this.isPaused;
      pauseBtn.textContent = this.isPaused ? '▶️ Resume [P]' : '⏸️ Pause [P]';
      pauseBtn.classList.toggle('active', this.isPaused);
      if (this.onPause) this.onPause(this.isPaused);
    });

    this.element.querySelector('#btn-repeat').addEventListener('click', () => {
      if (this.onRepeat) this.onRepeat();
    });

    this.element.querySelector('#btn-end').addEventListener('click', () => {
      if (this.onEnd) this.onEnd();
    });

    const micBtn = this.element.querySelector('#btn-mic');
    micBtn.addEventListener('click', () => {
      this.isMuted = spatialAudio.toggleMute();
      micBtn.textContent = this.isMuted ? '🔇 Mic OFF [M]' : '🎙️ Mic ON [M]';
      micBtn.classList.toggle('btn-danger', this.isMuted);
      micBtn.classList.toggle('btn-success', !this.isMuted);
    });

    const volSlider = this.element.querySelector('#vol-slider');
    volSlider.addEventListener('input', (e) => {
      spatialAudio.setVolume(parseFloat(e.target.value));
    });

    this.element.querySelector('#btn-vr-mode').addEventListener('click', () => {
      if (this.onEnterVR) this.onEnterVR();
    });

    // Keyboard Shortcuts
    window.addEventListener('keydown', (e) => {
      if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return;
      if (e.key === 'p' || e.key === 'P') pauseBtn.click();
      if (e.key === 'r' || e.key === 'R') this.element.querySelector('#btn-repeat').click();
      if (e.key === 'm' || e.key === 'M') micBtn.click();
      if (e.key === 'Escape') this.element.querySelector('#btn-end').click();
    });
  }

  updateTimer(elapsedSeconds, totalSeconds) {
    const elMin = Math.floor(elapsedSeconds / 60);
    const elSec = Math.floor(elapsedSeconds % 60);
    const totMin = Math.floor(totalSeconds / 60);
    const badge = this.element.querySelector('#session-timer');
    if (badge) {
      badge.textContent = `⏱️ ${String(elMin).padStart(2, '0')}:${String(elSec).padStart(2, '0')} / ${String(totMin).padStart(2, '0')}:00`;
    }
  }
}
