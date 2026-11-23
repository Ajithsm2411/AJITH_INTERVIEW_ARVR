import { apiClient } from '../services/apiClient.js';

/**
 * Audio Consent & Recording Deletion Control Dialog (Spec Section 12 & 14)
 */
export class ConsentModal {
  constructor(container) {
    this.container = container;
    this.element = null;
    this.render();
  }

  render() {
    this.element = document.createElement('div');
    this.element.className = 'consent-modal-overlay';
    this.element.style.display = 'none';
    this.element.innerHTML = `
      <div class="modal-card">
        <div class="modal-header">
          <h2>🔒 AUDIO PRIVACY & DELETION CONTROLS</h2>
          <button type="button" class="btn-close" id="btn-close-consent">✕</button>
        </div>
        <div class="modal-body">
          <p>By default, raw microphone audio recordings are <b>never saved to disk</b>; they are processed in real time for transcription and immediately discarded.</p>
          <p>If you opted in to audio retention for playback review, you retain full rights to delete your audio data at any time.</p>
          <div class="delete-control-box">
            <label>Delete Stored Audio for Answer ID:</label>
            <div class="input-action-row">
              <input type="text" id="delete-answer-id-input" placeholder="e.g. ans_a1b2c3d4">
              <button type="button" class="btn-danger" id="btn-delete-audio">Permanently Delete Audio</button>
            </div>
            <p id="delete-status-msg" class="status-msg"></p>
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn-secondary" id="btn-dismiss-consent">Close</button>
        </div>
      </div>
    `;

    this._setupEvents();
    this.container.appendChild(this.element);
  }

  _setupEvents() {
    this.element.querySelector('#btn-close-consent').addEventListener('click', () => this.hide());
    this.element.querySelector('#btn-dismiss-consent').addEventListener('click', () => this.hide());

    const deleteBtn = this.element.querySelector('#btn-delete-audio');
    const input = this.element.querySelector('#delete-answer-id-input');
    const statusMsg = this.element.querySelector('#delete-status-msg');

    deleteBtn.addEventListener('click', async () => {
      const id = input.value.trim();
      if (!id) return;
      try {
        const res = await apiClient.deleteAudio(id);
        statusMsg.textContent = res.message || 'Audio deleted successfully.';
        statusMsg.style.color = '#00ff88';
      } catch (err) {
        statusMsg.textContent = `Deletion failed: ${err.message}`;
        statusMsg.style.color = '#ff4444';
      }
    });
  }

  show() {
    this.element.style.display = 'flex';
  }

  hide() {
    this.element.style.display = 'none';
  }
}
