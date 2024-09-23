using Logging;
using Microsoft.AspNetCore.Mvc;

namespace WordService.Controllers;

[ApiController]
[Route("[controller]")]
public class OccurrenceController : ControllerBase
{
    private readonly Database _database;

    public OccurrenceController(Database database)
    {
        _database = database;
    }

    [HttpPost]
    public IActionResult Post(int docId, [FromBody] ISet<int> wordIds)
    {
        using var activity = LoggingService._activitySource.StartActivity("InsertAllOccurrences");
        try
        {
            LoggingService.Log.Information($"Inserting occurrences for document ID: {docId}");
            _database.InsertAllOccurrences(docId, wordIds);
            LoggingService.Log.Information("Occurrences inserted successfully.");
            return Ok("Occurrences inserted successfully.");
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error("Error while inserting occurrences", ex);
            return StatusCode(500, "An error occurred while inserting the occurrences.");
        }
       
    }
}