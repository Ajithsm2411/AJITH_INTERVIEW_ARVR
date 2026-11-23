import { InterviewSession } from '../models/InterviewSession.js';

/**
 * Middleware: Protect interview sessions from unauthorized access (Spec Section 14)
 */
export async function protectSessionAccess(req, res, next) {
  try {
    const sessionId = req.params.sessionId || req.body.sessionId || req.headers['x-session-id'];

    if (!sessionId) {
      return res.status(400).json({
        success: false,
        error: 'Missing required session identifier (sessionId).'
      });
    }

    const session = await InterviewSession.findById(sessionId);
    if (!session) {
      return res.status(404).json({
        success: false,
        error: `Interview session '${sessionId}' was not found.`
      });
    }

    // Attach validated session to request object
    req.interviewSession = session;
    next();
  } catch (err) {
    console.error('[AuthMiddleware] Error validating session:', err.message);
    res.status(500).json({
      success: false,
      error: 'Internal authorization error validating session access.'
    });
  }
}

/**
 * Optional API key guard for administrative endpoints
 */
export function requireApiKey(req, res, next) {
  const apiKey = req.headers['x-api-key'] || req.query.apiKey;
  const expectedKey = process.env.ADMIN_API_KEY;

  if (expectedKey && apiKey !== expectedKey) {
    return res.status(401).json({
      success: false,
      error: 'Unauthorized. Invalid or missing API key.'
    });
  }

  next();
}
