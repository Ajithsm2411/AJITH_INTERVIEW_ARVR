import { dbManager } from '../database/database.js';
import crypto from 'crypto';
import { ScoreLimitationsDisclaimer } from '../../../shared/types/index.js';

export class InterviewReport {
  static async create({
    sessionId,
    overallScore = 88,
    relevancePercentage = 90.0,
    technicalPercentage = 88.0,
    clarityPercentage = 92.0,
    completenessPercentage = 85.0,
    problemSolvingPercentage = 89.0,
    strengths = [],
    weaknesses = [],
    actionableRecommendations = [],
    suggestedPracticeTopics = [],
    scoreLimitationsDisclaimer = ScoreLimitationsDisclaimer.TEXT
  }) {
    const id = 'rep_' + crypto.randomBytes(8).toString('hex');
    const strengthsJson = JSON.stringify(strengths);
    const weaknessesJson = JSON.stringify(weaknesses);
    const recommendationsJson = JSON.stringify(actionableRecommendations);
    const practiceTopicsJson = JSON.stringify(suggestedPracticeTopics);

    const sql = `
      INSERT INTO InterviewReports (
        id, session_id, overall_score, relevance_percentage, technical_percentage,
        clarity_percentage, completeness_percentage, problem_solving_percentage,
        strengths_json, weaknesses_json, actionable_recommendations_json,
        suggested_practice_topics_json, score_limitations_disclaimer
      ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    `;

    await dbManager.run(sql, [
      id,
      sessionId,
      overallScore,
      relevancePercentage,
      technicalPercentage,
      clarityPercentage,
      completenessPercentage,
      problemSolvingPercentage,
      strengthsJson,
      weaknessesJson,
      recommendationsJson,
      practiceTopicsJson,
      scoreLimitationsDisclaimer
    ]);

    return this.findById(id);
  }

  static async findById(id) {
    const row = await dbManager.get(`SELECT * FROM InterviewReports WHERE id = ?`, [id]);
    return this._parseReportRow(row);
  }

  static async findBySessionId(sessionId) {
    const row = await dbManager.get(`SELECT * FROM InterviewReports WHERE session_id = ?`, [sessionId]);
    return this._parseReportRow(row);
  }

  static _parseReportRow(row) {
    if (!row) return null;
    try { row.strengths = JSON.parse(row.strengths_json); } catch (e) { row.strengths = []; }
    try { row.weaknesses = JSON.parse(row.weaknesses_json); } catch (e) { row.weaknesses = []; }
    try { row.actionableRecommendations = JSON.parse(row.actionable_recommendations_json); } catch (e) { row.actionableRecommendations = []; }
    try { row.suggestedPracticeTopics = JSON.parse(row.suggested_practice_topics_json); } catch (e) { row.suggestedPracticeTopics = []; }
    return row;
  }
}
