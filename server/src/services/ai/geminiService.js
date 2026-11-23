const MAX_RETRIES = parseInt(process.env.MAX_API_RETRIES || '3', 10);
const TIMEOUT_MS = parseInt(process.env.REQUEST_TIMEOUT_MS || '15000', 10);

/**
 * Robust Gemini / LLM client with retry handling, exponential backoff, and timeouts (Spec 14)
 */
export class GeminiService {
  constructor() {
    this.apiKey = process.env.GEMINI_API_KEY;
    this.modelName = process.env.GEMINI_MODEL || 'gemini-1.5-pro';
  }

  async generateContent(systemPrompt, userPrompt) {
    if (!this.apiKey || this.apiKey === 'your_gemini_api_key_here') {
      console.warn('[GeminiService] No live GEMINI_API_KEY configured. Utilizing intelligent high-fidelity offline synthesis engine.');
      return null;
    }

    const url = `https://generativelanguage.googleapis.com/v1beta/models/${this.modelName}:generateContent?key=${this.apiKey}`;
    const payload = {
      contents: [
        {
          role: 'user',
          parts: [
            { text: `${systemPrompt}\n\nTask:\n${userPrompt}` }
          ]
        }
      ],
      generationConfig: {
        temperature: 0.7,
        maxOutputTokens: 1024,
        responseMimeType: 'application/json'
      }
    };

    let attempt = 0;
    while (attempt < MAX_RETRIES) {
      attempt++;
      try {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), TIMEOUT_MS);

        const response = await fetch(url, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload),
          signal: controller.signal
        });
        clearTimeout(timeoutId);

        if (!response.ok) {
          const errText = await response.text();
          throw new Error(`HTTP ${response.status}: ${errText}`);
        }

        const data = await response.json();
        const candidateText = data?.candidates?.[0]?.content?.parts?.[0]?.text;
        if (!candidateText) {
          throw new Error('Malformed response from Gemini API.');
        }

        return candidateText;
      } catch (err) {
        console.warn(`[GeminiService] Attempt ${attempt} failed: ${err.message}`);
        if (attempt >= MAX_RETRIES) {
          console.error('[GeminiService] All API retries exhausted. Failing gracefully to structured engine.');
          return null;
        }
        // Exponential backoff
        await new Promise(r => setTimeout(r, Math.pow(2, attempt) * 500));
      }
    }

    return null;
  }
}

export const geminiService = new GeminiService();
export default geminiService;
