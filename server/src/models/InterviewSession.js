import { dbManager } from '../database/database.js';
import crypto from 'crypto';

export class InterviewSession {
  static async create({
    userId,
    jobRole,
    customRoleTitle = null,
    experienceLevel,
    interviewType,
    difficulty,
    durationMinutes = 15,
    audioConsentGranted = false
  }) {
    const id = 'sess_' + crypto.randomBytes(8).toString('hex');
    const sql = `
      INSERT INTO InterviewSessions (
        id, user_id, job_role, custom_role_title, experience_level,
        interview_type, difficulty, duration_minutes, status, audio_consent_granted, started_at
      ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, 'ACTIVE', ?, CURRENT_TIMESTAMP)
    `;
    await dbManager.run(sql, [
      id,
      userId,
      jobRole,
      customRoleTitle,
      experienceLevel,
      interviewType,
      difficulty,
      durationMinutes,
      audioConsentGranted ? 1 : 0
    ]);
    return this.findById(id);
  }

  static async findById(id) {
    return dbManager.get(`SELECT * FROM InterviewSessions WHERE id = ?`, [id]);
  }

  static async findByUserId(userId) {
    return dbManager.all(
      `SELECT * FROM InterviewSessions WHERE user_id = ? ORDER BY created_at DESC`,
      [userId]
    );
  }

  static async updateStatus(id, status) {
    const endedAtSql = (status === 'COMPLETED' || status === 'CANCELLED') ? ', ended_at = CURRENT_TIMESTAMP' : '';
    const sql = `UPDATE InterviewSessions SET status = ? ${endedAtSql} WHERE id = ?`;
    await dbManager.run(sql, [status, id]);
    return this.findById(id);
  }

  static async updateAudioConsent(id, consentGranted) {
    const sql = `UPDATE InterviewSessions SET audio_consent_granted = ? WHERE id = ?`;
    await dbManager.run(sql, [consentGranted ? 1 : 0, id]);
    return this.findById(id);
  }
}
