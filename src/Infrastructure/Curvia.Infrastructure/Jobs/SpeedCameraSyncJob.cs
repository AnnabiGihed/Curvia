using MediatR;
using Hangfire;
using Curvia.Application.Features.Awareness.Commands.SyncSpeedCameras;

namespace Curvia.Infrastructure.Jobs;

public sealed class SpeedCameraSyncJob(IMediator mediator)
{
	[AutomaticRetry(Attempts = 0)]
	public async Task RunAsync(string countryCode, CancellationToken ct)
	{
		var result = await mediator.Send(new SyncSpeedCamerasCommand(countryCode), ct);
		if (result.IsFailure)
			throw new InvalidOperationException($"Speed Camera  sync failed for {countryCode}: {result.Error}");
	}
}