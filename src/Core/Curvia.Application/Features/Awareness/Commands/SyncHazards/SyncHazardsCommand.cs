using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Awareness.Commands.SyncHazards;

public sealed record SyncHazardsCommand(string CountryCode) : ICommand;