using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Motorcycles.Commands.SetDefault;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Marks a motorcycle as the user's default — the pre-selected bike
///              on the route generation UI.
///              The handler clears the current default before setting the new one,
///              maintaining the one-default-per-user invariant.
/// </summary>
/// <param name="UserId">Authenticated user's application-level identifier.</param>
/// <param name="MotorcycleId">Identifier of the motorcycle to set as default.</param>
public sealed record SetDefaultMotorcycleCommand(Guid UserId, Guid MotorcycleId) : ICommand;