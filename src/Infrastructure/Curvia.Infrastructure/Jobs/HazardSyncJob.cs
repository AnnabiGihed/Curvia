using MediatR;
using Curvia.Application.Features.Awareness.Commands.SyncHazards;

namespace Curvia.Infrastructure.Jobs;

public sealed class HazardSyncJob(IMediator mediator)
{
	public Task RunAsync(string countryCode, CancellationToken ct)
		=> mediator.Send(new SyncHazardsCommand(countryCode), ct);
}