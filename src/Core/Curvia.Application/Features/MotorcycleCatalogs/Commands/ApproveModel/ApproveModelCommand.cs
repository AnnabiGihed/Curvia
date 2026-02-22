using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.ApproveModel;

public sealed record ApproveModelCommand(Guid ModelId, string ActorId) : ICommand;