namespace Curvia.API.DTOs.Motorcycles;

/// <summary>
/// Request body for adding a motorcycle to the garage.
/// </summary>
public sealed record AddMotorcycleRequest(string Maker, string Model, int Year, int? EngineCc = null, string? Nickname = null, bool IsDefault = false);