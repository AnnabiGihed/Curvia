using Curvia.Domain.Features.MotorcycleCatalog.Enums;
using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.AddModel;

/// <summary>
/// Represents a command to add a new official motorcycle model under an existing maker.
/// </summary>
/// <param name="MakerId">Identifier of the owning maker.</param>
/// <param name="Name">Model name to create (e.g. "Monster 937").</param>
/// <param name="Category">Model category used for UI grouping and filtering.</param>
/// <param name="ActorId">Identifier of the actor performing the operation (admin/system).</param>
public sealed record AddModelCommand(Guid MakerId, string Name, MotorcycleCategory Category, string ActorId) : ICommand<ModelDto>;