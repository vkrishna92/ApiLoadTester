using System;
using Microsoft.Extensions.Logging;

namespace ApiLoadTester.Services
{
    /// <summary>
    /// Provides centralized logging functionality for the ApiLoadTester application
    /// </summary>
    public static class LogHelper
    {
        private static ILogger? _logger;

        /// <summary>
        /// Initialize the logger with a specific logger instance
        /// </summary>
        /// <param name="logger">The logger to use for logging operations</param>
        public static void Initialize(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null");
        }

        /// <summary>
        /// Log an informational message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="args">Optional formatting arguments</param>
        public static void LogInfo(string? message, params object[] args)
        {
            EnsureLoggerInitialized();
            if (!string.IsNullOrEmpty(message))
            {
                _logger!.LogInformation(message, args);
            }
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        /// <param name="message">The warning message to log</param>
        /// <param name="args">Optional formatting arguments</param>
        public static void LogWarning(string? message, params object[] args)
        {
            EnsureLoggerInitialized();
            if (!string.IsNullOrEmpty(message))
            {
                _logger!.LogWarning(message, args);
            }
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="message">Optional error message</param>
        /// <param name="args">Optional formatting arguments</param>
        public static void LogError(Exception? exception, string? message = null, params object[] args)
        {
            EnsureLoggerInitialized();
            if (exception != null)
            {
                if (string.IsNullOrEmpty(message))
                {
                    _logger!.LogError(exception, exception.Message);
                }
                else
                {
                    _logger!.LogError(exception, message, args);
                }
            }
            else if (!string.IsNullOrEmpty(message))
            {
                _logger!.LogError(message, args);
            }
        }

        /// <summary>
        /// Log a debug message
        /// </summary>
        /// <param name="message">The debug message to log</param>
        /// <param name="args">Optional formatting arguments</param>
        public static void LogDebug(string? message, params object[] args)
        {
            EnsureLoggerInitialized();
            if (!string.IsNullOrEmpty(message))
            {
                _logger!.LogDebug(message, args);
            }
        }

        /// <summary>
        /// Ensures that the logger has been initialized before use
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when logger has not been initialized</exception>
        private static void EnsureLoggerInitialized()
        {
            if (_logger == null)
            {
                throw new InvalidOperationException("Logger has not been initialized. Call Initialize() first.");
            }
        }
    }
}
