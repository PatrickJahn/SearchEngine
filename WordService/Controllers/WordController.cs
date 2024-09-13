using Microsoft.AspNetCore.Mvc;

namespace WordService.Controllers;

[ApiController]
[Route("[controller]")]
public class WordController : ControllerBase
{
    private readonly Database _database = Database.GetInstance();

    [HttpGet]
    public async Task<Dictionary<string, int>> Get()
    {
        return await _database.GetAllWords();
    }

    [HttpPost]
    public async Task Post([FromBody]Dictionary<string, int> res)
    {
       await _database.InsertAllWords(res);
    }
}