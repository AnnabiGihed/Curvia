using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.ApproveModel;

/// <summary>
/// Represents a command to approve a pending motorcycle model in the catalog.
/// </summary>
/// <param name="ModelId">Identifier of the model to approve.</param>
/// <param name="ActorId">Identifier of the actor performing the approval (admin/system).</param>
public sealed record ApproveModelCommand(Guid ModelId, string ActorId) : ICommand;