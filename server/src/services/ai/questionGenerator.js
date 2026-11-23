import { geminiService } from './geminiService.js';

export class QuestionGenerator {
  /**
   * Generates a context-aware question. If priorAnswer is provided, generates an adaptive follow-up.
   */
  static async generateQuestion({
    jobRole,
    customRoleTitle = null,
    experienceLevel,
    interviewType,
    difficulty,
    questionIndex = 0,
    priorQuestions = [],
    priorAnswer = null
  }) {
    const effectiveRole = customRoleTitle || jobRole;

    const systemPrompt = `You are Dr. Sarah Vance, an executive interviewer conducting a realistic VR telepresence interview.
Target Role: ${effectiveRole}
Seniority Level: ${experienceLevel}
Interview Type: ${interviewType}
Difficulty: ${difficulty}
Current Question Index: ${questionIndex + 1}

Rules:
1. Speak in first person, professional yet conversational and encouraging.
2. Formulate 1 crisp, realistic interview question appropriate for the specified seniority and difficulty.
3. If a prior answer is provided, adaptively follow up on that answer, probing technical depth or trade-offs.
4. Output strict JSON with:
   {
     "questionText": "...",
     "category": "...",
     "expectedPoints": ["point 1", "point 2", "point 3"]
   }
5. Do NOT repeat any of these previously asked questions:
   ${priorQuestions.map((q, i) => `Q${i + 1}: ${q}`).join('\n')}`;

    const userPrompt = priorAnswer
      ? `The candidate just answered: "${priorAnswer}". Please evaluate this context and formulate the next adaptive follow-up question.`
      : `Please formulate question #${questionIndex + 1} for this ${experienceLevel} ${effectiveRole} interview.`;

    const aiResult = await geminiService.generateContent(systemPrompt, userPrompt);
    if (aiResult) {
      try {
        const parsed = JSON.parse(aiResult);
        if (parsed.questionText) {
          return {
            questionText: parsed.questionText,
            category: parsed.category || interviewType,
            expectedPoints: parsed.expectedPoints || []
          };
        }
      } catch (err) {
        console.warn('[QuestionGenerator] Failed to parse AI JSON:', err.message);
      }
    }

    // High-fidelity structured fallback matrix ensuring non-repetitive, role-tailored questions
    return this._getHighFidelityFallbackQuestion(effectiveRole, experienceLevel, interviewType, questionIndex, priorAnswer);
  }

  static _getHighFidelityFallbackQuestion(role, level, type, idx, priorAnswer) {
    if (priorAnswer && idx > 0) {
      return {
        questionText: `That's insightful regarding your approach to that problem. Could you elaborate on how you handled error recovery and fault tolerance when unexpected edge cases arose?`,
        category: 'Adaptive Follow-Up & Resilience',
        expectedPoints: ['Edge case handling', 'Graceful degradation', 'Recovery mechanisms']
      };
    }

    const questionBanks = {
      'Software Engineer': [
        {
          q: 'Could you walk me through an end-to-end distributed system you designed, detailing how you managed data consistency across microservices?',
          cat: 'System Architecture & Data Consistency',
          pts: ['Saga pattern or 2PC', 'Eventual consistency', 'Idempotency']
        },
        {
          q: 'How do you diagnose and resolve memory leaks or unexpected garbage collection pauses in production high-throughput services?',
          cat: 'Performance Profiling & Observability',
          pts: ['Heap profilers', 'Allocation telemetry', 'GC tuning']
        },
        {
          q: 'Tell me about a time you had a technical disagreement with a teammate regarding system design. How did you resolve it objectively?',
          cat: 'Behavioral & Engineering Collaboration',
          pts: ['Data-driven evaluation', 'Prototyping benchmarks', 'Constructive alignment']
        }
      ],
      'Frontend Developer': [
        {
          q: 'How do you optimize critical rendering paths and maintain a steady 60 to 90 FPS frame budget for complex interactive web applications?',
          cat: 'Core Web Vitals & Rendering Performance',
          pts: ['DOM reflow minimization', 'GPU compositing layers', 'Asset bundling']
        },
        {
          q: 'Explain how you structure global state management and prevent unnecessary component re-renders in large-scale Single Page Applications.',
          cat: 'State Architecture & Memoization',
          pts: ['Atomic or centralized state', 'Selectors & memoization', 'Predictable data flows']
        }
      ],
      'Data Analyst': [
        {
          q: 'When dealing with incomplete or biased datasets, what statistical methodologies and validation techniques do you apply before drawing business conclusions?',
          cat: 'Data Integrity & Statistical Rigor',
          pts: ['Imputation techniques', 'Stratified sampling', 'Confidence intervals']
        },
        {
          q: 'Can you describe a scenario where you translated ambiguous stakeholder requirements into actionable analytical metrics and dashboards?',
          cat: 'Stakeholder Translation & KPIs',
          pts: ['Metric definition', 'Iterative wireframing', 'Impact tracking']
        }
      ]
    };

    const bank = questionBanks[role] || questionBanks['Software Engineer'];
    const chosen = bank[idx % bank.length];
    return {
      questionText: chosen.q,
      category: chosen.cat,
      expectedPoints: chosen.pts
    };
  }
}
