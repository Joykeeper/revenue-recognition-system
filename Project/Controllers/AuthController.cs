using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Project.Dtos;
using Project.Exceptions;
using Project.Services;

namespace Project.Controllers;

[ApiController]
[Route("api/authentication")]
public class AuthenticationController : ControllerBase
{
    private readonly IConfiguration _applicationConfig;
    private readonly IAuthService _authenticationProvider;

    public AuthenticationController(IConfiguration applicationConfig, IAuthService authenticationProvider)
    {
        _applicationConfig = applicationConfig;
        _authenticationProvider = authenticationProvider;
    }

    [HttpPost("sign-in")]
    public async Task<IActionResult> SignIn([FromBody] LoginDto loginDetails)
    {
        var tokenString = await _authenticationProvider.AuthenticateUserAsync(loginDetails);
        return Ok(tokenString);
    }

    [HttpPost("sign-up")]
    public async Task<IActionResult> SignUp([FromBody] RegisterUserDto registrationData)
    {
        await _authenticationProvider.RegisterNewUserAsync(registrationData);
        return Ok(new { message = "Registration completed successfully!" });
    }
}