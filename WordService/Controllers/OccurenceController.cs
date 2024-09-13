using Microsoft.AspNetCore.Mvc;

namespace WordService.Controllers;

[ApiController]
[Route("[controller]")]
public class OccurrenceController : ControllerBase
{
    private readonly Database _database = Database.GetInstance();

    [HttpPost]
    public async Task Post(int docId, [FromBody]ISet<int> wordIds)
    {
        await _database.InsertAllOccurrences(docId, wordIds);
    }
}