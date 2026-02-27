using Curvia.Application.Features.Awareness.Contracts.Providers;
using Curvia.Infrastructure.Features.Awareness.Providers.Incidents;
using Curvia.Infrastructure.Features.Awareness.Providers.Incidents.Configurations;
using Curvia.Infrastructure.Features.Awareness.Providers.OpenSpeedCam;
using Curvia.Infrastructure.Features.Awareness.Providers.Osm;
using Curvia.Infrastructure.Features.Awareness.Providers.RoadWorks;
using Curvia.Infrastructure.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Templates.Core.Tools.DependencyInjection.Abstractions;

namespace Curvia.Infrastructure.ServiceInstallers;

public sealed class AwarenessInfrastructureServiceInstaller : IServiceInstaller
{
	public void Install(
		IServiceCollection services,
		IConfiguration configuration,
		bool includeConventionBasedRegistration = true)
	{
		// Bind configuration
		services.Configure<AwarenessSyncOptions>(
			configuration.GetSection(AwarenessSyncOptions.SectionName));

		// Named HTTP clients — separate clients allow per-provider timeout tuning
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
			c.Timeout = TimeSpan.FromSeconds(60);
		});

		services.AddHttpClient<GipodRoadWorkProvider>(c =>
		{
			c.Timeout = TimeSpan.FromSeconds(30);
		});

		services.AddHttpClient<DatexIIIncidentProvider>(c =>
		{
			c.Timeout = TimeSpan.FromSeconds(20);
		});

		// Speed camera providers — all registered; sync handler merges + deduplicates
		services.AddScoped<ISpeedCameraDataProvider, OsmSpeedCameraProvider>();
		services.AddScoped<ISpeedCameraDataProvider, OpenSpeedCamProvider>();

		// Hazard providers
		services.AddScoped<IHazardDataProvider, OsmHazardProvider>();

		// Road work providers — country-specific first, OSM fallback
		services.AddScoped<IRoadWorkDataProvider, GipodRoadWorkProvider>();
		services.AddScoped<IRoadWorkDataProvider, OsmRoadWorkProvider>();

		// Incident providers
		services.AddScoped<IIncidentFeedProvider, DatexIIIncidentProvider>();

		// Cleanup job (resolved by Hangfire)
		services.AddScoped<AwarenessCleanupJob>();
	}
}
