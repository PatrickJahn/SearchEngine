using Microsoft.AspNetCore.Mvc;

namespace WordService.Controllers;

[ApiController]
[Route("[controller]")]
public class DocumentController : ControllerBase
{
    private readonly Database _database = Database.GetInstance();

    [HttpGet("GetByDocIds")]
    public async Task<List<string>> GetByDocIds([FromQuery] List<int> docIds)
    {
        return await _database.GetDocDetails(docIds);
    }
    
    [HttpGet("GetByWordIds")]
    public async Task<Dictionary<int, int>> GetByWordIds([FromQuery] List<int> wordIds)
    {
        return await _database.GetDocuments(wordIds);
    }

    [HttpPost]
    public async Task Post(int id, string url)
    {
        await _database.InsertDocument(id, url);
    }
}