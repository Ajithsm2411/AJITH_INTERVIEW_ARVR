const requestTracker = new Map();
const WINDOW_MS = parseInt(process.env.RATE_LIMIT_WINDOW_MS || '60000', 10);
const MAX_REQUESTS = parseInt(process.env.RATE_LIMIT_MAX_REQUESTS || '120', 10);

/**
 * In-memory sliding window rate limiter
 */
export function rateLimiter(req, res, next) {
  const ip = req.ip || req.connection.remoteAddress || '127.0.0.1';
  const now = Date.now();

  let entry = requestTracker.get(ip);
  if (!entry || (now - entry.startTime) > WINDOW_MS) {
    entry = { count: 1, startTime: now };
    requestTracker.set(ip, entry);
    return next();
  }

  entry.count++;
  if (entry.count > MAX_REQUESTS) {
    return res.status(429).json({
      success: false,
      error: 'Too many requests. Please slow down and try again shortly.'
    });
  }

  next();
}

/**
 * Periodically purge stale IP entries
 */
setInterval(() => {
  const now = Date.now();
  for (const [ip, entry] of requestTracker.entries()) {
    if (now - entry.startTime > WINDOW_MS * 2) {
      requestTracker.delete(ip);
    }
  }
}, 120000);
