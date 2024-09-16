using Microsoft.AspNetCore.Mvc;

namespace WordService.Controllers;

[ApiController]
[Route("[controller]")]
public class DbManagementController : ControllerBase
{
    private readonly Database _database = Database.GetInstance();
    
    [HttpDelete]
    public async Task Delete()
    {
        await _database.DeleteDatabase();
    }

    [HttpPost]
    public async Task Post()
    {
       await _database.RecreateDatabase();
    }
}