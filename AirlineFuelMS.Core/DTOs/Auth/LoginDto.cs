namespace AirlineFuelMS.Core.DTOs.Auth;

public record LoginDto(string Username, string Password);
public record LoginResponseDto(string Token, string Role, string Username);
