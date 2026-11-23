import express from 'express';
import { SpeechController } from '../controllers/speechController.js';
import { validateAudioUpload, checkAudioConsent } from '../middleware/audioValidator.js';
import { protectSessionAccess } from '../middleware/authMiddleware.js';

const router = express.Router();

router.post('/transcribe', validateAudioUpload, checkAudioConsent, SpeechController.transcribe);
router.post('/synthesize', SpeechController.synthesize);

// Spec 12 requirement: Deletion controls for stored audio
router.delete('/audio/:answerId', SpeechController.deleteStoredAudio);

export default router;
