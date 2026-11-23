import { dbManager } from '../database/database.js';
import crypto from 'crypto';
import fs from 'fs';

export class CandidateAnswer {
  static async create({
    sessionId,
    questionId,
    transcriptText,
    speechDurationSeconds = 0,
    isTypedFallback = false,
    audioStoragePath = null
  }) {
    const id = 'ans_' + crypto.randomBytes(8).toString('hex');
    const sql = `
      INSERT INTO CandidateAnswers (
        id, session_id, question_id, transcript_text,
        speech_duration_seconds, is_typed_fallback, audio_storage_path
      ) VALUES (?, ?, ?, ?, ?, ?, ?)
    `;
    await dbManager.run(sql, [
      id,
      sessionId,
      questionId,
      transcriptText,
      speechDurationSeconds,
      isTypedFallback ? 1 : 0,
      audioStoragePath
    ]);
    return this.findById(id);
  }

  static async findById(id) {
    return dbManager.get(`SELECT * FROM CandidateAnswers WHERE id = ?`, [id]);
  }

  static async findBySessionId(sessionId) {
    return dbManager.all(
      `SELECT * FROM CandidateAnswers WHERE session_id = ? ORDER BY recorded_at ASC`,
      [sessionId]
    );
  }

  static async findByQuestionId(questionId) {
    return dbManager.get(
      `SELECT * FROM CandidateAnswers WHERE question_id = ? ORDER BY recorded_at DESC LIMIT 1`,
      [questionId]
    );
  }

  /**
   * Spec 12 & 14 requirement: Explicit deletion controls for recorded audio.
   */
  static async deleteAudioRecording(id) {
    const answer = await this.findById(id);
    if (!answer) return false;

    if (answer.audio_storage_path && fs.existsSync(answer.audio_storage_path)) {
      try {
        fs.unlinkSync(answer.audio_storage_path);
        console.log(`[CandidateAnswer] Deleted audio recording file: ${answer.audio_storage_path}`);
      } catch (err) {
        console.error(`[CandidateAnswer] Error deleting audio file: ${err.message}`);
      }
    }

    await dbManager.run(
      `UPDATE CandidateAnswers SET audio_storage_path = NULL WHERE id = ?`,
      [id]
    );
    return true;
  }
}
