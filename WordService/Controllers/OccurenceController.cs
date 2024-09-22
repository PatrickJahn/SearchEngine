using Microsoft.AspNetCore.Mvc;
using WordService.Services;

namespace WordService.Controllers;

[ApiController]
[Route("[controller]")]
public class OccurrenceController : ControllerBase
{
    private readonly Database _database;
    private readonly LoggingService _loggingService;

    public OccurrenceController(Database database, LoggingService loggingService)
    {
        _database = database;
        _loggingService = loggingService;
    }

    [HttpPost]
    public IActionResult Post(int docId, [FromBody] ISet<int> wordIds)
    {
        var activity = _loggingService.StartTrace("InsertAllOccurrences");
        try
        {
            _loggingService.LogInformation($"Inserting occurrences for document ID: {docId}");
            _database.InsertAllOccurrences(docId, wordIds);
            _loggingService.LogInformation("Occurrences inserted successfully.");
            return Ok("Occurrences inserted successfully.");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error while inserting occurrences", ex);
            return StatusCode(500, "An error occurred while inserting the occurrences.");
        }
        finally
        {
            _loggingService.EndTrace(activity);
        }
    }
}