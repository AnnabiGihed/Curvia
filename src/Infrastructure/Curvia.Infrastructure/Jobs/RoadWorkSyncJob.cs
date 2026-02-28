using MediatR;
using Hangfire;
using Curvia.Application.Features.Awareness.Commands.SyncRoadWorks;

namespace Curvia.Infrastructure.Jobs;

public sealed class RoadWorkSyncJob(IMediator mediator)
{
	[AutomaticRetry(Attempts = 0)]
	public async Task RunAsync(string countryCode, CancellationToken ct)
	{
		var result = await mediator.Send(new SyncRoadWorksCommand(countryCode), ct);
		if (result.IsFailure)
			throw new InvalidOperationException($"Road work sync failed for {countryCode}: {result.Error}");
	}
}