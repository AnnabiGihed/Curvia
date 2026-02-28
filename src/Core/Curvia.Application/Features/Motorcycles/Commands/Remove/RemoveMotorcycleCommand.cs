using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Motorcycles.Commands.Remove;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Soft-deletes a motorcycle from the authenticated user's garage.
///              Only the owner may remove their own motorcycle.
/// </summary>
/// <param name="UserId">Authenticated user's application-level identifier.</param>
/// <param name="MotorcycleId">Identifier of the motorcycle to remove.</param>
public sealed record RemoveMotorcycleCommand(Guid UserId, Guid MotorcycleId) : ICommand;