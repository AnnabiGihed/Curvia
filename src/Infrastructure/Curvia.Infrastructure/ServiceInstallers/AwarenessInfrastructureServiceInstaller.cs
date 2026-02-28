using Curvia.Infrastructure.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pivot.Framework.Infrastructure.Scheduling.Extensions;
using Pivot.Framework.Tools.DependencyInjection.Abstractions;
using Curvia.Infrastructure.Features.Awareness.Providers.Osm;
using Curvia.Application.Features.Awareness.Contracts.Providers;
using Curvia.Infrastructure.Features.Awareness.Providers.Incidents;
using Curvia.Infrastructure.Features.Awareness.Providers.RoadWorks;
using Curvia.Infrastructure.Features.Awareness.Providers.OpenSpeedCam;
using Curvia.Infrastructure.Features.Awareness.Providers.Incidents.Configurations;
using Pivot.Framework.Authentication.Hangfire.Extensions;

namespace Curvia.Infrastructure.ServiceInstallers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Registers all awareness-related infrastructure services with the DI container.
///              Configures named HTTP clients with per-provider timeout tuning, registers
///              all data providers by their interface contracts, registers the cleanup job,
///              and sets up Hangfire (storage + server) via Pivot.Framework.Infrastructure.Scheduling
///              so that recurring sync jobs can be scheduled at pipeline startup.
/// </summary>
public sealed class AwarenessInfrastructureServiceInstaller : IServiceInstaller
{
	#region IServiceInstaller
	/// <summary>
	/// Installs awareness infrastructure: Hangfire, HTTP clients, provider registrations, and the cleanup job.
	/// </summary>
	/// <param name="services">The service collection to register into.</param>
	/// <param name="configuration">The application configuration for options binding.</param>
	/// <param name="includeConventionBasedRegistration">When true, convention-based registrations are included via base.</param>
	public void Install(IServiceCollection services, IConfiguration configuration, bool includeConventionBasedRegistration = true)
	{
		#region Awareness options
		services.Configure<AwarenessSyncOptions>(configuration.GetSection(AwarenessSyncOptions.SectionName));
		#endregion

		#region Hangfire — storage + server (via Pivot.Framework.Infrastructure.Scheduling)
		// Uses the "HangfireConnection" connection string from appsettings.
		// AddHangfireWithDashboard registers AddHangfire (SQL Server storage) + AddHangfireServer.
		// The dashboard middleware is mounted separately in HostingExtensions.ConfigurePipeline
		// via app.UseHangfireDashboardWithOptions().
		services.AddHangfireWithDashboard(configuration); 
		services.AddHangfireKeycloakBrowserAuth(configuration);
		#endregion

		#region HTTP clients — named per provider for independent timeout tuning
		services.AddHttpClient<OsmSpeedCameraProvider>(c =>
		{
			c.Timeout = TimeSpan.FromSeconds(120);
			c.DefaultRequestHeaders.Add("User-Agent", "Curvia/1.0 (awareness-sync)");
		});

		services.AddHttpClient<OsmHazardProvider>(c =>
		{
			c.Timeout = TimeSpan.FromSeconds(180); // hazard union query is larger
			c.DefaultRequestHeaders.Add("User-Agent", "Curvia/1.0 (awareness-sync)");
		});

		services.AddHttpClient<OsmRoadWorkProvider>(c =>
		{
			c.Timeout = TimeSpan.FromSeconds(120);
			c.DefaultRequestHeaders.Add("User-Agent", "Curvia/1.0 (awareness-sync)");
		});

		services.AddHttpClient<OpenSpeedCamProvider>(c =>
		{
			c.Timeout = TimeSpan.FromSeconds(120);
		});

		services.AddHttpClient<GipodRoadWorkProvider>(c =>
		{
			c.Timeout = TimeSpan.FromSeconds(30);
		});

		services.AddHttpClient<DatexIIIncidentProvider>(c =>
		{
			c.Timeout = TimeSpan.FromSeconds(20);
		});
		#endregion

		#region Speed camera providers — all registered; sync handler merges + deduplicates
		services.AddScoped<ISpeedCameraDataProvider, OsmSpeedCameraProvider>();
		services.AddScoped<ISpeedCameraDataProvider, OpenSpeedCamProvider>();
		#endregion

		#region Hazard providers
		services.AddScoped<IHazardDataProvider, OsmHazardProvider>();
		#endregion

		#region Road work providers — country-specific first, OSM fallback
		services.AddScoped<IRoadWorkDataProvider, GipodRoadWorkProvider>();
		services.AddScoped<IRoadWorkDataProvider, OsmRoadWorkProvider>();
		#endregion

		#region Incident providers
		services.AddScoped<IIncidentFeedProvider, DatexIIIncidentProvider>();
		#endregion

		#region Cleanup job — resolved by Hangfire per-job DI scope
		services.AddScoped<AwarenessCleanupJob>();
		#endregion

		services.AddScoped<IncidentSyncJob>();
		services.AddScoped<SpeedCameraSyncJob>();
		services.AddScoped<HazardSyncJob>();
		services.AddScoped<RoadWorkSyncJob>();
	}
	#endregion
}