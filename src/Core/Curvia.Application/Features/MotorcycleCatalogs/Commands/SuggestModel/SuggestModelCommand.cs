using Curvia.Domain.Features.MotorcycleCatalog.Enums;
using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.SuggestModel;

/// <summary>
/// Any authenticated user can suggest a model for a given maker.
/// MakerId must be either an Official or PendingValidation maker.
/// </summary>
public sealed record SuggestModelCommand(Guid MakerId, string Name, MotorcycleCategory Category, Guid UserId) : ICommand<ModelDto>;