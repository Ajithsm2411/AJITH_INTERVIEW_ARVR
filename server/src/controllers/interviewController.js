import { InterviewSession } from '../models/InterviewSession.js';
import { InterviewQuestion } from '../models/InterviewQuestion.js';
import { CandidateAnswer } from '../models/CandidateAnswer.js';
import { InterviewEvaluation } from '../models/InterviewEvaluation.js';
import { InterviewReport } from '../models/InterviewReport.js';
import { User } from '../models/User.js';
import { QuestionGenerator } from '../services/ai/questionGenerator.js';
import { AnswerEvaluator } from '../services/ai/answerEvaluator.js';
import { TTSService } from '../services/speech/ttsService.js';

export class InterviewController {
  /**
   * POST /api/interviews - Create a new session (Step 1 Configuration)
   */
  static async createSession(req, res, next) {
    try {
      const {
        jobRole,
        customRoleTitle,
        experienceLevel,
        interviewType,
        difficulty,
        durationMinutes,
        audioConsentGranted
      } = req.body;

      const user = await User.getOrCreateDefaultUser();

      const session = await InterviewSession.create({
        userId: user.id,
        jobRole: jobRole || 'Software Engineer',
        customRoleTitle,
        experienceLevel: experienceLevel || 'Junior (1-3y)',
        interviewType: interviewType || 'Technical Architecture',
        difficulty: difficulty || 'Medium',
        durationMinutes: durationMinutes || 15,
        audioConsentGranted: Boolean(audioConsentGranted)
      });

      res.status(201).json({
        success: true,
        session
      });
    } catch (err) {
      next(err);
    }
  }

  /**
   * POST /api/interviews/:sessionId/start - Start session and generate Question 1
   */
  static async startSession(req, res, next) {
    try {
      const session = req.interviewSession;

      // Generate Question 1
      const qData = await QuestionGenerator.generateQuestion({
        jobRole: session.job_role,
        customRoleTitle: session.custom_role_title,
        experienceLevel: session.experience_level,
        interviewType: session.interview_type,
        difficulty: session.difficulty,
        questionIndex: 0
      });

      const question = await InterviewQuestion.create({
        sessionId: session.id,
        questionIndex: 0,
        questionText: qData.questionText,
        category: qData.category,
        expectedPoints: qData.expectedPoints
      });

      const tts = TTSService.synthesizeSpeech(question.question_text);

      res.json({
        success: true,
        session,
        question,
        speech: tts
      });
    } catch (err) {
      next(err);
    }
  }

  /**
   * POST /api/interviews/:sessionId/questions/next - Adaptive Question Progression
   */
  static async getNextQuestion(req, res, next) {
    try {
      const session = req.interviewSession;
      const priorQuestions = await InterviewQuestion.findBySessionId(session.id);
      const nextIndex = priorQuestions.length;

      // Retrieve last answer for adaptive follow-up
      const priorAnswers = await CandidateAnswer.findBySessionId(session.id);
      const lastAnswer = priorAnswers.length > 0 ? priorAnswers[priorAnswers.length - 1].transcript_text : null;

      const qData = await QuestionGenerator.generateQuestion({
        jobRole: session.job_role,
        customRoleTitle: session.custom_role_title,
        experienceLevel: session.experience_level,
        interviewType: session.interview_type,
        difficulty: session.difficulty,
        questionIndex: nextIndex,
        priorQuestions: priorQuestions.map(q => q.question_text),
        priorAnswer: lastAnswer
      });

      const question = await InterviewQuestion.create({
        sessionId: session.id,
        questionIndex: nextIndex,
        questionText: qData.questionText,
        category: qData.category,
        expectedPoints: qData.expectedPoints
      });

      const tts = TTSService.synthesizeSpeech(question.question_text);

      res.json({
        success: true,
        question,
        speech: tts
      });
    } catch (err) {
      next(err);
    }
  }

  /**
   * POST /api/interviews/:sessionId/answers - Submit candidate answer & evaluate
   */
  static async submitAnswer(req, res, next) {
    try {
      const session = req.interviewSession;
      const {
        questionId,
        transcriptText,
        speechDurationSeconds = 0,
        isTypedFallback = false
      } = req.body;

      if (!questionId || !transcriptText) {
        return res.status(400).json({
          success: false,
          error: 'questionId and transcriptText are required.'
        });
      }

      const question = await InterviewQuestion.findById(questionId);
      if (!question) {
        return res.status(404).json({ success: false, error: 'Question not found.' });
      }

      // Record candidate answer
      const answer = await CandidateAnswer.create({
        sessionId: session.id,
        questionId: question.id,
        transcriptText,
        speechDurationSeconds,
        isTypedFallback: Boolean(isTypedFallback),
        audioStoragePath: req.savedAudioPath || null
      });

      // Perform transparent multi-criteria evaluation (Spec Section 13)
      const evalData = await AnswerEvaluator.evaluateAnswer({
        questionText: question.question_text,
        expectedPoints: question.expectedPoints || [],
        candidateTranscript: transcriptText,
        speechDurationSeconds,
        jobRole: session.job_role,
        experienceLevel: session.experience_level,
        interviewType: session.interview_type
      });

      const evaluation = await InterviewEvaluation.create({
        sessionId: session.id,
        answerId: answer.id,
        relevanceScore: evalData.relevanceScore,
        technicalAccuracyScore: evalData.technicalAccuracyScore,
        clarityScore: evalData.clarityScore,
        completenessScore: evalData.completenessScore,
        problemSolvingScore: evalData.problemSolvingScore,
        confidenceIndicators: evalData.confidenceIndicators,
        evidenceFeedback: evalData.evidenceFeedback,
        missingKeyConcepts: evalData.missingKeyConcepts,
        interviewerAckReaction: evalData.interviewerAckReaction
      });

      // Synthesize interviewer verbal reaction
      const tts = TTSService.synthesizeSpeech(evalData.interviewerAckReaction);

      res.json({
        success: true,
        answer,
        evaluation,
        interviewerReaction: tts
      });
    } catch (err) {
      next(err);
    }
  }

  /**
   * POST /api/interviews/:sessionId/finish - Conclude interview and compile final report
   */
  static async finishSession(req, res, next) {
    try {
      const session = req.interviewSession;
      await InterviewSession.updateStatus(session.id, 'COMPLETED');

      const questions = await InterviewQuestion.findBySessionId(session.id);
      const answers = await CandidateAnswer.findBySessionId(session.id);
      const evaluations = await InterviewEvaluation.findBySessionId(session.id);

      const reportData = AnswerEvaluator.compileFinalReport(session, evaluations, questions, answers);

      let report = await InterviewReport.findBySessionId(session.id);
      if (!report) {
        report = await InterviewReport.create({
          sessionId: session.id,
          overallScore: reportData.overallScore,
          relevancePercentage: reportData.relevancePercentage,
          technicalPercentage: reportData.technicalPercentage,
          clarityPercentage: reportData.clarityPercentage,
          completenessPercentage: reportData.completenessPercentage,
          problemSolvingPercentage: reportData.problemSolvingPercentage,
          strengths: reportData.strengths,
          weaknesses: reportData.weaknesses,
          actionableRecommendations: reportData.actionableRecommendations,
          suggestedPracticeTopics: reportData.suggestedPracticeTopics,
          scoreLimitationsDisclaimer: reportData.scoreLimitationsDisclaimer
        });
      }

      res.json({
        success: true,
        report,
        reportDetails: reportData
      });
    } catch (err) {
      next(err);
    }
  }
}
