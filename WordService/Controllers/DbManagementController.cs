using Microsoft.AspNetCore.Mvc;

namespace WordService.Controllers;

[ApiController]
[Route("[controller]")]
public class DbManagementController : ControllerBase
{
    private readonly Database _database = Database.GetInstance();
    
    [HttpDelete]
    public void Delete()
    {
        _database.DeleteDatabase();
    }

    [HttpPost]
    public void Post()
    {
        _database.RecreateDatabase();
    }
}