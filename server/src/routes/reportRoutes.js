import express from 'express';
import { ReportController } from '../controllers/reportController.js';
import { protectSessionAccess } from '../middleware/authMiddleware.js';

const router = express.Router();

router.get('/:sessionId', protectSessionAccess, ReportController.getReport);
router.get('/:sessionId/transcript', protectSessionAccess, ReportController.getTranscript);

export default router;
