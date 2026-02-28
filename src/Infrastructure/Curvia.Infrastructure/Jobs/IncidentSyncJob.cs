using MediatR;
using Curvia.Application.Features.Awareness.Commands.SyncIncidents;

namespace Curvia.Infrastructure.Jobs;

public sealed class IncidentSyncJob(IMediator mediator)
{
	public Task RunAsync(string countryCode, CancellationToken ct)
		=> mediator.Send(new SyncIncidentsCommand(countryCode), ct);
}