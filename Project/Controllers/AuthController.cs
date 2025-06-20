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
        try
        {
            var tokenString = await _authenticationProvider.AuthenticateUserAsync(loginDetails);
            return Ok(tokenString);
        }
        catch (UnauthorizedAccessException authEx)
        {
            return Unauthorized(new { message = authEx.Message });
        }
        catch (Exception e)
        {
            return StatusCode(500, new { message = "An unexpected error occurred during sign-in." });
        }
    }

    [HttpPost("sign-up")]
    public async Task<IActionResult> SignUp([FromBody] RegisterUserDto registrationData)
    {
        try
        {
            await _authenticationProvider.RegisterNewUserAsync(registrationData);
            return Ok(new { message = "Registration completed successfully!" });
        }
        catch (ConflictException userExistsEx)
        {
            return Conflict(new { message = userExistsEx.Message });
        }
        catch (Exception e)
        {
            return StatusCode(500, new { message = "An unexpected error occurred during registration." });
        }
    }
}