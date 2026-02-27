using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Awareness.Commands.SyncRoadWorks;

public sealed record SyncRoadWorksCommand(string CountryCode) : ICommand;
