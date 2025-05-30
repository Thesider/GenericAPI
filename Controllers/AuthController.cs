using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AutoMapper;
using GenericAPI.DTOs;
using GenericAPI.Helpers;
using GenericAPI.Models;
using GenericAPI.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace GenericAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly JwtHelper _jwtHelper;
    private const int KeySize = 64;
    private const int Iterations = 350000;
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA512;

    public AuthController(IUserRepository userRepository, IMapper mapper, JwtHelper jwtHelper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _jwtHelper = jwtHelper;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto registerDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new 
            { 
                message = "Validation failed", 
                errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
            });
        }

        registerDto.Username = registerDto.Username.Trim();
        registerDto.Email = registerDto.Email.Trim().ToLower();
        registerDto.FirstName = registerDto.FirstName.Trim();
        registerDto.LastName = registerDto.LastName.Trim();

        // Additional whitespace validation
        if (string.IsNullOrWhiteSpace(registerDto.Username) || string.IsNullOrWhiteSpace(registerDto.Email) || 
            string.IsNullOrWhiteSpace(registerDto.Password) || string.IsNullOrWhiteSpace(registerDto.FirstName) || 
            string.IsNullOrWhiteSpace(registerDto.LastName))
        {
            return BadRequest(new { message = "Fields cannot contain only whitespace" });
        }

        if (await _userRepository.IsEmailUniqueAsync(registerDto.Email) == false)
            return BadRequest(new { message = "Email is already registered" });

        if (await _userRepository.IsUsernameUniqueAsync(registerDto.Username) == false)
            return BadRequest(new { message = "Username is already taken" });

        var user = _mapper.Map<User>(registerDto);
        
        // Generate salt and hash the password
        var salt = RandomNumberGenerator.GetBytes(KeySize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(registerDto.Password),
            salt,
            Iterations,
            HashAlgorithm,
            KeySize);

        user.PasswordHash = Convert.ToBase64String(hash);
        user.PasswordSalt = Convert.ToBase64String(salt);
        user.Role = "User"; // Default role
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        user.IsActive = true;

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var token = _jwtHelper.GenerateToken(user);
        var refreshToken = _jwtHelper.GenerateRefreshToken();

        return Ok(new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            User = _mapper.Map<UserDto>(user)
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new 
            { 
                message = "Validation failed", 
                errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
            });
        }

        loginDto.Email = loginDto.Email.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
            return BadRequest(new { message = "Email and password are required" });

        var user = await _userRepository.GetByEmailAsync(loginDto.Email);
        if (user == null)
            return Unauthorized(new { message = "Invalid email or password" });

        if (!user.IsActive)
            return Unauthorized(new { message = "This account has been deactivated" });

        // Verify password using salt
        var salt = Convert.FromBase64String(user.PasswordSalt);
        var hashToCompare = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(loginDto.Password),
            salt,
            Iterations,
            HashAlgorithm,
            KeySize);

        var passwordHash = Convert.ToBase64String(hashToCompare);
        if (passwordHash != user.PasswordHash)
            return Unauthorized(new { message = "Invalid email or password" });

        var token = _jwtHelper.GenerateToken(user);
        var refreshToken = _jwtHelper.GenerateRefreshToken();

        return Ok(new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            User = _mapper.Map<UserDto>(user)
        });
    }
}
