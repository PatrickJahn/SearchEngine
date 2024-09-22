using Microsoft.AspNetCore.Mvc;
using WordService.Services;

namespace WordService.Controllers;

[ApiController]
[Route("[controller]")]
public class DocumentController : ControllerBase
{
    private readonly Database _database;
    private readonly LoggingService _loggingService;

    public DocumentController(Database database, LoggingService loggingService)
    {
        _database = database;
        _loggingService = loggingService;
    }

    [HttpGet("GetByDocIds")]
    public IActionResult GetByDocIds([FromQuery] List<int> docIds)
    {
        var activity = _loggingService.StartTrace("GetByDocIds");
        try
        {
            _loggingService.LogInformation($"Retrieving documents by IDs: {string.Join(",", docIds)}");
            var result = _database.GetDocDetails(docIds);
            _loggingService.LogInformation("Documents retrieved successfully.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error while retrieving documents by IDs", ex);
            return StatusCode(500, "An error occurred while retrieving the documents.");
        }
        finally
        {
            _loggingService.EndTrace(activity);
        }
    }
    
    [HttpGet("GetByWordIds")]
    public IActionResult GetByWordIds([FromQuery] List<int> wordIds)
    {
        var activity = _loggingService.StartTrace("GetByWordIds");
        try
        {
            _loggingService.LogInformation($"Retrieving documents by word IDs: {string.Join(",", wordIds)}");
            var result = _database.GetDocuments(wordIds);
            _loggingService.LogInformation("Documents retrieved successfully.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error while retrieving documents by word IDs", ex);
            return StatusCode(500, "An error occurred while retrieving the documents.");
        }
        finally
        {
            _loggingService.EndTrace(activity);
        }
    }

    [HttpPost]
    public IActionResult Post(int id, string url)
    {
        var activity = _loggingService.StartTrace("InsertDocument");
        try
        {
            _loggingService.LogInformation($"Inserting document with ID: {id} and URL: {url}");
            _database.InsertDocument(id, url);
            _loggingService.LogInformation("Document inserted successfully.");
            return Ok("Document inserted successfully.");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error while inserting document", ex);
            return StatusCode(500, "An error occurred while inserting the document.");
        }
        finally
        {
            _loggingService.EndTrace(activity);
        }
    }
}
