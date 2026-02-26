namespace Curvia.API.DTOs.Motorcycles;

/// <summary>
/// Request body for updating a motorcycle's details.
/// </summary>
public sealed record UpdateMotorcycleRequest(string Maker, string Model, int Year, int? EngineCc = null, string? Nickname = null);