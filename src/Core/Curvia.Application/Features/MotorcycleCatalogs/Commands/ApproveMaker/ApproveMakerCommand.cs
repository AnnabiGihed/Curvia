using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.ApproveMaker;

public sealed record ApproveMakerCommand(Guid MakerId, string ActorId) : ICommand;