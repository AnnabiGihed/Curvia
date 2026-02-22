using Curvia.Domain.Features.MotorcycleCatalog.Enums;
using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.AddModel;

public sealed record AddModelCommand(Guid MakerId, string Name, MotorcycleCategory Category, string ActorId) : ICommand<ModelDto>;