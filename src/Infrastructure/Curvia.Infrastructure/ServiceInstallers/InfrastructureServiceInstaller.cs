using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Templates.Core.Tools.DependencyInjection;
using Templates.Core.Tools.DependencyInjection.Abstractions;
using Curvia.Infrastructure.Features.Routing.Routes.Engines.Valhalla;
using Curvia.Infrastructure.Features.Routing.Routes.Services.Scoring;
using Curvia.Infrastructure.Features.Routing.Routes.Engines.Valhalla.Builder;
using Curvia.Infrastructure.Features.Routing.Routes.Engines.Valhalla.HeightClient;
using Curvia.Infrastructure.Features.Routing.Routes.Engines.Valhalla.RoutingClient;
using Curvia.Infrastructure.Features.Routing.Routes.Engines.Valhalla.CandidateGenerator;
using Curvia.Application.Features.Routes.Contracts.Services.Scoring;
using Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.Builder;
using Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.CandidateGenerator;
using Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.HeightClient;
using Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.RoutingClient;

namespace Curvia.Infrastructure.ServiceInstallers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Registers all infrastructure-layer services into the DI container.
///              Scrutor convention-based scanning is disabled project-wide
///              (includeConventionBasedRegistration = false), so every service
///              must be registered explicitly here.
/// </summary>
public sealed class InfrastructureServiceInstaller : BaseServiceInstaller, IServiceInstaller
{
	/// <summary>
	/// Installs all infrastructure services.
	/// </summary>
	/// <param name="services">The DI service collection.</param>
	/// <param name="configuration">Application configuration.</param>
	/// <param name="includeConventionBasedRegistration">
	/// When true, Scrutor scanning is also applied (disabled in production by design).
	/// </param>
	public void Install(IServiceCollection services, IConfiguration configuration, bool includeConventionBasedRegistration = true)
	{
		if (includeConventionBasedRegistration)
		{
			base.IncludeConventionBasedRegistrations(services, configuration, new List<Assembly> { AssemblyReference.Assembly }.ToArray());
		}

		#region Valhalla — Options
		services.Configure<ValhallaOptions>(configuration.GetSection("Routing:Valhalla"));
		#endregion

		#region Valhalla — HTTP clients
		// Route client — POST /route
		services.AddHttpClient<IValhallaRoutingClient, ValhallaRoutingClient>();

		// Height client — POST /height (elevation data for scoring)
		services.AddHttpClient<IValhallaHeightClient, ValhallaHeightClient>();
		#endregion

		#region Routing services
		services.AddScoped<IRouteCandidateGeneratorService, RouteCandidateGeneratorService>();
		services.AddScoped<IRouteBuilder, RouteBuilder>();
		services.AddScoped<IRouteScoringService, RouteScoringService>();
		#endregion
	}
}