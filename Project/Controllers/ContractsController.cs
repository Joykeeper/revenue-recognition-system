using Microsoft.AspNetCore.Authorization;
using Project.Exceptions;
using Project.Services;
using Microsoft.AspNetCore.Mvc;
using Project.Dtos;

namespace Project.Controllers;

[Authorize(Roles = "User")]
[Route("api/[controller]")]
[ApiController]
public class ContractsControler : ControllerBase
{
    private  readonly IContractsService _dbService;

    public ContractsControler (IContractsService db)
    {
        _dbService = db;
    }
    
    [HttpPost]
    public async Task<IActionResult> AddContract([FromBody] NewContractDto data)
    {
        await _dbService.CreateContract(data);
        return Created();
    }

    [HttpPut("{id}/pay")]
    public async Task<IActionResult> PayContract([FromRoute] PaymentDto paymentData)
    {
        await _dbService.PayContract(paymentData);
        return Ok();
    }

}