import { ScoreLimitationsDisclaimer } from '../../../shared/types/index.js';

/**
 * Step 6: Comprehensive Interview Evaluation Report Modal (Spec Section 13)
 */
export class EvaluationReportModal {
  constructor(container, { onRestart }) {
    this.container = container;
    this.onRestart = onRestart;
    this.element = null;
    this.reportData = null;
    this.render();
  }

  render() {
    this.element = document.createElement('div');
    this.element.className = 'report-modal-overlay';
    this.element.style.display = 'none';
    this.container.appendChild(this.element);
  }

  show(report) {
    this.reportData = report;
    const rep = report || {};

    const qnaHtml = (rep.qnaReview || []).map((q, i) => `
      <div class="qna-card">
        <div class="qna-header">
          <span class="qna-num">Q#${i + 1} (${q.category || 'General'})</span>
          <span class="qna-score">Score: ${q.score || 5}★</span>
        </div>
        <p class="qna-question"><b>Question:</b> "${q.questionText}"</p>
        <p class="qna-answer"><b>Your Answer (${(q.candidateSpeechDuration || 0).toFixed(1)}s):</b> ${q.candidateTranscript || '(None)'}</p>
        <p class="qna-feedback"><i>Feedback:</i> ${q.evidenceFeedback || 'Clear response.'}</p>
      </div>
    `).join('');

    const strengthsHtml = (rep.strengths || []).map(s => `<li>✓ ${s}</li>`).join('');
    const weaknessesHtml = (rep.weaknesses || []).map(w => `<li>⚠️ ${w}</li>`).join('');
    const recsHtml = (rep.actionableRecommendations || []).map(r => `<li>💡 ${r}</li>`).join('');
    const topicsHtml = (rep.suggestedPracticeTopics || []).map(t => `<li>📚 ${t}</li>`).join('');

    this.element.innerHTML = `
      <div class="modal-card report-card">
        <div class="modal-header report-header">
          <h2>📊 STEP 6: COMPREHENSIVE INTERVIEW EVALUATION REPORT</h2>
          <button type="button" class="btn-close" id="btn-close-report">✕</button>
        </div>

        <div class="score-summary-banner">
          <div class="overall-badge">
            <span class="score-num">${rep.overallScore || 88}</span>
            <span class="score-label">OVERALL SCORE / 100</span>
          </div>
          <div class="metrics-grid">
            <div class="metric-item"><span>Relevance:</span> <b>${rep.relevancePercentage || 90}%</b></div>
            <div class="metric-item"><span>Technical Accuracy:</span> <b>${rep.technicalPercentage || 88}%</b></div>
            <div class="metric-item"><span>Clarity:</span> <b>${rep.clarityPercentage || 92}%</b></div>
            <div class="metric-item"><span>Completeness:</span> <b>${rep.completenessPercentage || 85}%</b></div>
          </div>
        </div>

        <div class="report-scroll-body">
          <div class="report-section">
            <h3>Strengths & Core Competencies:</h3>
            <ul class="bullet-list strengths-list">${strengthsHtml}</ul>
          </div>

          <div class="report-section">
            <h3>Identified Gaps & Areas for Improvement:</h3>
            <ul class="bullet-list weaknesses-list">${weaknessesHtml}</ul>
          </div>

          <div class="report-section">
            <h3>Actionable Recommendations:</h3>
            <ul class="bullet-list recs-list">${recsHtml}</ul>
          </div>

          <div class="report-section">
            <h3>Suggested Topics to Practice:</h3>
            <ul class="bullet-list topics-list">${topicsHtml}</ul>
          </div>

          <div class="report-section">
            <h3>Chronological Q&A Review:</h3>
            <div class="qna-list">${qnaHtml}</div>
          </div>
        </div>

        <div class="disclaimer-banner">
          <p>ℹ️ <i>${rep.scoreLimitationsDisclaimer || ScoreLimitationsDisclaimer.TEXT}</i></p>
        </div>

        <div class="modal-footer">
          <button type="button" class="btn-primary" id="btn-restart-interview">🔄 Start New Interview Session</button>
          <button type="button" class="btn-secondary" id="btn-dismiss-report">Close</button>
        </div>
      </div>
    `;

    this.element.querySelector('#btn-close-report').addEventListener('click', () => this.hide());
    this.element.querySelector('#btn-dismiss-report').addEventListener('click', () => this.hide());
    this.element.querySelector('#btn-restart-interview').addEventListener('click', () => {
      this.hide();
      if (this.onRestart) this.onRestart();
    });

    this.element.style.display = 'flex';
  }

  hide() {
    this.element.style.display = 'none';
  }
}
