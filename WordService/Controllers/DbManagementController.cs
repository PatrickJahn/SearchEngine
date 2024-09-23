using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Logging;

namespace WordService.Controllers;

[ApiController]
[Route("[controller]")]
public class DbManagementController : ControllerBase
{
    private readonly Database _database;

    public DbManagementController(Database database)
    {
        _database = database;
    }

    [HttpDelete]
    public IActionResult Delete()
    {
        using var activity = LoggingService._activitySource.StartActivity("Delete Database");
        try
        {
            LoggingService.Log.Information("Database deletion initiated.");
            _database.DeleteDatabase();
            LoggingService.Log.Information("Database deletion completed.");
            return Ok("Database deleted successfully.");
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error("Error while deleting database", ex);
            return StatusCode(500, "An error occurred while deleting the database.");
        }
       
    }

    [HttpPost]
    public IActionResult Post()
    {
        using var activity = LoggingService._activitySource.StartActivity("Recreate Database");
        try
        {
            LoggingService.Log.Information("Recreating database.");
            _database.RecreateDatabase();
            LoggingService.Log.Information("Database recreation completed.");
            return Ok("Database recreated successfully.");
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error("Error while recreating database", ex);
            return StatusCode(500, "An error occurred while recreating the database.");
        }
       
    }
}
