using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.Data;
using Project.Data;
using Project.Dtos;
using Project.Exceptions;
using Project.Models;
using Project.Services;

// Assuming these are your DTOs and custom exceptions
// public class LoginRequest { public string Login { get; set; } public string Password { get; set; } }
// public class RegisterUserRequest { public string Login { get; set; } public string Password { get; set; } public string Email { get; set; } public string Rola { get; set; } }
// public class User { public int IdUser { get; set; } public string Login { get; set; } public string Password { get; set; } public string Salt { get; set; } public string Email { get; set; } public int IdRola { get; set; } public Rola IdRolaNavigation { get; set; } }
// public class Rola { public int IdRola { get; set; } public string Nazwa { get; set; } }
// public class UserExistsException : Exception { public UserExistsException(string message) : base(message) { } }
// public class UnauthorizedAccessException : Exception { public UnauthorizedAccessException(string message) : base(message) { } }


public class AuthService : IAuthService
{
    private readonly DatabaseContext _databaseContext;
    private readonly IConfiguration _appConfiguration;

    public AuthService(DatabaseContext context, IConfiguration configuration)
    {
        _databaseContext = context;
        _appConfiguration = configuration;
    }

    public async Task<string> AuthenticateUserAsync(LoginDto credentials)
    {
        var userRecord = await _databaseContext.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Login == credentials.Login);

        // Validate user existence
        if (userRecord == null)
        {
            throw new BadRequestException("Authentication failed: Invalid username or password.");
        }

        // Verify password hash
        var hashedPassword = GeneratePasswordHash(credentials.Password + userRecord.UserSalt);
        if (userRecord.Password != hashedPassword)
        {
            throw new BadRequestException("Authentication failed: Invalid username or password.");
        }

        // Issue JWT
        var authToken = CreateJwtToken(userRecord);
        return authToken;
    }

    public async Task RegisterNewUserAsync(RegisterUserDto registrationDetails)
    {
        // Check for existing user with the same login
        var existingUser = await _databaseContext.Users.FirstOrDefaultAsync(u => u.Login == registrationDetails.Login);
        if (existingUser != null)
        {
            throw new ConflictException("A user with the provided login already exists.");
        }

        // Generate salt and hash password
        var userSalt = GenerateRandomSalt();
        var userPasswordHash = GeneratePasswordHash(registrationDetails.Password + userSalt);

        // Determine user role
        var assignedRole = await _databaseContext.Roles.FirstOrDefaultAsync(r => r.Name == registrationDetails.Role);
        int roleIdToAssign = assignedRole?.Id ?? 2; // Default to role ID 2 (e.g., 'User') if not found

        // Create new user entity
        var newUser = new User
        {
            Login = registrationDetails.Login,
            Password = userPasswordHash,
            UserSalt = userSalt,
            Email = registrationDetails.Email,
            RoleId = roleIdToAssign,
            // Assuming IdUser is auto-incremented by the DB or managed elsewhere.
            // If you truly need to manually assign the max ID + 1, uncomment/re-add this line,
            // but it's generally discouraged for primary keys in production databases.
            // IdUser = (await _databaseContext.Users.Select(a => (int?)a.IdUser).MaxAsync() ?? 0) + 1
        };

        // Add to context and save
        await _databaseContext.Users.AddAsync(newUser);
        await _databaseContext.SaveChangesAsync();
    }

    private string CreateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, user.Role.Name)
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appConfiguration["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _appConfiguration["Jwt:Issuer"],
            Audience = _appConfiguration["Jwt:Audience"],
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(60), // Use UtcNow for consistency
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GeneratePasswordHash(string inputString)
    {
        using var shaAlgorithm = SHA256.Create();
        var bytesToHash = Encoding.UTF8.GetBytes(inputString);
        var hashedBytes = shaAlgorithm.ComputeHash(bytesToHash);
        return Convert.ToBase64String(hashedBytes);
    }

    private static string GenerateRandomSalt()
    {
        byte[] saltBytes = new byte[16]; // 128 bits / 8 bits per byte = 16 bytes
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }

        return Convert.ToBase64String(saltBytes);
    }
}