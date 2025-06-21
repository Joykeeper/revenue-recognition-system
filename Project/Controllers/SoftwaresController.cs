using Project.Exceptions;
using Project.Services;
using Microsoft.AspNetCore.Mvc;
using Project.Dtos;

namespace Project.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SoftwaresController : ControllerBase
{
    private readonly ISoftwaresService _dbService;

    public SoftwaresController(ISoftwaresService db)
    {
        _dbService = db;
    }
    
    [HttpGet("{id}/income")]
    public async Task<IActionResult> GetSoftwareIncome([FromRoute] int id, [FromQuery] string currency = "PLN")
    {
        try
        {
            var income = await _dbService.GetSoftwareIncome(id, currency);
            return Ok(income);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
    
    [HttpGet("{id}/expected")]
    public async Task<IActionResult> GetSoftwareExpectedIncome([FromRoute] int id, [FromQuery] string currency = "PLN")
    {
        try
        {
            var income = await _dbService.GetSoftwareExpectedIncome(id, currency);
            return Ok(income);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
    
}