using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Awareness.Commands.SyncSpeedCameras;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Triggers a full speed camera sync for the given country.
///              Invoked by the Hangfire weekly recurring job registered in
///              <c>AwarenessJobsSetup</c>.
/// </summary>
public sealed record SyncSpeedCamerasCommand(string CountryCode) : ICommand;