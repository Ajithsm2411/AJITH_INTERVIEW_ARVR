import { InterviewReport } from '../models/InterviewReport.js';
import { InterviewQuestion } from '../models/InterviewQuestion.js';
import { CandidateAnswer } from '../models/CandidateAnswer.js';
import { InterviewEvaluation } from '../models/InterviewEvaluation.js';
import { AnswerEvaluator } from '../services/ai/answerEvaluator.js';

export class ReportController {
  /**
   * GET /api/reports/:sessionId - Retrieve final report
   */
  static async getReport(req, res, next) {
    try {
      const session = req.interviewSession;
      let report = await InterviewReport.findBySessionId(session.id);

      const questions = await InterviewQuestion.findBySessionId(session.id);
      const answers = await CandidateAnswer.findBySessionId(session.id);
      const evaluations = await InterviewEvaluation.findBySessionId(session.id);

      const detailedReport = AnswerEvaluator.compileFinalReport(session, evaluations, questions, answers);

      res.json({
        success: true,
        session,
        report: report || detailedReport,
        details: detailedReport
      });
    } catch (err) {
      next(err);
    }
  }

  /**
   * GET /api/reports/:sessionId/transcript - Retrieve chronological Q&A transcript
   */
  static async getTranscript(req, res, next) {
    try {
      const session = req.interviewSession;
      const questions = await InterviewQuestion.findBySessionId(session.id);
      const answers = await CandidateAnswer.findBySessionId(session.id);
      const evaluations = await InterviewEvaluation.findBySessionId(session.id);

      const transcript = questions.map((q) => {
        const ans = answers.find(a => a.question_id === q.id);
        const ev = evaluations.find(e => e.answer_id === ans?.id);
        return {
          questionIndex: q.question_index + 1,
          category: q.category,
          interviewerQuestion: q.question_text,
          candidateAnswer: ans ? ans.transcript_text : null,
          speechDuration: ans ? ans.speech_duration_seconds : 0,
          isTypedFallback: ans ? Boolean(ans.is_typed_fallback) : false,
          score: ev ? ev.relevance_score : null,
          feedback: ev ? ev.evidence_feedback : null,
          recordedAt: ans ? ans.recorded_at : null
        };
      });

      res.json({
        success: true,
        sessionId: session.id,
        jobRole: session.job_role,
        experienceLevel: session.experience_level,
        transcript
      });
    } catch (err) {
      next(err);
    }
  }
}
