const MAX_AUDIO_BYTES = parseInt(process.env.MAX_AUDIO_UPLOAD_SIZE_BYTES || '26214400', 10); // 25 MB
const ALLOWED_MIME_TYPES = (process.env.ALLOWED_AUDIO_MIME_TYPES || 'audio/webm,audio/wav,audio/ogg,audio/mpeg')
  .split(',')
  .map(m => m.trim().toLowerCase());

/**
 * Middleware: Validate and limit audio uploads (Spec Section 14)
 */
export function validateAudioUpload(req, res, next) {
  const contentType = (req.headers['content-type'] || '').toLowerCase();
  const contentLength = parseInt(req.headers['content-length'] || '0', 10);

  // Check upload size limit
  if (contentLength > MAX_AUDIO_BYTES) {
    return res.status(413).json({
      success: false,
      error: `Audio upload exceeds maximum permitted payload limit of ${MAX_AUDIO_BYTES / (1024 * 1024)} MB.`
    });
  }

  // If file upload is multipart or binary audio
  if (contentType.includes('audio/') || contentType.includes('multipart/form-data')) {
    // If direct audio stream
    if (contentType.startsWith('audio/')) {
      const mime = contentType.split(';')[0].trim();
      if (!ALLOWED_MIME_TYPES.includes(mime)) {
        return res.status(415).json({
          success: false,
          error: `Unsupported audio format '${mime}'. Allowed formats: ${ALLOWED_MIME_TYPES.join(', ')}.`
        });
      }
    }
  }

  next();
}

/**
 * Enforce audio retention consent before storing files on disk
 */
export function checkAudioConsent(req, res, next) {
  const session = req.interviewSession;
  const allowRetention = process.env.ALLOW_AUDIO_RETENTION_WITH_CONSENT === 'true';

  if (!allowRetention || !session || !session.audio_consent_granted) {
    // Audio retention not consented; transcribe in-memory and discard raw audio buffer
    req.shouldRetainAudioFile = false;
  } else {
    req.shouldRetainAudioFile = true;
  }

  next();
}
