/**
 * VR Telepresence AI Interview Platform — REST API Client
 */
export class ApiClient {
  constructor(baseUrl = '') {
    this.baseUrl = baseUrl || (window.location.origin.includes(':3000') ? 'http://localhost:5000' : '');
    this.currentSessionId = null;
  }

  async request(endpoint, options = {}) {
    const url = `${this.baseUrl}${endpoint}`;
    const headers = {
      'Content-Type': 'application/json',
      ...(this.currentSessionId ? { 'x-session-id': this.currentSessionId } : {}),
      ...(options.headers || {})
    };

    let attempt = 0;
    const maxRetries = 3;

    while (attempt < maxRetries) {
      attempt++;
      try {
        const controller = new AbortController();
        const timeout = setTimeout(() => controller.abort(), 15000);

        const res = await fetch(url, {
          ...options,
          headers,
          signal: controller.signal
        });
        clearTimeout(timeout);

        if (!res.ok) {
          const errData = await res.json().catch(() => ({}));
          throw new Error(errData.error || `HTTP ${res.status}: ${res.statusText}`);
        }

        return await res.json();
      } catch (err) {
        console.warn(`[ApiClient] Request to ${endpoint} failed (attempt ${attempt}): ${err.message}`);
        if (attempt >= maxRetries) {
          throw err;
        }
        await new Promise(r => setTimeout(r, Math.pow(2, attempt) * 400));
      }
    }
  }

  async createSession(config) {
    const data = await this.request('/api/interviews', {
      method: 'POST',
      body: JSON.stringify(config)
    });
    if (data.session) {
      this.currentSessionId = data.session.id;
    }
    return data;
  }

  async startSession(sessionId = this.currentSessionId) {
    return this.request(`/api/interviews/${sessionId}/start`, { method: 'POST' });
  }

  async getNextQuestion(sessionId = this.currentSessionId) {
    return this.request(`/api/interviews/${sessionId}/questions/next`, { method: 'POST' });
  }

  async submitAnswer({ questionId, transcriptText, speechDurationSeconds, isTypedFallback }) {
    return this.request(`/api/interviews/${this.currentSessionId}/answers`, {
      method: 'POST',
      body: JSON.stringify({
        questionId,
        transcriptText,
        speechDurationSeconds,
        isTypedFallback
      })
    });
  }

  async finishSession(sessionId = this.currentSessionId) {
    return this.request(`/api/interviews/${sessionId}/finish`, { method: 'POST' });
  }

  async getReport(sessionId = this.currentSessionId) {
    return this.request(`/api/reports/${sessionId}`);
  }

  async getTranscript(sessionId = this.currentSessionId) {
    return this.request(`/api/reports/${sessionId}/transcript`);
  }

  async deleteAudio(answerId) {
    return this.request(`/api/speech/audio/${answerId}`, { method: 'DELETE' });
  }
}

export const apiClient = new ApiClient();
export default apiClient;
