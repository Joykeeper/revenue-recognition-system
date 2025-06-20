using Project.Exceptions;
using Project.Services;
using Microsoft.AspNetCore.Mvc;
using Project.Dtos;

namespace Poprawa.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClientsController : ControllerBase
{
    private  readonly IDbService _dbService;

    public ClientsController (IDbService db)
    {
        _dbService = db;
    }
    
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
        try
        {
            await _dbService.AddClient(data);
            return Created();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (ConflictException e)
        {
            return Conflict(e.Message);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClient([FromRoute] int id)
    {
        try
        {
            await _dbService.DeleteClient(id);
            return Ok();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (ConflictException e)
        {
            return Conflict(e.Message);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClient([FromRoute] int id,[FromBody] UpdateClientDto data)
    {
        try
        {
            await _dbService.UpdateClient(id, data);
            return Created();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (ConflictException e)
        {
            return Conflict(e.Message);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

}