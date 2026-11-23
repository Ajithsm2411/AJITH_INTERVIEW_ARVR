import { dbManager } from '../database/database.js';
import crypto from 'crypto';

export class InterviewEvaluation {
  static async create({
    sessionId,
    answerId,
    relevanceScore = 5,
    technicalAccuracyScore = 5,
    clarityScore = 5,
    completenessScore = 5,
    problemSolvingScore = 5,
    confidenceIndicators = '',
    evidenceFeedback = '',
    missingKeyConcepts = [],
    interviewerAckReaction = ''
  }) {
    const id = 'eval_' + crypto.randomBytes(8).toString('hex');
    const missingConceptsJson = Array.isArray(missingKeyConcepts)
      ? JSON.stringify(missingKeyConcepts)
      : (missingKeyConcepts || '[]');

    const sql = `
      INSERT INTO InterviewEvaluations (
        id, session_id, answer_id, relevance_score, technical_accuracy_score,
        clarity_score, completeness_score, problem_solving_score, confidence_indicators,
        evidence_feedback, missing_key_concepts, interviewer_ack_reaction
      ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    `;

    await dbManager.run(sql, [
      id,
      sessionId,
      answerId,
      relevanceScore,
      technicalAccuracyScore,
      clarityScore,
      completenessScore,
      problemSolvingScore,
      confidenceIndicators,
      evidenceFeedback,
      missingConceptsJson,
      interviewerAckReaction
    ]);

    return this.findById(id);
  }

  static async findById(id) {
    const row = await dbManager.get(`SELECT * FROM InterviewEvaluations WHERE id = ?`, [id]);
    if (row && row.missing_key_concepts) {
      try { row.missingKeyConcepts = JSON.parse(row.missing_key_concepts); } catch (e) { row.missingKeyConcepts = []; }
    }
    return row;
  }

  static async findBySessionId(sessionId) {
    const rows = await dbManager.all(
      `SELECT * FROM InterviewEvaluations WHERE session_id = ? ORDER BY created_at ASC`,
      [sessionId]
    );
    return rows.map(r => {
      try { r.missingKeyConcepts = JSON.parse(r.missing_key_concepts); } catch (e) { r.missingKeyConcepts = []; }
      return r;
    });
  }
}
