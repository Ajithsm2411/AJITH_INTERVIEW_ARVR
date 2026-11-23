const DEFAULT_TIMEOUT_MS = parseInt(process.env.REQUEST_TIMEOUT_MS || '15000', 10);

/**
 * Request timeout middleware ensuring calls do not hang indefinitely
 */
export function requestTimeout(timeoutMs = DEFAULT_TIMEOUT_MS) {
  return (req, res, next) => {
    const timer = setTimeout(() => {
      if (!res.headersSent) {
        console.warn(`[Timeout] Request ${req.method} ${req.url} timed out after ${timeoutMs}ms`);
        res.status(504).json({
          success: false,
          error: `Request timed out after ${timeoutMs / 1000} seconds. Please retry.`
        });
      }
    }, timeoutMs);

    res.on('finish', () => clearTimeout(timer));
    res.on('close', () => clearTimeout(timer));
    next();
  };
}

/**
 * Global Error Handler Middleware
 */
export function globalErrorHandler(err, req, res, next) {
  console.error('[GlobalErrorHandler]', err.stack || err.message);

  if (res.headersSent) {
    return next(err);
  }

  const statusCode = err.statusCode || 500;
  res.status(statusCode).json({
    success: false,
    error: err.message || 'An unexpected internal server error occurred.',
    timestamp: new Date().toISOString()
  });
}
