using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.AddMaker;

/// <summary>
/// Represents a command to add a new official motorcycle maker to the catalog.
/// </summary>
/// <param name="Name">Maker name to create (e.g. "Ducati", "BMW Motorrad").</param>
/// <param name="ActorId">Identifier of the actor performing the operation (admin/system).</param>
public sealed record AddMakerCommand(string Name, string ActorId) : ICommand<MakerDto>;