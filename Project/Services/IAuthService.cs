using Project.Dtos;

namespace Project.Services;

public interface IAuthService
{
    Task<string> AuthenticateUserAsync(LoginDto credentials);
    Task RegisterNewUserAsync(RegisterUserDto registrationDetails);
    

}