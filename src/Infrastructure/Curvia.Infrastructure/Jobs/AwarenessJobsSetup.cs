using MediatR;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Curvia.Infrastructure.Features.Awareness.Providers.Osm;
using Curvia.Application.Features.Awareness.Commands.SyncHazards;
using Curvia.Application.Features.Awareness.Commands.SyncIncidents;
using Curvia.Application.Features.Awareness.Commands.SyncRoadWorks;
using Curvia.Application.Features.Awareness.Commands.SyncSpeedCameras;

namespace Curvia.Infrastructure.Jobs;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Registers all awareness data sync recurring jobs with Hangfire.
///              Called once from HostingExtensions.ConfigurePipeline after Hangfire
///              has been initialised by AwarenessInfrastructureServiceInstaller.
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
///              Hangfire creates a new DI scope per job execution — all scoped services
///              (repositories, HTTP clients, IMediator) resolve correctly.
/// </summary>
public static class AwarenessJobsSetup
{
	/// <summary>
	/// Registers all awareness recurring jobs with Hangfire's <see cref="RecurringJob"/> manager.
	/// Idempotent — AddOrUpdate replaces any previously registered job with the same id.
	/// </summary>
	/// <param name="app">The application builder whose service provider Hangfire will use.</param>
	public static void RegisterAwarenessJobs(this IApplicationBuilder app)
	{
		#region Speed cameras — weekly per country (Sunday 02:00 UTC)
		foreach (var countryCode in CountryBoundingBoxes.AllCountryCodes)
		{
			var cc = countryCode; // closure capture

			RecurringJob.AddOrUpdate<IncidentSyncJob>(
				$"sync-speed-cameras-{cc.ToLower()}",
				job => job.RunAsync(cc, CancellationToken.None),
				"*/3 * * * *");
		}
		#endregion

		#region Hazards — monthly per country (1st of month, 03:00 UTC)
		foreach (var countryCode in CountryBoundingBoxes.AllCountryCodes)
		{
			var cc = countryCode;

			RecurringJob.AddOrUpdate<IncidentSyncJob>(
				$"sync-hazards-{cc.ToLower()}",
				job => job.RunAsync(cc, CancellationToken.None),
				"*/3 * * * *");
		}
		#endregion

		#region Road works — every 4 hours per country
		foreach (var countryCode in CountryBoundingBoxes.AllCountryCodes)
		{
			var cc = countryCode;

			RecurringJob.AddOrUpdate<IncidentSyncJob>(
				$"sync-roadworks-{cc.ToLower()}",
				job => job.RunAsync(cc, CancellationToken.None),
				"*/3 * * * *");
		}
		#endregion

		#region Incidents — every 3 minutes per country
		foreach (var countryCode in CountryBoundingBoxes.AllCountryCodes)
		{
			var cc = countryCode;

			RecurringJob.AddOrUpdate<IncidentSyncJob>(
				$"sync-incidents-{cc.ToLower()}",
				job => job.RunAsync(cc, CancellationToken.None),
				"*/3 * * * *");
		}
		#endregion

		#region Cleanup — expired road works + incidents, hourly
		RecurringJob.AddOrUpdate(
			recurringJobId: "cleanup-expired-awareness",
			methodCall: (AwarenessCleanupJob job) => job.RunAsync(CancellationToken.None),
			cronExpression: Cron.Hourly());
		#endregion
	}
}