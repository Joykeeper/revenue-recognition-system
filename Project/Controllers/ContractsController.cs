using Project.Exceptions;
using Project.Services;
using Microsoft.AspNetCore.Mvc;
using Project.Dtos;

namespace Project.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ContractsControler : ControllerBase
{
    private  readonly IDbService _dbService;

    public ContractsControler (IDbService db)
    {
        _dbService = db;
    }
    

    [HttpPost]
    public async Task<IActionResult> AddContract([FromBody] NewContractDto data)
    {
        try
        {
            await _dbService.CreateContract(data);
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

    [HttpPut("{id}/pay")]
    public async Task<IActionResult> PayContract([FromRoute] PaymentDto paymentData)
    {
        try
        {
            await _dbService.PayContract(paymentData);
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

}