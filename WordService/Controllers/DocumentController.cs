using System.Diagnostics;
using Logging;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace WordService.Controllers;

[ApiController]
[Route("[controller]")]
public class DocumentController : ControllerBase
{
    private readonly Database _database;

    public DocumentController(Database database)
    {
        _database = database;
    }

    [HttpGet("GetByDocIds")]
    public IActionResult GetByDocIds([FromQuery] List<int> docIds)
    {
        var propagator = new TraceContextPropagator();
        
        var parentContext = propagator.Extract(default, Request, (r, key) =>
        {
            return new List<string>( new [] {r.Headers.ContainsKey(key) ? r.Headers[key].ToString() : string.Empty});
        });

        Baggage.Current = parentContext.Baggage;

        using var activity = LoggingService._activitySource.StartActivity("GetByWordIds Document endpoint", ActivityKind.Consumer, parentContext.ActivityContext);          try
        {
            LoggingService.Log.Information($"Retrieving documents by IDs: {string.Join(",", docIds)}");
            var result = _database.GetDocDetails(docIds);
            LoggingService.Log.Information("Documents retrieved successfully.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error("Error while retrieving documents by IDs", ex);
            return StatusCode(500, "An error occurred while retrieving the documents.");
        }
      
    }
    
    [HttpGet("GetByWordIds")]
    public IActionResult GetByWordIds([FromQuery] List<int> wordIds)
    {
        var propagator = new TraceContextPropagator();
        
        var parentContext = propagator.Extract(default, Request, (r, key) =>
        {
            return new List<string>( new [] {r.Headers.ContainsKey(key) ? r.Headers[key].ToString() : string.Empty});
        });

        Baggage.Current = parentContext.Baggage;

        using var activity = LoggingService._activitySource.StartActivity("GetByWordIds Document endpoint", ActivityKind.Consumer, parentContext.ActivityContext);       
        try
        {
            LoggingService.Log.Information($"Retrieving documents by word IDs: {string.Join(",", wordIds)}");
            var result = _database.GetDocuments(wordIds);
            LoggingService.Log.Information("Documents retrieved successfully.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error("Error while retrieving documents by word IDs", ex);
            return StatusCode(500, "An error occurred while retrieving the documents.");
        }
       
    }

    [HttpPost]
    public IActionResult Post(int id, string url)
    {
        
        var propagator = new TraceContextPropagator();
        
        var parentContext = propagator.Extract(default, Request, (r, key) =>
        {
            LoggingService.Log.Information(key + " : " + r.Headers[key].ToString());
            return new List<string>() {r.Headers.ContainsKey(key) ? r.Headers[key].ToString() : string.Empty};
        });

        Baggage.Current = parentContext.Baggage;

        using var activity = LoggingService._activitySource.StartActivity("Insert documnet endpoint", ActivityKind.Consumer, parentContext.ActivityContext);
        try
        {
            LoggingService.Log.Information($"Calling database to insert documnet");
            _database.InsertDocument(id, url);
            LoggingService.Log.Information("Document inserted successfully.");
            return Ok("Document inserted successfully.");
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error("Error while inserting document", ex);
            return StatusCode(500, "An error occurred while inserting the document.");
        }
       
    }
}
