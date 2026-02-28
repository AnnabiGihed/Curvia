using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.SuggestMaker;

/// <summary>
/// Any authenticated user can suggest a maker not in the official catalog.
/// If an official maker with the same name already exists, returns a conflict
/// so the UI can redirect the user to pick from the existing list.
/// </summary>
public sealed record SuggestMakerCommand(string Name, Guid UserId) : ICommand<MakerDto>;