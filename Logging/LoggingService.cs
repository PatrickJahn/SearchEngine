using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Core;
using Serilog.Core.Enrichers;

namespace Logging;

public static class LoggingService 
{
    private static readonly string ServiceName = Assembly.GetCallingAssembly().GetName().Name ?? "Unknown"; 
    public static ActivitySource _activitySource = new ActivitySource(ServiceName);
    public static TracerProvider _tracerProvider;

    public static ILogger Log => Serilog.Log.Logger;
    static LoggingService()
    {
        // Open telemetry config
        _tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(_activitySource.Name)
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(ServiceName))
            .AddZipkinExporter(options =>
            {
                options.Endpoint = new Uri("http://zipkin:9411/api/v2/spans"); // Zipkin 
            })
            .Build();
        
        // Serilog config
        Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .WriteTo.Seq("http://seq:5341") // Seq running on this address
            .Enrich.FromLogContext()
            .CreateLogger();
            


    }


    public static HttpRequestMessage AddActivityInfoToHttpRequest(HttpRequestMessage httpRequestMessage, Activity activity)
    {
        var activityContext = activity?.Context ?? Activity.Current?.Context ?? default;
        var propagationContext = new PropagationContext(activityContext, Baggage.Current);
        var propagator = new TraceContextPropagator();
        
        propagator.Inject(propagationContext, httpRequestMessage, (r, key, value) =>
        {
            r.Headers.Add(key,value);
        });
        
        return httpRequestMessage;
    }
    
    public static HttpRequestMessage ExtractActivityInfoFromHttpRequest(HttpRequestMessage httpRequestMessage, Activity activity)
    {
      
        var propagator = new TraceContextPropagator();
        
        var parentContext = propagator.Extract(default, httpRequestMessage, (r, key) => r.Headers.GetValues(key));

        Baggage.Current = parentContext.Baggage;
        
        
        return httpRequestMessage;
    }
}

