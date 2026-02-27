using MediatR;

namespace Curvia.API.Jobs;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Registers all awareness data sync recurring jobs with Hangfire.
///              Called once from Program.cs / HostingExtensions after the application
///              is built and Hangfire is available.
///
///              Job schedule summary:
///
///                Speed cameras  — weekly (Sunday 02:00 UTC), per country
///                  OSM + OpenSpeedCam merged. Static data changes rarely.
///
///                Hazards        — monthly (1st of month, 03:00 UTC), per country
///                  OSM permanent hazard tags change very infrequently.
///
///                Road works     — every 4 hours, per country
///                  GIPOD (BE) + OSM fallback. Works start and end frequently.
///
///                Incidents      — every 3 minutes, per country with a configured feed
///                  DATEX II live feeds. High frequency required for Waze-like alerts.
///
///                Expired cleanup — hourly
///                  Prunes expired RoadWork and Incident rows to keep tables lean.
///
///              All jobs dispatch MediatR commands so the sync logic stays in the
///              application layer (testable, not Hangfire-coupled).
/// </summary>
public static class AwarenessJobsSetup
{
	public static void RegisterAwarenessJobs(this IApplicationBuilder app)
	{
		// Speed cameras: weekly per country
		foreach (var countryCode in CountryBoundingBoxes.AllCountryCodes)
		{
			var cc = countryCode; // closure capture

			RecurringJob.AddOrUpdate(
				recurringJobId: $"sync-speed-cameras-{cc.ToLower()}",
				methodCall: (IMediator mediator) =>
					mediator.Send(new SyncSpeedCamerasCommand(cc), CancellationToken.None),
				cronExpression: Cron.Weekly(DayOfWeek.Sunday, 2, 0)); // Sunday 02:00 UTC

			RecurringJob.AddOrUpdate(
				recurringJobId: $"sync-hazards-{cc.ToLower()}",
				methodCall: (IMediator mediator) =>
					mediator.Send(new SyncHazardsCommand(cc), CancellationToken.None),
				cronExpression: "0 3 1 * *"); // 1st of every month, 03:00 UTC

			RecurringJob.AddOrUpdate(
				recurringJobId: $"sync-roadworks-{cc.ToLower()}",
				methodCall: (IMediator mediator) =>
					mediator.Send(new SyncRoadWorksCommand(cc), CancellationToken.None),
				cronExpression: "0 */4 * * *"); // every 4 hours

			RecurringJob.AddOrUpdate(
				recurringJobId: $"sync-incidents-{cc.ToLower()}",
				methodCall: (IMediator mediator) =>
					mediator.Send(new SyncIncidentsCommand(cc), CancellationToken.None),
				cronExpression: "*/3 * * * *"); // every 3 minutes
		}

		// Cleanup: expired road works + incidents — hourly
		RecurringJob.AddOrUpdate(
			recurringJobId: "cleanup-expired-awareness",
			methodCall: (AwarenessCleanupJob job) => job.RunAsync(CancellationToken.None),
			cronExpression: Cron.Hourly());
	}
}