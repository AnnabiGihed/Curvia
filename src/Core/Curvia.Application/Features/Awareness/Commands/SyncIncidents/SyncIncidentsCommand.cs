using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Awareness.Commands.SyncIncidents;

public sealed record SyncIncidentsCommand(string CountryCode) : ICommand;