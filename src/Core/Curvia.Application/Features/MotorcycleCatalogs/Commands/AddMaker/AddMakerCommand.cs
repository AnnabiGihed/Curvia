using Templates.Core.Application.Abstractions.Messaging.Commands;
using Curvia.Application.Features.MotorcycleCatalogs.Responses;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.AddMaker;

public sealed record AddMakerCommand(string Name, string ActorId) : ICommand<MakerDto>;