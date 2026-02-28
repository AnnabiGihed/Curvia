using Curvia.Application.Features.Motorcycles.Responses;
using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Motorcycles.Commands.Add;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Adds a new motorcycle to the authenticated user's garage.
///              If <see cref="IsDefault"/> is true, the handler will clear any existing
///              default on sibling motorcycles before setting this one as default.
/// </summary>
/// <param name="UserId">Authenticated user's application-level identifier.</param>
/// <param name="Maker">Manufacturer name (e.g. "Ducati"). Max 100 chars.</param>
/// <param name="Model">Model name (e.g. "Monster 937"). Max 150 chars.</param>
/// <param name="Year">Model year (1900 – current year + 2).</param>
/// <param name="EngineCc">Engine displacement in cc (50–2500). Optional.</param>
/// <param name="Nickname">User-given nickname. Optional, max 100 chars.</param>
/// <param name="IsDefault">Whether to pre-select this bike on route generation. Default: false.</param>
public sealed record AddMotorcycleCommand(Guid UserId, string Maker, string Model, int Year, int? EngineCc = null, string? Nickname = null, bool IsDefault = false) : ICommand<MotorcycleDto>;