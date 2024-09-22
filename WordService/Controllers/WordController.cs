using Microsoft.AspNetCore.Mvc;
using WordService.Services;

namespace WordService.Controllers;

[ApiController]
[Route("[controller]")]
public class WordController : ControllerBase
{
    private readonly Database _database;
    private readonly LoggingService _loggingService;

    public WordController(Database database, LoggingService loggingService)
    {
        _database = database;
        _loggingService = loggingService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var activity = _loggingService.StartTrace("GetAllWords");
        try
        {
            _loggingService.LogInformation("Retrieving all words.");
            var result = _database.GetAllWords();
            _loggingService.LogInformation("Words retrieved successfully.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error while retrieving words", ex);
            return StatusCode(500, "An error occurred while retrieving words.");
        }
        finally
        {
            _loggingService.EndTrace(activity);
        }
    }

    [HttpPost]
    public IActionResult Post([FromBody] Dictionary<string, int> res)
    {
        
        using var activity = _loggingService.StartTrace("Word Post endpoint");
        try
        {
            _loggingService.LogInformation("Inserting all words.");
            _database.InsertAllWords(res);
            _loggingService.LogInformation("Words inserted successfully.");
            return Ok("Words inserted successfully.");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error while inserting words", ex);
            return StatusCode(500, "An error occurred while inserting words.");
        }
        
    }
}