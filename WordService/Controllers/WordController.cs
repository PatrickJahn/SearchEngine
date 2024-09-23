using Logging;
using Microsoft.AspNetCore.Mvc;

namespace WordService.Controllers;

[ApiController]
[Route("[controller]")]
public class WordController : ControllerBase
{
    private readonly Database _database;

    public WordController(Database database)
    {
        _database = database;
    }

    [HttpGet]
    public IActionResult Get()
    {
        using var activity = LoggingService._activitySource.StartActivity("GetAllWords");
        try
        {
            LoggingService.Log.Information("Retrieving all words.");
            var result = _database.GetAllWords();
            LoggingService.Log.Information("Words retrieved successfully.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error("Error while retrieving words", ex);
            return StatusCode(500, "An error occurred while retrieving words.");
        }
       
    }

    [HttpPost]
    public IActionResult Post([FromBody] Dictionary<string, int> res)
    {
        
        using var activity =  LoggingService._activitySource.StartActivity("Word Post endpoint");
        try
        {
            LoggingService.Log.Information("Inserting all words.");
            _database.InsertAllWords(res);
            LoggingService.Log.Information("Words inserted successfully.");
            return Ok("Words inserted successfully.");
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error("Error while inserting words", ex);
            return StatusCode(500, "An error occurred while inserting words.");
        }
        
    }
}