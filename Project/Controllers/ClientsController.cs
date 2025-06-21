using Microsoft.AspNetCore.Authorization;
using Project.Exceptions;
using Project.Services;
using Microsoft.AspNetCore.Mvc;
using Project.Dtos;

namespace Project.Controllers;

[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
[ApiController]
public class ClientsController : ControllerBase
{
    private  readonly IClientsService _dbService;

    public ClientsController (IClientsService db)
    {
        _dbService = db;
    }
    
    [Authorize(Roles = "User")]
    [HttpGet("{id}/income")]
    public async Task<IActionResult> GetClientIncome([FromRoute] int id, [FromQuery] string currency = "PLN")
    {
        try
        {
            var income = await _dbService.GetClientIncome(id, currency);
            return Ok(income);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
    
    [Authorize(Roles = "User")]
    [HttpGet("{id}/expected")]
    public async Task<IActionResult> GetClientExpectedIncome([FromRoute] int id, [FromQuery] string currency = "PLN")
    {
        try
        {
            var income = await _dbService.GetClientExpectedIncome(id, currency);
            return Ok(income);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
    
    [HttpPost]
    public async Task<IActionResult> AddClient([FromBody] NewClientDto data)
    {
        await _dbService.AddClient(data);
        return Created();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClient([FromRoute] int id)
    {
        await _dbService.DeleteClient(id);
        return Ok();
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClient([FromRoute] int id,[FromBody] UpdateClientDto data)
    {
        await _dbService.UpdateClient(id, data);
        return Ok();
    }

}