using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace WordService.Services
{
    public class LoggingService
    {
        private readonly ILogger<LoggingService> _logger;
        private static readonly ActivitySource _activitySource = new ActivitySource("WordServiceTracer");

        public LoggingService(ILogger<LoggingService> logger)
        {
            _logger = logger;
        }

        // Log Information
        public void LogInformation(string message)
        {
            _logger.LogInformation(message);
        }

        // Log Error
        public void LogError(string message, Exception ex)
        {
            _logger.LogError(ex, message);
        }

        // Start a manual trace using ActivitySource
        public Activity? StartTrace(string operationName)
        {
            var activity = _activitySource.StartActivity(operationName, ActivityKind.Internal);
            if (activity != null)
            {
                activity.SetTag("operation", operationName);
            }
            return activity;
        }

        // End trace by recording exception (if any)
        public void EndTrace(Activity? activity, Exception? exception = null)
        {
            if (activity != null)
            {
                if (exception != null)
                {
                    activity.SetTag("error", true);
                    activity.SetTag("error.message", exception.Message);
                }
                activity.Stop();
            }
        }
    }
}