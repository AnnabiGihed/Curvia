using MediatR;
using Curvia.Application.Features.Awareness.Commands.SyncSpeedCameras;

namespace Curvia.Infrastructure.Jobs;

public sealed class SpeedCameraSyncJob(IMediator mediator)
{
	public Task RunAsync(string countryCode, CancellationToken ct)
		=> mediator.Send(new SyncSpeedCamerasCommand(countryCode), ct);
}