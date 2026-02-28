using MediatR;
using Hangfire;
using Curvia.Application.Features.Awareness.Commands.SyncHazards;

namespace Curvia.Infrastructure.Jobs;

public sealed class HazardSyncJob(IMediator mediator)
{
	[AutomaticRetry(Attempts = 0)]
	public async Task RunAsync(string countryCode, CancellationToken ct)
	{
		var result = await mediator.Send(new SyncHazardsCommand(countryCode), ct);
		if (result.IsFailure)
			throw new InvalidOperationException($"Hazard sync failed for {countryCode}: {result.Error}");
	}
}