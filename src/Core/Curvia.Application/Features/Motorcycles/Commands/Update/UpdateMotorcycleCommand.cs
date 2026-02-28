using Curvia.Application.Features.Motorcycles.Responses;
using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Motorcycles.Commands.Update;
/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Updates the details of an existing motorcycle in the authenticated user's garage.
///              Ownership is enforced by the handler — only the owner may update their motorcycle.
/// </summary>
/// <param name="UserId">Authenticated user's application-level identifier.</param>
/// <param name="MotorcycleId">Identifier of the motorcycle to update.</param>
/// <param name="Maker">Updated manufacturer name. Max 100 chars.</param>
/// <param name="Model">Updated model name. Max 150 chars.</param>
/// <param name="Year">Updated model year (1900 – current year + 2).</param>
/// <param name="EngineCc">Updated engine displacement in cc (50–2500). Optional.</param>
/// <param name="Nickname">Updated nickname. Optional, max 100 chars.</param>
public sealed record UpdateMotorcycleCommand(Guid UserId, Guid MotorcycleId, string Maker, string Model, int Year, int? EngineCc = null,	string? Nickname = null) : ICommand<MotorcycleDto>;