namespace Jex.WebAPI.Models;

/// <summary>Payload of a successful login.</summary>
public sealed record LoginResponse(string Token);

/// <summary>Payload returned by create endpoints.</summary>
public sealed record CreatedResponse(long Id);
