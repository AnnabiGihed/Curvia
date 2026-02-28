using MediatR;
using Hangfire;
using Curvia.Application.Features.Awareness.Commands.SyncIncidents;

namespace Curvia.Infrastructure.Jobs;

public sealed class IncidentSyncJob(IMediator mediator)
{
	[AutomaticRetry(Attempts = 0)]
	public async Task RunAsync(string countryCode, CancellationToken ct)
	{
		var result = await mediator.Send(new SyncIncidentsCommand(countryCode), ct);
		if (result.IsFailure)
			throw new InvalidOperationException($"Incident sync failed for {countryCode}: {result.Error}");
	}
}