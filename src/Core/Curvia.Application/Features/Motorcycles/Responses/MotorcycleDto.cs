namespace Curvia.Application.Features.Motorcycles.Responses;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Flat DTO representing a single motorcycle in a user's garage.
///              All domain value objects are unwrapped to primitives.
/// </summary>
/// <param name="Id">Motorcycle identifier.</param>
/// <param name="UserId">Owner's application-level user identifier.</param>
/// <param name="Maker">Manufacturer name (e.g. "Ducati").</param>
/// <param name="Model">Model name (e.g. "Monster 937").</param>
/// <param name="Year">Model year.</param>
/// <param name="EngineCc">Engine displacement in cc. Null when not provided.</param>
/// <param name="Nickname">User-given nickname. Null when not set.</param>
/// <param name="IsDefault">Whether this is the pre-selected bike for route generation.</param>
/// <param name="AddedAtUtc">UTC timestamp when the motorcycle was added to the garage.</param>
public sealed record MotorcycleDto(Guid Id,	Guid UserId, string Maker, string Model, int Year, int? EngineCc, string? Nickname, bool IsDefault, DateTime AddedAtUtc);