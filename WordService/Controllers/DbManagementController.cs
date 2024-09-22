using Microsoft.AspNetCore.Mvc;
using WordService.Services;
using System.Diagnostics;

namespace WordService.Controllers;

[ApiController]
[Route("[controller]")]
public class DbManagementController : ControllerBase
{
    private readonly Database _database;
    private readonly LoggingService _loggingService;

    public DbManagementController(Database database, LoggingService loggingService)
    {
        _database = database;
        _loggingService = loggingService;
    }

    [HttpDelete]
    public IActionResult Delete()
    {
        var activity = _loggingService.StartTrace("Delete Database");
        try
        {
            _loggingService.LogInformation("Database deletion initiated.");
            _database.DeleteDatabase();
            _loggingService.LogInformation("Database deletion completed.");
            return Ok("Database deleted successfully.");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error while deleting database", ex);
            _loggingService.EndTrace(activity, ex);
            return StatusCode(500, "An error occurred while deleting the database.");
        }
        finally
        {
            _loggingService.EndTrace(activity);
        }
    }

    [HttpPost]
    public IActionResult Post()
    {
        var activity = _loggingService.StartTrace("Recreate Database");
        try
        {
            _loggingService.LogInformation("Recreating database.");
            _database.RecreateDatabase();
            _loggingService.LogInformation("Database recreation completed.");
            return Ok("Database recreated successfully.");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error while recreating database", ex);
            _loggingService.EndTrace(activity, ex);
            return StatusCode(500, "An error occurred while recreating the database.");
        }
        finally
        {
            _loggingService.EndTrace(activity);
        }
    }
}
