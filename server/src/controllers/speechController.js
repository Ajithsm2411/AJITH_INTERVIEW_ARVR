import { STTService } from '../services/speech/sttService.js';
import { TTSService } from '../services/speech/ttsService.js';
import { CandidateAnswer } from '../models/CandidateAnswer.js';
import path from 'path';
import fs from 'fs';
import crypto from 'crypto';

export class SpeechController {
  /**
   * POST /api/speech/transcribe - Transcribe uploaded audio or text fallback
   */
  static async transcribe(req, res, next) {
    try {
      const { textFallback, mimeType } = req.body;
      const audioBuffer = req.file ? req.file.buffer : (req.body.audioBase64 ? Buffer.from(req.body.audioBase64, 'base64') : null);

      let savedAudioPath = null;
      // Spec 12 & 14: Only retain audio on disk if candidate gave explicit consent
      if (req.shouldRetainAudioFile && audioBuffer) {
        const audioDir = path.join(process.cwd(), 'data/recordings');
        if (!fs.existsSync(audioDir)) fs.mkdirSync(audioDir, { recursive: true });
        const filename = `rec_${crypto.randomBytes(8).toString('hex')}.webm`;
        savedAudioPath = path.join(audioDir, filename);
        fs.writeFileSync(savedAudioPath, audioBuffer);
      }

      const result = await STTService.transcribeAudio({
        audioBuffer,
        mimeType: mimeType || 'audio/webm',
        textFallback
      });

      req.savedAudioPath = savedAudioPath;

      res.json({
        success: true,
        transcript: result.transcript,
        durationSeconds: result.durationSeconds,
        isTypedFallback: result.isTypedFallback,
        confidence: result.confidence,
        audioRetained: Boolean(savedAudioPath)
      });
    } catch (err) {
      next(err);
    }
  }

  /**
   * POST /api/speech/synthesize - Generate speech visemes and audio metadata
   */
  static async synthesize(req, res, next) {
    try {
      const { text, voice } = req.body;
      if (!text) {
        return res.status(400).json({ success: false, error: 'text is required.' });
      }

      const speech = TTSService.synthesizeSpeech(text, voice);
      res.json({
        success: true,
        speech
      });
    } catch (err) {
      next(err);
    }
  }

  /**
   * DELETE /api/speech/audio/:answerId - Explicit deletion control (Spec Section 12)
   */
  static async deleteStoredAudio(req, res, next) {
    try {
      const { answerId } = req.params;
      const deleted = await CandidateAnswer.deleteAudioRecording(answerId);
      if (!deleted) {
        return res.status(404).json({ success: false, error: 'Answer record not found or no audio attached.' });
      }

      res.json({
        success: true,
        message: `Audio recording for answer '${answerId}' was permanently deleted.`
      });
    } catch (err) {
      next(err);
    }
  }
}
