using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace WordService.Services
{
    public class LoggingService
    {
        private static readonly string ServiceName = Assembly.GetCallingAssembly().GetName().Name ?? "Unknown"; 
        private readonly ILogger<LoggingService> _logger;
        private static ActivitySource _activitySource = new ActivitySource(ServiceName);
        public static TracerProvider _tracerProvider;

            
        public LoggingService(ILogger<LoggingService> logger)
        {
            _tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddAspNetCoreInstrumentation()
                .AddSource(_activitySource.Name)
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(ServiceName))
                .AddZipkinExporter(options =>
                {
                    options.Endpoint = new Uri("http://zipkin:9411/api/v2/spans"); // Zipkin 
                })
                .Build();
            
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
            var activity = _activitySource.StartActivity(operationName);
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