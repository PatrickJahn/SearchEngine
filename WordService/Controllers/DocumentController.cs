using Microsoft.AspNetCore.Mvc;

namespace WordService.Controllers;

[ApiController]
[Route("[controller]")]
public class DocumentController : ControllerBase
{
    private readonly Database _database = Database.GetInstance();

    [HttpGet("GetByDocIds")]
    public List<string> GetByDocIds([FromQuery] List<int> docIds)
    {
        return _database.GetDocDetails(docIds);
    }
    
    [HttpGet("GetByWordIds")]
    public Dictionary<int, int> GetByWordIds([FromQuery] List<int> wordIds)
    {
        return _database.GetDocuments(wordIds);
    }

    [HttpPost]
    public void Post(int id, string url)
    {
        _database.InsertDocument(id, url);
    }
}