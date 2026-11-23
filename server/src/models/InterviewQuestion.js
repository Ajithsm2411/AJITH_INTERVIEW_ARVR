import { dbManager } from '../database/database.js';
import crypto from 'crypto';

export class InterviewQuestion {
  static async create({ sessionId, questionIndex, questionText, category, expectedPoints = [] }) {
    const id = 'q_' + crypto.randomBytes(8).toString('hex');
    const expectedPointsJson = Array.isArray(expectedPoints) ? JSON.stringify(expectedPoints) : (expectedPoints || '[]');
    const sql = `
      INSERT INTO InterviewQuestions (id, session_id, question_index, question_text, category, expected_points)
      VALUES (?, ?, ?, ?, ?, ?)
    `;
    await dbManager.run(sql, [id, sessionId, questionIndex, questionText, category, expectedPointsJson]);
    return this.findById(id);
  }

  static async findById(id) {
    const row = await dbManager.get(`SELECT * FROM InterviewQuestions WHERE id = ?`, [id]);
    if (row && row.expected_points) {
      try { row.expectedPoints = JSON.parse(row.expected_points); } catch (e) { row.expectedPoints = []; }
    }
    return row;
  }

  static async findBySessionId(sessionId) {
    const rows = await dbManager.all(
      `SELECT * FROM InterviewQuestions WHERE session_id = ? ORDER BY question_index ASC`,
      [sessionId]
    );
    return rows.map(r => {
      try { r.expectedPoints = JSON.parse(r.expected_points); } catch (e) { r.expectedPoints = []; }
      return r;
    });
  }

  static async getLatestQuestion(sessionId) {
    const row = await dbManager.get(
      `SELECT * FROM InterviewQuestions WHERE session_id = ? ORDER BY question_index DESC LIMIT 1`,
      [sessionId]
    );
    if (row && row.expected_points) {
      try { row.expectedPoints = JSON.parse(row.expected_points); } catch (e) { row.expectedPoints = []; }
    }
    return row;
  }
}
