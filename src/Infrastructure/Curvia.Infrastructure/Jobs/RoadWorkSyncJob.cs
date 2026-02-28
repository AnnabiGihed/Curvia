using MediatR;
using Curvia.Application.Features.Awareness.Commands.SyncRoadWorks;

namespace Curvia.Infrastructure.Jobs;

public sealed class RoadWorkSyncJob(IMediator mediator)
{
	public Task RunAsync(string countryCode, CancellationToken ct)
		=> mediator.Send(new SyncRoadWorksCommand(countryCode), ct);
}