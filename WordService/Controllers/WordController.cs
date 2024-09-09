using Microsoft.AspNetCore.Mvc;

namespace WordService.Controllers;

[ApiController]
[Route("[controller]")]
public class WordController : ControllerBase
{
    private readonly Database _database = Database.GetInstance();

    [HttpGet]
    public Dictionary<string, int> Get()
    {
        return _database.GetAllWords();
    }

    [HttpPost]
    public void Post([FromBody]Dictionary<string, int> res)
    {
        _database.InsertAllWords(res);
    }
}