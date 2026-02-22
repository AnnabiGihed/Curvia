using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.ApproveMaker;

/// <summary>
/// Represents a command to approve a pending motorcycle maker in the catalog.
/// </summary>
/// <param name="MakerId">Identifier of the maker to approve.</param>
/// <param name="ActorId">Identifier of the actor performing the approval (admin/system).</param>
public sealed record ApproveMakerCommand(Guid MakerId, string ActorId) : ICommand;