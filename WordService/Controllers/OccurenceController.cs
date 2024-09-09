using Microsoft.AspNetCore.Mvc;

namespace WordService.Controllers;

[ApiController]
[Route("[controller]")]
public class OccurrenceController : ControllerBase
{
    private readonly Database _database = Database.GetInstance();

    [HttpPost]
    public void Post(int docId, [FromBody]ISet<int> wordIds)
    {
        _database.InsertAllOccurrences(docId, wordIds);
    }
}