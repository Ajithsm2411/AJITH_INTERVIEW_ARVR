import express from 'express';
import { InterviewController } from '../controllers/interviewController.js';
import { protectSessionAccess } from '../middleware/authMiddleware.js';

const router = express.Router();

// Session creation & starting
router.post('/', InterviewController.createSession);
router.post('/:sessionId/start', protectSessionAccess, InterviewController.startSession);

// Question progression & answer submission
router.post('/:sessionId/questions/next', protectSessionAccess, InterviewController.getNextQuestion);
router.post('/:sessionId/answers', protectSessionAccess, InterviewController.submitAnswer);

// Session conclusion
router.post('/:sessionId/finish', protectSessionAccess, InterviewController.finishSession);

export default router;
