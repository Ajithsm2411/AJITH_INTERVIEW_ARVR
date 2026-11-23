/**
 * Step 1 Configuration Modal Component
 */
export class InterviewSetupModal {
  constructor(container, onStart) {
    this.container = container;
    this.onStart = onStart;
    this.config = {
      jobRole: 'Software Engineer',
      customRoleTitle: '',
      experienceLevel: 'Junior (1-3y)',
      interviewType: 'Technical Architecture',
      difficulty: 'Medium',
      durationMinutes: 15,
      audioConsentGranted: false
    };
    this.element = null;
    this.render();
  }

  render() {
    this.element = document.createElement('div');
    this.element.className = 'setup-modal-overlay';
    this.element.innerHTML = `
      <div class="modal-card">
        <div class="modal-header">
          <h2>🎯 STEP 1: CONFIGURE AI INTERVIEW SESSION</h2>
          <p class="subtitle">Customize target job role, experience level, interview focus, difficulty, and duration.</p>
        </div>
        
        <div class="form-group">
          <label>1. Target Job Role:</label>
          <div class="button-toggle-group" id="role-group">
            <button type="button" class="btn-toggle active" data-val="Software Engineer">Software Engineer</button>
            <button type="button" class="btn-toggle" data-val="Frontend Developer">Frontend Developer</button>
            <button type="button" class="btn-toggle" data-val="Backend Developer">Backend Developer</button>
            <button type="button" class="btn-toggle" data-val="Data Analyst">Data Analyst</button>
            <button type="button" class="btn-toggle" data-val="Human Resources">HR</button>
          </div>
        </div>

        <div class="form-group">
          <label>2. Experience Level:</label>
          <div class="button-toggle-group" id="level-group">
            <button type="button" class="btn-toggle" data-val="Fresher (0-1y)">Fresher (0-1y)</button>
            <button type="button" class="btn-toggle active" data-val="Junior (1-3y)">Junior (1-3y)</button>
            <button type="button" class="btn-toggle" data-val="Mid-Level (3-6y)">Mid-Level (3-6y)</button>
            <button type="button" class="btn-toggle" data-val="Senior (6+y)">Senior (6+y)</button>
          </div>
        </div>

        <div class="form-group">
          <label>3. Interview Focus:</label>
          <div class="button-toggle-group" id="type-group">
            <button type="button" class="btn-toggle active" data-val="Technical Architecture">Technical Architecture</button>
            <button type="button" class="btn-toggle" data-val="Behavioral (STAR)">Behavioral (STAR)</button>
            <button type="button" class="btn-toggle" data-val="HR / Culture">HR / Culture</button>
            <button type="button" class="btn-toggle" data-val="Mixed Competency">Mixed Competency</button>
          </div>
        </div>

        <div class="form-group">
          <label>4. Difficulty:</label>
          <div class="button-toggle-group" id="diff-group">
            <button type="button" class="btn-toggle" data-val="Easy">🟢 Easy</button>
            <button type="button" class="btn-toggle active" data-val="Medium">🟡 Medium</button>
            <button type="button" class="btn-toggle" data-val="Hard">🔴 Hard</button>
          </div>
        </div>

        <div class="form-group">
          <label>5. Session Duration:</label>
          <div class="button-toggle-group" id="duration-group">
            <button type="button" class="btn-toggle" data-val="5">⏱️ 5 Mins</button>
            <button type="button" class="btn-toggle" data-val="10">⏱️ 10 Mins</button>
            <button type="button" class="btn-toggle active" data-val="15">⏱️ 15 Mins</button>
            <button type="button" class="btn-toggle" data-val="30">⏱️ 30 Mins</button>
          </div>
        </div>

        <div class="consent-checkbox-row">
          <input type="checkbox" id="consent-check">
          <label for="consent-check">Enable optional audio recording retention with explicit deletion controls (default: off, uncheck to discard audio immediately after transcription).</label>
        </div>

        <div class="modal-footer">
          <button type="button" class="btn-primary" id="btn-start-session">🚀 START VR INTERVIEW [STEP 2]</button>
        </div>
      </div>
    `;

    this._setupEvents();
    this.container.appendChild(this.element);
  }

  _setupEvents() {
    const bindToggle = (groupId, field, isInt = false) => {
      const group = this.element.querySelector(groupId);
      group.querySelectorAll('.btn-toggle').forEach(btn => {
        btn.addEventListener('click', () => {
          group.querySelectorAll('.btn-toggle').forEach(b => b.classList.remove('active'));
          btn.classList.add('active');
          this.config[field] = isInt ? parseInt(btn.dataset.val, 10) : btn.dataset.val;
        });
      });
    };

    bindToggle('#role-group', 'jobRole');
    bindToggle('#level-group', 'experienceLevel');
    bindToggle('#type-group', 'interviewType');
    bindToggle('#diff-group', 'difficulty');
    bindToggle('#duration-group', 'durationMinutes', true);

    const consentCheck = this.element.querySelector('#consent-check');
    consentCheck.addEventListener('change', (e) => {
      this.config.audioConsentGranted = e.target.checked;
    });

    this.element.querySelector('#btn-start-session').addEventListener('click', () => {
      this.element.style.display = 'none';
      if (this.onStart) this.onStart(this.config);
    });
  }

  show() {
    this.element.style.display = 'flex';
  }

  hide() {
    this.element.style.display = 'none';
  }
}
